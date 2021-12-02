using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using ConfigManager.UI;
using BepInEx.Configuration;
using UniverseLib.UI;
using UniverseLib;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveNumber : InteractiveValue
    {
        internal InputFieldRef m_valueInput;
        private Slider m_slider;

        public MethodInfo ParseMethod => m_parseMethod ??= Value.GetType().GetMethod("Parse", new Type[] { typeof(string) });
        private MethodInfo m_parseMethod;

        public InteractiveNumber(object value, Type valueType) : base(value, valueType) { }

        public override bool SupportsType(Type type)
            => (type.IsPrimitive && type != typeof(bool)) || type == typeof(decimal);

        public override void RefreshUIForValue()
        {
            m_valueInput.Text = Value.ToString();

            if (!m_valueInput.Component.gameObject.activeSelf)
                m_valueInput.Component.gameObject.SetActive(true);

            if (m_slider)
                m_slider.value = (float)Convert.ChangeType(Value, typeof(float));
        }

        internal void SetValueFromInput()
        {
            try
            {
                Value = ParseMethod.Invoke(null, new object[] { m_valueInput.Text });
                
                if (Owner.RefConfig.Description?.AcceptableValues is AcceptableValueBase acceptable
                    && !acceptable.IsValid(Value))
                {
                    throw new Exception();
                }

                Owner.SetValueFromIValue();
                RefreshUIForValue();

                m_valueInput.Component.textComponent.color = Color.white;
            }
            catch 
            {
                m_valueInput.Component.textComponent.color = Color.red;
            }
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            m_valueInput = UIFactory.CreateInputField(m_mainContent, "InteractiveNumberInput", "...");
            UIFactory.SetLayoutElement(m_valueInput.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 0);
            m_valueInput.Component.gameObject.SetActive(false);
            m_valueInput.OnValueChanged += (string val) =>
            {
                SetValueFromInput();
            };

            //var type = Value.GetType();
            //if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            //    m_valueInput.characterValidation = InputField.CharacterValidation.Decimal;
            //else
            //    m_valueInput.characterValidation = InputField.CharacterValidation.Integer;

            if (Owner.RefConfig.Description.AcceptableValues is AcceptableValueBase range)
            {
                var gtype = typeof(AcceptableValueRange<>).MakeGenericType(range.ValueType);
                object minValue = ReflectionUtility.GetPropertyInfo(gtype, "MinValue").GetValue(range, null);
                object maxValue = ReflectionUtility.GetPropertyInfo(gtype, "MaxValue").GetValue(range, null);

                Owner.m_mainLabel.text += $" <color=grey><i>[{minValue.ToString()} - {maxValue.ToString()}]</i></color>";

                var sliderObj = UIFactory.CreateSlider(m_mainContent, "ValueSlider", out m_slider);
                UIFactory.SetLayoutElement(sliderObj, minWidth: 250, minHeight: 25);

                m_slider.minValue = (float)Convert.ChangeType(minValue, typeof(float));
                m_slider.maxValue = (float)Convert.ChangeType(maxValue, typeof(float));

                m_slider.value = (float)Convert.ChangeType(Value, typeof(float));

                m_slider.onValueChanged.AddListener((float val) =>
                {
                    Value = Convert.ChangeType(val, FallbackType);
                    Owner.SetValueFromIValue();
                    m_valueInput.Text = Value.ToString();
                });

                //m_valueInput.onValueChanged.AddListener((string val) => 
                //{
                //    SetValueFromInput
                //    //slider.value = (float)Convert.ChangeType(Value, typeof(float));
                //});
            }
        }
    }
}
