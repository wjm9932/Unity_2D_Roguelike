# Action Tool Timeline Core

Unity Timeline/Animation 창의 저수준 시간축 동작을 Editor 전용 어셈블리로
분리한 모듈이다. 런타임 데이터나 트랙 타입을 포함하지 않는다.

사용할 Editor 어셈블리에서 `ActionTool.Core`를 참조하고, `EditorWindow.OnGUI`
안에서 `ActionTimelineArea.Draw`를 호출한다.

```csharp
private ActionTimelineArea timeline;

private void OnEnable()
{
    timeline = new ActionTimelineArea(60d, 5f);
}

private void OnGUI()
{
    timeline.Draw(rulerRect, trackRect);

    float startX = timeline.FrameToPixel(startFrame, trackRect);
    float endX = timeline.FrameToPixel(endFrame, trackRect);
    EditorGUI.DrawRect(Rect.MinMaxRect(startX, y, endX, y + height), color);
}
```

`Window > Action Tool > Timeline Core Sample`에서 데이터 없이 동작을 확인할 수
있다. 샘플은 Core 사용법만 보여주며 실제 Action Tool 에디터와 데이터 모델은
별도로 구현한다.
