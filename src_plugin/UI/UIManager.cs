using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.Input;
using UniverseLib;
using UniverseLib.UI.Models;
using UniverseLib.Utility;
using HarmonyLib;
using UniverseLib.UI.Panels;
using System.IO;
#if CPP
using BepInEx.IL2CPP;
#endif

namespace ConfigManager.UI
{
    internal class ConfigFileInfo
    {
        public ConfigFile RefConfigFile;
        public BepInPlugin Meta;

        private List<EntryInfo> entries = new();

        internal bool isCompletelyHidden;
        internal ButtonRef listButton;
        internal GameObject contentObj;

        internal IEnumerable<GameObject> HiddenEntries
            => Entries.Where(it => it.IsHidden).Select(it => it.content);

        internal List<EntryInfo> Entries { get => entries; set => entries = value; }
    }

    internal class EntryInfo
    {
        public EntryInfo(CachedConfigEntry cached) { Cached = cached; }
        public CachedConfigEntry Cached { get; }
        public ConfigEntryBase RefEntry;
        public bool IsHidden { get; internal set; }

        internal GameObject content;
    }

    public class UIManager : PanelBase
    {
        public static UIManager Instance { get; internal set; }

        static readonly Dictionary<string, ConfigFileInfo> ConfigFiles = new();
        static readonly Dictionary<ConfigEntryBase, CachedConfigEntry> configsToCached = new();
        static ConfigFileInfo currentCategory;

        static readonly HashSet<CachedConfigEntry> editingEntries = new();
        internal static ButtonRef saveButton;

        internal static UIBase uiBase;
        public override string Name => $"<b>{ConfigManager.NAME}</b> <i>{ConfigManager.VERSION}</i>";
        public override int MinWidth => 750;
        public override int MinHeight => 750;
        public override Vector2 DefaultAnchorMin => new(0.2f, 0.02f);
        public override Vector2 DefaultAnchorMax => new(0.8f, 0.98f);

        public static bool ShowMenu
        {
            get => uiBase != null && uiBase.Enabled;
            set
            {
                if (uiBase == null || !uiBase.RootObject || uiBase.Enabled == value)
                    return;

                UniversalUI.SetUIActive(ConfigManager.GUID, value);
                Instance.SetActive(value);
            }
        }

        public static bool ShowHiddenConfigs { get; internal set; }

        internal static GameObject CategoryListContent;
        internal static GameObject ConfigEditorContent;

        internal static string Filter => currentFilter ?? "";
        private static string currentFilter;

        private static Color normalInactiveColor = new(0.38f, 0.34f, 0.34f);
        private static Color normalActiveColor = UnityHelpers.ToColor("c2b895");

        public UIManager(UIBase owner) : base(owner)
        {
            Instance = this;
        }

        internal static void Init()
        {
            uiBase = UniversalUI.RegisterUI(ConfigManager.GUID, null);

            CreateMenu();

            // Force refresh of anchors etc
            Canvas.ForceUpdateCanvases();

            ShowMenu = false;

            SetupCategories();
        }

        internal static void SetupCategories()
        {
#if CPP
            ConfigFile coreConfig = ConfigFile.CoreConfig;
#else
            ConfigFile coreConfig = (ConfigFile)AccessTools.Property(typeof(ConfigFile), "CoreConfig").GetValue(null, null);
#endif
            if (coreConfig != null)
                SetupCategory(coreConfig, null, new BepInPlugin("bepinex.core.config", "BepInEx", "1.0"), true, true);

            foreach (CachedConfigFile cachedConfig in Patcher.ConfigFiles)
                ProcessConfigFile(cachedConfig);

            Patcher.ConfigFileCreated += ProcessConfigFile;
        }

        static void ProcessConfigFile(CachedConfigFile cachedConfig)
        {
            RuntimeHelper.StartCoroutine(DelayedProcess(cachedConfig));
        }

        static IEnumerator DelayedProcess(CachedConfigFile cachedConfig)
        {
            yield return null;

            SetupCategory(cachedConfig.configFile, null, cachedConfig.metadata, false);

            cachedConfig.configFile.SettingChanged += ConfigFile_SettingChanged;
        }

        private static void ConfigFile_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            try
            {
                configsToCached[sender as ConfigEntryBase].OnSettingChanged(sender, e);
            }
            catch (Exception ex)
            {
                ConfigManager.LogSource.LogWarning(ex);
            }
        }

        internal static void SetupCategory(ConfigFile configFile, object plugin, BepInPlugin meta, bool isCoreConfig, bool forceAdvanced = false)
        {
            try
            {
                string GUID = meta?.GUID ?? Path.GetFileNameWithoutExtension(configFile.ConfigFilePath);
                string name = meta?.Name ?? GUID;

#if CPP
                BasePlugin basePlugin = plugin as BasePlugin;
#else
                BaseUnityPlugin basePlugin = plugin as BaseUnityPlugin;
#endif

                if (!forceAdvanced && basePlugin != null)
                {
                    Type type = basePlugin.GetType();
                    if (type.GetCustomAttributes(typeof(BrowsableAttribute), false)
                            .Cast<BrowsableAttribute>()
                            .Any(it => !it.Browsable))
                    {
                        forceAdvanced = true;
                    }
                }

                ConfigFileInfo info = new()
                {
                    RefConfigFile = configFile,
                    Meta = meta,
                };

                // List button

                ButtonRef btn = UIFactory.CreateButton(CategoryListContent, "BUTTON_" + GUID, name);
                btn.OnClick += () => { SetActiveCategory(GUID); };
                UIFactory.SetLayoutElement(btn.Component.gameObject, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 0);

                RuntimeHelper.SetColorBlock(btn.Component, normalInactiveColor, new Color(0.6f, 0.55f, 0.45f),
                    new Color(0.20f, 0.18f, 0.15f));

                info.listButton = btn;

                // Editor content

                GameObject content = UIFactory.CreateVerticalGroup(ConfigEditorContent, "CATEGORY_" + GUID,
                    true, false, true, true, 4, default, new Color(0.05f, 0.05f, 0.05f));

                Dictionary<string, List<ConfigEntryBase>> dict = new()
                {
                    { "", new List<ConfigEntryBase>() } // make sure the null category is first.
                };

                // Iterate and prepare categories
                foreach (ConfigDefinition entry in configFile.Keys)
                {
                    string sec = entry.Section;
                    if (sec == null)
                        sec = "";

                    if (!dict.ContainsKey(sec))
                        dict.Add(sec, new List<ConfigEntryBase>());

                    dict[sec].Add(configFile[entry]);
                }

                // Create actual entry editors
                foreach (KeyValuePair<string, List<ConfigEntryBase>> ctg in dict)
                {
                    if (!string.IsNullOrEmpty(ctg.Key))
                    {
                        GameObject bg = UIFactory.CreateHorizontalGroup(content, "TitleBG", true, true, true, true, 0, default,
                            new Color(0.07f, 0.07f, 0.07f));
                        Text title = UIFactory.CreateLabel(bg, $"Title_{ctg.Key}", ctg.Key, TextAnchor.MiddleCenter, default, true, 17);
                        UIFactory.SetLayoutElement(title.gameObject, minHeight: 30, minWidth: 200, flexibleWidth: 9999);
                    }

                    foreach (ConfigEntryBase configEntry in ctg.Value)
                    {
                        CachedConfigEntry cache = new(configEntry, content);
                        cache.Enable();

                        configsToCached.Add(configEntry, cache);

                        GameObject obj = cache.UIroot;

                        bool advanced = forceAdvanced;

                        if (!advanced)
                        {
                            object[] tags = configEntry.Description?.Tags;
                            if (tags != null && tags.Any())
                            {
                                if (tags.Any(it => it is string s && s == "Advanced"))
                                {
                                    advanced = true;
                                }
                                else if (tags.FirstOrDefault(it => it.GetType().Name == "ConfigurationManagerAttributes") is object attributes)
                                {
                                    advanced = (bool?)attributes.GetType().GetField("IsAdvanced")?.GetValue(attributes) == true;
                                }
                            }
                        }

                        info.Entries.Add(new EntryInfo(cache)
                        {
                            RefEntry = configEntry,
                            content = obj,
                            IsHidden = advanced
                        });
                    }
                }

                // hide buttons for completely-hidden categories.
                if (!info.Entries.Any(it => !it.IsHidden))
                {
                    btn.Component.gameObject.SetActive(false);
                    info.isCompletelyHidden = true;
                }

                content.SetActive(false);

                info.contentObj = content;

                ConfigFiles.Add(GUID, info);
            }
            catch (Exception ex)
            {
                if (meta != null)
                    ConfigManager.LogSource.LogWarning($"Exception setting up category '{meta.GUID}'!\r\n{ex}");
                else
                {
                    string name;
                    try { name = Path.GetFileNameWithoutExtension(configFile.ConfigFilePath); }
                    catch { name = "UNKNOWN";  }

                    ConfigManager.LogSource.LogWarning($"Exception setting up category (no meta): {name}\n{ex}");
                }
            }
        }

        // called by UIManager.Init
        internal static void CreateMenu()
        {
            if (Instance != null)
            {
                ConfigManager.LogSource.LogWarning("An instance of ConfigurationEditor already exists, cannot create another!");
                return;
            }

            new UIManager(uiBase);
        }

        public static void OnEntryEdit(CachedConfigEntry entry)
        {
            if (!editingEntries.Contains(entry))
                editingEntries.Add(entry);

            if (!saveButton.Component.interactable)
                saveButton.Component.interactable = true;
        }

        public static void OnEntrySaveOrUndo(CachedConfigEntry entry)
        {
            if (editingEntries.Contains(entry))
                editingEntries.Remove(entry);

            if (!editingEntries.Any())
                saveButton.Component.interactable = false;
        }

        public static void SavePreferences()
        {
            foreach (ConfigFileInfo ctg in ConfigFiles.Values)
            {
                foreach (EntryInfo entry in ctg.Entries)
                    entry.RefEntry.BoxedValue = entry.Cached.EditedValue;

                ConfigFile file = ctg.RefConfigFile;
                if (!file.SaveOnConfigSet)
                    file.Save();
            }

            for (int i = editingEntries.Count - 1; i >= 0; i--)
                editingEntries.ElementAt(i).OnSaveOrUndo();

            editingEntries.Clear();
            saveButton.Component.interactable = false;
        }

        public static void SetHiddenConfigVisibility(bool show)
        {
            if (ShowHiddenConfigs == show)
                return;

            ShowHiddenConfigs = show;

            foreach (KeyValuePair<string, ConfigFileInfo> entry in ConfigFiles)
            {
                ConfigFileInfo info = entry.Value;

                if (info.isCompletelyHidden)
                    info.listButton.Component.gameObject.SetActive(ShowHiddenConfigs);
            }

            if (currentCategory != null && !ShowHiddenConfigs && currentCategory.isCompletelyHidden)
                UnsetActiveCategory();

            RefreshFilter();
        }

        public static void FilterConfigs(string search)
        {
            currentFilter = search.ToLower();
            RefreshFilter();
        }

        internal static void RefreshFilter()
        {
            if (currentCategory == null)
                return;

            foreach (EntryInfo entry in currentCategory.Entries)
            {
                bool val = (string.IsNullOrEmpty(currentFilter) 
                                || entry.RefEntry.Definition.Key.ToLower().Contains(currentFilter)
                                || (entry.RefEntry.Description?.Description?.Contains(currentFilter) ?? false))
                           && (!entry.IsHidden || ShowHiddenConfigs);

                entry.content.SetActive(val);
            }
        }

        public static void SetActiveCategory(string categoryIdentifier)
        {
            if (!ConfigFiles.ContainsKey(categoryIdentifier))
                return;

            UnsetActiveCategory();

            ConfigFileInfo info = ConfigFiles[categoryIdentifier];

            currentCategory = info;

            GameObject obj = info.contentObj;
            obj.SetActive(true);

            ButtonRef btn = info.listButton;
            RuntimeHelper.SetColorBlock(btn.Component, normalActiveColor);

            RefreshFilter();
        }

        internal static void UnsetActiveCategory()
        {
            if (currentCategory == null)
                return;

            RuntimeHelper.SetColorBlock(currentCategory.listButton.Component, normalInactiveColor);
            currentCategory.contentObj.SetActive(false);

            currentCategory = null;
        }

        protected override void ConstructPanelContent()
        {
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ContentRoot, true, false, true, true);

            ConstructTitleBar();

            ConstructSaveButton();

            ConstructToolbar();

            ConstructEditorViewport();
        }

        protected override void OnClosePanelClicked()
        {
            base.OnClosePanelClicked();

            ShowMenu = false;
        }

        private void ConstructTitleBar()
        {
            Text titleText = TitleBar.transform.GetChild(0).GetComponent<Text>();
            titleText.text = $"<b><color=#8b736b>BepInExConfigManager</color></b> <i><color=#ffe690>v{ConfigManager.VERSION}</color></i>";

            Button closeButton = TitleBar.GetComponentInChildren<Button>();
            RuntimeHelper.SetColorBlock(closeButton, new(1, 0.2f, 0.2f), new(1, 0.6f, 0.6f), new(0.3f, 0.1f, 0.1f));

            Text hideText = closeButton.GetComponentInChildren<Text>();
            hideText.color = Color.white;
            hideText.resizeTextForBestFit = true;
            hideText.resizeTextMinSize = 8;
            hideText.resizeTextMaxSize = 14;
        }

        private void ConstructSaveButton()
        {
            saveButton = UIFactory.CreateButton(ContentRoot, "SaveButton", "Save Preferences");
            saveButton.OnClick += SavePreferences;
            UIFactory.SetLayoutElement(saveButton.Component.gameObject, minHeight: 35, flexibleHeight: 0, flexibleWidth: 9999);
            RuntimeHelper.SetColorBlock(saveButton.Component, new Color(0.1f, 0.3f, 0.1f),
                new Color(0.2f, 0.5f, 0.2f), new Color(0.1f, 0.2f, 0.1f), new Color(0.2f, 0.2f, 0.2f));

            saveButton.Component.interactable = false;

            saveButton.Component.gameObject.SetActive(!ConfigManager.Auto_Save_Configs.Value);
        }

        private void ConstructToolbar()
        {
            GameObject toolbarGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "Toolbar", false, false, true, true, 4, new Vector4(3, 3, 3, 3),
                new Color(0.1f, 0.1f, 0.1f));

            GameObject toggleObj = UIFactory.CreateToggle(toolbarGroup, "HiddenConfigsToggle", out Toggle toggle, out Text toggleText);
            toggle.isOn = false;
            toggle.onValueChanged.AddListener((bool val) =>
            {
                SetHiddenConfigVisibility(val);
            });
            toggleText.text = "Show Advanced Settings";
            UIFactory.SetLayoutElement(toggleObj, minWidth: 280, minHeight: 25, flexibleHeight: 0, flexibleWidth: 0);

            InputFieldRef inputField = UIFactory.CreateInputField(toolbarGroup, "FilterInput", "Search...");
            UIFactory.SetLayoutElement(inputField.Component.gameObject, flexibleWidth: 9999, minHeight: 25);
            inputField.OnValueChanged += FilterConfigs;
        }

        private void ConstructEditorViewport()
        {
            GameObject horiGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "Main", true, true, true, true, 2, default, new Color(0.08f, 0.08f, 0.08f));

            GameObject ctgList = UIFactory.CreateScrollView(horiGroup, "CategoryList", out GameObject ctgContent, out _, new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(ctgList, minWidth: 300, flexibleWidth: 0);
            CategoryListContent = ctgContent;
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ctgContent, spacing: 3);

            GameObject editor = UIFactory.CreateScrollView(horiGroup, "ConfigEditor", out GameObject editorContent, out _, new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutElement(editor, flexibleWidth: 9999);
            ConfigEditorContent = editorContent;
        }
    }
}
