using System;
using System.Reflection;
using UnityEngine;

namespace ActionTool.Core
{
	public class CustomGUI : GUI
	{
		private static readonly int s_SliderHash = "Slider".GetHashCode();
		private static readonly int s_RepeatButtonHash = "repeatButton".GetHashCode();

		private static int s_ScrollControlId;
		internal static DateTime nextScrollStepTime { get; set; }

		public static float Scroller(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
		{
			//GUIUtility.CheckOnGUI();
			int controlID = GUIUtility.GetControlID(s_SliderHash, FocusType.Passive, position);
			Rect position2;
			Rect rect;
			Rect rect2;
			if (horiz)
			{
				position2 = new Rect(position.x + leftButton.fixedWidth, position.y, position.width - leftButton.fixedWidth - rightButton.fixedWidth, position.height);
				rect = new Rect(position.x, position.y, leftButton.fixedWidth, position.height);
				rect2 = new Rect(position.xMax - rightButton.fixedWidth, position.y, rightButton.fixedWidth, position.height);
			}
			else
			{
				position2 = new Rect(position.x, position.y + leftButton.fixedHeight, position.width, position.height - leftButton.fixedHeight - rightButton.fixedHeight);
				rect = new Rect(position.x, position.y, position.width, leftButton.fixedHeight);
				rect2 = new Rect(position.x, position.yMax - rightButton.fixedHeight, position.width, rightButton.fixedHeight);
			}

			value = Slider(position2, value, size, leftValue, rightValue, slider, thumb, horiz, controlID);
			bool flag = Event.current.type == EventType.MouseUp;
			if (ScrollerRepeatButton(controlID, rect, leftButton))
			{
				value -= 10f * ((leftValue < rightValue) ? 1f : (-1f));
			}

			if (ScrollerRepeatButton(controlID, rect2, rightButton))
			{
				value += 10f * ((leftValue < rightValue) ? 1f : (-1f));
			}

			if (flag && Event.current.type == EventType.Used)
			{
				s_ScrollControlId = 0;
			}

			value = ((!(leftValue < rightValue)) ? Mathf.Clamp(value, rightValue, leftValue - size) : Mathf.Clamp(value, leftValue, rightValue - size));
			return value;
		}

		internal static bool ScrollerRepeatButton(int scrollerID, Rect rect, GUIStyle style)
		{
			bool result = false;
			if (DoRepeatButton(rect, GUIContent.none, style, FocusType.Passive))
			{
				bool flag = s_ScrollControlId != scrollerID;
				s_ScrollControlId = scrollerID;
				if (flag)
				{
					result = true;
					nextScrollStepTime = DateTime.Now.AddMilliseconds(250.0);
				}
				else if (DateTime.Now >= nextScrollStepTime)
				{
					result = true;
					nextScrollStepTime = DateTime.Now.AddMilliseconds(30.0);
				}

				if (Event.current.type == EventType.Repaint)
				{
					typeof(GUI).GetMethod("InternalRepaintEditorWindow", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
				}
			}

			return result;
		}

		private static bool DoRepeatButton(Rect position, GUIContent content, GUIStyle style, FocusType focusType)
		{
			typeof(GUIUtility).GetMethod("CheckOnGUI", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
			int controlID = GUIUtility.GetControlID(s_RepeatButtonHash, focusType, position);
			switch (Event.current.GetTypeForControl(controlID))
			{
				case EventType.MouseDown:
					if (position.Contains(Event.current.mousePosition))
					{
						GUIUtility.hotControl = controlID;
						Event.current.Use();
					}

					return false;

				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlID)
					{
						GUIUtility.hotControl = 0;
						Event.current.Use();
						return position.Contains(Event.current.mousePosition);
					}

					return false;

				case EventType.Repaint:
					style.Draw(position, content, controlID, on: false, position.Contains(Event.current.mousePosition));
					return controlID == GUIUtility.hotControl && position.Contains(Event.current.mousePosition);

				default:
					return false;
			}
		}
	}
}
