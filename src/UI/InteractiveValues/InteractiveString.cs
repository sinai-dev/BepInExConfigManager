using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using ConfigManager.UI;
using UniverseLib.UI;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveString : InteractiveValue
    {
        public InteractiveString(object value, Type valueType) : base(value, valueType) { }

        internal InputFieldRef m_valueInput;
        internal GameObject m_hiddenObj;
        internal Text m_placeholderText;

        public override bool SupportsType(Type type) => type == typeof(string);

        public override void RefreshUIForValue()
        {
            if (!m_hiddenObj.gameObject.activeSelf)
                m_hiddenObj.gameObject.SetActive(true);

            if (!string.IsNullOrEmpty((string)Value))
            {
                var toString = (string)Value;
                if (toString.Length > 15000)
                    toString = toString.Substring(0, 15000);

                m_valueInput.Text = toString;
                m_placeholderText.text = toString;
            }
            else
            {
                string s = Value == null 
                            ? "null" 
                            : "empty";

                m_valueInput.Text = "";
                m_placeholderText.text = s;
            }
        }

        internal void SetValueFromInput()
        {
            Value = m_valueInput.Text;
            Owner.SetValueFromIValue();
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            m_hiddenObj = UIFactory.CreateLabel(m_mainContent, "HiddenLabel", "", TextAnchor.MiddleLeft).gameObject;
            m_hiddenObj.SetActive(false);
            var hiddenText = m_hiddenObj.GetComponent<Text>();
            hiddenText.color = Color.clear;
            hiddenText.fontSize = 14;
            hiddenText.raycastTarget = false;
            hiddenText.supportRichText = false;
            var hiddenFitter = m_hiddenObj.AddComponent<ContentSizeFitter>();
            hiddenFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.SetLayoutElement(m_hiddenObj, minHeight: 25, flexibleHeight: 500, minWidth: 250, flexibleWidth: 9000);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(m_hiddenObj, true, true, true, true);

            m_valueInput = UIFactory.CreateInputField(m_hiddenObj, "StringInputField", "...");
            UIFactory.SetLayoutElement(m_valueInput.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 5000, flexibleHeight: 5000);

            m_valueInput.Component.lineType = InputField.LineType.MultiLineNewline;

            m_placeholderText = m_valueInput.Component.placeholder.GetComponent<Text>();

            m_placeholderText.supportRichText = false;
            m_valueInput.Component.textComponent.supportRichText = false;

            m_valueInput.OnValueChanged += (string val) =>
            {
                hiddenText.text = val ?? "";
                LayoutRebuilder.ForceRebuildLayoutImmediate(Owner.ContentRect);
                SetValueFromInput();
            };

            RefreshUIForValue();
        }
    }
}
