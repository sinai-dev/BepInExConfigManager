using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.Input;
using UniverseLib.UI;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveKeycode : InteractiveValue
    {
        internal Text labelText;
        internal ButtonRef rebindButton;
        internal ButtonRef confirmButton;
        internal ButtonRef cancelButton;

        private readonly bool isInputSystem;

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
            rebindButton.Component.gameObject.SetActive(false);
            confirmButton.Component.gameObject.SetActive(true);
            confirmButton.Component.interactable = false;
            cancelButton.Component.gameObject.SetActive(true);

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
                object key = InputSystem.KeyCodeToKeyEnum(kc);
                labelText.text = $"<i>{key.ToString()}</i>";
            }

            confirmButton.Component.interactable = true;
        }

        private void OnKeycodeConfirmed(KeyCode? kc)
        {
            if (kc != null)
            {
                if (!isInputSystem)
                    Value = kc;
                else
                    Value = InputSystem.KeyCodeToKeyEnum(kc.Value);
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
            rebindButton.Component.gameObject.SetActive(true);
            confirmButton.Component.gameObject.SetActive(false);
            cancelButton.Component.gameObject.SetActive(false);
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            labelText = UIFactory.CreateLabel(m_mainContent, "Label", Value?.ToString() ?? "<notset>", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(labelText.gameObject, minWidth: 150, minHeight: 25);

            rebindButton = UIFactory.CreateButton(m_mainContent, "RebindButton", "Rebind");
            rebindButton.OnClick += BeginRebind;
            UIFactory.SetLayoutElement(rebindButton.Component.gameObject, minHeight: 25, minWidth: 100);

            confirmButton = UIFactory.CreateButton(m_mainContent, "ConfirmButton", "Confirm", new Color(0.1f, 0.4f, 0.1f));
            confirmButton.OnClick += ConfirmEndRebind;
            UIFactory.SetLayoutElement(confirmButton.Component.gameObject, minHeight: 25, minWidth: 100);
            confirmButton.Component.gameObject.SetActive(false);
            RuntimeProvider.Instance.SetColorBlock(confirmButton.Component, disabled: new Color(0.3f, 0.3f, 0.3f));

            cancelButton = UIFactory.CreateButton(m_mainContent, "EndButton", "Cancel", new Color(0.4f, 0.1f, 0.1f));
            cancelButton.OnClick += CancelEndRebind;
            UIFactory.SetLayoutElement(cancelButton.Component.gameObject, minHeight: 25, minWidth: 100);
            cancelButton.Component.gameObject.SetActive(false);
        }
    }
}
