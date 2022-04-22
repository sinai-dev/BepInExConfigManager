using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace ConfigManager.UI.InteractiveValues
{
    public abstract class InteractiveValue
    {
        private static readonly HashSet<Type> customIValueTypes = new();
        private static readonly List<InteractiveValue> customIValueInstances = new();

        public static void RegisterIValueType<T>() where T : InteractiveValue
        {
            if (customIValueTypes.Contains(typeof(T)))
                return;

            customIValueInstances.Add((T)Activator.CreateInstance(typeof(T), new object[] { null, typeof(object) }));
            customIValueTypes.Add(typeof(T));
        }

        public static Type GetIValueForType(Type type)
        {
            // Boolean
            if (type == typeof(bool))
                return typeof(InteractiveBool);
            // Number
            else if (type.IsPrimitive || typeof(decimal).IsAssignableFrom(type))
                return typeof(InteractiveNumber);
            // String
            else if (type == typeof(string))
                return typeof(InteractiveString);
            // KeyCode
            else if (type == typeof(KeyCode) || type.FullName == "UnityEngine.InputSystem.Key")
                return typeof(InteractiveKeycode);
            // Flags and Enum
            else if (typeof(Enum).IsAssignableFrom(type))
                if (type.GetCustomAttributes(typeof(FlagsAttribute), true) is object[] fa && fa.Any())
                    return typeof(InteractiveFlags);
                else
                    return typeof(InteractiveEnum);
            // Color
            else if (type == typeof(Color) || type == typeof(Color32))
                return typeof(InteractiveColor);
            // Vector / Rect
            else if (InteractiveFloatStruct.IsTypeSupported(type))
                return typeof(InteractiveFloatStruct);
            // Custom defined handlers
            else if (customIValueInstances.FirstOrDefault(it => it.SupportsType(type)) is InteractiveValue custom)
                return custom.GetType();
            // fallback to default handler
            else
                return typeof(InteractiveTomlObject);
        }

        public static InteractiveValue Create(object value, Type fallbackType)
        {
            Type type = value.GetActualType() ?? fallbackType;
            Type iType = GetIValueForType(type);

            return (InteractiveValue)Activator.CreateInstance(iType, new object[] { value, type });
        }

        // ~~~~~~~~~ Instance ~~~~~~~~~

        public InteractiveValue(object value, Type valueType)
        {
            this.Value = value;
            this.FallbackType = valueType;
        }

        public virtual bool SupportsType(Type type) => true;

        public CachedConfigEntry Owner;

        public object Value;
        public readonly Type FallbackType;

        public virtual bool HasSubContent => false;
        public virtual bool SubContentWanted => false;

        public bool UIConstructed;

        protected internal GameObject mainContentParent;
        protected internal GameObject subContentParent;

        protected internal GameObject mainContent;

        protected internal ButtonRef subExpandBtn;
        protected internal bool subContentConstructed;

        public virtual void OnValueUpdated()
        {
            if (!UIConstructed)
                ConstructUI(mainContentParent);

            RefreshUIForValue();
        }

        public virtual void RefreshUIForValue()
        {
        }

        public void RefreshSubContentState()
        {
            if (HasSubContent)
            {
                if (subExpandBtn.Component.gameObject.activeSelf != SubContentWanted)
                    subExpandBtn.Component.gameObject.SetActive(SubContentWanted);

                if (!SubContentWanted && subContentParent.activeSelf)
                    ToggleSubcontent();
            }
        }

        public virtual void ConstructSubcontent()
        {
            subContentConstructed = true;
        }

        public virtual void DestroySubContent()
        {
            if (this.subContentParent && HasSubContent)
            {
                for (int i = 0; i < this.subContentParent.transform.childCount; i++)
                {
                    Transform child = subContentParent.transform.GetChild(i);
                    if (child)
                        GameObject.Destroy(child.gameObject);
                }
            }

            subContentConstructed = false;
        }

        public void ToggleSubcontent()
        {
            if (!this.subContentParent.activeSelf)
            {
                this.subContentParent.SetActive(true);
                this.subContentParent.transform.SetAsLastSibling();
                subExpandBtn.ButtonText.text = "▼ Click to hide";
            }
            else
            {
                this.subContentParent.SetActive(false);
                subExpandBtn.ButtonText.text = "▲ Expand to edit";
            }

            OnToggleSubcontent(subContentParent.activeSelf);

            RefreshSubContentState();
        }

        protected internal virtual void OnToggleSubcontent(bool toggle)
        {
            if (!subContentConstructed)
                ConstructSubcontent();
        }

        public virtual void ConstructUI(GameObject parent)
        {
            UIConstructed = true;

            mainContent = UIFactory.CreateHorizontalGroup(parent, $"InteractiveValue_{this.GetType().Name}", false, false, true, true, 4, default, 
                new Color(1, 1, 1, 0), TextAnchor.UpperLeft);

            RectTransform mainRect = mainContent.GetComponent<RectTransform>();
            mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);

            UIFactory.SetLayoutElement(mainContent, flexibleWidth: 9000, minWidth: 175, minHeight: 25, flexibleHeight: 0);

            // subcontent expand button
            if (HasSubContent)
            {
                subExpandBtn = UIFactory.CreateButton(mainContent, "ExpandSubcontentButton", "▲ Expand to edit", new Color(0.3f, 0.3f, 0.3f));
                subExpandBtn.OnClick += ToggleSubcontent;
                UIFactory.SetLayoutElement(subExpandBtn.Component.gameObject, minHeight: 25, minWidth: 120, flexibleWidth: 0, flexibleHeight: 0);
            }
        }
    }
}
