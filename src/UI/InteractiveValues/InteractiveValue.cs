using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigManager.UI.InteractiveValues
{
    public abstract class InteractiveValue
    {
        private static readonly HashSet<Type> s_customIValueTypes = new HashSet<Type>();
        private static readonly List<InteractiveValue> s_customIValueInstances = new List<InteractiveValue>();

        public static void RegisterIValueType<T>() where T : InteractiveValue
        {
            if (s_customIValueTypes.Contains(typeof(T)))
                return;

            s_customIValueInstances.Add((T)Activator.CreateInstance(typeof(T), new object[] { null, typeof(object) }));
            s_customIValueTypes.Add(typeof(T));
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
            else if (s_customIValueInstances.FirstOrDefault(it => it.SupportsType(type)) is InteractiveValue custom)
                return custom.GetType();
            // fallback to default handler
            else
                return typeof(InteractiveTomlObject);
        }

        public static InteractiveValue Create(object value, Type fallbackType)
        {
            var type = ReflectionUtility.GetActualType(value) ?? fallbackType;
            var iType = GetIValueForType(type);

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

        public bool m_UIConstructed;

        protected internal GameObject m_mainContentParent;
        protected internal GameObject m_subContentParent;

        protected internal GameObject m_mainContent;

        protected internal Button m_subExpandBtn;
        protected internal bool m_subContentConstructed;

        public virtual void OnValueUpdated()
        {
            if (!m_UIConstructed)
                ConstructUI(m_mainContentParent);

            RefreshUIForValue();
        }

        public virtual void RefreshUIForValue()
        {
        }

        public void RefreshSubContentState()
        {
            if (HasSubContent)
            {
                if (m_subExpandBtn.gameObject.activeSelf != SubContentWanted)
                    m_subExpandBtn.gameObject.SetActive(SubContentWanted);

                if (!SubContentWanted && m_subContentParent.activeSelf)
                    ToggleSubcontent();
            }
        }

        public virtual void ConstructSubcontent()
        {
            m_subContentConstructed = true;
        }

        public virtual void DestroySubContent()
        {
            if (this.m_subContentParent && HasSubContent)
            {
                for (int i = 0; i < this.m_subContentParent.transform.childCount; i++)
                {
                    var child = m_subContentParent.transform.GetChild(i);
                    if (child)
                        GameObject.Destroy(child.gameObject);
                }
            }

            m_subContentConstructed = false;
        }

        public void ToggleSubcontent()
        {
            if (!this.m_subContentParent.activeSelf)
            {
                this.m_subContentParent.SetActive(true);
                this.m_subContentParent.transform.SetAsLastSibling();
                m_subExpandBtn.GetComponentInChildren<Text>().text = "▼ Click to hide";
            }
            else
            {
                this.m_subContentParent.SetActive(false);
                m_subExpandBtn.GetComponentInChildren<Text>().text = "▲ Expand to edit";
            }

            OnToggleSubcontent(m_subContentParent.activeSelf);

            RefreshSubContentState();
        }

        protected internal virtual void OnToggleSubcontent(bool toggle)
        {
            if (!m_subContentConstructed)
                ConstructSubcontent();
        }

        public virtual void ConstructUI(GameObject parent)
        {
            m_UIConstructed = true;

            m_mainContent = UIFactory.CreateHorizontalGroup(parent, $"InteractiveValue_{this.GetType().Name}", false, false, true, true, 4, default, 
                new Color(1, 1, 1, 0), TextAnchor.UpperLeft);

            var mainRect = m_mainContent.GetComponent<RectTransform>();
            mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);

            UIFactory.SetLayoutElement(m_mainContent, flexibleWidth: 9000, minWidth: 175, minHeight: 25, flexibleHeight: 0);

            // subcontent expand button
            if (HasSubContent)
            {
                m_subExpandBtn = UIFactory.CreateButton(m_mainContent, "ExpandSubcontentButton", 
                    "▲ Expand to edit", ToggleSubcontent, new Color(0.3f, 0.3f, 0.3f));

                UIFactory.SetLayoutElement(m_subExpandBtn.gameObject, minHeight: 25, minWidth: 120, flexibleWidth: 0, flexibleHeight: 0);
            }
        }
    }
}
