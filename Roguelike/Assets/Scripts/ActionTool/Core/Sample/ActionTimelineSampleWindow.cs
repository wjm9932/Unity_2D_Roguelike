using UnityEditor;
using UnityEngine;

namespace ActionTool.Core.Sample
{
    public sealed class ActionTimelineSampleWindow : EditorWindow
    {
        private const float HeaderWidth = 220f;
        private const float RulerHeight = 24f;
        private const float TrackHeight = 34f;
        private const int FrameRate = 60;
        private const int MaxFrame = 240;

        private readonly SampleTrack[] tracks =
        {
            new("Animation", 0, 80, new Color(0.27f, 0.55f, 0.88f)),
            new("Movement", 12, 46, new Color(0.26f, 0.72f, 0.48f)),
            new("Hitbox", 34, 10, new Color(0.90f, 0.32f, 0.25f)),
            new("VFX", 28, 40, new Color(0.68f, 0.38f, 0.90f))
        };

        private ActionTimelineArea timeline;
        private int currentFrame;

        [MenuItem("Window/Action Tool/Timeline Core Sample")]
        public static void Open()
        {
            GetWindow<ActionTimelineSampleWindow>("Timeline Core Sample").Show();
        }

        private void OnEnable()
        {
            timeline = new ActionTimelineArea(FrameRate, MaxFrame / (float)FrameRate);
            minSize = new Vector2(640f, 260f);
        }

        private void OnGUI()
        {
            DrawToolbar();

            Rect fullRect = GUILayoutUtility.GetRect(
                0f,
                100000f,
                0f,
                100000f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            Rect headerRulerRect = new(fullRect.x, fullRect.y, HeaderWidth, RulerHeight);
            Rect rulerRect = new(fullRect.x + HeaderWidth, fullRect.y, fullRect.width - HeaderWidth, RulerHeight);
            Rect headerRect = new(fullRect.x, fullRect.y + RulerHeight, HeaderWidth, fullRect.height - RulerHeight);
            Rect contentRect = new(fullRect.x + HeaderWidth, fullRect.y + RulerHeight, fullRect.width - HeaderWidth, fullRect.height - RulerHeight);

            EditorGUI.DrawRect(headerRulerRect, new Color(0.18f, 0.18f, 0.18f));
            EditorGUI.DrawRect(headerRect, new Color(0.18f, 0.18f, 0.18f));
            timeline.Draw(rulerRect, contentRect);
            DrawTrackHeaders(headerRect);
            DrawTrackContents(contentRect);
            HandleScrub(rulerRect);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Frame All", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    timeline.FrameAll();
                }

                GUILayout.Space(8f);
                GUILayout.Label("Current Frame", GUILayout.Width(88f));
                int nextFrame = EditorGUILayout.IntField(currentFrame, GUILayout.Width(60f));
                currentFrame = Mathf.Clamp(nextFrame, 0, MaxFrame);
                GUILayout.FlexibleSpace();
                GUILayout.Label("Wheel: Zoom  |  Middle Drag: Pan", EditorStyles.miniLabel);
            }
        }

        private void DrawTrackHeaders(Rect rect)
        {
            GUI.BeginClip(rect);

            for (int i = 0; i < tracks.Length; i++)
            {
                Rect rowRect = new(0f, i * TrackHeight, rect.width, TrackHeight);
                EditorGUI.DrawRect(rowRect, GetRowColor(i));
                EditorGUI.DrawRect(new Rect(0f, rowRect.y, 4f, rowRect.height), tracks[i].Color);
                GUI.Label(new Rect(12f, rowRect.y + 7f, rect.width - 18f, 18f), tracks[i].Name);
            }

            GUI.EndClip();
        }

        private void DrawTrackContents(Rect rect)
        {
            GUI.BeginClip(rect);
            Rect localRect = new(Vector2.zero, rect.size);

            for (int i = 0; i < tracks.Length; i++)
            {
                SampleTrack track = tracks[i];
                Rect rowRect = new(0f, i * TrackHeight, rect.width, TrackHeight);
                EditorGUI.DrawRect(rowRect, GetRowColor(i));

                float start = timeline.FrameToPixel(track.StartFrame, localRect);
                float end = timeline.FrameToPixel(track.EndFrame, localRect);
                Rect clipRect = Rect.MinMaxRect(
                    start,
                    rowRect.y + 5f,
                    Mathf.Max(start + 4f, end),
                    rowRect.yMax - 5f);
                EditorGUI.DrawRect(clipRect, track.Color);
            }

            float playhead = timeline.FrameToPixel(currentFrame, localRect);
            Handles.color = new Color(1f, 0.35f, 0.15f);
            Handles.DrawLine(new Vector3(playhead, 0f), new Vector3(playhead, rect.height));
            GUI.EndClip();
        }

        private void HandleScrub(Rect rulerRect)
        {
            Event current = Event.current;

            if ((current.type != EventType.MouseDown && current.type != EventType.MouseDrag) ||
                current.button != 0 ||
                !rulerRect.Contains(current.mousePosition))
            {
                return;
            }

            currentFrame = Mathf.Clamp(
                timeline.PixelToFrame(current.mousePosition.x, rulerRect),
                0,
                MaxFrame);
            current.Use();
            Repaint();
        }

        private static Color GetRowColor(int index)
        {
            return index % 2 == 0
                ? new Color(0.15f, 0.15f, 0.15f)
                : new Color(0.17f, 0.17f, 0.17f);
        }

        private readonly struct SampleTrack
        {
            public string Name { get; }
            public int StartFrame { get; }
            public int EndFrame { get; }
            public Color Color { get; }

            public SampleTrack(string name, int startFrame, int duration, Color color)
            {
                Name = name;
                StartFrame = startFrame;
                EndFrame = startFrame + duration;
                Color = color;
            }
        }
    }
}
