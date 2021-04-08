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

namespace ConfigManager.Runtime.Il2Cpp
{
    public class Il2CppProvider : RuntimeProvider
    {
        public override void Initialize()
        {
            Reflection = new Il2CppReflection();
            TextureUtil = new Il2CppTextureUtil();
        }

        // Unity API Handlers

        internal static bool? s_doPropertiesExist;

        public override ColorBlock SetColorBlock(ColorBlock colors, Color? normal = null, Color? highlighted = null, Color? pressed = null,
            Color? disabled = null)
        {
            if (s_doPropertiesExist == null)
            {
                var prop = ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "normalColor") as PropertyInfo;
                s_doPropertiesExist = prop != null && prop.CanWrite;
            }

            colors.colorMultiplier = 1;

            object boxed = (object)colors;

            if (s_doPropertiesExist == true)
            {
                if (normal != null)
                    ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "normalColor").SetValue(boxed, (Color)normal);
                if (pressed != null)
                    ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "pressedColor").SetValue(boxed, (Color)pressed);
                if (highlighted != null)
                    ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "highlightedColor").SetValue(boxed, (Color)highlighted);
                if (disabled != null)
                    ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "disabledColor").SetValue(boxed, (Color)disabled);
            }
            else if (s_doPropertiesExist == false)
            {
                if (normal != null)
                    ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_NormalColor").SetValue(boxed, (Color)normal);
                if (pressed != null)
                    ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_PressedColor").SetValue(boxed, (Color)pressed);
                if (highlighted != null)
                    ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_HighlightedColor").SetValue(boxed, (Color)highlighted);
                if (disabled != null)
                    ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_DisabledColor").SetValue(boxed, (Color)disabled);
            }

            colors = (ColorBlock)boxed;

            return colors;
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