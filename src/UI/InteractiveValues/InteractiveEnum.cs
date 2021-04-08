using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ConfigManager.UI;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveEnum : InteractiveValue
    {
        internal static Dictionary<Type, KeyValuePair<long, string>[]> s_enumNamesCache = new Dictionary<Type, KeyValuePair<long, string>[]>();

        internal KeyValuePair<long, string>[] m_values = new KeyValuePair<long, string>[0];
        internal Dictionary<string, Dropdown.OptionData> m_dropdownOptions = new Dictionary<string, Dropdown.OptionData>();

        internal Dropdown m_dropdown;

        public InteractiveEnum(object value, Type valueType) : base(value, valueType)
        {
            if (value != null)
                GetNames();
        }

        public override bool SupportsType(Type type) => type.IsEnum;

        internal void GetNames()
        {
            var type = Value?.GetType() ?? FallbackType;

            if (!s_enumNamesCache.ContainsKey(type))
            {
                // using GetValues not GetNames, to catch instances of weird enums (eg CameraClearFlags)
                var values = Enum.GetValues(type);

                var list = new List<KeyValuePair<long, string>>();
                var set = new HashSet<string>();

                foreach (var value in values)
                {
                    var name = value.ToString();

                    if (set.Contains(name)) 
                        continue;

                    set.Add(name);

                    var backingType = Enum.GetUnderlyingType(type);
                    long longValue;
                    try
                    {
                        // this approach is necessary, a simple '(int)value' is not sufficient.

                        var unbox = Convert.ChangeType(value, backingType);

                        longValue = (long)Convert.ChangeType(unbox, typeof(long));
                    }
                    catch (Exception ex)
                    {
                        ConfigMngrPlugin.Logger.LogWarning("[InteractiveEnum] Could not Unbox underlying type " + backingType.Name + " from " + type.FullName);
                        ConfigMngrPlugin.Logger.LogMessage(ex.ToString());
                        continue;
                    }

                    list.Add(new KeyValuePair<long, string>(longValue, name));
                }

                s_enumNamesCache.Add(type, list.ToArray());
            }

            m_values = s_enumNamesCache[type];
        }

        public override void OnValueUpdated()
        {
            GetNames();

            base.OnValueUpdated();
        }

        public override void RefreshUIForValue()
        {
            base.RefreshUIForValue();

            if (this is InteractiveFlags)
                return;

            string key = Value.ToString();
            if (m_dropdownOptions.ContainsKey(key))
                m_dropdown.value = m_dropdown.options.IndexOf(m_dropdownOptions[key]);
        }

        private void SetValueFromDropdown()
        {
            var type = Value?.GetType() ?? FallbackType;
            var index = m_dropdown.value;

            var value = Enum.Parse(type, s_enumNamesCache[type][index].Value);

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

            if (this is InteractiveFlags)
                return;

            // dropdown

            var dropdownObj = UIFactory.CreateDropdown(m_mainContent, out m_dropdown, "", 14, null);
            UIFactory.SetLayoutElement(dropdownObj, minWidth: 400, minHeight: 25);

            m_dropdown.onValueChanged.AddListener((int val) =>
            {
                SetValueFromDropdown();
            });

            foreach (var kvp in m_values)
            {
                var opt = new Dropdown.OptionData
                {
                    text = kvp.Value
                };
                m_dropdown.options.Add(opt);
                m_dropdownOptions.Add(kvp.Value, opt);
            }
        }
    }
}
