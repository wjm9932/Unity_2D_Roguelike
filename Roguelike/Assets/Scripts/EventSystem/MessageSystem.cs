using System;
using System.Collections.Generic;

public delegate void SubscribeCallback(Event e);

internal struct SubscribeCallbackInfo : IEquatable<SubscribeCallbackInfo>
{
    internal SubscribeCallback callback;

    internal bool subscribeOnce;

    internal SubscribeCallbackInfo(SubscribeCallback callback, bool subscribeOnce = false)
    {
        this.callback = callback;
        this.subscribeOnce = subscribeOnce;
    }

    public bool Equals(SubscribeCallbackInfo subscribeCallbackInfo)
    {
        return subscribeCallbackInfo.callback == callback;
    }

    public override bool Equals(object obj)
    {
        return obj is SubscribeCallbackInfo rObj && Equals(rObj);
    }

    public override int GetHashCode()
    {
        return callback.GetHashCode();
    }
}

public class MessageSystem
{
    public static MessageSystem Instance { get; } = new();

    private struct PublishedEvent
    {
        public IEventListener Target;

        public Event Event;
    }

    /// <summary>
    /// 이번 프레임에 발행될 이벤트 목록
    /// </summary>
    private readonly List<PublishedEvent> publishedEvents = new();
    /// <summary>
    /// 구독 콜백 수행 중에 구독 요청이 들어올 경우를 대비해 일괄 추가
    /// </summary>
    private readonly List<KeyValuePair<Type, SubscribeCallbackInfo>> subscribeRequests = new();
    /// <summary>
    /// 구독 콜백 수행 중에 구독 해제 요청이 들어올 경우를 대비해 일괄 해제
    /// </summary>
    private readonly List<KeyValuePair<Type, SubscribeCallback>> unsubscribeRequests = new();
    /// <summary>
    /// 현재 구독 콜백 목록
    /// </summary>
    private readonly Dictionary<Type, List<SubscribeCallbackInfo>> subscribeCallbacks = new();

    public void Update()
    {
        ProcessRequests();

        PublishEvents();

    }

    // 구독보다 구독 해제의 우선 순위가 더 높게 정책상 결정
    private void ProcessRequests()
    {
        // 구독보다 구독 해제의 우선 순위가 더 높게 정책상 결정
        foreach (var subRequest in subscribeRequests)
        {
            if (subscribeCallbacks.TryGetValue(subRequest.Key, out var callbacks))
            {
                callbacks.Add(subRequest.Value);
            }
            else
            {
                callbacks = new List<SubscribeCallbackInfo>
                {
                    subRequest.Value
                };

                subscribeCallbacks.Add(subRequest.Key, callbacks);
            }
        }

        subscribeRequests.Clear();

        var infoToUnsubscribe = new SubscribeCallbackInfo(null, false);

        foreach (var unsubRequest in unsubscribeRequests)
        {
            if (subscribeCallbacks.TryGetValue(unsubRequest.Key, out var callbacks))
            {
                infoToUnsubscribe.callback = unsubRequest.Value;

                if (callbacks.Remove(infoToUnsubscribe))
                {
                    if (callbacks.Count == 0)
                    {
                        subscribeCallbacks.Remove(unsubRequest.Key);
                    }
                }
            }
        }

        unsubscribeRequests.Clear();
    }

    private void PublishEvents()
    {
        // 콜백 수행중 같은 이벤트를 발행할 경우 무한루프 가능성 존재
        // 콜백 실행 중 publish 요청이 들어올 수 있기 때문에 foreach가 아닌 for를 사용한다.
        for(int i = 0; i < publishedEvents.Count; i++)
        {
            var toBePublishedEvent = publishedEvents[i];

            // publish를 통해 발행된 이벤트인 경우
            if (toBePublishedEvent.Target == null)
            {
                if (subscribeCallbacks.TryGetValue(toBePublishedEvent.Event.GetType(), out var callbacks))
                {
                    for (int j = 0; j < callbacks.Count; j++)
                    {
                        var callbackInfo = callbacks[j];

                        // 무한 재귀호출 방지 위해 subOnce면 콜백 수행전 리스트에서 지워주기
                        if (callbackInfo.subscribeOnce)
                        {
                            callbacks.RemoveAt(j--);
                        }

                        callbackInfo.callback.Invoke(toBePublishedEvent.Event);
                    }
                }
            }
            // send를 통해 발송된 이벤트인 경우
            else
            {
                toBePublishedEvent.Target.OnEvent(toBePublishedEvent.Event);
            }

            toBePublishedEvent.Event.Dispose();
        }

        publishedEvents.Clear();
    }

    /// <summary>
    /// 이벤트 발행
    /// </summary>
    public void Publish<T>(T e) where T : Event
    {
        var publishedEvent = new PublishedEvent
        {
            Event = e,
        };

        publishedEvents.Add(publishedEvent);
    }

    /// <summary>
    /// 이벤트 즉시 발행
    /// </summary>
    public void PublishSync<T>(T e) where T : Event
    {
        if (subscribeCallbacks.TryGetValue(e.GetType(), out var callbacks))
        {
            foreach (var callbackInfo in callbacks)
            {
                callbackInfo.callback.Invoke(e);
            }
        }

        e.Dispose();
    }

    /// <summary>
    /// 특정 대상에게 이벤트 전송
    /// </summary>
    public void Send<T>(IEventListener target, T e) where T : Event
    {
        if (target == null) return;

        var publishedEvent = new PublishedEvent
        {
            Target = target,
            Event = e
        };

        publishedEvents.Add(publishedEvent);
    }

    public void SendSync<T>(IEventListener target, T e) where T : Event
    {
        if (target == null) return;

        target.OnEvent(e);

        e.Dispose();
    }

    public void Subscribe<T>(SubscribeCallback callback) where T : Event
    {
        // Dictionary가 아닌 List로 하는 이유는 일단 같은 type이 중복되어서 들어오더라도 단순 참조값 8바이트가 중복되는 구조이고
        // Type을 key로 Dictionary로 저장한다면 Type별 리스트 객체가 필요하고 중복되지 않는다면 단순히 List객체만 불필요하게 많이 생성되어 메모리적으로 더 손해다
        // 또한 요청을 순회할 때 List는 메모리 상 요소끼리 붙어 있기 때문에 캐시 히트 비율이 더 좋다
        subscribeRequests.Add(
                new KeyValuePair<Type, SubscribeCallbackInfo>(typeof(T), new SubscribeCallbackInfo(callback)));
    }

    public void SubscribeOnce<T>(SubscribeCallback callback) where T : Event
    {
        subscribeRequests.Add(
            new KeyValuePair<Type, SubscribeCallbackInfo>(typeof(T), new SubscribeCallbackInfo(callback, true)));
    }

    public void UnSubscribe<T>(SubscribeCallback callback) where T : Event
    {
        unsubscribeRequests.Add(
               new KeyValuePair<Type, SubscribeCallback>(typeof(T), callback));
    }
}
