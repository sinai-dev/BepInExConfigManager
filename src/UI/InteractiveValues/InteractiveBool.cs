using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ConfigManager.UI;
using UniverseLib.UI;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveBool : InteractiveValue
    {
        public InteractiveBool(object value, Type valueType) : base(value, valueType) { }

        internal Toggle m_toggle;
        private Text labelText;

        public override bool SupportsType(Type type) => type == typeof(bool);

        public override void RefreshUIForValue()
        {
            var val = (bool)Value;

            if (!m_toggle.gameObject.activeSelf)
                m_toggle.gameObject.SetActive(true);

            if (val != m_toggle.isOn)
                m_toggle.isOn = val;

            var color = val
                ? "6bc981"  // on
                : "c96b6b"; // off

            labelText.text = $"<color=#{color}>{val}</color>";
        }

        internal void OnToggleValueChanged(bool val)
        {
            Value = val;
            RefreshUIForValue();
            Owner.SetValueFromIValue();
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            var toggleObj = UIFactory.CreateToggle(m_mainContent, "InteractiveBoolToggle", out m_toggle, out _, new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(toggleObj, minWidth: 24);
            m_toggle.onValueChanged.AddListener(OnToggleValueChanged);

            labelText = UIFactory.CreateLabel(m_mainContent, "TrueFalseLabel", "False", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(labelText.gameObject, minWidth: 60, minHeight: 25);

            RefreshUIForValue();
        }
    }
}
