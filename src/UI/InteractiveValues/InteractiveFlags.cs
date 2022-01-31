using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ConfigManager.UI;
using UniverseLib.UI;
using UniverseLib;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveFlags : InteractiveEnum
    {
        public override bool HasSubContent => true;
        public override bool SubContentWanted => true;

        internal bool[] enabledFlags;
        internal Toggle[] toggles;

        public InteractiveFlags(object value, Type valueType) : base(value, valueType)
        {
            toggles = new Toggle[values.Length];
            enabledFlags = new bool[values.Length];
        }

        public override bool SupportsType(Type type)
            => type.IsEnum && type.GetCustomAttributes(true).Any(it => it is FlagsAttribute);

        public override void OnValueUpdated()
        {
            var enabledNames = new List<string>();

            var enabled = Value?.ToString().Split(',').Select(it => it.Trim());
            if (enabled != null)
                enabledNames.AddRange(enabled);

            for (int i = 0; i < values.Length; i++)
                enabledFlags[i] = enabledNames.Contains(values[i].Value);

            base.OnValueUpdated();
        }

        private bool refreshingToggleStates;
        public override void RefreshUIForValue()
        {
            base.RefreshUIForValue();

            if (subContentConstructed)
            {
                refreshingToggleStates = true;
                for (int i = 0; i < values.Length; i++)
                {
                    var toggle = toggles[i];
                    if (toggle.isOn != enabledFlags[i])
                        toggle.isOn = enabledFlags[i];
                }
                refreshingToggleStates = false;
            }
        }

        private void SetValueFromToggles()
        {
            string val = "";
            for (int i = 0; i < values.Length; i++)
            {
                if (enabledFlags[i])
                {
                    if (val != "") val += ", ";
                    val += values[i].Value;
                }
            }

            // Cannot set nothing as the value.
            if (string.IsNullOrEmpty(val))
                return;

            var type = Value?.GetType() ?? FallbackType;
            Value = Enum.Parse(type, val);
            RefreshUIForValue();
            Owner.SetValueFromIValue();
        }

        protected internal override void OnToggleSubcontent(bool toggle)
        {
            base.OnToggleSubcontent(toggle);

            RefreshUIForValue();
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);
        }

        public override void ConstructSubcontent()
        {
            subContentConstructed = true;

            var groupObj = UIFactory.CreateVerticalGroup(subContentParent, "InteractiveFlagsContent", false, true, true, true, 5,
                   new Vector4(3, 3, 3, 3), new Color(1, 1, 1, 0));

            // toggles

            for (int i = 0; i < values.Length; i++)
                AddToggle(i, groupObj);
        }

        internal void AddToggle(int index, GameObject groupObj)
        {
            var value = values[index];

            var toggleObj = UIFactory.CreateToggle(groupObj, "FlagToggle", out Toggle toggle, out Text text, new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(toggleObj, minWidth: 100, flexibleWidth: 2000, minHeight: 25);

            toggles[index] = toggle;

            toggle.onValueChanged.AddListener((bool val) => 
            {
                if (refreshingToggleStates)
                    return;

                enabledFlags[index] = val;
                SetValueFromToggles();
                RefreshUIForValue();
            });

            text.text = value.Value;
        }
    }
}
