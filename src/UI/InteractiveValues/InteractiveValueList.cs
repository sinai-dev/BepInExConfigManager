using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ConfigManager.UI;
using System.Collections;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveValueList : InteractiveValue
    {
        internal IEnumerable<object> m_acceptableValues;

        internal Dropdown m_dropdown;
        internal Dictionary<object, Dropdown.OptionData> m_dropdownOptions = new Dictionary<object, Dropdown.OptionData>();

        public InteractiveValueList(object value, Type valueType) : base(value, valueType) { }

        public override bool SupportsType(Type type) => type.IsEnum;

        internal void GetAcceptableValues()
        {
            if (m_acceptableValues != null)
                return;

            var acceptable = Owner.RefConfig.Description.AcceptableValues;
            var field = acceptable.GetType().GetProperty("AcceptableValues");
            m_acceptableValues = (field.GetValue(acceptable, null) as IList).Cast<object>();
        }

        public override void OnValueUpdated()
        {
            GetAcceptableValues();

            base.OnValueUpdated();
        }

        public override void RefreshUIForValue()
        {
            base.RefreshUIForValue();

            if (m_dropdownOptions.ContainsKey(Value))
                m_dropdown.value = m_dropdown.options.IndexOf(m_dropdownOptions[Value]);
        }

        private void SetValueFromDropdown()
        {
            //var type = Value?.GetType() ?? FallbackType;
            //var index = m_dropdown.value;

            //var value = Enum.Parse(type, s_enumNamesCache[type][index].Value);

            var value = m_acceptableValues.ElementAt(m_dropdown.value);

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

            var dropdownObj = UIFactory.CreateDropdown(m_mainContent, out m_dropdown, "", 14, null);
            UIFactory.SetLayoutElement(dropdownObj, minWidth: 400, minHeight: 25);

            m_dropdown.onValueChanged.AddListener((int val) =>
            {
                SetValueFromDropdown();
            });

            foreach (var obj in m_acceptableValues)
            {
                var opt = new Dropdown.OptionData
                {
                    text = obj.ToString()
                };
                m_dropdown.options.Add(opt);
                m_dropdownOptions.Add(obj, opt);
            }
        }
    }
}
