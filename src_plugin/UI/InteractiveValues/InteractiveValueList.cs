using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ConfigManager.UI;
using System.Collections;
using UniverseLib.UI;
using UniverseLib;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveValueList : InteractiveValue
    {
        internal IEnumerable<object> acceptableValues;

        internal Dropdown dropdown;
        internal Dictionary<object, Dropdown.OptionData> dropdownOptions = new();

        public InteractiveValueList(object value, Type valueType) : base(value, valueType) { }

        public override bool SupportsType(Type type) => type.IsEnum;

        internal void GetAcceptableValues()
        {
            if (acceptableValues != null)
                return;

            var acceptable = Owner.RefConfig.Description.AcceptableValues;
            var field = acceptable.GetType().GetProperty("AcceptableValues");
            acceptableValues = (field.GetValue(acceptable, null) as IList).Cast<object>();
        }

        public override void OnValueUpdated()
        {
            GetAcceptableValues();

            base.OnValueUpdated();
        }

        public override void RefreshUIForValue()
        {
            base.RefreshUIForValue();

            if (dropdownOptions.ContainsKey(Value))
                dropdown.value = dropdown.options.IndexOf(dropdownOptions[Value]);
        }

        private void SetValueFromDropdown()
        {
            //var type = Value?.GetType() ?? FallbackType;
            //var index = m_dropdown.value;

            //var value = Enum.Parse(type, s_enumNamesCache[type][index].Value);

            var value = acceptableValues.ElementAt(dropdown.value);

            if (value != null)
            {
                Value = value;
                Owner.SetValueFromIValue();
                RefreshUIForValue();
            }
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            // dropdown

            var dropdownObj = UIFactory.CreateDropdown(mainContent, "InteractiveValueList", out dropdown, "", 14, null);
            UIFactory.SetLayoutElement(dropdownObj, minWidth: 400, minHeight: 25);

            dropdown.onValueChanged.AddListener((int val) =>
            {
                SetValueFromDropdown();
            });

            foreach (var obj in acceptableValues)
            {
                var opt = new Dropdown.OptionData
                {
                    text = obj.ToString()
                };
                dropdown.options.Add(opt);
                dropdownOptions.Add(obj, opt);
            }
        }
    }
}
