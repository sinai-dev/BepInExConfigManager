using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveTomlObject : InteractiveValue
    {
        public InteractiveTomlObject(object value, Type valueType) : base(value, valueType) { }

        // Default handler for any type without a specific handler.
        public override bool SupportsType(Type type) => true;

        internal InputFieldRef valueInput;
        internal GameObject hiddenObj;
        internal Text placeholderText;

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();

            try
            {
                valueInput.Text = TomlTypeConverter.ConvertToString(Value, FallbackType);
                placeholderText.text = valueInput.Text;
            }
            catch
            {
                ConfigManager.LogSource.LogWarning($"Unable to edit entry '{Owner.RefConfig.Definition.Key}' due to an error with the Toml converter!");
            }
        }

        internal void SetValueFromInput()
        {
            try
            {
                Value = TomlTypeConverter.ConvertToValue(valueInput.Text, FallbackType);

                Owner.SetValueFromIValue();

                valueInput.Component.textComponent.color = Color.white;
            }
            catch
            {
                valueInput.Component.textComponent.color = Color.red;
            }
        }

        public override void RefreshUIForValue()
        {
            if (!hiddenObj.gameObject.activeSelf)
                hiddenObj.gameObject.SetActive(true);
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            hiddenObj = UIFactory.CreateLabel(mainContent, "HiddenLabel", "", TextAnchor.MiddleLeft).gameObject;
            hiddenObj.SetActive(false);
            Text hiddenText = hiddenObj.GetComponent<Text>();
            hiddenText.color = Color.clear;
            hiddenText.fontSize = 14;
            hiddenText.raycastTarget = false;
            hiddenText.supportRichText = false;
            ContentSizeFitter hiddenFitter = hiddenObj.AddComponent<ContentSizeFitter>();
            hiddenFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.SetLayoutElement(hiddenObj, minHeight: 25, flexibleHeight: 500, minWidth: 250, flexibleWidth: 9000);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(hiddenObj, true, true, true, true);

            valueInput = UIFactory.CreateInputField(hiddenObj, "StringInputField", "...");
            UIFactory.SetLayoutElement(valueInput.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 5000, flexibleHeight: 5000);

            valueInput.Component.lineType = InputField.LineType.MultiLineNewline;

            placeholderText = valueInput.Component.placeholder.GetComponent<Text>();

            placeholderText.supportRichText = false;
            valueInput.Component.textComponent.supportRichText = false;

            OnValueUpdated();

            valueInput.OnValueChanged += (string val) =>
            {
                hiddenText.text = val ?? "";
                LayoutRebuilder.ForceRebuildLayoutImmediate(Owner.ContentRect);
                SetValueFromInput();
            };
        }
    }
}
