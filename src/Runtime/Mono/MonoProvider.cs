#if MONO
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ConfigManager.Runtime.Mono
{
    public class MonoProvider : RuntimeProvider
    {
        public override void Initialize()
        {
            Reflection = new MonoReflection();
        }

        public override void SetColorBlock(Selectable selectable, Color? normal = null, Color? highlighted = null, Color? pressed = null,
            Color? disabled = null)
        {
            var colors = selectable.colors;

            if (normal != null)
                colors.normalColor = (Color)normal;

            if (highlighted != null)
                colors.highlightedColor = (Color)highlighted;

            if (pressed != null)
                colors.pressedColor = (Color)pressed;

            if (disabled != null)
                colors.disabledColor = (Color)disabled;

            SetColorBlock(selectable, colors);
        }

        public override void SetColorBlock(Selectable selectable, ColorBlock colors)
        {
            selectable.colors = colors;
        }

        public override T AddComponent<T>(GameObject obj, Type type)
        {
            return (T)obj.AddComponent(type);
        }

        public override ScriptableObject CreateScriptable(Type type)
        {
            return ScriptableObject.CreateInstance(type);
        }
    }
}

public static class MonoExtensions
{
    public static void AddListener(this UnityEvent _event, Action listener)
    {
        _event.AddListener(new UnityAction(listener));
    }

    public static void AddListener<T>(this UnityEvent<T> _event, Action<T> listener)
    {
        _event.AddListener(new UnityAction<T>(listener));
    }

    public static void Clear(this StringBuilder sb)
    {
        sb.Remove(0, sb.Length);
    }

    private static PropertyInfo pi_childControlHeight;

    public static void SetChildControlHeight(this HorizontalOrVerticalLayoutGroup group, bool value)
    {
        if (pi_childControlHeight == null)
            pi_childControlHeight = group.GetType().GetProperty("childControlHeight");
        
        pi_childControlHeight?.SetValue(group, value, null);
    }

    private static PropertyInfo pi_childControlWidth;

    public static void SetChildControlWidth(this HorizontalOrVerticalLayoutGroup group, bool value)
    {
        if (pi_childControlWidth == null)
            pi_childControlWidth = group.GetType().GetProperty("childControlWidth");

        pi_childControlWidth?.SetValue(group, value, null);
    }
}

#endif