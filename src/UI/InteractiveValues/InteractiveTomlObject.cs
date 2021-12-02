using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveTomlObject : InteractiveValue
    {
        public InteractiveTomlObject(object value, Type valueType) : base(value, valueType) { }

        // Default handler for any type without a specific handler.
        public override bool SupportsType(Type type) => true;

        internal InputFieldRef m_valueInput;
        internal GameObject m_hiddenObj;
        internal Text m_placeholderText;

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();

            try
            {
                m_valueInput.Text = TomlTypeConverter.ConvertToString(Value, FallbackType);
                m_placeholderText.text = m_valueInput.Text;
            }
            catch
            {
                ConfigManager.Log.LogWarning($"Unable to edit entry '{Owner.RefConfig.Definition.Key}' due to an error with the Toml converter!");
            }
        }

        internal void SetValueFromInput()
        {
            try
            {
                Value = TomlTypeConverter.ConvertToValue(m_valueInput.Text, FallbackType);

                Owner.SetValueFromIValue();

                m_valueInput.Component.textComponent.color = Color.white;
            }
            catch
            {
                m_valueInput.Component.textComponent.color = Color.red;
            }
        }

        public override void RefreshUIForValue()
        {
            if (!m_hiddenObj.gameObject.activeSelf)
                m_hiddenObj.gameObject.SetActive(true);
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

            OnValueUpdated();

            m_valueInput.OnValueChanged += (string val) =>
            {
                hiddenText.text = val ?? "";
                LayoutRebuilder.ForceRebuildLayoutImmediate(Owner.ContentRect);
                SetValueFromInput();
            };
        }
    }
}
