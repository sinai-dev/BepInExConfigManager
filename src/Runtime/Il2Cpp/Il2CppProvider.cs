#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BF = System.Reflection.BindingFlags;
using System.Text;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using ConfigManager.Input;
using UnityEngine.EventSystems;
using ConfigManager.Utility;

namespace ConfigManager.Runtime.Il2Cpp
{
    public class Il2CppProvider : RuntimeProvider
    {
        public override void Initialize()
        {
            Reflection = new Il2CppReflection();
        }

        // Unity API Handlers

        internal static bool triedToGetColorBlockProps;
        internal static PropertyInfo _normalColorProp;
        internal static PropertyInfo _highlightColorProp;
        internal static PropertyInfo _pressedColorProp;
        internal static PropertyInfo _disabledColorProp;

        public override void SetColorBlock(Selectable selectable, Color? normal = null, Color? highlighted = null, Color? pressed = null, 
            Color? disabled = null)
        {
            var colors = selectable.colors;

            colors.colorMultiplier = 1;

            object boxed = (object)colors;

            if (!triedToGetColorBlockProps)
            {
                triedToGetColorBlockProps = true;

                if (ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "normalColor") is PropertyInfo norm && norm.CanWrite)
                    _normalColorProp = norm;
                if (ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "highlightedColor") is PropertyInfo high && high.CanWrite)
                    _highlightColorProp = high;
                if (ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "pressedColor") is PropertyInfo pres && pres.CanWrite)
                    _pressedColorProp = pres;
                if (ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "disabledColor") is PropertyInfo disa && disa.CanWrite)
                    _disabledColorProp = disa;
            }

            try
            {
                if (normal != null)
                {
                    if (_normalColorProp != null)
                        _normalColorProp.SetValue(boxed, (Color)normal);
                    else if (ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_NormalColor") is FieldInfo fi)
                        fi.SetValue(boxed, (Color)normal);
                }

                if (highlighted != null)
                {
                    if (_highlightColorProp != null)
                        _highlightColorProp.SetValue(boxed, (Color)highlighted);
                    else if (ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_HighlightedColor") is FieldInfo fi)
                        fi.SetValue(boxed, (Color)highlighted);
                }

                if (pressed != null)
                {
                    if (_pressedColorProp != null)
                        _pressedColorProp.SetValue(boxed, (Color)pressed);
                    else if (ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_PressedColor") is FieldInfo fi)
                        fi.SetValue(boxed, (Color)pressed);
                }

                if (disabled != null)
                {
                    if (_disabledColorProp != null)
                        _disabledColorProp.SetValue(boxed, (Color)disabled);
                    else if (ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_DisabledColor") is FieldInfo fi)
                        fi.SetValue(boxed, (Color)disabled);
                }
            }
            catch (Exception ex)
            {
                ConfigManager.Log.LogWarning(ex);
            }

            colors = (ColorBlock)boxed;

            SetColorBlock(selectable, colors);
        }

        public override void SetColorBlock(Selectable selectable, ColorBlock _colorBlock)
        {
            try
            {
                selectable = selectable.TryCast<Selectable>();

                ReflectionUtility.GetPropertyInfo(typeof(Selectable), "m_Colors")
                    .SetValue(selectable, _colorBlock, null);

                ReflectionUtility.GetMethodInfo(typeof(Selectable), "OnSetProperty", new Type[0])
                    .Invoke(selectable, new object[0]);
            }
            catch (Exception ex)
            {
                ConfigManager.Log.LogMessage(ex);
            }
        }

        public override T AddComponent<T>(GameObject obj, Type type)
        {
            return obj.AddComponent(Il2CppType.From(type)).TryCast<T>();
        }

        public override ScriptableObject CreateScriptable(Type type)
        {
            return ScriptableObject.CreateInstance(Il2CppType.From(type));
        }
    }
}

public static class Il2CppExtensions
{
    public static void AddListener(this UnityEvent action, Action listener)
    {
        action.AddListener(listener);
    }

    public static void AddListener<T>(this UnityEvent<T> action, Action<T> listener)
    {
        action.AddListener(listener);
    }

    public static void SetChildControlHeight(this HorizontalOrVerticalLayoutGroup group, bool value) => group.childControlHeight = value;
    public static void SetChildControlWidth(this HorizontalOrVerticalLayoutGroup group, bool value) => group.childControlWidth = value;
}

#endif