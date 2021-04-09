using BepInEx.Configuration;
using ConfigManager.UI.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveTomlObject : InteractiveValue
    {
        public InteractiveTomlObject(object value, Type valueType) : base(value, valueType) { }

        // Default handler for any type without a specific handler.
        public override bool SupportsType(Type type) => true;

        internal InputField m_valueInput;
        internal GameObject m_hiddenObj;
        internal Text m_placeholderText;

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();

            try
            {
                m_valueInput.text = TomlTypeConverter.ConvertToString(Value, FallbackType);
                m_placeholderText.text = m_valueInput.text;
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
                Value = TomlTypeConverter.ConvertToValue(m_valueInput.text, FallbackType);

                Owner.SetValueFromIValue();

                m_valueInput.textComponent.color = Color.white;
            }
            catch
            {
                m_valueInput.textComponent.color = Color.red;
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

            var inputObj = UIFactory.CreateInputField(m_hiddenObj, "StringInputField", "...", 14, 3);
            UIFactory.SetLayoutElement(inputObj, minWidth: 120, minHeight: 25, flexibleWidth: 5000, flexibleHeight: 5000);

            m_valueInput = inputObj.GetComponent<InputField>();
            m_valueInput.lineType = InputField.LineType.MultiLineNewline;

            m_placeholderText = m_valueInput.placeholder.GetComponent<Text>();

            m_placeholderText.supportRichText = false;
            m_valueInput.textComponent.supportRichText = false;

            OnValueUpdated();

            m_valueInput.onValueChanged.AddListener((string val) =>
            {
                hiddenText.text = val ?? "";
                LayoutRebuilder.ForceRebuildLayoutImmediate(Owner.ContentRect);
                SetValueFromInput();
            });
        }
    }
}
