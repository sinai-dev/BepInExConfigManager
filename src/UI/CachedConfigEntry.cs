using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ConfigManager.UI.InteractiveValues;
using System.Reflection;
using BepInEx.Configuration;
using UniverseLib;
using UniverseLib.UI;

namespace ConfigManager.UI
{
    public class CachedConfigEntry
    {
        public ConfigEntryBase RefConfig { get; }

        public object EditedValue { get; internal set; }
        public InteractiveValue IValue;

        // UI
        public bool UIConstructed;
        public GameObject m_parentContent;
        public GameObject ContentGroup;
        public RectTransform ContentRect;
        public GameObject SubContentGroup;

        public Text m_mainLabel;

        internal GameObject m_UIroot;
        internal GameObject m_undoButton;

        public Type FallbackType => RefConfig.SettingType;

        public CachedConfigEntry(ConfigEntryBase config, GameObject parent)
        {
            RefConfig = config;
            m_parentContent = parent;

            EditedValue = config.BoxedValue;

            var eventInfo = typeof(ConfigEntry<>).MakeGenericType(config.SettingType).GetEvent("SettingChanged");
            var methodInfo = typeof(CachedConfigEntry).GetMethod("OnSettingChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, methodInfo);
            eventInfo.AddEventHandler(config, handler);

            CreateIValue(config.BoxedValue, FallbackType);
        }

        private void OnSettingChanged(object sender, EventArgs e)
        {
            // ConfigMngrPlugin.Logger.LogMessage($"OnSettingChanged: {(e as SettingChangedEventArgs).ChangedSetting.Definition.Key}");

            var val = (e as SettingChangedEventArgs).ChangedSetting.BoxedValue;
            this.EditedValue = val; 
            this.IValue.Value = val;
            this.IValue.OnValueUpdated();
        }

        public void CreateIValue(object value, Type fallbackType)
        {
            if (RefConfig.Description?.AcceptableValues != null
                && RefConfig.Description.AcceptableValues.GetType().Name.StartsWith("AcceptableValueList"))
            {
                var type = value.GetActualType();
                IValue = new InteractiveValueList(value, type);
            }
            else
                IValue = InteractiveValue.Create(value, fallbackType);

            IValue.Owner = this;
            IValue.m_mainContentParent = ContentGroup;
            IValue.m_subContentParent = this.SubContentGroup;
        }

        public void UpdateValue()
        {
            IValue.Value = RefConfig.BoxedValue;
            EditedValue = RefConfig.BoxedValue;

            IValue.OnValueUpdated();
            IValue.RefreshSubContentState();
        }

        public void SetValueFromIValue()
        {
            if (RefConfig.Description.AcceptableValues != null)
                IValue.Value = RefConfig.Description.AcceptableValues.Clamp(IValue.Value);

            var edited = EditedValue;
            if ((edited == null && IValue.Value == null) || (edited != null && edited.Equals(IValue.Value)))
                return;

            if (ConfigManager.Auto_Save_Configs.Value)
            {
                RefConfig.BoxedValue = IValue.Value;
                if (!RefConfig.ConfigFile.SaveOnConfigSet)
                    RefConfig.ConfigFile.Save();
                UpdateValue();
            }
            else
            {
                EditedValue = IValue.Value;
                UIManager.OnEntryEdit(this);
                m_undoButton.SetActive(true);
            }
        }

        public void UndoEdits()
        {
            EditedValue = RefConfig.BoxedValue;
            IValue.Value = EditedValue;
            IValue.OnValueUpdated();

            OnSaveOrUndo();
        }

        public void RevertToDefault()
        {
            RefConfig.BoxedValue = RefConfig.DefaultValue;
            UpdateValue();
            OnSaveOrUndo();
        }

        internal void OnSaveOrUndo()
        {
            m_undoButton.SetActive(false);
            UIManager.OnEntrySaveOrUndo(this);
        }

        public void Enable()
        {
            if (!UIConstructed)
            {
                ConstructUI();
                UpdateValue();
            }

            m_UIroot.SetActive(true);
            m_UIroot.transform.SetAsLastSibling();
        }

        public void Disable()
        {
            if (m_UIroot)
                m_UIroot.SetActive(false);
        }

        public void Destroy()
        {
            if (this.m_UIroot)
                GameObject.Destroy(this.m_UIroot);
        }

        internal void ConstructUI()
        {
            UIConstructed = true;

            m_UIroot = UIFactory.CreateVerticalGroup(m_parentContent, "CacheObjectBase.MainContent", true, false, true, true, 0, 
                default, new Color(1,1,1,0));
            ContentRect = m_UIroot.GetComponent<RectTransform>();
            ContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);
            UIFactory.SetLayoutElement(m_UIroot, minHeight: 25, flexibleHeight: 9999, minWidth: 200, flexibleWidth: 5000);

            ContentGroup = UIFactory.CreateVerticalGroup(m_UIroot, "ConfigHolder", true, false, true, true, 5, new Vector4(2, 2, 5, 5),
                new Color(0.12f, 0.12f, 0.12f));

            var horiGroup = UIFactory.CreateHorizontalGroup(ContentGroup, "ConfigEntryHolder", false, false, true, true,
                5, default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 30, flexibleHeight: 0);

            // config entry label

            m_mainLabel = UIFactory.CreateLabel(horiGroup, "ConfigLabel", this.RefConfig.Definition.Key, TextAnchor.MiddleLeft, 
                new Color(0.9f, 0.9f, 0.7f));
            m_mainLabel.text += $" <i>({SignatureHighlighter.Parse(RefConfig.SettingType, false)})</i>";
            UIFactory.SetLayoutElement(m_mainLabel.gameObject, minWidth: 200, minHeight: 22, flexibleWidth: 9999, flexibleHeight: 0);

            // Undo button

            var undoButton = UIFactory.CreateButton(horiGroup, "UndoButton", "Undo", new Color(0.3f, 0.3f, 0.3f));
            undoButton.OnClick += UndoEdits;
            m_undoButton = undoButton.Component.gameObject;
            m_undoButton.SetActive(false);
            UIFactory.SetLayoutElement(m_undoButton, minWidth: 80, minHeight: 22, flexibleWidth: 0);

            // Default button

            var defaultButton = UIFactory.CreateButton(horiGroup, "DefaultButton", "Default", new Color(0.3f, 0.3f, 0.3f));
            defaultButton.OnClick += RevertToDefault;
            UIFactory.SetLayoutElement(defaultButton.Component.gameObject, minWidth: 80, minHeight: 22, flexibleWidth: 0);

            // Description label

            if (RefConfig.Description != null && !string.IsNullOrEmpty(RefConfig.Description.Description))
            {
                var desc = UIFactory.CreateLabel(ContentGroup, "Description", $"<i>{RefConfig.Description.Description}</i>",
                    TextAnchor.MiddleLeft, Color.grey);
                UIFactory.SetLayoutElement(desc.gameObject, minWidth: 250, minHeight: 18, flexibleWidth: 9999, flexibleHeight: 0);
            }

            // subcontent

            SubContentGroup = UIFactory.CreateVerticalGroup(ContentGroup, "CacheObjectBase.SubContent", true, false, true, true, 0, default,
                new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(SubContentGroup, minHeight: 30, flexibleHeight: 9999, minWidth: 125, flexibleWidth: 9000);

            SubContentGroup.SetActive(false);

            // setup IValue references

            if (IValue != null)
            {
                IValue.m_mainContentParent = ContentGroup;
                IValue.m_subContentParent = this.SubContentGroup;
            }
        }
    }
}
