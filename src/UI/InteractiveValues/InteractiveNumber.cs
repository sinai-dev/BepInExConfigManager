using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using ConfigManager.UI;
using ConfigManager.UI.Utility;
using BepInEx.Configuration;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveNumber : InteractiveValue
    {
        internal InputField m_valueInput;

        public MethodInfo ParseMethod => m_parseMethod ??= Value.GetType().GetMethod("Parse", new Type[] { typeof(string) });
        private MethodInfo m_parseMethod;

        public InteractiveNumber(object value, Type valueType) : base(value, valueType) { }

        public override bool SupportsType(Type type)
            => (type.IsPrimitive && type != typeof(bool)) || type == typeof(decimal);

        public override void RefreshUIForValue()
        {
            m_valueInput.text = Value.ToString();

            if (!m_valueInput.gameObject.activeSelf)
                m_valueInput.gameObject.SetActive(true);
        }

        internal void SetValueFromInput()
        {
            try
            {
                Value = ParseMethod.Invoke(null, new object[] { m_valueInput.text });
                Owner.SetValueFromIValue();
                RefreshUIForValue();

                m_valueInput.textComponent.color = Color.white;
            }
            catch 
            {
                m_valueInput.textComponent.color = Color.red;
            }
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            var inputObj = UIFactory.CreateInputField(m_mainContent, "InteractiveNumberInput", "...");
            UIFactory.SetLayoutElement(inputObj, minWidth: 120, minHeight: 25, flexibleWidth: 0);

            m_valueInput = inputObj.GetComponent<InputField>();
            m_valueInput.gameObject.SetActive(false);

            m_valueInput.onValueChanged.AddListener((string val) =>
            {
                SetValueFromInput();
            });

            var type = Value.GetType();
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                m_valueInput.characterValidation = InputField.CharacterValidation.Decimal;
            else
                m_valueInput.characterValidation = InputField.CharacterValidation.Integer;

            if (Owner.RefConfig.Description.AcceptableValues is AcceptableValueBase range)
            {
                var gtype = typeof(AcceptableValueRange<>).MakeGenericType(range.ValueType);
                object minValue = ReflectionUtility.GetPropertyInfo(gtype, "MinValue").GetValue(range);
                object maxValue = ReflectionUtility.GetPropertyInfo(gtype, "MaxValue").GetValue(range);

                Owner.m_mainLabel.text += $" <color=grey><i>[{minValue.ToString()} - {maxValue.ToString()}]</i></color>";

                var sliderObj = UIFactory.CreateSlider(m_mainContent, "ValueSlider", out Slider slider);
                UIFactory.SetLayoutElement(sliderObj, minWidth: 250, minHeight: 25);

                slider.minValue = (float)Convert.ChangeType(minValue, typeof(float));
                slider.maxValue = (float)Convert.ChangeType(maxValue, typeof(float));

                slider.value = (float)Convert.ChangeType(Value, typeof(float));

                slider.onValueChanged.AddListener((float val) =>
                {
                    Value = Convert.ChangeType(val, FallbackType);
                    Owner.SetValueFromIValue();
                    m_valueInput.text = Value.ToString();
                });

                m_valueInput.onValueChanged.AddListener((string val) => 
                {
                    slider.value = (float)Convert.ChangeType(Value, typeof(float));
                });
            }
        }
    }
}
