using System;
using UnityEditor;
using UnityEngine;

namespace ActionTool.Core
{
    /// <summary>
    /// Unity Timeline 형태의 시간 눈금, 확대/축소, 이동과 좌표 변환을 제공한다.
    /// 실제 트랙 데이터와 렌더링은 사용하는 EditorWindow가 소유한다.
    /// </summary>
    public sealed class ActionTimelineArea
    {
        private const double MinimumFrameRate = 1d;
        private const float MinimumDuration = 0.1f;

        private readonly TimeArea timeArea;
        private double frameRate;
        private float duration;

        public double FrameRate
        {
            get => frameRate;
            set
            {
                frameRate = Math.Max(MinimumFrameRate, value);
                timeArea.hTicks.SetTickModulosForFrameRate((float)frameRate);
            }
        }

        public float Duration
        {
            get => duration;
            set
            {
                duration = Mathf.Clamp(
                    value,
                    MinimumDuration,
                    (float)TimeUtilityReflect.k_MaxTimelineDurationInSeconds);
                timeArea.hBaseRangeMax = duration;
            }
        }

        public Rect ShownArea => timeArea.shownArea;
        public Vector2 Scale => timeArea.scale;
        public Vector2 Translation => timeArea.translation;

        public ActionTimelineArea(double frameRate = 60d, float duration = 10f)
        {
            timeArea = new TimeArea(false)
            {
                hRangeLocked = false,
                vRangeLocked = true,
                margin = 10f,
                scaleWithWindow = true,
                hSlider = true,
                vSlider = false,
                hBaseRangeMin = 0f,
                hRangeMin = 0f,
                hScaleMax = 90000f
            };

            FrameRate = frameRate;
            Duration = duration;
            FrameAll();
        }

        /// <summary>
        /// OnGUI 안에서 호출한다. rulerRect에는 눈금을, contentRect에는 grid와 스크롤바를 그린다.
        /// </summary>
        public void Draw(Rect rulerRect, Rect contentRect)
        {
            if (rulerRect.width <= 0f || contentRect.width <= 0f || contentRect.height <= 0f)
            {
                return;
            }

            timeArea.rect = contentRect;
            EditorGUI.DrawRect(rulerRect, CustomGUIStyles.colorTimelineBackground);
            EditorGUI.DrawRect(contentRect, CustomGUIStyles.colorEventListBackground);

            timeArea.BeginViewGUI();
            timeArea.DrawMajorTicks(contentRect, (float)FrameRate);
            timeArea.TimeRuler(
                rulerRect,
                (float)FrameRate,
                true,
                false,
                1f,
                TimeArea.TimeFormat.TimeFrame);
            timeArea.EndViewGUI();
        }

        public float TimeToPixel(double time, Rect rect)
        {
            return timeArea.TimeToPixel((float)time, rect);
        }

        public double PixelToTime(float pixel, Rect rect)
        {
            return timeArea.PixelToTime(pixel, rect);
        }

        public float FrameToPixel(int frame, Rect rect)
        {
            return TimeToPixel(frame / FrameRate, rect);
        }

        public int PixelToFrame(float pixel, Rect rect)
        {
            return Mathf.RoundToInt((float)(PixelToTime(pixel, rect) * FrameRate));
        }

        public double SnapToFrame(double time)
        {
            return Math.Round(time * FrameRate) / FrameRate;
        }

        public void SetShownRange(float minTime, float maxTime)
        {
            float min = Mathf.Max(0f, minTime);
            float max = Mathf.Clamp(maxTime, min + MinimumDuration, Duration);
            timeArea.SetShownHRange(min, max);
        }

        public void FrameAll()
        {
            timeArea.SetShownHRange(0f, Duration);
        }
    }
}
