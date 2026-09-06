using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace ActionTool.Core
{
	public static class CustomEditorExtention
	{
		public static bool IsNullOrEmpty<T>(this List<T> list)
		{
			return list == null || list.Count == 0;
		}

		public static bool IsDirectManipulationDevice(this Event e)
		{
			return e.pointerType == PointerType.Pen || e.pointerType == PointerType.Touch;
		}

		#region Reflection

		public static bool TryGetField(this Type type, string location, string name, BindingFlags bindingAttr, out FieldInfo fieldInfo)
		{
			FieldInfo result = type.GetField(name, bindingAttr);
			fieldInfo = result;
			return AssertReflectionResult(result, "FieldInfo", name, location);
		}

		public static bool TryGetMethod(this Type type, string location, string name, BindingFlags bindingAttr, out MethodInfo methodInfo)
		{
			MethodInfo result = type.GetMethod(name, bindingAttr);
			methodInfo = result;
			return AssertReflectionResult(result, "MethodInfo", name, location);
		}

		public static bool TryGetNestedType(this Type type, string location, string name, BindingFlags bindingAttr, out Type nestedTypeInfo)
		{
			Type result = type.GetNestedType(name, bindingAttr);
			nestedTypeInfo = result;
			return AssertReflectionResult(result, "Nested Type", name, location);
		}

		//type.GetConstructor
		//type.GetEvent()
		//type.GetInterface
		//type.GetProperty()

		/// <summary>
		/// AppDomain.CurrentDomain의 모든 어셈블리 내에서 검색
		/// </summary>
		/// <param name="type"></param>
		/// <param name="location"></param>
		/// <param name="typeName"></param>
		/// <param name="ignoreCase"> 대소문자 무시?</param>
		/// <param name="foundType"></param>
		/// <returns></returns>
		public static bool TryGetType(this Type type, string location, string typeName, bool ignoreCase, out Type foundType)
		{
			Type found = null;
			string fullPath = string.IsNullOrEmpty(location) ? typeName : $"{location}.{typeName}";
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = asm.GetType(fullPath, false, ignoreCase);
				if (found != null)
					break;
			}

			foundType = found;
			return AssertReflectionResult(found, "Type", typeName, location);
		}

		private static bool AssertReflectionResult(object result, string infoType, string targetName, string targetLocation)
		{
			if (result == null)
			{
				Debug.LogError($"Fail to get {infoType} info \"{targetName}\" from {targetLocation}");
			}

			return result != null;
		}

		#endregion
	}

	#region EditorGUI

	public static class CustomEditorUtil
	{
		public static readonly float EPSILON = 0.0001f;

		static bool HitTest(Rect rect, Vector2 point, int offset)
		{
			return point.x >= rect.xMin - (float)offset && point.x < rect.xMax + (float)offset && point.y >= rect.yMin - (float)offset && point.y < rect.yMax + (float)offset;
		}

		static bool HitTest(Rect rect, Vector2 point, bool isDirectManipulationDevice)
		{
			int offset = 0;
			return HitTest(rect, point, offset);
		}

		public static bool HitTest(Rect rect, Event evt)
		{
			return HitTest(rect, evt.mousePosition, evt.IsDirectManipulationDevice());
		}

		// Context Menu
		public struct ContextMenuItem
		{
			public string menuName;
			public int priority;
			public bool isChecked;
			public bool isEnabled;
			public GenericMenu.MenuFunction callback;
		}

		public static ContextMenuItem CreateConTextMenuItem(string strName, int nPriority, GenericMenu.MenuFunction cb, bool bEnabled = true)
		{
			ContextMenuItem menuItem = new ContextMenuItem()
			{
				menuName = strName,
				priority = nPriority,
				isChecked = false,
				callback = cb,
				isEnabled = bEnabled
			};
			return menuItem;
		}

		public static void ShowContextMenu(params ContextMenuItem[] items)
		{
			GenericMenu menu = new GenericMenu();
			foreach (var item in items)
			{
				if (item.isEnabled)
					menu.AddItem(new GUIContent(item.menuName), item.isChecked, item.callback);
				else
					menu.AddDisabledItem(new GUIContent(item.menuName), item.isChecked);
			}

			menu.ShowAsContext();
		}
	}

	public static class CustomGUIStyles
	{
		public static Color colorTimelineBackground = new Color(0.2f, 0.2f, 0.2f, 1f);

		//Unity Default Resources
		public static readonly GUIContent GotoBeginingContent =
			L10n.IconContent("Animation.FirstKey", "Go to the beginning of the Animation");

		public static readonly GUIContent GotoEndContent =
			L10n.IconContent("Animation.LastKey", "Go to the end of the Animation");

		public static readonly GUIContent NextFrameContent =
			L10n.IconContent("Animation.NextKey", "Go to the next frame");

		public static readonly GUIContent PreviousFrameContent =
			L10n.IconContent("Animation.PrevKey", "Go to the previous frame");

		public static readonly GUIContent NewContent = L10n.IconContent("CreateAddNew", "Add new event.");
		public static readonly GUIContent OptionsCogIcon = L10n.IconContent("_Popup", "Options");
		public static readonly GUIContent PlayIcon = L10n.IconContent("Animation.Play", "Play");
		public static readonly GUIContent PauseIcon = L10n.IconContent("d_PauseButton On@2x", "Pause");
		public static readonly GUIContent StopIcon = L10n.IconContent("d_PreMatQuad@2x", "Stop");
		public static readonly GUIContent LoopIcon = L10n.IconContent("d_preAudioLoopOff@2x", "Repeat");
		public static readonly GUIContent AudioIcon = EditorGUIUtility.IconContent("d_Profiler.Audio@2x");
		public static readonly GUIContent EventKeyIconGray = EditorGUIUtility.IconContent("sv_icon_dot10_pix16_gizmo");

		public static readonly GUIContent EventKeyIconYellow =
			EditorGUIUtility.IconContent("sv_icon_dot12_pix16_gizmo");

		public static readonly GUIContent EventKeyIconRed = EditorGUIUtility.IconContent("sv_icon_dot14_pix16_gizmo");
		public static readonly GUIContent EventKeyIconBlue = EditorGUIUtility.IconContent("sv_icon_dot9_pix16_gizmo");

		public static readonly GUIContent EventKeyIconPurple =
			EditorGUIUtility.IconContent("sv_icon_dot15_pix16_gizmo");

		public static readonly GUIContent EventKeyIconYellowCircle =
			EditorGUIUtility.IconContent("sv_icon_dot4_pix16_gizmo");

		public static readonly GUIContent EventKeyIconRedCircle =
			EditorGUIUtility.IconContent("sv_icon_dot6_pix16_gizmo");

		public static readonly GUIContent EventMarkIcon =
			EditorGUIUtility.IconContent("AnimationWindowEvent Icon"); //SignalAsset Icon

		public static readonly GUIContent EventKeyIconHaloIcon =
			EditorGUIUtility.IconContent("Halo Icon");

		public static readonly GUIContent EventKeyIconPlusCircle =
			EditorGUIUtility.IconContent("PrefabOverlayAdded Icon");

		public static readonly GUIContent EventKeyIconMinusCircle =
			EditorGUIUtility.IconContent("PrefabOverlayRemoved Icon");

		public static readonly GUIContent EventKeyIconNoneCircle =
			EditorGUIUtility.IconContent("PrefabOverlayModified Icon");

		public static readonly GUIContent EventKeyIconEnableInput =
			EditorGUIUtility.IconContent("d_scenepicking_pickable_hover@2x");

		public static readonly GUIContent EventKeyIconDisableInput =
			EditorGUIUtility.IconContent("d_scenepicking_notpickable_hover@2x");

		public static readonly GUIContent EventKeyIconAimConstraint = EditorGUIUtility.IconContent("d_AimConstraint Icon");

		// TrackGUI
		public static Color colorContentBackground = new Color(0.5f, 0.5f, 0.5f, 1f);
		public static Color colorSelectedContentBackground = new Color(0.7f, 0.7f, 0.7f, 1f);
		public static Color defaultEventBackground = new Color(0.254902f, 0.254902f, 0.254902f, 1f);
		public static Color hoverEventBackground = new Color(0.6f, 0.6f, 0.6f, 1f);
		public static Color colorEventListBackground = new Color(0.16f, 0.16f, 0.16f, 1f);

		public static GUIStyle eventSwatchStyle = GetGUIStyle("Icon-TrackHeaderSwatch");
		public static GUIStyle eventAddButton = GetGUIStyle("sequenceTrackGroupAddButton");
		public static GUIStyle eventLockButton = GetGUIStyle("trackLockButton");
		public static GUIContent eventEnableState = EditorGUIUtility.IconContent("d_animationvisibilitytoggleon@2x");
		public static GUIContent eventDisableState = EditorGUIUtility.IconContent("d_animationvisibilitytoggleoff@2x");
		public static GUIContent eventAttackNotice = EditorGUIUtility.IconContent("d_AvatarSelector@2x");

		public static GUIStyle clipIn = GetGUIStyle("Icon-ClipIn");
		public static GUIStyle timeCursor = GetGUIStyle("Icon-TimeCursor");
		public static GUIStyle displayBackground = GetGUIStyle("sequenceClip");

		private static GUIStyle GetGUIStyle(string s)
		{
			return EditorStyles.FromUSS(s);
		}
	}

	#endregion

	internal struct GUIViewportScope : IDisposable
	{
		bool m_Open;

		public GUIViewportScope(Rect position)
		{
			m_Open = false;
			if (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout)
			{
				GUI.BeginClip(position, -position.min, Vector2.zero, false);
				m_Open = true;
			}
		}

		public void Dispose()
		{
			CloseScope();
		}

		void CloseScope()
		{
			if (m_Open)
			{
				GUI.EndClip();
				m_Open = false;
			}
		}
	}

	internal struct GUIColorOverride : IDisposable
	{
		readonly Color m_OldColor;

		public GUIColorOverride(Color newColor)
		{
			m_OldColor = GUI.color;
			GUI.color = newColor;
		}

		public void Dispose()
		{
			GUI.color = m_OldColor;
		}
	}

	internal static class DisplayDialog
	{
		internal static void ShowDisplayDialog(string message, UnityAction onClickOK, string title = "Ani Event Tool", string ok = "OK")
		{
			bool bClickedOk = EditorUtility.DisplayDialog(title, message, ok);
			if (bClickedOk)
			{
				if (onClickOK != null)
					onClickOK.Invoke();
			}
		}

		internal static void ShowDisplayDialog(string message, UnityAction onClickOK, UnityAction onClickCancel, string title = "Ani Event Tool", string ok = "OK", string cancel = "Cancel")
		{
			bool bClickedOk = EditorUtility.DisplayDialog(title, message, ok, cancel);

			if (bClickedOk)
			{
				if (onClickOK != null)
					onClickOK.Invoke();
			}
			else
			{
				if (onClickCancel != null)
					onClickCancel.Invoke();
			}
		}

		static IEnumerator DisplayProgressBarRoutine()
		{
			// 로딩 팝업 표시
			EditorUtility.DisplayProgressBar("Saving", "Saving data...", 0.5f);

			yield return new WaitForSeconds(1f);

			// 로딩 팝업 닫기
			EditorUtility.ClearProgressBar();

			// 에셋 데이터베이스 갱신
			AssetDatabase.Refresh();
		}
	}
}
