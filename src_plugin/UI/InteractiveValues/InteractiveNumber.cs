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
using UniverseLib.UI.Models;
using HarmonyLib;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveNumber : InteractiveValue
    {
        internal InputFieldRef valueInput;
        private Slider slider;

        public MethodInfo ParseMethod => parseMethod ??= Value.GetType().GetMethod("Parse", new Type[] { typeof(string) });
        private MethodInfo parseMethod;

        public InteractiveNumber(object value, Type valueType) : base(value, valueType) { }

        public override bool SupportsType(Type type)
            => (type.IsPrimitive && type != typeof(bool)) || type == typeof(decimal);

        public override void RefreshUIForValue()
        {
            valueInput.Text = Value.ToString();

            if (!valueInput.Component.gameObject.activeSelf)
                valueInput.Component.gameObject.SetActive(true);

            if (slider)
                slider.value = (float)Convert.ChangeType(Value, typeof(float));
        }

        internal void SetValueFromInput()
        {
            try
            {
                Value = ParseMethod.Invoke(null, new object[] { valueInput.Text });
                
                if (Owner.RefConfig.Description?.AcceptableValues is AcceptableValueBase acceptable
                    && !acceptable.IsValid(Value))
                {
                    throw new Exception();
                }

                Owner.SetValueFromIValue();
                RefreshUIForValue();

                valueInput.Component.textComponent.color = Color.white;
            }
            catch 
            {
                valueInput.Component.textComponent.color = Color.red;
            }
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            valueInput = UIFactory.CreateInputField(mainContent, "InteractiveNumberInput", "...");
            UIFactory.SetLayoutElement(valueInput.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 0);
            valueInput.Component.gameObject.SetActive(false);
            valueInput.OnValueChanged += (string val) =>
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
                Type gtype = typeof(AcceptableValueRange<>).MakeGenericType(range.ValueType);
                object minValue = AccessTools.Property(gtype, "MinValue").GetValue(range, null);
                object maxValue = AccessTools.Property(gtype, "MaxValue").GetValue(range, null);

                Owner.mainLabel.text += $" <color=grey><i>[{minValue.ToString()} - {maxValue.ToString()}]</i></color>";

                GameObject sliderObj = UIFactory.CreateSlider(mainContent, "ValueSlider", out slider);
                UIFactory.SetLayoutElement(sliderObj, minWidth: 250, minHeight: 25);

                slider.minValue = (float)Convert.ChangeType(minValue, typeof(float));
                slider.maxValue = (float)Convert.ChangeType(maxValue, typeof(float));

                slider.value = (float)Convert.ChangeType(Value, typeof(float));

                slider.onValueChanged.AddListener((float val) =>
                {
                    Value = Convert.ChangeType(val, FallbackType);
                    Owner.SetValueFromIValue();
                    valueInput.Text = Value.ToString();
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
