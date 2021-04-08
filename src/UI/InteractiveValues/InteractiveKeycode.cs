using ConfigManager.Input;
using ConfigManager.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveKeycode : InteractiveValue
    {
        internal Text labelText;
        internal Button rebindButton;
        internal Button confirmButton;
        internal Button cancelButton;

        private bool isInputSystem;

        public InteractiveKeycode(object value, Type valueType) : base(value, valueType)
        {
            isInputSystem = !(value is KeyCode);
        }

        public override bool SupportsType(Type type) => type == typeof(KeyCode) || type.FullName == "UnityEngine.InputSystem.Key";

        public override void RefreshUIForValue()
        {
            base.RefreshUIForValue();

            labelText.text = Value.ToString();
        }

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();
        }

        public void BeginRebind()
        {
            rebindButton.gameObject.SetActive(false);
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = false;
            cancelButton.gameObject.SetActive(true);

            labelText.text = "<i>Press a key...</i>";

            InputManager.BeginRebind(OnRebindKeyPressed, OnKeycodeConfirmed);
        }

        private void OnRebindKeyPressed(KeyCode kc)
        {
            if (!isInputSystem)
            {
                labelText.text = $"<i>{kc.ToString()}</i>";
            }
            else
            {
                object key = InputSystem.KeyCodeToKeyEnumDict[kc];
                labelText.text = $"<i>{key.ToString()}</i>";
            }

            confirmButton.interactable = true;
        }

        private void OnKeycodeConfirmed(KeyCode? kc)
        {
            if (kc != null)
            {
                if (!isInputSystem)
                    Value = kc;
                else
                    Value = InputSystem.KeyCodeToKeyEnumDict[(KeyCode)kc];
            }

            Owner.SetValueFromIValue();
            RefreshUIForValue();
        }

        public void ConfirmEndRebind()
        {
            InputManager.EndRebind();
            OnRebindEnd();
        }

        public void CancelEndRebind()
        {
            InputManager.LastRebindKey = null;
            InputManager.EndRebind();
            OnRebindEnd();
        }

        internal void OnRebindEnd()
        {
            rebindButton.gameObject.SetActive(true);
            confirmButton.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(false);
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            labelText = UIFactory.CreateLabel(m_mainContent, "Label", Value?.ToString() ?? "<notset>", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(labelText.gameObject, minWidth: 150, minHeight: 25);

            rebindButton = UIFactory.CreateButton(m_mainContent, "RebindButton", "Rebind", BeginRebind);
            UIFactory.SetLayoutElement(rebindButton.gameObject, minHeight: 25, minWidth: 100);

            confirmButton = UIFactory.CreateButton(m_mainContent, "ConfirmButton", "Confirm", ConfirmEndRebind, new Color(0.1f, 0.4f, 0.1f));
            UIFactory.SetLayoutElement(confirmButton.gameObject, minHeight: 25, minWidth: 100);
            confirmButton.gameObject.SetActive(false);
            confirmButton.colors = RuntimeProvider.Instance.SetColorBlock(confirmButton.colors, disabled: new Color(0.3f, 0.3f, 0.3f));

            cancelButton = UIFactory.CreateButton(m_mainContent, "EndButton", "Cancel", CancelEndRebind, new Color(0.4f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(cancelButton.gameObject, minHeight: 25, minWidth: 100);
            cancelButton.gameObject.SetActive(false);
        }
    }
}
