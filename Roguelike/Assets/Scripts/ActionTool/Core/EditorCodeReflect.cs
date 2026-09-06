using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionTool.Core
{
	internal struct PropertyGUIData //: UnityEditor.PropertyGUIData
	{
		public SerializedProperty property;
		public Rect totalPosition;
		public bool wasBoldDefaultFont;
		public bool wasEnabled;
		public Color color;

		public PropertyGUIData(SerializedProperty property, Rect totalPosition, bool wasBoldDefaultFont, bool wasEnabled, Color color)
		{
			this.property = property;
			this.totalPosition = totalPosition;
			this.wasBoldDefaultFont = wasBoldDefaultFont;
			this.wasEnabled = wasEnabled;
			this.color = color;
		}
	}

	public static class CustomEditorGUI //: UnityEditor.EditorGUI
	{
		private static FieldInfo fieldInfo_RecycledEditor = null;

		public static object RecycledEditor
		{
			get
			{
				bool isInfoExist = fieldInfo_RecycledEditor == null;
				if (isInfoExist == false)
					isInfoExist = typeof(EditorGUI).TryGetField("UnityEditor.EditorGUI", "s_RecycledEditor", BindingFlags.NonPublic | BindingFlags.Static, out fieldInfo_RecycledEditor);


				return isInfoExist ? fieldInfo_RecycledEditor.GetValue(null) : null;
			}
		}

		private static Type typeInfo_RecycledTextEditor = null;

		internal static Type type_RecycledTextEditor
		{
			get
			{
				bool isInfoExist = typeInfo_RecycledTextEditor == null;
				if (isInfoExist == false)
					isInfoExist = typeof(EditorGUI).TryGetNestedType("UnityEditor.EditorGUI", "RecycledTextEditor", BindingFlags.NonPublic, out typeInfo_RecycledTextEditor);


				return isInfoExist ? typeInfo_RecycledTextEditor : null;
			}
		}

		private static MethodInfo methodInfo_RecycledTextEditor_IsEditingControl = null;

		/// <summary>
		///reflection of internal static bool Method /
		///Original : UnityEditor.EditorGUI.RecycledTextEditor.IsEditingControl()
		/// </summary>
		internal static bool IsEditingControl(int id)
		{
			bool isInfoExist = methodInfo_RecycledTextEditor_IsEditingControl == null;
			if (isInfoExist == false)
			{
				if (type_RecycledTextEditor == null)
					return false;

				isInfoExist = type_RecycledTextEditor.TryGetMethod("UnityEditor.EditorGUI.RecycledTextEditor", "IsEditingControl", BindingFlags.NonPublic, out methodInfo_RecycledTextEditor_IsEditingControl);
			}

			return isInfoExist ? (bool)methodInfo_RecycledTextEditor_IsEditingControl.Invoke(RecycledEditor, new object[] { id }) : false;
		}

		private static MethodInfo methodInfo_DoTextField = null;

		private static void AssertReflectionResult(object result, string infoType, string targetName, string targetLocation, out bool isSuccess)
		{
			if (result == null)
			{
				Debug.LogError($"Fail to get {infoType} info \"{targetName}\" from {targetLocation}");
			}

			isSuccess = result != null;
		}

		private static PropertyInfo searchFieldProperty = null;
		private static GUIStyle searchFieldValue = null;
		private static GUIStyle searchField => searchFieldValue == null ? GUIStyle.none : searchFieldValue;
		private static Material lineMaterial = null;

		internal static Material LineMaterial
		{
			get
			{
				if (!lineMaterial)
				{
					Shader shader = Shader.Find("Hidden/Internal-Colored");
					lineMaterial = new Material(shader)
					{
						hideFlags = HideFlags.HideAndDontSave
					};
					lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
					lineMaterial.SetInt("_ZWrite", 0);
					//lineMaterial.SetPass(0);
				}

				return lineMaterial;
			}
		}

		private static readonly Stack<PropertyGUIData> s_PropertyStack;

		internal static string s_OriginalText;
		internal static string s_FloatFieldFormatString;
		internal static string s_IntFieldFormatString;

		internal static bool s_DragToPosition;
		internal static bool s_Dragged;

		static CustomEditorGUI()
		{
			s_OriginalText = "";
			s_FloatFieldFormatString = "g7";
			s_IntFieldFormatString = "#######0";

			s_FloatFieldHash = "EditorTextField".GetHashCode();

			s_DragCandidateState = DragCandidateState.NotDragging;
			s_DragStartValue = 0.0;
			s_DragStartIntValue = 0L;
			s_DragSensitivity = 0.0;

			s_AllowedCharactersForFloat = "inftynaeINFTYNAE0123456789.,-*/+%^()cosqrludxvRL=pP#";
			s_AllowedCharactersForInt = "0123456789-*/+%^()cosintaqrtelfundxvRL,=pPI#";

			if (searchFieldProperty == null)
				searchFieldProperty = typeof(EditorStyles).GetProperty("searchField", BindingFlags.NonPublic | BindingFlags.Static);

			if (searchFieldValue == null || searchFieldValue == GUIStyle.none)
				searchFieldValue = (GUIStyle)searchFieldProperty.GetValue(null);
		}

		// Summary:
		//     Makes the following controls give the appearance of editing multiple different
		//     values.
		public static bool showMixedValue { get; set; }


		#region NumberField

		private enum DragCandidateState
		{
			NotDragging,
			InitiatedDragging,
			CurrentlyDragging
		}

		public struct NumberFieldValue
		{
			public bool isDouble;
			public double doubleVal;
			public long longVal;
			public bool success;

			public NumberFieldValue(double v)
			{
				isDouble = true;
				doubleVal = v;
				longVal = 0L;
				success = false;
			}

			public NumberFieldValue(long v)
			{
				isDouble = false;
				doubleVal = 0.0;
				longVal = v;
				success = false;
			}
		}

		static string s_AllowedCharactersForFloat;
		static string s_AllowedCharactersForInt;

		static DragCandidateState s_DragCandidateState;

		static int s_FloatFieldHash;
		static double s_DragStartValue;
		static long s_DragStartIntValue;
		static Vector2 s_DragStartPos;
		static double s_DragSensitivity;

		internal static string s_RecycledCurrentEditingString;

		public static int FloatFieldHash => s_FloatFieldHash;
		public static double DragStartValue => s_DragStartValue;

		internal static bool StringToDouble(string str, out double value)
		{
			NumberFieldValue value2 = default(NumberFieldValue);
			StringToDouble(str, ref value2);
			value = value2.doubleVal;
			return value2.success;
		}

		private static void StringToDouble(string str, ref NumberFieldValue value)
		{
			value.success = TryConvertStringToDouble(str, out value.doubleVal); //, out value.expression);
		}

		internal static bool StringToLong(string str, out long value)
		{
			NumberFieldValue value2 = default(NumberFieldValue);
			StringToLong(str, ref value2);
			value = value2.longVal;
			return value2.success;
		}

		private static void StringToLong(string str, ref NumberFieldValue value)
		{
			//value.expression = null;
			value.success = TryConvertStringToLong(str, out value.longVal); //, out value.expression);
		}

		internal static void StringToNumericValue(in string str, ref NumberFieldValue value)
		{
			if (value.isDouble)
			{
				StringToDouble(str, ref value);
			}
			else
			{
				StringToLong(str, ref value);
			}
		}

		// copied from UnityEngine.UINumericFieldsUtils
		public static bool TryConvertStringToDouble(string str, out double value) //, out ExpressionEvaluator.Expression expr)
		{
			//expr = null;
			switch (str.ToLower())
			{
				case "inf":
				case "infinity":
					value = double.PositiveInfinity;
					break;
				case "-inf":
				case "-infinity":
					value = double.NegativeInfinity;
					break;
				case "nan":
					value = double.NaN;
					break;
				default:
					return ExpressionEvaluator.Evaluate<double>(str, out value); //, out expr);
			}

			return true;
		}

		public static bool TryConvertStringToLong(string str, out long value)
		{
			return ExpressionEvaluator.Evaluate<long>(str, out value);
		}

		internal static double CalculateFloatDragSensitivity(double value)
		{
			if (double.IsInfinity(value) || double.IsNaN(value))
			{
				return 0.0;
			}

			return Math.Max(1.0, Math.Pow(Math.Abs(value), 0.5)) * 0.029999999329447746;
		}

		internal static void UpdateNumberValueIfNeeded(ref NumberFieldValue value, in string str)
		{
			StringToNumericValue(in str, ref value);
			GUI.changed = value.success; // || value.expression != null;
		}

		private static bool HasKeyboardFocus(int controlID)
		{
			Type type = typeof(EditorGUI);
			MethodInfo methodInfo = type.GetMethod("HasKeyboardFocus", BindingFlags.NonPublic | BindingFlags.Static);

			if (methodInfo == null)
				throw new InvalidOperationException("Method not found");

			return (bool)methodInfo.Invoke(null, new object[] { controlID });
		}

		private static EventType GetEventTypeForControlAllowDisabledContextMenuPaste(Event evt, int id)
		{
			if (GUI.enabled)
			{
				return evt.GetTypeForControl(id);
			}

			GUI.enabled = true;
			EventType typeForControl = evt.GetTypeForControl(id);
			GUI.enabled = false;
			if (typeForControl == EventType.Repaint || typeForControl == EventType.Layout || typeForControl == EventType.Used)
			{
				return typeForControl;
			}

			int num;
			switch (typeForControl)
			{
				case EventType.ContextClick:
					return typeForControl;
				case EventType.MouseDown:
					num = ((evt.button == 1) ? 1 : 0);
					break;
				default:
					num = 0;
					break;
			}

			if (num != 0)
			{
				return typeForControl;
			}

			if ((typeForControl == EventType.ValidateCommand || typeForControl == EventType.ExecuteCommand) && evt.commandName == "Copy")
			{
				return typeForControl;
			}

			return EventType.Ignore;
		}

		internal static bool MightBePrintableKey(Event evt)
		{
			if (evt.command || evt.control)
			{
				return false;
			}

			if (evt.keyCode >= KeyCode.Mouse0 && evt.keyCode <= KeyCode.Mouse6)
			{
				return false;
			}

			if (evt.keyCode >= KeyCode.JoystickButton0 && evt.keyCode <= KeyCode.Joystick8Button19)
			{
				return false;
			}

			if (evt.keyCode >= KeyCode.F1 && evt.keyCode <= KeyCode.F15)
			{
				return false;
			}

			switch (evt.keyCode)
			{
				case KeyCode.Backspace:
				case KeyCode.Clear:
				case KeyCode.Pause:
				case KeyCode.Escape:
				case KeyCode.Delete:
				case KeyCode.UpArrow:
				case KeyCode.DownArrow:
				case KeyCode.RightArrow:
				case KeyCode.LeftArrow:
				case KeyCode.Insert:
				case KeyCode.Home:
				case KeyCode.End:
				case KeyCode.PageUp:
				case KeyCode.PageDown:
				case KeyCode.Numlock:
				case KeyCode.CapsLock:
				case KeyCode.ScrollLock:
				case KeyCode.RightShift:
				case KeyCode.LeftShift:
				case KeyCode.RightControl:
				case KeyCode.LeftControl:
				case KeyCode.RightAlt:
				case KeyCode.LeftAlt:
				case KeyCode.RightMeta:
				case KeyCode.LeftMeta:
				case KeyCode.LeftWindows:
				case KeyCode.RightWindows:
				case KeyCode.AltGr:
				case KeyCode.Help:
				case KeyCode.Print:
				case KeyCode.SysReq:
				case KeyCode.Menu:
					return false;
				case KeyCode.None:
					return IsPrintableChar(evt.character);
				default:
					return true;
			}
		}

		private static bool IsPrintableChar(char c)
		{
			if (c < ' ')
			{
				return false;
			}

			return true;
		}

		#endregion

		public static void DragNumberField(Rect dragHotZone, int id, ref NumberFieldValue value, double dragSensitivity)
		{
			string allowedletters = (value.isDouble ? s_AllowedCharactersForFloat : s_AllowedCharactersForInt);
			if (GUI.enabled)
			{
				DragNumberValue(dragHotZone, id, ref value, dragSensitivity);
			}
		}

		private static void DragNumberValue(Rect dragHotZone, int id, bool isDouble, ref double doubleVal, ref long longVal, double dragSensitivity)
		{
			NumberFieldValue value = default(NumberFieldValue);
			value.isDouble = isDouble;
			value.doubleVal = doubleVal;
			value.longVal = longVal;
			DragNumberValue(dragHotZone, id, ref value, dragSensitivity);
			doubleVal = value.doubleVal;
			longVal = value.longVal;
		}

		private static void DragNumberValue(Rect dragHotZone, int id, ref NumberFieldValue value, double dragSensitivity)
		{
			Event current = Event.current;
			switch (current.GetTypeForControl(id))
			{
				case EventType.MouseDown:
					if (CustomEditorUtil.HitTest(dragHotZone, current) && current.button == 0)
					{
						EditorGUIUtility.editingTextField = false;
						GUIUtility.hotControl = id;
						EndEditing_Text();
						current.Use();
						GUIUtility.keyboardControl = id;
						s_DragCandidateState = DragCandidateState.InitiatedDragging;
						s_DragStartValue = value.doubleVal;
						s_DragStartIntValue = value.longVal;
						s_DragStartPos = current.mousePosition;
						s_DragSensitivity = dragSensitivity;
						current.Use();
						EditorGUIUtility.SetWantsMouseJumping(1);
					}

					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == id && s_DragCandidateState != 0)
					{
						GUIUtility.hotControl = 0;
						s_DragCandidateState = DragCandidateState.NotDragging;
						current.Use();
						EditorGUIUtility.SetWantsMouseJumping(0);
					}

					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl != id)
					{
						break;
					}

					switch (s_DragCandidateState)
					{
						case DragCandidateState.InitiatedDragging:
							if ((Event.current.mousePosition - s_DragStartPos).sqrMagnitude > 16f)
							{
								s_DragCandidateState = DragCandidateState.CurrentlyDragging;
								GUIUtility.keyboardControl = id;
							}

							current.Use();
							break;
						case DragCandidateState.CurrentlyDragging:
							if (value.isDouble)
							{
								value.doubleVal += (double)HandleUtility.niceMouseDelta * s_DragSensitivity;
								value.doubleVal = CustomMathUtils.RoundBasedOnMinimumDifference(value.doubleVal, s_DragSensitivity);
							}
							else
							{
								value.longVal += (long)System.Math.Round((double)HandleUtility.niceMouseDelta * s_DragSensitivity);
							}

							value.success = true;
							GUI.changed = true;
							current.Use();
							break;
					}

					break;
				case EventType.KeyDown:
					if (GUIUtility.hotControl == id && current.keyCode == KeyCode.Escape && s_DragCandidateState != 0)
					{
						value.doubleVal = s_DragStartValue;
						value.longVal = s_DragStartIntValue;
						value.success = true;
						GUI.changed = true;
						GUIUtility.hotControl = 0;
						current.Use();
					}

					break;
				case EventType.Repaint:
					EditorGUIUtility.AddCursorRect(dragHotZone, MouseCursor.SlideArrow);
					break;
				case EventType.MouseMove:
				case EventType.KeyUp:
				case EventType.ScrollWheel:
					break;
			}
		}

		public static float DoFloatField(Rect position, Rect dragHotZone, int id, float value, string formatString, GUIStyle style, bool draggable)
		{
			return DoFloatField(position, dragHotZone, id, value, formatString, style, draggable, (Event.current.GetTypeForControl(id) == EventType.MouseDown) ? ((float)CalculateFloatDragSensitivity(s_DragStartValue)) : 0f);
		}

		internal static float DoFloatField(Rect position, Rect dragHotZone, int id, float value, string formatString, GUIStyle style, bool draggable, float dragSensitivity)
		{
			long longVal = 0L;
			double doubleVal = value;
			DoNumberField(position, dragHotZone, id, isDouble: true, ref doubleVal, ref longVal, formatString, style, draggable, dragSensitivity);
			return CustomMathUtils.ClampToFloat(doubleVal);
		}

		internal static int DoIntField(Rect position, Rect dragHotZone, int id, int value, string formatString, GUIStyle style, bool draggable, float dragSensitivity)
		{
			double doubleVal = 0.0;
			long longVal = value;
			DoNumberField(position, dragHotZone, id, isDouble: false, ref doubleVal, ref longVal, formatString, style, draggable, dragSensitivity);
			return CustomMathUtils.ClampToInt(longVal);
		}

		internal static void DoNumberField(Rect position, Rect dragHotZone, int id, bool isDouble, ref double doubleVal, ref long longVal, string formatString, GUIStyle style, bool draggable, double dragSensitivity)
		{
			NumberFieldValue value = default(NumberFieldValue);
			value.isDouble = isDouble;
			value.doubleVal = doubleVal;
			value.longVal = longVal;
			DoNumberField(position, dragHotZone, id, ref value, formatString, style, draggable, dragSensitivity);
			if (value.success)
			{
				doubleVal = value.doubleVal;
				longVal = value.longVal;
			}
		}

		internal static void DoNumberField(Rect position, Rect dragHotZone, int id, ref NumberFieldValue value, string formatString, GUIStyle style, bool draggable, double dragSensitivity)
		{
			string allowedletters = (value.isDouble ? s_AllowedCharactersForFloat : s_AllowedCharactersForInt);
			if (draggable && GUI.enabled)
			{
				DragNumberValue(dragHotZone, id, ref value, dragSensitivity);
			}

			Event current = Event.current;
			string text;
			if (HasKeyboardFocus(id) || (current.type == EventType.MouseDown && current.button == 0 && position.Contains(current.mousePosition)))
			{
				if (!IsEditingControl(id))
				{
					text = (s_RecycledCurrentEditingString = (value.isDouble ? value.doubleVal.ToString(formatString, CultureInfo.InvariantCulture) : value.longVal.ToString(formatString, CultureInfo.InvariantCulture)));
				}
				else
				{
					text = s_RecycledCurrentEditingString;
					if (current.type == EventType.ValidateCommand && current.commandName == "UndoRedoPerformed")
					{
						text = (value.isDouble ? value.doubleVal.ToString(formatString, CultureInfo.InvariantCulture) : value.longVal.ToString(formatString, CultureInfo.InvariantCulture));
					}
				}
			}
			else
			{
				text = (value.isDouble ? value.doubleVal.ToString(formatString, CultureInfo.InvariantCulture) : value.longVal.ToString(formatString, CultureInfo.InvariantCulture));
			}

			text = DoTextField(id, position, text, style, allowedletters, out var changed, reset: false, multiline: false, passwordField: false);
			if (GUIUtility.keyboardControl == id && changed)
			{
				s_RecycledCurrentEditingString = text;
				UpdateNumberValueIfNeeded(ref value, in text);
			}
		}

		public static string DoTextField(int id, Rect position, string text, GUIStyle style, string allowedletters, out bool changed, bool reset, bool multiline, bool passwordField)
		{
			return DoTextField(id, position, text, style, allowedletters, out changed, reset, multiline, passwordField, null);
		}

		/// <summary>
		/// Added_Reflection
		/// </summary>
		/// <param name="id"></param>
		/// <param name="position"></param>
		/// <param name="text"></param>
		/// <param name="style"></param>
		/// <param name="allowedletters"></param>
		/// <param name="changed"></param>
		/// <param name="reset"></param>
		/// <param name="multiline"></param>
		/// <param name="passwordField"></param>
		/// <param name="cancelButtonStyle"></param>
		/// <param name="checkTextLimit"></param>
		/// <returns></returns>
		internal static string DoTextField(int id, Rect position, string text, GUIStyle style, string allowedletters, out bool changed, bool reset, bool multiline, bool passwordField, GUIStyle cancelButtonStyle, bool checkTextLimit = false)
		{
			if (methodInfo_DoTextField == null)
			{
				MethodInfo[] methods = typeof(EditorGUI).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.Name == "DoTextField").ToArray();
				methodInfo_DoTextField = methods.FirstOrDefault(m => m.GetParameters().Length == 12);
			}

			if (methodInfo_DoTextField == null)
			{
				Debug.LogError($"Fail to get method info \"DoTextField\" from UnityEditor.EditorGUI");
				changed = false;
				return string.Empty;
			}

			bool outChanged = false;
			object[] parameters = new object[] { RecycledEditor, id, position, text, style, allowedletters, outChanged, reset, multiline, passwordField, cancelButtonStyle, checkTextLimit };
			string result = (string)methodInfo_DoTextField?.Invoke(null, parameters) ?? string.Empty;
			changed = outChanged;
			return result;
		}

		private static void EndEditing_Text()
		{
			System.Type type = typeof(EditorGUI);
			FieldInfo activeEditor_FieldInfo = type.GetField("activeEditor", BindingFlags.Static | BindingFlags.NonPublic);
			object activeEditor = activeEditor_FieldInfo.GetValue(null);
			if (activeEditor != null)
			{
				MethodInfo EndEditing_MethodInfo = activeEditor.GetType().GetMethod("EndEditing", BindingFlags.Public | BindingFlags.Instance);
				EndEditing_MethodInfo.Invoke(activeEditor_FieldInfo.GetValue(null), new object[] { });
			}
		}
	}

	internal class CustomEditorGUIExt
	{
		private class MinMaxSliderState
		{
			public float dragStartPos = 0f;

			public float dragStartValue = 0f;

			public float dragStartSize = 0f;

			public float dragStartValuesPerPixel = 0f;

			public float dragStartLimit = 0f;

			public float dragEndLimit = 0f;

			public int whereWeDrag = -1;
		}

		internal static int s_MinMaxSliderHash = "MinMaxSlider".GetHashCode();
		private static int repeatButtonHash = "repeatButton".GetHashCode();

		private static int scrollWait = 30;
		private static int firstScrollWait = 250;
		private static float nextScrollStepTime = 0f;

		private static int scrollControlID;

		private static MinMaxSliderState s_MinMaxSliderState;
		private static DateTime s_NextScrollStepTime = DateTime.Now;

		public static void MinMaxSlider(Rect position, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, bool horiz)
		{
			DoMinMaxSlider(position, GUIUtility.GetControlID(s_MinMaxSliderHash, FocusType.Passive), ref value, ref size, visualStart, visualEnd, startLimit, endLimit, slider, thumb, horiz);
		}

		internal static void DoMinMaxSlider(Rect position, int id, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, bool horiz)
		{
			Event current = Event.current;
			bool flag = size == 0f;
			float num = Mathf.Min(visualStart, visualEnd);
			float num2 = Mathf.Max(visualStart, visualEnd);
			float num3 = Mathf.Min(startLimit, endLimit);
			float num4 = Mathf.Max(startLimit, endLimit);
			MinMaxSliderState minMaxSliderState = s_MinMaxSliderState;
			if (GUIUtility.hotControl == id && minMaxSliderState != null)
			{
				num = minMaxSliderState.dragStartLimit;
				num3 = minMaxSliderState.dragStartLimit;
				num2 = minMaxSliderState.dragEndLimit;
				num4 = minMaxSliderState.dragEndLimit;
			}

			float num5 = 0f;
			float num6 = Mathf.Clamp(value, num, num2);
			float num7 = Mathf.Clamp(value + size, num, num2) - num6;
			float num8 = ((!(visualStart > visualEnd)) ? 1 : (-1));
			if (slider == null || thumb == null)
			{
				return;
			}

			Rect rect = thumb.margin.Remove(slider.padding.Remove(position));
			float num9 = ThumbSize(horiz, thumb);
			float num10;
			Rect position2;
			Rect position3;
			Rect position4;
			float num11;
			if (horiz)
			{
				float height = ((thumb.fixedHeight != 0f) ? thumb.fixedHeight : rect.height);
				num10 = (position.width - (float)slider.padding.horizontal - num9) / (num2 - num);
				position2 = new Rect((num6 - num) * num10 + rect.x, rect.y, num7 * num10 + num9, height);
				position3 = new Rect(position2.x, position2.y, thumb.padding.left, position2.height);
				position4 = new Rect(position2.xMax - (float)thumb.padding.right, position2.y, thumb.padding.right, position2.height);
				num11 = current.mousePosition.x - position.x;
			}
			else
			{
				float width = ((thumb.fixedWidth != 0f) ? thumb.fixedWidth : rect.width);
				num10 = (position.height - (float)slider.padding.vertical - num9) / (num2 - num);
				position2 = new Rect(rect.x, (num6 - num) * num10 + rect.y, width, num7 * num10 + num9);
				position3 = new Rect(position2.x, position2.y, position2.width, thumb.padding.top);
				position4 = new Rect(position2.x, position2.yMax - (float)thumb.padding.bottom, position2.width, thumb.padding.bottom);
				num11 = current.mousePosition.y - position.y;
			}

			switch (current.GetTypeForControl(id))
			{
				case EventType.MouseDown:
					if (current.button != 0 || !position.Contains(current.mousePosition) || num - num2 == 0f)
					{
						break;
					}

					if (minMaxSliderState == null)
					{
						minMaxSliderState = (s_MinMaxSliderState = new MinMaxSliderState());
					}

					minMaxSliderState.dragStartLimit = startLimit;
					minMaxSliderState.dragEndLimit = endLimit;
					if (position2.Contains(current.mousePosition))
					{
						minMaxSliderState.dragStartPos = num11;
						minMaxSliderState.dragStartValue = value;
						minMaxSliderState.dragStartSize = size;
						minMaxSliderState.dragStartValuesPerPixel = num10;
						if (position3.Contains(current.mousePosition))
						{
							minMaxSliderState.whereWeDrag = 1;
						}
						else if (position4.Contains(current.mousePosition))
						{
							minMaxSliderState.whereWeDrag = 2;
						}
						else
						{
							minMaxSliderState.whereWeDrag = 0;
						}

						GUIUtility.hotControl = id;
						current.Use();
					}
					else
					{
						if (slider == GUIStyle.none)
						{
							break;
						}

						if (size != 0f && flag)
						{
							if (horiz)
							{
								if (num11 > position2.xMax - position.x)
								{
									value += size * num8 * 0.9f;
								}
								else
								{
									value -= size * num8 * 0.9f;
								}
							}
							else if (num11 > position2.yMax - position.y)
							{
								value += size * num8 * 0.9f;
							}
							else
							{
								value -= size * num8 * 0.9f;
							}

							minMaxSliderState.whereWeDrag = 0;
							GUI.changed = true;
							s_NextScrollStepTime = DateTime.Now.AddMilliseconds(firstScrollWait);
							float num12 = (horiz ? current.mousePosition.x : current.mousePosition.y);
							float num13 = (horiz ? position2.x : position2.y);
							minMaxSliderState.whereWeDrag = ((num12 > num13) ? 4 : 3);
						}
						else
						{
							if (horiz)
							{
								value = (num11 - position2.width * 0.5f) / num10 + num - size * 0.5f;
							}
							else
							{
								value = (num11 - position2.height * 0.5f) / num10 + num - size * 0.5f;
							}

							minMaxSliderState.dragStartPos = num11;
							minMaxSliderState.dragStartValue = value;
							minMaxSliderState.dragStartSize = size;
							minMaxSliderState.dragStartValuesPerPixel = num10;
							minMaxSliderState.whereWeDrag = 0;
							GUI.changed = true;
						}

						GUIUtility.hotControl = id;
						value = Mathf.Clamp(value, num3, num4 - size);
						current.Use();
					}

					break;
				case EventType.MouseDrag:
				{
					if (GUIUtility.hotControl != id)
					{
						break;
					}

					float num15 = (num11 - minMaxSliderState.dragStartPos) / minMaxSliderState.dragStartValuesPerPixel;
					switch (minMaxSliderState.whereWeDrag)
					{
						case 0:
							value = Mathf.Clamp(minMaxSliderState.dragStartValue + num15, num3, num4 - size);
							break;
						case 1:
							value = minMaxSliderState.dragStartValue + num15;
							size = minMaxSliderState.dragStartSize - num15;
							if (value < num3)
							{
								size -= num3 - value;
								value = num3;
							}

							if (size < num5)
							{
								value -= num5 - size;
								size = num5;
							}

							break;
						case 2:
							size = minMaxSliderState.dragStartSize + num15;
							if (value + size > num4)
							{
								size = num4 - value;
							}

							if (size < num5)
							{
								size = num5;
							}

							break;
					}

					GUI.changed = true;
					current.Use();
					break;
				}
				case EventType.MouseUp:
					if (GUIUtility.hotControl == id)
					{
						current.Use();
						GUIUtility.hotControl = 0;
					}

					break;
				case EventType.Repaint:
					slider.Draw(position, GUIContent.none, id);
					thumb.Draw(position2, GUIContent.none, id);
					EditorGUIUtility.AddCursorRect(position3, horiz ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical, (minMaxSliderState != null && minMaxSliderState.whereWeDrag == 1) ? id : (-1));
					EditorGUIUtility.AddCursorRect(position4, horiz ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical, (minMaxSliderState != null && minMaxSliderState.whereWeDrag == 2) ? id : (-1));
					if (GUIUtility.hotControl != id || !position.Contains(current.mousePosition) || num - num2 == 0f)
					{
						break;
					}

					if (position2.Contains(current.mousePosition))
					{
						if (minMaxSliderState != null && (minMaxSliderState.whereWeDrag == 3 || minMaxSliderState.whereWeDrag == 4))
						{
							GUIUtility.hotControl = 0;
						}
					}
					else
					{
						if (DateTime.Now < s_NextScrollStepTime)
						{
							break;
						}

						float num12 = (horiz ? current.mousePosition.x : current.mousePosition.y);
						float num13 = (horiz ? position2.x : position2.y);
						int num14 = ((num12 > num13) ? 4 : 3);
						if (minMaxSliderState != null && num14 != minMaxSliderState.whereWeDrag)
						{
							break;
						}

						if (size != 0f && flag)
						{
							if (horiz)
							{
								if (num11 > position2.xMax - position.x)
								{
									value += size * num8 * 0.9f;
								}
								else
								{
									value -= size * num8 * 0.9f;
								}
							}
							else if (num11 > position2.yMax - position.y)
							{
								value += size * num8 * 0.9f;
							}
							else
							{
								value -= size * num8 * 0.9f;
							}

							if (minMaxSliderState != null)
							{
								minMaxSliderState.whereWeDrag = -1;
							}

							GUI.changed = true;
						}

						value = Mathf.Clamp(value, num3, num4 - size);
						s_NextScrollStepTime = DateTime.Now.AddMilliseconds(scrollWait);
					}

					break;
			}
		}

		public static void MinMaxScroller(Rect position, int id, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
		{
			float num = ((!horiz) ? (size * 10f / position.height) : (size * 10f / position.width));
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

			float num2 = Mathf.Min(visualStart, value);
			float num3 = Mathf.Max(visualEnd, value + size);
			MinMaxSlider(position2, ref value, ref size, num2, num3, num2, num3, slider, thumb, horiz);
			if (ScrollerRepeatButton(id, rect, leftButton))
			{
				value -= num * ((visualStart < visualEnd) ? 1f : (-1f));
			}

			if (ScrollerRepeatButton(id, rect2, rightButton))
			{
				value += num * ((visualStart < visualEnd) ? 1f : (-1f));
			}

			if (Event.current.type == EventType.MouseUp && Event.current.type == EventType.Used)
			{
				scrollControlID = 0;
			}

			if (startLimit < endLimit)
			{
				value = Mathf.Clamp(value, startLimit, endLimit - size);
			}
			else
			{
				value = Mathf.Clamp(value, endLimit, startLimit - size);
			}
		}

		private static float ThumbSize(bool horiz, GUIStyle thumb)
		{
			if (horiz)
			{
				return (thumb.fixedWidth != 0f) ? thumb.fixedWidth : ((float)thumb.padding.horizontal);
			}

			return (thumb.fixedHeight != 0f) ? thumb.fixedHeight : ((float)thumb.padding.vertical);
		}

		private static bool ScrollerRepeatButton(int scrollerID, Rect rect, GUIStyle style)
		{
			bool result = false;
			if (DoRepeatButton(rect, GUIContent.none, style, FocusType.Passive))
			{
				bool flag = scrollControlID != scrollerID;
				scrollControlID = scrollerID;
				if (flag)
				{
					result = true;
					nextScrollStepTime = Time.realtimeSinceStartup + 0.001f * (float)firstScrollWait;
				}
				else if (Time.realtimeSinceStartup >= nextScrollStepTime)
				{
					result = true;
					nextScrollStepTime = Time.realtimeSinceStartup + 0.001f * (float)scrollWait;
				}

				if (Event.current.type == EventType.Repaint)
				{
					HandleUtility.Repaint();
				}
			}

			return result;
		}

		private static bool DoRepeatButton(Rect position, GUIContent content, GUIStyle style, FocusType focusType)
		{
			int controlID = GUIUtility.GetControlID(repeatButtonHash, focusType, position);
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

	[Serializable]
	internal class TickHandler
	{
		[SerializeField] private float[] m_TickModulos = new float[0];

		[SerializeField] private float[] m_TickStrengths = new float[0];

		[SerializeField] private int m_SmallestTick = 0;

		[SerializeField] private int m_BiggestTick = -1;

		[SerializeField] private float m_MinValue = 0f;

		[SerializeField] private float m_MaxValue = 1f;

		[SerializeField] private float m_PixelRange = 1f;

		private List<float> m_TickList = new List<float>(1000);

		public int tickLevels => m_BiggestTick - m_SmallestTick + 1;

		public void SetTickModulos(float[] tickModulos)
		{
			m_TickModulos = tickModulos;
		}

		public List<float> GetTickModulosForFrameRate(float frameRate)
		{
			if (frameRate > 1.07374182E+09f || frameRate != Mathf.Round(frameRate))
			{
				return new List<float>
				{
					1f / frameRate,
					5f / frameRate,
					10f / frameRate,
					50f / frameRate,
					100f / frameRate,
					500f / frameRate,
					1000f / frameRate,
					5000f / frameRate,
					10000f / frameRate,
					50000f / frameRate,
					100000f / frameRate,
					500000f / frameRate
				};
			}

			List<int> list = new List<int>();
			int num = 1;
			while ((float)num < frameRate && !((double)Math.Abs((float)num - frameRate) < 1E-05))
			{
				int num2 = Mathf.RoundToInt(frameRate / (float)num);
				if (num2 % 60 == 0)
				{
					num *= 2;
					list.Add(num);
				}
				else if (num2 % 30 == 0)
				{
					num *= 3;
					list.Add(num);
				}
				else if (num2 % 20 == 0)
				{
					num *= 2;
					list.Add(num);
				}
				else if (num2 % 10 == 0)
				{
					num *= 2;
					list.Add(num);
				}
				else if (num2 % 5 == 0)
				{
					num *= 5;
					list.Add(num);
				}
				else if (num2 % 2 == 0)
				{
					num *= 2;
					list.Add(num);
				}
				else if (num2 % 3 == 0)
				{
					num *= 3;
					list.Add(num);
				}
				else
				{
					num = Mathf.RoundToInt(frameRate);
				}
			}

			List<float> list2 = new List<float>(13 + list.Count);
			for (int i = 0; i < list.Count; ++i)
			{
				list2.Add(1f / (float)list[list.Count - i - 1]);
			}

			list2.Add(1f);
			list2.Add(5f);
			list2.Add(10f);
			list2.Add(30f);
			list2.Add(60f);
			list2.Add(300f);
			list2.Add(600f);
			list2.Add(1800f);
			list2.Add(3600f);
			list2.Add(21600f);
			list2.Add(86400f);
			list2.Add(604800f);
			list2.Add(1209600f);
			return list2;
		}

		public void SetTickModulosForFrameRate(float frameRate)
		{
			List<float> tickModulosForFrameRate = GetTickModulosForFrameRate(frameRate);
			SetTickModulos(tickModulosForFrameRate.ToArray());
		}

		public void SetRanges(float minValue, float maxValue, float minPixel, float maxPixel)
		{
			m_MinValue = minValue;
			m_MaxValue = maxValue;
			m_PixelRange = maxPixel - minPixel;
		}

		public float[] GetTicksAtLevel(int level, bool excludeTicksFromHigherlevels)
		{
			if (level < 0)
			{
				return new float[0];
			}

			m_TickList.Clear();
			GetTicksAtLevel(level, excludeTicksFromHigherlevels, m_TickList);
			return m_TickList.ToArray();
		}

		public void GetTicksAtLevel(int level, bool excludeTicksFromHigherlevels, List<float> list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}

			int num = Mathf.Clamp(m_SmallestTick + level, 0, m_TickModulos.Length - 1);
			int num2 = Mathf.FloorToInt(m_MinValue / m_TickModulos[num]);
			int num3 = Mathf.CeilToInt(m_MaxValue / m_TickModulos[num]);
			for (int i = num2; i <= num3; ++i)
			{
				if (!excludeTicksFromHigherlevels || num >= m_BiggestTick || i % Mathf.RoundToInt(m_TickModulos[num + 1] / m_TickModulos[num]) != 0)
				{
					list.Add((float)i * m_TickModulos[num]);
				}
			}
		}

		public float GetStrengthOfLevel(int level)
		{
			return m_TickStrengths[m_SmallestTick + level];
		}

		public float GetPeriodOfLevel(int level)
		{
			return m_TickModulos[Mathf.Clamp(m_SmallestTick + level, 0, m_TickModulos.Length - 1)];
		}

		public int GetLevelWithMinSeparation(float pixelSeparation)
		{
			for (int i = 0; i < m_TickModulos.Length; ++i)
			{
				float num = m_TickModulos[i] * m_PixelRange / (m_MaxValue - m_MinValue);
				if (num >= pixelSeparation)
				{
					return i - m_SmallestTick;
				}
			}

			return -1;
		}

		public void SetTickStrengths(float tickMinSpacing, float tickMaxSpacing, bool sqrt)
		{
			if (m_TickStrengths == null || m_TickStrengths.Length != m_TickModulos.Length)
			{
				m_TickStrengths = new float[m_TickModulos.Length];
			}

			m_SmallestTick = 0;
			m_BiggestTick = m_TickModulos.Length - 1;
			for (int num = m_TickModulos.Length - 1; num >= 0; num--)
			{
				float num2 = m_TickModulos[num] * m_PixelRange / (m_MaxValue - m_MinValue);
				m_TickStrengths[num] = (num2 - tickMinSpacing) / (tickMaxSpacing - tickMinSpacing);
				if (m_TickStrengths[num] >= 1f)
				{
					m_BiggestTick = num;
				}

				if (num2 <= tickMinSpacing)
				{
					m_SmallestTick = num;
					break;
				}
			}

			for (int i = m_SmallestTick; i <= m_BiggestTick; ++i)
			{
				m_TickStrengths[i] = Mathf.Clamp01(m_TickStrengths[i]);
				if (sqrt)
				{
					m_TickStrengths[i] = Mathf.Sqrt(m_TickStrengths[i]);
				}
			}
		}
	}

	/// <summary>
	/// Custom Copy of UnityEditor.ZoomableArea
	/// </summary>
	[Tooltip("Custom Copy of UnityEditor.ZoomableArea")]
	[System.Serializable]
	internal class ZoomableArea //: UnityEditor.ZoomableArea
	{
		public enum YDirection
		{
			Positive,
			Negative
		}

		public class Styles
		{
			private class SliderTypeStyles
			{
				public class SliderAxisStyles
				{
					public GUIStyle horizontal;

					public GUIStyle vertical;
				}

				public SliderAxisStyles scrollbar;

				public SliderAxisStyles minMaxSliders;
			}

			public GUIStyle horizontalScrollbar;

			public GUIStyle horizontalScrollbarLeftButton;

			public GUIStyle horizontalScrollbarRightButton;

			public GUIStyle verticalScrollbar;

			public GUIStyle verticalScrollbarUpButton;

			public GUIStyle verticalScrollbarDownButton;

			public bool enableSliderZoomHorizontal;

			public bool enableSliderZoomVertical;

			public float sliderWidth;

			public float visualSliderWidth;

			private bool minimalGUI;

			private static SliderTypeStyles minimalSliderStyles;

			private static SliderTypeStyles normalSliderStyles;

			public GUIStyle horizontalMinMaxScrollbarThumb => GetSliderAxisStyle(enableSliderZoomHorizontal).horizontal;

			public GUIStyle verticalMinMaxScrollbarThumb => GetSliderAxisStyle(enableSliderZoomVertical).vertical;

			private SliderTypeStyles.SliderAxisStyles GetSliderAxisStyle(bool enableSliderZoom)
			{
				if (minimalGUI)
				{
					return enableSliderZoom ? minimalSliderStyles.minMaxSliders : minimalSliderStyles.scrollbar;
				}

				return enableSliderZoom ? normalSliderStyles.minMaxSliders : normalSliderStyles.scrollbar;
			}

			public Styles(bool minimalGUI)
			{
				if (minimalGUI)
				{
					visualSliderWidth = 0f;
					sliderWidth = 13f;
				}
				else
				{
					visualSliderWidth = 13f;
					sliderWidth = 13f;
				}
			}

			public void InitGUIStyles(bool minimalGUI, bool enableSliderZoom)
			{
				InitGUIStyles(minimalGUI, enableSliderZoom, enableSliderZoom);
			}

			public void InitGUIStyles(bool minimalGUI, bool enableSliderZoomHorizontal, bool enableSliderZoomVertical)
			{
				this.minimalGUI = minimalGUI;
				this.enableSliderZoomHorizontal = enableSliderZoomHorizontal;
				this.enableSliderZoomVertical = enableSliderZoomVertical;
				if (minimalGUI)
				{
					if (minimalSliderStyles == null)
					{
						minimalSliderStyles = new SliderTypeStyles
						{
							scrollbar = new SliderTypeStyles.SliderAxisStyles
							{
								horizontal = "MiniSliderhorizontal",
								vertical = "MiniSliderVertical"
							},
							minMaxSliders = new SliderTypeStyles.SliderAxisStyles
							{
								horizontal = "MiniMinMaxSliderHorizontal",
								vertical = "MiniMinMaxSlidervertical"
							}
						};
					}

					horizontalScrollbarLeftButton = GUIStyle.none;
					horizontalScrollbarRightButton = GUIStyle.none;
					horizontalScrollbar = GUIStyle.none;
					verticalScrollbarUpButton = GUIStyle.none;
					verticalScrollbarDownButton = GUIStyle.none;
					verticalScrollbar = GUIStyle.none;
				}
				else
				{
					if (normalSliderStyles == null)
					{
						normalSliderStyles = new SliderTypeStyles
						{
							scrollbar = new SliderTypeStyles.SliderAxisStyles
							{
								horizontal = "horizontalscrollbarthumb",
								vertical = "verticalscrollbarthumb"
							},
							minMaxSliders = new SliderTypeStyles.SliderAxisStyles
							{
								horizontal = "horizontalMinMaxScrollbarThumb",
								vertical = "verticalMinMaxScrollbarThumb"
							}
						};
					}

					horizontalScrollbarLeftButton = "horizontalScrollbarLeftbutton";
					horizontalScrollbarRightButton = "horizontalScrollbarRightbutton";
					horizontalScrollbar = GUI.skin.horizontalScrollbar;
					verticalScrollbarUpButton = "verticalScrollbarUpbutton";
					verticalScrollbarDownButton = "verticalScrollbarDownbutton";
					verticalScrollbar = GUI.skin.verticalScrollbar;
				}
			}
		}

		private static Vector2 m_MouseDownPosition = new Vector2(-1000000f, -1000000f);
		private static int zoomableAreaHash = "ZoomableArea".GetHashCode();
		private static int s_MinMaxSliderHash = "MinMaxSlider".GetHashCode();

		[SerializeField] private bool m_HRangeLocked;

		[SerializeField] private bool m_VRangeLocked;

		public bool hZoomLockedByDefault = false;

		public bool vZoomLockedByDefault = false;

		[SerializeField] private float m_HBaseRangeMin = 0f;

		[SerializeField] private float m_HBaseRangeMax = 1f;

		[SerializeField] private float m_VBaseRangeMin = 0f;

		[SerializeField] private float m_VBaseRangeMax = 1f;

		[SerializeField] private bool m_HAllowExceedBaseRangeMin = true;

		[SerializeField] private bool m_HAllowExceedBaseRangeMax = true;

		[SerializeField] private bool m_VAllowExceedBaseRangeMin = true;

		[SerializeField] private bool m_VAllowExceedBaseRangeMax = true;

		private const float kMinScale = 1E-05f;

		private const float kMaxScale = 100000f;

		private float m_HScaleMin = 1E-05f;

		private float m_HScaleMax = 100000f;

		private float m_VScaleMin = 1E-05f;

		private float m_VScaleMax = 100000f;

		private float m_MinWidth = 0.05f;

		private const float kMinHeight = 0.05f;

		private const float k_ScrollStepSize = 10f;

		[SerializeField] private bool m_ScaleWithWindow = false;

		[SerializeField] private bool m_HSlider = true;

		[SerializeField] private bool m_VSlider = true;

		[SerializeField] private bool m_IgnoreScrollWheelUntilClicked = false;

		[SerializeField] private bool m_EnableMouseInput = true;

		[SerializeField] private bool m_EnableSliderZoomHorizontal = true;

		[SerializeField] private bool m_EnableSliderZoomVertical = true;

		public bool m_UniformScale;

		[SerializeField] private YDirection m_UpDirection = YDirection.Positive;

		[SerializeField] private Rect m_DrawArea = new Rect(0f, 0f, 100f, 100f);

		[SerializeField] internal Vector2 m_Scale = new Vector2(1f, -1f);

		[SerializeField] internal Vector2 m_Translation = new Vector2(0f, 0f);

		[SerializeField] private float m_MarginLeft;

		[SerializeField] private float m_MarginRight;

		[SerializeField] private float m_MarginTop;

		[SerializeField] private float m_MarginBottom;

		[SerializeField] private Rect m_LastShownAreaInsideMargins = new Rect(0f, 0f, 100f, 100f);

		internal int areaControlID;

		private int verticalScrollbarID;

		private int horizontalScrollbarID;

		[SerializeField] private bool m_MinimalGUI;

		private Styles m_Styles;

		public bool hRangeLocked
		{
			get { return m_HRangeLocked; }
			set { m_HRangeLocked = value; }
		}

		public bool vRangeLocked
		{
			get { return m_VRangeLocked; }
			set { m_VRangeLocked = value; }
		}

		public float hBaseRangeMin
		{
			get { return m_HBaseRangeMin; }
			set { m_HBaseRangeMin = value; }
		}

		public float hBaseRangeMax
		{
			get { return m_HBaseRangeMax; }
			set { m_HBaseRangeMax = value; }
		}

		public float vBaseRangeMin
		{
			get { return m_VBaseRangeMin; }
			set { m_VBaseRangeMin = value; }
		}

		public float vBaseRangeMax
		{
			get { return m_VBaseRangeMax; }
			set { m_VBaseRangeMax = value; }
		}

		public bool hAllowExceedBaseRangeMin
		{
			get { return m_HAllowExceedBaseRangeMin; }
			set { m_HAllowExceedBaseRangeMin = value; }
		}

		public bool hAllowExceedBaseRangeMax
		{
			get { return m_HAllowExceedBaseRangeMax; }
			set { m_HAllowExceedBaseRangeMax = value; }
		}

		public bool vAllowExceedBaseRangeMin
		{
			get { return m_VAllowExceedBaseRangeMin; }
			set { m_VAllowExceedBaseRangeMin = value; }
		}

		public bool vAllowExceedBaseRangeMax
		{
			get { return m_VAllowExceedBaseRangeMax; }
			set { m_VAllowExceedBaseRangeMax = value; }
		}

		public float hRangeMin
		{
			get { return hAllowExceedBaseRangeMin ? float.NegativeInfinity : hBaseRangeMin; }
			set { SetAllowExceed(ref m_HBaseRangeMin, ref m_HAllowExceedBaseRangeMin, value); }
		}

		public float hRangeMax
		{
			get { return hAllowExceedBaseRangeMax ? float.PositiveInfinity : hBaseRangeMax; }
			set { SetAllowExceed(ref m_HBaseRangeMax, ref m_HAllowExceedBaseRangeMax, value); }
		}

		public float vRangeMin
		{
			get { return vAllowExceedBaseRangeMin ? float.NegativeInfinity : vBaseRangeMin; }
			set { SetAllowExceed(ref m_VBaseRangeMin, ref m_VAllowExceedBaseRangeMin, value); }
		}

		public float vRangeMax
		{
			get { return vAllowExceedBaseRangeMax ? float.PositiveInfinity : vBaseRangeMax; }
			set { SetAllowExceed(ref m_VBaseRangeMax, ref m_VAllowExceedBaseRangeMax, value); }
		}

		public float minWidth
		{
			get { return m_MinWidth; }
			set
			{
				if (value > 0f)
				{
					m_MinWidth = value;
					return;
				}

				Debug.LogWarning("Zoomable area width cannot have a value of or below 0. Reverting back to a default value of 0.05f");
				m_MinWidth = 0.05f;
			}
		}

		public float hScaleMin
		{
			get { return m_HScaleMin; }
			set
			{
				m_HScaleMin = Mathf.Clamp(value, 1E-05f, 100000f);
				styles.enableSliderZoomHorizontal = allowSliderZoomHorizontal;
			}
		}

		public float hScaleMax
		{
			get { return m_HScaleMax; }
			set
			{
				m_HScaleMax = Mathf.Clamp(value, 1E-05f, 100000f);
				styles.enableSliderZoomHorizontal = allowSliderZoomHorizontal;
			}
		}

		public float vScaleMin
		{
			get { return m_VScaleMin; }
			set
			{
				m_VScaleMin = Mathf.Clamp(value, 1E-05f, 100000f);
				styles.enableSliderZoomVertical = allowSliderZoomVertical;
			}
		}

		public float vScaleMax
		{
			get { return m_VScaleMax; }
			set
			{
				m_VScaleMax = Mathf.Clamp(value, 1E-05f, 100000f);
				styles.enableSliderZoomVertical = allowSliderZoomVertical;
			}
		}

		public bool scaleWithWindow
		{
			get { return m_ScaleWithWindow; }
			set { m_ScaleWithWindow = value; }
		}

		public bool hSlider
		{
			get { return m_HSlider; }
			set
			{
				Rect rect = this.rect;
				m_HSlider = value;
				this.rect = rect;
			}
		}

		public bool vSlider
		{
			get { return m_VSlider; }
			set
			{
				Rect rect = this.rect;
				m_VSlider = value;
				this.rect = rect;
			}
		}

		public bool ignoreScrollWheelUntilClicked
		{
			get { return m_IgnoreScrollWheelUntilClicked; }
			set { m_IgnoreScrollWheelUntilClicked = value; }
		}

		public bool enableMouseInput
		{
			get { return m_EnableMouseInput; }
			set { m_EnableMouseInput = value; }
		}

		protected bool allowSliderZoomHorizontal => m_EnableSliderZoomHorizontal && m_HScaleMin < m_HScaleMax;

		protected bool allowSliderZoomVertical => m_EnableSliderZoomVertical && m_VScaleMin < m_VScaleMax;

		public bool uniformScale
		{
			get { return m_UniformScale; }
			set { m_UniformScale = value; }
		}

		public YDirection upDirection
		{
			get { return m_UpDirection; }
			set
			{
				if (m_UpDirection != value)
				{
					m_UpDirection = value;
					m_Scale.y = 0f - m_Scale.y;
				}
			}
		}

		public Vector2 scale => m_Scale;

		public Vector2 translation => m_Translation;

		public float margin
		{
			set { m_MarginLeft = (m_MarginRight = (m_MarginTop = (m_MarginBottom = value))); }
		}

		public float leftmargin
		{
			get { return m_MarginLeft; }
			set { m_MarginLeft = value; }
		}

		public float rightmargin
		{
			get { return m_MarginRight; }
			set { m_MarginRight = value; }
		}

		public float topmargin
		{
			get { return m_MarginTop; }
			set { m_MarginTop = value; }
		}

		public float bottommargin
		{
			get { return m_MarginBottom; }
			set { m_MarginBottom = value; }
		}

		public float vSliderWidth => vSlider ? styles.sliderWidth : 0f;

		public float hSliderHeight => hSlider ? styles.sliderWidth : 0f;

		protected Styles styles
		{
			get
			{
				if (m_Styles == null)
				{
					m_Styles = new Styles(m_MinimalGUI);
				}

				return m_Styles;
			}
		}

		public Rect rect
		{
			get { return new Rect(drawRect.x, drawRect.y, drawRect.width + (m_VSlider ? styles.visualSliderWidth : 0f), drawRect.height + (m_HSlider ? styles.visualSliderWidth : 0f)); }
			set
			{
				Rect rect = new Rect(value.x, value.y, value.width - (m_VSlider ? styles.visualSliderWidth : 0f), value.height - (m_HSlider ? styles.visualSliderWidth : 0f));
				if (rect != m_DrawArea)
				{
					if (m_ScaleWithWindow)
					{
						m_DrawArea = rect;
						shownAreaInsideMargins = m_LastShownAreaInsideMargins;
					}
					else
					{
						m_Translation += new Vector2((rect.width - m_DrawArea.width) / 2f, (rect.height - m_DrawArea.height) / 2f);
						m_DrawArea = rect;
					}
				}

				EnforceScaleAndRange();
			}
		}

		public Rect drawRect => m_DrawArea;

		public Rect shownArea
		{
			get
			{
				if (m_UpDirection == YDirection.Positive)
				{
					return new Rect((0f - m_Translation.x) / m_Scale.x, (0f - (m_Translation.y - drawRect.height)) / m_Scale.y, drawRect.width / m_Scale.x, drawRect.height / (0f - m_Scale.y));
				}

				return new Rect((0f - m_Translation.x) / m_Scale.x, (0f - m_Translation.y) / m_Scale.y, drawRect.width / m_Scale.x, drawRect.height / m_Scale.y);
			}
			set
			{
				float num = ((value.width < m_MinWidth) ? m_MinWidth : value.width);
				float num2 = ((value.height < 0.05f) ? 0.05f : value.height);
				if (m_UpDirection == YDirection.Positive)
				{
					m_Scale.x = drawRect.width / num;
					m_Scale.y = (0f - drawRect.height) / num2;
					m_Translation.x = (0f - value.x) * m_Scale.x;
					m_Translation.y = drawRect.height - value.y * m_Scale.y;
				}
				else
				{
					m_Scale.x = drawRect.width / num;
					m_Scale.y = drawRect.height / num2;
					m_Translation.x = (0f - value.x) * m_Scale.x;
					m_Translation.y = (0f - value.y) * m_Scale.y;
				}

				EnforceScaleAndRange();
			}
		}

		public Rect shownAreaInsideMargins
		{
			get { return shownAreaInsideMarginsInternal; }
			set
			{
				shownAreaInsideMarginsInternal = value;
				EnforceScaleAndRange();
			}
		}

		private Rect shownAreaInsideMarginsInternal
		{
			get
			{
				float num = leftmargin / m_Scale.x;
				float num2 = rightmargin / m_Scale.x;
				float num3 = topmargin / m_Scale.y;
				float num4 = bottommargin / m_Scale.y;
				Rect result = shownArea;
				result.x += num;
				result.y -= num3;
				result.width -= num + num2;
				result.height += num3 + num4;
				return result;
			}
			set
			{
				float num = ((value.width < m_MinWidth) ? m_MinWidth : value.width);
				float num2 = ((value.height < 0.05f) ? 0.05f : value.height);
				float num3 = drawRect.width - leftmargin - rightmargin;
				if (num3 < m_MinWidth)
				{
					num3 = m_MinWidth;
				}

				float num4 = drawRect.height - topmargin - bottommargin;
				if (num4 < 0.05f)
				{
					num4 = 0.05f;
				}

				if (m_UpDirection == YDirection.Positive)
				{
					m_Scale.x = num3 / num;
					m_Scale.y = (0f - num4) / num2;
					m_Translation.x = (0f - value.x) * m_Scale.x + leftmargin;
					m_Translation.y = drawRect.height - value.y * m_Scale.y - topmargin;
				}
				else
				{
					m_Scale.x = num3 / num;
					m_Scale.y = num4 / num2;
					m_Translation.x = (0f - value.x) * m_Scale.x + leftmargin;
					m_Translation.y = (0f - value.y) * m_Scale.y + topmargin;
				}
			}
		}

		public virtual Bounds drawingBounds => new Bounds(new Vector3((hBaseRangeMin + hBaseRangeMax) * 0.5f, (vBaseRangeMin + vBaseRangeMax) * 0.5f, 0f), new Vector3(hBaseRangeMax - hBaseRangeMin, vBaseRangeMax - vBaseRangeMin, 1f));

		public Matrix4x4 drawingToViewMatrix => Matrix4x4.TRS(m_Translation, Quaternion.identity, new Vector3(m_Scale.x, m_Scale.y, 1f));

		public Vector2 mousePositionInDrawing => ViewToDrawingTransformPoint(Event.current.mousePosition);

		private void SetAllowExceed(ref float rangeEnd, ref bool allowExceed, float value)
		{
			if (value == float.NegativeInfinity || value == float.PositiveInfinity)
			{
				rangeEnd = ((value != float.NegativeInfinity) ? 1 : 0);
				allowExceed = true;
			}
			else
			{
				rangeEnd = value;
				allowExceed = false;
			}
		}

		internal void SetDrawRectHack(Rect r)
		{
			m_DrawArea = r;
		}

		public void SetShownHRangeInsideMargins(float min, float max)
		{
			float num = drawRect.width - leftmargin - rightmargin;
			if (num < m_MinWidth)
			{
				num = m_MinWidth;
			}

			float num2 = max - min;
			if (num2 < m_MinWidth)
			{
				num2 = m_MinWidth;
			}

			m_Scale.x = num / num2;
			m_Translation.x = (0f - min) * m_Scale.x + leftmargin;
			EnforceScaleAndRange();
		}

		public void SetShownHRange(float min, float max)
		{
			float num = max - min;
			if (num < m_MinWidth)
			{
				num = m_MinWidth;
			}

			m_Scale.x = drawRect.width / num;
			m_Translation.x = (0f - min) * m_Scale.x;
			EnforceScaleAndRange();
		}

		public void SetShownVRangeInsideMargins(float min, float max)
		{
			float num = drawRect.height - topmargin - bottommargin;
			if (num < 0.05f)
			{
				num = 0.05f;
			}

			float num2 = max - min;
			if (num2 < 0.05f)
			{
				num2 = 0.05f;
			}

			if (m_UpDirection == YDirection.Positive)
			{
				m_Scale.y = (0f - num) / num2;
				m_Translation.y = drawRect.height - min * m_Scale.y - topmargin;
			}
			else
			{
				m_Scale.y = num / num2;
				m_Translation.y = (0f - min) * m_Scale.y - bottommargin;
			}

			EnforceScaleAndRange();
		}

		public void SetShownVRange(float min, float max)
		{
			float num = max - min;
			if (num < 0.05f)
			{
				num = 0.05f;
			}

			if (m_UpDirection == YDirection.Positive)
			{
				m_Scale.y = (0f - drawRect.height) / num;
				m_Translation.y = drawRect.height - min * m_Scale.y;
			}
			else
			{
				m_Scale.y = drawRect.height / num;
				m_Translation.y = (0f - min) * m_Scale.y;
			}

			EnforceScaleAndRange();
		}

		private float GetWidthInsideMargins(float widthWithMargins, bool substractSliderWidth = false)
		{
			float num = ((widthWithMargins < m_MinWidth) ? m_MinWidth : widthWithMargins);
			float a = num - leftmargin - rightmargin - ((!substractSliderWidth) ? 0f : (m_VSlider ? styles.visualSliderWidth : 0f));
			return Mathf.Max(a, m_MinWidth);
		}

		private float GetHeightInsideMargins(float heightWithMargins, bool substractSliderHeight = false)
		{
			float num = ((heightWithMargins < 0.05f) ? 0.05f : heightWithMargins);
			float a = num - topmargin - bottommargin - ((!substractSliderHeight) ? 0f : (m_HSlider ? styles.visualSliderWidth : 0f));
			return Mathf.Max(a, 0.05f);
		}

		public Vector2 DrawingToViewTransformPoint(Vector2 lhs)
		{
			return new Vector2(lhs.x * m_Scale.x + m_Translation.x, lhs.y * m_Scale.y + m_Translation.y);
		}

		public Vector3 DrawingToViewTransformPoint(Vector3 lhs)
		{
			return new Vector3(lhs.x * m_Scale.x + m_Translation.x, lhs.y * m_Scale.y + m_Translation.y, 0f);
		}

		public Vector2 ViewToDrawingTransformPoint(Vector2 lhs)
		{
			return new Vector2((lhs.x - m_Translation.x) / m_Scale.x, (lhs.y - m_Translation.y) / m_Scale.y);
		}

		public Vector3 ViewToDrawingTransformPoint(Vector3 lhs)
		{
			return new Vector3((lhs.x - m_Translation.x) / m_Scale.x, (lhs.y - m_Translation.y) / m_Scale.y, 0f);
		}

		public Vector2 DrawingToViewTransformVector(Vector2 lhs)
		{
			return new Vector2(lhs.x * m_Scale.x, lhs.y * m_Scale.y);
		}

		public Vector3 DrawingToViewTransformVector(Vector3 lhs)
		{
			return new Vector3(lhs.x * m_Scale.x, lhs.y * m_Scale.y, 0f);
		}

		public Vector2 ViewToDrawingTransformVector(Vector2 lhs)
		{
			return new Vector2(lhs.x / m_Scale.x, lhs.y / m_Scale.y);
		}

		public Vector3 ViewToDrawingTransformVector(Vector3 lhs)
		{
			return new Vector3(lhs.x / m_Scale.x, lhs.y / m_Scale.y, 0f);
		}

		public Vector2 NormalizeInViewSpace(Vector2 vec)
		{
			vec = Vector2.Scale(vec, m_Scale);
			vec /= vec.magnitude;
			return Vector2.Scale(vec, new Vector2(1f / m_Scale.x, 1f / m_Scale.y));
		}

		private bool IsZoomEvent()
		{
			return Event.current.button == 1 && Event.current.alt;
		}

		private bool IsPanEvent()
		{
			return (Event.current.button == 0 && Event.current.alt) || (Event.current.button == 2 && !Event.current.command);
		}

		public ZoomableArea()
		{
			m_MinimalGUI = false;
		}

		public ZoomableArea(bool minimalGUI)
		{
			m_MinimalGUI = minimalGUI;
		}

		public ZoomableArea(bool minimalGUI, bool enableSliderZoom)
			: this(minimalGUI, enableSliderZoom, enableSliderZoom)
		{
		}

		public ZoomableArea(bool minimalGUI, bool enableSliderZoomHorizontal, bool enableSliderZoomVertical)
		{
			m_MinimalGUI = minimalGUI;
			m_EnableSliderZoomHorizontal = enableSliderZoomHorizontal;
			m_EnableSliderZoomVertical = enableSliderZoomVertical;
		}

		public void BeginViewGUI()
		{
			if (styles.horizontalScrollbar == null)
			{
				styles.InitGUIStyles(m_MinimalGUI, allowSliderZoomHorizontal, allowSliderZoomVertical);
			}

			if (enableMouseInput)
			{
				HandleZoomAndPanEvents(m_DrawArea);
			}

			horizontalScrollbarID = GUIUtility.GetControlID(s_MinMaxSliderHash, FocusType.Passive);
			verticalScrollbarID = GUIUtility.GetControlID(s_MinMaxSliderHash, FocusType.Passive);
			if (!m_MinimalGUI || Event.current.type != EventType.Repaint)
			{
				SliderGUI();
			}
		}

		public void HandleZoomAndPanEvents(Rect area)
		{
			GUILayout.BeginArea(area);
			area.x = 0f;
			area.y = 0f;
			int num = (areaControlID = GUIUtility.GetControlID(zoomableAreaHash, FocusType.Passive, area));
			switch (Event.current.GetTypeForControl(num))
			{
				case EventType.MouseDown:
					if (area.Contains(Event.current.mousePosition))
					{
						GUIUtility.keyboardControl = num;
						if (IsZoomEvent() || IsPanEvent())
						{
							GUIUtility.hotControl = num;
							m_MouseDownPosition = mousePositionInDrawing;
							Event.current.Use();
						}
					}

					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == num)
					{
						GUIUtility.hotControl = 0;
						m_MouseDownPosition = new Vector2(-1000000f, -1000000f);
					}

					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == num)
					{
						if (IsZoomEvent())
						{
							HandleZoomEvent(m_MouseDownPosition, scrollwhell: false);
							Event.current.Use();
						}
						else if (IsPanEvent())
						{
							Pan();
							Event.current.Use();
						}
					}

					break;
				case EventType.ScrollWheel:
					if (!area.Contains(Event.current.mousePosition))
					{
						HandleScrolling(area);
					}
					else if (!m_IgnoreScrollWheelUntilClicked || GUIUtility.keyboardControl == num)
					{
						HandleZoomEvent(mousePositionInDrawing, scrollwhell: true);
						Event.current.Use();
					}

					break;
			}

			GUILayout.EndArea();
		}

		private void HandleScrolling(Rect area)
		{
			if (!m_MinimalGUI)
			{
				if (m_VSlider && new Rect(area.x + area.width, area.y + GUI.skin.verticalScrollbarUpButton.fixedHeight, vSliderWidth, area.height - (GUI.skin.verticalScrollbarDownButton.fixedHeight + hSliderHeight)).Contains(Event.current.mousePosition))
				{
					SetTransform(new Vector2(m_Translation.x, m_Translation.y - Event.current.delta.y * 10f), m_Scale);
					Event.current.Use();
				}
				else if (m_HSlider && new Rect(area.x + GUI.skin.horizontalScrollbarLeftButton.fixedWidth, area.y + area.height, area.width - (GUI.skin.horizontalScrollbarRightButton.fixedWidth + vSliderWidth), hSliderHeight).Contains(Event.current.mousePosition))
				{
					SetTransform(new Vector2(m_Translation.x + Event.current.delta.y * 10f, m_Translation.y), m_Scale);
					Event.current.Use();
				}
			}
		}

		public void EndViewGUI()
		{
			if (m_MinimalGUI && Event.current.type == EventType.Repaint)
			{
				SliderGUI();
			}
		}

		private void SliderGUI()
		{
			if (!m_HSlider && !m_VSlider)
			{
				return;
			}

			using (new EditorGUI.DisabledScope(!enableMouseInput))
			{
				Bounds bounds = drawingBounds;
				Rect rect = shownAreaInsideMargins;
				float num = styles.sliderWidth - styles.visualSliderWidth;
				float num2 = ((vSlider && hSlider) ? num : 0f);
				Vector2 vector = m_Scale;
				if (m_HSlider)
				{
					Rect position = new Rect(drawRect.x + 1f, drawRect.yMax - num, drawRect.width - num2, styles.sliderWidth);
					float size = rect.width;
					float value = rect.xMin;
					if (allowSliderZoomHorizontal)
					{
						CustomEditorGUIExt.MinMaxScroller(position, horizontalScrollbarID, ref value, ref size, bounds.min.x, bounds.max.x, float.NegativeInfinity, float.PositiveInfinity, styles.horizontalScrollbar, styles.horizontalMinMaxScrollbarThumb, styles.horizontalScrollbarLeftButton, styles.horizontalScrollbarRightButton, horiz: true);
					}
					else
					{
						value = CustomGUI.Scroller(position, value, size, bounds.min.x, bounds.max.x, styles.horizontalScrollbar, styles.horizontalMinMaxScrollbarThumb, styles.horizontalScrollbarLeftButton, styles.horizontalScrollbarRightButton, horiz: true);
					}

					float num3 = value;
					float num4 = value + size;
					float widthInsideMargins = GetWidthInsideMargins(this.rect.width, substractSliderWidth: true);
					if (num3 > rect.xMin)
					{
						num3 = Mathf.Min(num3, num4 - widthInsideMargins / m_HScaleMax);
					}

					if (num4 < rect.xMax)
					{
						num4 = Mathf.Max(num4, num3 + widthInsideMargins / m_HScaleMax);
					}

					SetShownHRangeInsideMargins(num3, num4);
				}

				if (m_VSlider)
				{
					if (m_UpDirection == YDirection.Positive)
					{
						Rect position2 = new Rect(drawRect.xMax - num, drawRect.y, styles.sliderWidth, drawRect.height - num2);
						float size2 = rect.height;
						float value2 = 0f - rect.yMax;
						if (allowSliderZoomVertical)
						{
							CustomEditorGUIExt.MinMaxScroller(position2, verticalScrollbarID, ref value2, ref size2, 0f - bounds.max.y, 0f - bounds.min.y, float.NegativeInfinity, float.PositiveInfinity, styles.verticalScrollbar, styles.verticalMinMaxScrollbarThumb, styles.verticalScrollbarUpButton, styles.verticalScrollbarDownButton, horiz: false);
						}
						else
						{
							value2 = CustomGUI.Scroller(position2, value2, size2, 0f - bounds.max.y, 0f - bounds.min.y, styles.verticalScrollbar, styles.verticalMinMaxScrollbarThumb, styles.verticalScrollbarUpButton, styles.verticalScrollbarDownButton, horiz: false);
						}

						float num3 = 0f - (value2 + size2);
						float num4 = 0f - value2;
						float heightInsideMargins = GetHeightInsideMargins(this.rect.height, substractSliderHeight: true);
						if (num3 > rect.yMin)
						{
							num3 = Mathf.Min(num3, num4 - heightInsideMargins / m_VScaleMax);
						}

						if (num4 < rect.yMax)
						{
							num4 = Mathf.Max(num4, num3 + heightInsideMargins / m_VScaleMax);
						}

						SetShownVRangeInsideMargins(num3, num4);
					}
					else
					{
						Rect position3 = new Rect(drawRect.xMax - num, drawRect.y, styles.sliderWidth, drawRect.height - num2);
						float size3 = rect.height;
						float value3 = rect.yMin;
						if (allowSliderZoomVertical)
						{
							CustomEditorGUIExt.MinMaxScroller(position3, verticalScrollbarID, ref value3, ref size3, bounds.min.y, bounds.max.y, float.NegativeInfinity, float.PositiveInfinity, styles.verticalScrollbar, styles.verticalMinMaxScrollbarThumb, styles.verticalScrollbarUpButton, styles.verticalScrollbarDownButton, horiz: false);
						}
						else
						{
							value3 = CustomGUI.Scroller(position3, value3, size3, bounds.min.y, bounds.max.y, styles.verticalScrollbar, styles.verticalMinMaxScrollbarThumb, styles.verticalScrollbarUpButton, styles.verticalScrollbarDownButton, horiz: false);
						}

						float num3 = value3;
						float num4 = value3 + size3;
						float heightInsideMargins2 = GetHeightInsideMargins(this.rect.height, substractSliderHeight: true);
						if (num3 > rect.yMin)
						{
							num3 = Mathf.Min(num3, num4 - heightInsideMargins2 / m_VScaleMax);
						}

						if (num4 < rect.yMax)
						{
							num4 = Mathf.Max(num4, num3 + heightInsideMargins2 / m_VScaleMax);
						}

						SetShownVRangeInsideMargins(num3, num4);
					}
				}

				if (uniformScale)
				{
					float num5 = drawRect.width / drawRect.height;
					vector -= m_Scale;
					Vector2 vector2 = new Vector2((0f - vector.y) * num5, (0f - vector.x) / num5);
					m_Scale -= vector2;
					m_Translation.x -= vector.y / 2f;
					m_Translation.y -= vector.x / 2f;
					EnforceScaleAndRange();
				}
			}
		}

		private void Pan()
		{
			if (!m_HRangeLocked)
			{
				m_Translation.x += Event.current.delta.x;
			}

			if (!m_VRangeLocked)
			{
				m_Translation.y += Event.current.delta.y;
			}

			EnforceScaleAndRange();
		}

		private void HandleZoomEvent(Vector2 zoomAround, bool scrollwhell)
		{
			float num = Event.current.delta.x + Event.current.delta.y;
			if (scrollwhell)
			{
				num = 0f - num;
			}

			float num2 = Mathf.Max(0.01f, 1f + num * 0.01f);
			float width = shownAreaInsideMargins.width;
			if (!(width / num2 <= m_MinWidth))
			{
				SetScaleFocused(zoomAround, num2 * m_Scale, Event.current.shift, EditorGUI.actionKey);
			}
		}

		public void SetScaleFocused(Vector2 focalPoint, Vector2 newScale)
		{
			SetScaleFocused(focalPoint, newScale, lockHorizontal: false, lockVertical: false);
		}

		public void SetScaleFocused(Vector2 focalPoint, Vector2 newScale, bool lockHorizontal, bool lockVertical)
		{
			if (uniformScale)
			{
				lockHorizontal = (lockVertical = false);
			}
			else
			{
				if (hZoomLockedByDefault)
				{
					lockHorizontal = !lockHorizontal;
				}

				if (hZoomLockedByDefault)
				{
					lockVertical = !lockVertical;
				}
			}

			if (!m_HRangeLocked && !lockHorizontal)
			{
				m_Translation.x -= focalPoint.x * (newScale.x - m_Scale.x);
				m_Scale.x = newScale.x;
			}

			if (!m_VRangeLocked && !lockVertical)
			{
				m_Translation.y -= focalPoint.y * (newScale.y - m_Scale.y);
				m_Scale.y = newScale.y;
			}

			EnforceScaleAndRange();
		}

		public void SetTransform(Vector2 newTranslation, Vector2 newScale)
		{
			m_Scale = newScale;
			m_Translation = newTranslation;
			EnforceScaleAndRange();
		}

		public void EnforceScaleAndRange()
		{
			Rect lastShownAreaInsideMargins = m_LastShownAreaInsideMargins;
			Rect rect = shownAreaInsideMargins;
			if (rect == lastShownAreaInsideMargins)
			{
				return;
			}

			float num = 0.01f;
			if (!Mathf.Approximately(rect.width, lastShownAreaInsideMargins.width))
			{
				float width = rect.width;
				if (rect.width < lastShownAreaInsideMargins.width)
				{
					width = GetWidthInsideMargins(drawRect.width / m_HScaleMax);
				}
				else
				{
					width = GetWidthInsideMargins(drawRect.width / m_HScaleMin);
					if (hRangeMax != float.PositiveInfinity && hRangeMin != float.NegativeInfinity)
					{
						float num2 = hRangeMax - hRangeMin;
						if (num2 < m_MinWidth)
						{
							num2 = m_MinWidth;
						}

						width = Mathf.Min(width, num2);
					}
				}

				float t = Mathf.InverseLerp(lastShownAreaInsideMargins.width, rect.width, width);
				float num3 = Mathf.Lerp(lastShownAreaInsideMargins.width, rect.width, t);
				float num4 = Mathf.Abs(num3 - rect.width);
				rect = new Rect((num4 > num) ? Mathf.Lerp(lastShownAreaInsideMargins.x, rect.x, t) : rect.x, rect.y, num3, rect.height);
			}

			if (!Mathf.Approximately(rect.height, lastShownAreaInsideMargins.height))
			{
				float height = rect.height;
				if (rect.height < lastShownAreaInsideMargins.height)
				{
					height = GetHeightInsideMargins(drawRect.height / m_VScaleMax);
				}
				else
				{
					height = GetHeightInsideMargins(drawRect.height / m_VScaleMin);
					if (vRangeMax != float.PositiveInfinity && vRangeMin != float.NegativeInfinity)
					{
						float num5 = vRangeMax - vRangeMin;
						if (num5 < 0.05f)
						{
							num5 = 0.05f;
						}

						height = Mathf.Min(height, num5);
					}
				}

				float t2 = Mathf.InverseLerp(lastShownAreaInsideMargins.height, rect.height, height);
				float num6 = Mathf.Lerp(lastShownAreaInsideMargins.height, rect.height, t2);
				float num7 = Mathf.Abs(num6 - rect.height);
				rect = new Rect(rect.x, (num7 > num) ? Mathf.Lerp(lastShownAreaInsideMargins.y, rect.y, t2) : rect.y, rect.width, num6);
			}

			if (rect.xMin < hRangeMin)
			{
				rect.x = hRangeMin;
			}

			if (rect.xMax > hRangeMax)
			{
				rect.x = hRangeMax - rect.width;
			}

			if (rect.yMin < vRangeMin)
			{
				rect.y = vRangeMin;
			}

			if (rect.yMax > vRangeMax)
			{
				rect.y = vRangeMax - rect.height;
			}

			shownAreaInsideMarginsInternal = rect;
			m_LastShownAreaInsideMargins = shownAreaInsideMargins;
		}

		public float PixelToTime(float pixelX, Rect rect)
		{
			Rect rect2 = shownArea;
			return (pixelX - rect.x) * rect2.width / rect.width + rect2.x;
		}

		public float TimeToPixel(float time, Rect rect)
		{
			Rect rect2 = shownArea;
			return (time - rect2.x) / rect2.width * rect.width + rect.x;
		}

		public float PixelDeltaToTime(Rect rect)
		{
			return shownArea.width / rect.width;
		}

		public void UpdateZoomScale(float fMaxScaleValue, float fMinScaleValue)
		{
			if (m_Scale.y > fMaxScaleValue || m_Scale.y < fMinScaleValue)
			{
				m_Scale.y = ((m_Scale.y > fMaxScaleValue) ? fMaxScaleValue : fMinScaleValue);
			}

			if (m_Scale.x > fMaxScaleValue || m_Scale.x < fMinScaleValue)
			{
				m_Scale.x = ((m_Scale.x > fMaxScaleValue) ? fMaxScaleValue : fMinScaleValue);
			}
		}
	}
	/// <summary>
	/// Custom Copy of UnityEditor.TimeArea
	/// </summary>
	[Tooltip("Custom Copy of UnityEditor.TimeArea")]
	[System.Serializable]
	internal class TimeArea : ZoomableArea //UnityEditor.TimeArea
	{
		private static void ApplyWireMaterial(int pass)
		{
			Material material = CustomEditorGUI.LineMaterial;
			material.SetPass(pass);
		}

		public enum TimeFormat
		{
			None,
			TimeFrame,
			Frame
		}

		private class Styles2
		{
			public GUIStyle timelineTick = "AnimationTimelineTick";

			public GUIStyle playhead = "AnimationPlayHead";
		}

		public enum TimeRulerDragMode
		{
			None,
			Start,
			End,
			Dragging,
			Cancel
		}

		[SerializeField] private TickHandler m_HTicks;

		[SerializeField] private TickHandler m_VTicks;

		private List<float> m_TickCache = new List<float>(1000);

		internal const int kTickRulerDistMin = 3;

		internal const int kTickRulerDistFull = 80;

		internal const int kTickRulerDistLabel = 40;

		internal const float kTickRulerHeightMax = 0.7f;

		internal const float kTickRulerFatThreshold = 0.5f;

		private static Styles2 timeAreaStyles;

		private static float s_OriginalTime;

		private static float s_PickOffset;

		public TickHandler hTicks
		{
			get => m_HTicks;
			set => m_HTicks = value;
		}

		public TickHandler vTicks
		{
			get => m_VTicks;
			set => m_VTicks = value;
		}

		private static void InitStyles()
		{
			if (timeAreaStyles == null)
			{
				timeAreaStyles = new Styles2();
			}
		}

		public TimeArea(bool minimalGUI)
			: this(minimalGUI, enableSliderZoomHorizontal: true, enableSliderZoomVertical: true)
		{
		}

		public TimeArea(bool minimalGUI, bool enableSliderZoom)
			: this(minimalGUI, enableSliderZoom, enableSliderZoom)
		{
		}

		public TimeArea(bool minimalGUI, bool enableSliderZoomHorizontal, bool enableSliderZoomVertical)
			: base(minimalGUI, enableSliderZoomHorizontal, enableSliderZoomVertical)
		{
			float[] tickModulos = new float[29]
			{
				1E-07f, 5E-07f, 1E-06f, 5E-06f, 1E-05f, 5E-05f, 0.0001f, 0.0005f, 0.001f, 0.005f,
				0.01f, 0.05f, 0.1f, 0.5f, 1f, 5f, 10f, 50f, 100f, 500f,
				1000f, 5000f, 10000f, 50000f, 100000f, 500000f, 1000000f, 5000000f, 1E+07f
			};
			hTicks = new TickHandler();
			hTicks.SetTickModulos(tickModulos);
			vTicks = new TickHandler();
			vTicks.SetTickModulos(tickModulos);
		}

		public void SetTickMarkerRanges()
		{
			hTicks.SetRanges(base.shownArea.xMin, base.shownArea.xMax, base.drawRect.xMin, base.drawRect.xMax);
			vTicks.SetRanges(base.shownArea.yMin, base.shownArea.yMax, base.drawRect.yMin, base.drawRect.yMax);
		}

		public void DrawMajorTicks(Rect position, float frameRate)
		{
			GUI.BeginGroup(position);
			if (Event.current.type != EventType.Repaint)
			{
				GUI.EndGroup();
				return;
			}

			InitStyles();
			ApplyWireMaterial(0);

			SetTickMarkerRanges();
			hTicks.SetTickStrengths(3f, 80f, sqrt: true);
			Color textColor = timeAreaStyles.timelineTick.normal.textColor;
			textColor.a = 0.1f;
			if (Application.platform == RuntimePlatform.WindowsEditor)
				GL.Begin(GL.QUADS);
			else
				GL.Begin(GL.LINES);

			Rect theShownArea = base.shownArea;
			for (int i = 0; i < hTicks.tickLevels; ++i)
			{
				float num = hTicks.GetStrengthOfLevel(i) * 0.9f;
				if (!(num > 0.5f))
				{
					continue;
				}

				m_TickCache.Clear();
				hTicks.GetTicksAtLevel(i, excludeTicksFromHigherlevels: true, m_TickCache);
				for (int j = 0; j < m_TickCache.Count; ++j)
				{
					if (!(m_TickCache[j] < 0f))
					{
						int num2 = Mathf.RoundToInt(m_TickCache[j] * frameRate);
						float x = FrameToPixel(num2, frameRate, position, theShownArea);

						// FIX : ui 버그 때문로 인해 임의로 추가한 코드.
						if (position.x > x) continue;

						DrawVerticalLineFast(x, 0f, position.height, textColor);
					}
				}
			}

			GL.End();
			GUI.EndGroup();
		}

		public void TimeRuler(Rect position, float frameRate)
		{
			TimeRuler(position, frameRate, labels: true, useEntireHeight: false, 1f, TimeFormat.TimeFrame);
		}

		public void TimeRuler(Rect position, float frameRate, bool labels, bool useEntireHeight, float alpha, TimeFormat timeFormat)
		{
			Color color = GUI.color;
			GUI.BeginGroup(position);
			InitStyles();
			ApplyWireMaterial(0);

			//HandleUtility.ApplyWireMaterial();
			Color backgroundColor = GUI.backgroundColor;
			SetTickMarkerRanges();
			hTicks.SetTickStrengths(3f, 80f, sqrt: true);
			Color textColor = timeAreaStyles.timelineTick.normal.textColor;
			textColor.a = 0.75f * alpha;
			if (Event.current.type == EventType.Repaint)
			{
				if (Application.platform == RuntimePlatform.WindowsEditor)
					GL.Begin(GL.QUADS);
				else
					GL.Begin(GL.LINES);

				Rect theShownArea = base.shownArea;
				for (int i = 0; i < hTicks.tickLevels; ++i)
				{
					float num = hTicks.GetStrengthOfLevel(i) * 0.9f;
					m_TickCache.Clear();
					hTicks.GetTicksAtLevel(i, excludeTicksFromHigherlevels: true, m_TickCache);
					for (int j = 0; j < m_TickCache.Count; ++j)
					{
						if (!(m_TickCache[j] < base.hRangeMin) && !(m_TickCache[j] > base.hRangeMax))
						{
							int num2 = Mathf.RoundToInt(m_TickCache[j] * frameRate);
							float num3 = (useEntireHeight ? position.height : (position.height * Mathf.Min(1f, num) * 0.7f));
							float x = FrameToPixel(num2, frameRate, position, theShownArea);

							// FIX : ui 버그 때문로 인해 임의로 추가한 코드.
							if (position.x > x) continue;

							DrawVerticalLineFast(x, position.height - num3 + 0.5f, position.height - 0.5f,
								new Color(1f, 1f, 1f, num / 0.5f) * textColor);
						}
					}
				}

				GL.End();
			}

			if (labels)
			{
				int levelWithMinSeparation = hTicks.GetLevelWithMinSeparation(40f);
				m_TickCache.Clear();
				hTicks.GetTicksAtLevel(levelWithMinSeparation, excludeTicksFromHigherlevels: false, m_TickCache);
				for (int k = 0; k < m_TickCache.Count; ++k)
				{
					if (!(m_TickCache[k] < base.hRangeMin) && !(m_TickCache[k] > base.hRangeMax))
					{
						int num4 = Mathf.RoundToInt(m_TickCache[k] * frameRate);
						float num5 = Mathf.Floor(FrameToPixel(num4, frameRate, position));
						string text = FormatTickTime(m_TickCache[k], frameRate, timeFormat);
						GUI.Label(new Rect(num5 + 3f, -1f, 40f, 20f), text, timeAreaStyles.timelineTick);
					}
				}
			}

			GUI.EndGroup();
			GUI.backgroundColor = backgroundColor;
			GUI.color = color;
		}

		public static void DrawPlayhead(float x, float yMin, float yMax, float thickness, float alpha)
		{
			if (Event.current.type == EventType.Repaint)
			{
				InitStyles();
				float num = thickness * 0.5f;
				Color color = timeAreaStyles.playhead.normal.textColor;
				color.a *= alpha;
				if (thickness > 1f)
				{
					Rect rect = Rect.MinMaxRect(x - num, yMin, x + num, yMax);
					EditorGUI.DrawRect(rect, color);
				}
				else
				{
					DrawVerticalLine(x, yMin, yMax, color);
				}
			}
		}

		public static void DrawVerticalLine(float x, float minY, float maxY, Color color)
		{
			if (Event.current.type == EventType.Repaint)
			{
				Color color2 = Handles.color;
				ApplyWireMaterial(0);
				//HandleUtility.ApplyWireMaterial();
				if (Application.platform == RuntimePlatform.WindowsEditor)
				{
					GL.Begin(7);
				}
				else
				{
					GL.Begin(1);
				}

				DrawVerticalLineFast(x, minY, maxY, color);
				GL.End();
				Handles.color = color2;
			}
		}

		public static void DrawVerticalLineFast(float x, float minY, float maxY, Color color)
		{
			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				GL.Color(color);
				GL.Vertex(new Vector3(x - 0.5f, minY, 0f));
				GL.Vertex(new Vector3(x + 0.5f, minY, 0f));
				GL.Vertex(new Vector3(x + 0.5f, maxY, 0f));
				GL.Vertex(new Vector3(x - 0.5f, maxY, 0f));
			}
			else
			{
				GL.Color(color);
				GL.Vertex(new Vector3(x, minY, 0f));
				GL.Vertex(new Vector3(x, maxY, 0f));
			}
		}

		public TimeRulerDragMode BrowseRuler(Rect position, ref float time, float frameRate, bool pickAnywhere, GUIStyle thumbStyle)
		{
			int controlID = GUIUtility.GetControlID(3126789, FocusType.Passive);
			return BrowseRuler(position, controlID, ref time, frameRate, pickAnywhere, thumbStyle);
		}

		public TimeRulerDragMode BrowseRuler(Rect position, int id, ref float time, float frameRate, bool pickAnywhere, GUIStyle thumbStyle)
		{
			Event current = Event.current;
			Rect position2 = position;
			if (time != -1f)
			{
				position2.x = Mathf.Round(TimeToPixel(time, position)) - (float)thumbStyle.overflow.left;
				position2.width = thumbStyle.fixedWidth + (float)thumbStyle.overflow.horizontal;
			}

			switch (current.GetTypeForControl(id))
			{
				case EventType.Repaint:
					if (time != -1f)
					{
						bool flag = position.Contains(current.mousePosition);
						position2.x += thumbStyle.overflow.left;
						thumbStyle.Draw(position2, id == GUIUtility.hotControl, flag || id == GUIUtility.hotControl, on: false, hasKeyboardFocus: false);
					}

					break;
				case EventType.MouseDown:
					if (position2.Contains(current.mousePosition))
					{
						GUIUtility.hotControl = id;
						s_PickOffset = current.mousePosition.x - TimeToPixel(time, position);
						current.Use();
						return TimeRulerDragMode.Start;
					}

					if (pickAnywhere && position.Contains(current.mousePosition))
					{
						GUIUtility.hotControl = id;
						float num2 = SnapTimeToWholeFPS(PixelToTime(current.mousePosition.x, position), frameRate);
						s_OriginalTime = time;
						if (num2 != time)
						{
							GUI.changed = true;
						}

						time = num2;
						s_PickOffset = 0f;
						current.Use();
						return TimeRulerDragMode.Start;
					}

					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == id)
					{
						float num = SnapTimeToWholeFPS(PixelToTime(current.mousePosition.x - s_PickOffset, position), frameRate);
						if (num != time)
						{
							GUI.changed = true;
						}

						time = num;
						current.Use();
						return TimeRulerDragMode.Dragging;
					}

					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == id)
					{
						GUIUtility.hotControl = 0;
						current.Use();
						return TimeRulerDragMode.End;
					}

					break;
				case EventType.KeyDown:
					if (GUIUtility.hotControl == id && current.keyCode == KeyCode.Escape)
					{
						if (time != s_OriginalTime)
						{
							GUI.changed = true;
						}

						time = s_OriginalTime;
						GUIUtility.hotControl = 0;
						current.Use();
						return TimeRulerDragMode.Cancel;
					}

					break;
			}

			return TimeRulerDragMode.None;
		}

		private float FrameToPixel(float i, float frameRate, Rect rect, Rect theShownArea)
		{
			return (i - theShownArea.xMin * frameRate) * rect.width / (theShownArea.width * frameRate);
		}

		public float FrameToPixel(float i, float frameRate, Rect rect)
		{
			return FrameToPixel(i, frameRate, rect, base.shownArea);
		}

		public float TimeField(Rect rect, int id, float time, float frameRate, TimeFormat timeFormat)
		{
			switch (timeFormat)
			{
				case TimeFormat.None:
				{
					float time2 = CustomEditorGUI.DoFloatField(rect, new Rect(0f, 0f, 0f, 0f), id, time, CustomEditorGUI.s_FloatFieldFormatString, EditorStyles.numberField, draggable: false);
					return SnapTimeToWholeFPS(time2, frameRate);
				}
				case TimeFormat.Frame:
				{
					int value = Mathf.RoundToInt(time * frameRate);
					int num2 = CustomEditorGUI.DoIntField(rect, new Rect(0f, 0f, 0f, 0f), id, value, CustomEditorGUI.s_IntFieldFormatString, EditorStyles.numberField, draggable: false, 0f);
					return (float)num2 / frameRate;
				}
				default:
				{
					string text = FormatTime(time, frameRate, TimeFormat.TimeFrame);
					string allowedletters = "0123456789.,:";
					text = CustomEditorGUI.DoTextField(id, rect, text, EditorStyles.numberField, allowedletters, out var changed, reset: false, multiline: false, passwordField: false);
					//text = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, id, rect, text, EditorStyles.numberField, allowedletters, out var changed, reset: false, multiline: false, passwordField: false);
					if (changed && GUIUtility.keyboardControl == id)
					{
						GUI.changed = true;
						text = text.Replace(',', '.');
						int num = text.IndexOf(':');
						float result3;
						if (num >= 0)
						{
							string s = text.Substring(0, num);
							string s2 = text.Substring(num + 1);
							if (int.TryParse(s, out var result) && int.TryParse(s2, out var result2))
							{
								return (float)result + (float)result2 / frameRate;
							}
						}
						else if (float.TryParse(text, out result3))
						{
							return SnapTimeToWholeFPS(result3, frameRate);
						}
					}

					return time;
				}
			}
		}

		public float ValueField(Rect rect, int id, float value)
		{
			return CustomEditorGUI.DoFloatField(rect, new Rect(0f, 0f, 0f, 0f), id, value, CustomEditorGUI.s_FloatFieldFormatString, EditorStyles.numberField, draggable: false);
		}

		public string FormatTime(float time, float frameRate, TimeFormat timeFormat)
		{
			if (timeFormat == TimeFormat.None)
			{
				return time.ToString("N" + ((frameRate == 0f) ? CustomMathUtils.GetNumberOfDecimalsForMinimumDifference(base.shownArea.width / base.drawRect.width) : CustomMathUtils.GetNumberOfDecimalsForMinimumDifference(1f / frameRate)), CultureInfo.InvariantCulture.NumberFormat);
			}

			int num = Mathf.RoundToInt(time * frameRate);
			if (timeFormat == TimeFormat.TimeFrame)
			{
				int totalWidth = ((frameRate == 0f) ? 1 : ((int)frameRate - 1).ToString().Length);
				string text = string.Empty;
				if (num < 0)
				{
					text = "-";
					num = -num;
				}

				return text + num / (int)frameRate + ":" + ((float)num % frameRate).ToString().PadLeft(totalWidth, '0');
			}

			return num.ToString();
		}

		public virtual string FormatTickTime(float time, float frameRate, TimeFormat timeFormat)
		{
			return FormatTime(time, frameRate, timeFormat);
		}

		public string FormatValue(float value)
		{
			return value.ToString("N" + CustomMathUtils.GetNumberOfDecimalsForMinimumDifference(base.shownArea.height / base.drawRect.height), CultureInfo.InvariantCulture.NumberFormat);
		}

		public float SnapTimeToWholeFPS(float time, float frameRate)
		{
			if (frameRate == 0f)
			{
				return time;
			}

			return Mathf.Round(time * frameRate) / frameRate;
		}

		public void DrawTimeOnSlider(float time, Color c, float maxTime, float leftSidePadding = 0f, float rightSidePadding = 0f)
		{
			if (base.hSlider)
			{
				if (base.styles.horizontalScrollbar == null)
				{
					base.styles.InitGUIStyles(minimalGUI: false, base.allowSliderZoomHorizontal, base.allowSliderZoomVertical);
				}

				float num = TimeToPixel(0f, base.rect);
				float num2 = TimeToPixel(maxTime, base.rect);
				float num3 = TimeToPixel(base.shownAreaInsideMargins.xMin, base.rect) + base.styles.horizontalScrollbarLeftButton.fixedWidth + leftSidePadding;
				float num4 = TimeToPixel(base.shownAreaInsideMargins.xMax, base.rect) - (base.styles.horizontalScrollbarRightButton.fixedWidth + rightSidePadding);
				float num5 = (TimeToPixel(time, base.rect) - num) * (num4 - num3) / (num2 - num) + num3;
				if (!(num5 > base.rect.xMax - (base.styles.horizontalScrollbarLeftButton.fixedWidth + leftSidePadding + 3f)))
				{
					float num6 = base.styles.sliderWidth - base.styles.visualSliderWidth;
					float num7 = ((base.vSlider && base.hSlider) ? num6 : 0f);
					Rect rect = new Rect(base.drawRect.x + 1f, base.drawRect.yMax - num6, base.drawRect.width - num7, base.styles.sliderWidth);
					Vector2 vector = new Vector2(num5, rect.yMin);
					Vector2 vector2 = new Vector2(num5, rect.yMax);
					Rect rect2 = Rect.MinMaxRect(vector.x - 0.5f, vector.y, vector2.x + 0.5f, vector2.y);
					EditorGUI.DrawRect(rect2, c);
				}
			}
		}

		public void DrawTimeOnSlider(Rect contentRect, float time, Color c, float maxTime, float leftSidePadding = 0f, float rightSidePadding = 0f)
		{
			if (base.hSlider)
			{
				if (base.styles.horizontalScrollbar == null)
				{
					base.styles.InitGUIStyles(minimalGUI: false, base.allowSliderZoomHorizontal, base.allowSliderZoomVertical);
				}

				float num = TimeToPixel(0f, base.rect);
				float num2 = TimeToPixel(maxTime, base.rect);
				float num3 = TimeToPixel(base.shownAreaInsideMargins.xMin, base.rect) + base.styles.horizontalScrollbarLeftButton.fixedWidth + leftSidePadding;
				float num4 = TimeToPixel(base.shownAreaInsideMargins.xMax, base.rect) - (base.styles.horizontalScrollbarRightButton.fixedWidth + rightSidePadding);
				float num5 = (TimeToPixel(time, base.rect) - num) * (num4 - num3) / (num2 - num) + num3;
				if (!(num5 > base.rect.xMax - (base.styles.horizontalScrollbarLeftButton.fixedWidth + leftSidePadding + 3f)))
				{
					float num6 = base.styles.sliderWidth - base.styles.visualSliderWidth;
					float num7 = ((base.vSlider && base.hSlider) ? num6 : 0f);
					Rect rect = new Rect(contentRect.x + 1f, contentRect.yMax - base.drawRect.yMax - num6, contentRect.width - num7, base.styles.sliderWidth);
					Vector2 vector = new Vector2(num5, rect.yMin);
					Vector2 vector2 = new Vector2(num5, rect.yMax);
					Rect rect2 = Rect.MinMaxRect(vector.x - 0.5f, vector.y, vector2.x + 0.5f, vector2.y);
					EditorGUI.DrawRect(rect2, c);
				}
			}
		}
	}
}
