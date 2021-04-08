using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ConfigManager;
using ConfigManager.Input;
using ConfigManager.Runtime;
using ConfigManager.UI;
using UnityEngine;
using HarmonyLib;

namespace ConfigManager
{
    public static class ConfigManager
    {
        internal static Harmony Harmony { get; } = new Harmony(ConfigManagerPlugin.GUID);
        internal static ManualLogSource Log => ConfigManagerPlugin.LogSource;

        // Internal config
        internal const string CTG_ID = "BepInExConfigManager";
        internal static string CTG = "Settings";
        internal static ConfigEntry<KeyCode> Main_Menu_Toggle;
        internal static ConfigEntry<bool> Auto_Save_Configs;
        internal static ConfigEntry<float> UI_Scale;

        public static void Init()
        {
            RuntimeProvider.Init();
            InputManager.Init();

            InitConfig();

            UIFactory.Init();
            UIManager.Init();

            Log.LogMessage("ConfigManager initialized.");
        }

        private static bool doneSetupCategories;

        public static void Update()
        {
            if (!doneSetupCategories)
            {
                ConfigurationEditor.SetupCategories();

                doneSetupCategories = true;
            }

            UIManager.Update();
            InputManager.Update();
        }

        public static void InitConfig()
        {
            Main_Menu_Toggle = ConfigManagerPlugin.Instance.Config.Bind(new ConfigDefinition(CTG, "Main Menu Toggle"), 
                KeyCode.F5, 
                new ConfigDescription("The toggle for the Config Manager menu"));

            Auto_Save_Configs = ConfigManagerPlugin.Instance.Config.Bind(new ConfigDefinition(CTG, "Auto-save settings"),
                false,
                new ConfigDescription("Automatically save settings after changing them? This will mean the undo feature will be unavailable."));

            Auto_Save_Configs.SettingChanged += Auto_Save_Configs_SettingChanged;

            UI_Scale = ConfigManagerPlugin.Instance.Config.Bind(new ConfigDefinition(CTG, "UI Scale"),
                1f,
                new ConfigDescription("The scale of the UI elements", new AcceptableValueRange<float>(0.75f, 1.25f)));

            UI_Scale.SettingChanged += UiScale_SettingChanged;

            // InitTest();
        }

        private static void Auto_Save_Configs_SettingChanged(object sender, EventArgs e)
        {
            bool val = (bool)(e as SettingChangedEventArgs).ChangedSetting.BoxedValue;
            ConfigurationEditor.saveButton.gameObject.SetActive(!val);
        }

        private static void UiScale_SettingChanged(object sender, EventArgs e)
        {
            float scale = (float)(e as SettingChangedEventArgs).ChangedSetting.BoxedValue;
            UIManager.CanvasRoot.GetComponent<Canvas>().scaleFactor = scale;
        }

        ////  ~~~~~~~~~~~~~~~~ TEST CONFIG ~~~~~~~~~~~~~~~~

        //static void InitTest()
        //{
        //    TomlTypeConverter.AddConverter(typeof(TestConfigClass), new TypeConverter()
        //    {
        //        ConvertToObject = (string s, Type t) => 
        //        {
        //            var split = s.Split(',');
        //            return new TestConfigClass() { myInt1 = int.Parse(split[0]), myInt2 = int.Parse(split[1]) };
        //        },
        //        ConvertToString = (object o, Type t) =>
        //        {
        //            var x = (TestConfigClass)o;
        //            return $"{x.myInt1},{x.myInt2}";
        //        }
        //    });
        //    TomlTypeConverter.AddConverter(typeof(Color), new TypeConverter()
        //    {
        //        ConvertToObject = (string s, Type t) =>
        //        {
        //            var split = s.Split(',');
        //            var c = new CultureInfo("en-US");
        //            return new Color() 
        //            {
        //                r = float.Parse(split[0], c), 
        //                g = float.Parse(split[1], c), 
        //                b = float.Parse(split[2], c), 
        //                a = float.Parse(split[3], c) 
        //            };
        //        },
        //        ConvertToString = (object o, Type t) =>
        //        {
        //            var x = (Color)o;
        //            return string.Format(new CultureInfo("en-US"), "{0},{1},{2},{3}",
        //                x.r, 
        //                x.g, 
        //                x.b, 
        //                x.a);
        //        }
        //    });

        //    string ctg1 = "Category One";
        //    string ctg2 = "Category Two";
        //    var file = Instance.Config;
        //    file.Bind(new ConfigDefinition(ctg1, "This is a setting name"), true, new ConfigDescription("This is a description\r\nwith a new line"));
        //    file.Bind(new ConfigDefinition(ctg1, "Take a byte"), (byte)0xD, new ConfigDescription("Bytes have a max value of 255"));
        //    file.Bind(new ConfigDefinition(ctg1, "Int slider"), 32, new ConfigDescription("You can use sliders for any number type", 
        //        new AcceptableValueRange<int>(0, 100)));
        //    file.Bind(new ConfigDefinition(ctg1, "Float slider"), 666.6f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1000f)));
        //    file.Bind(new ConfigDefinition(ctg1, "Key binding"), KeyCode.Dollar, new ConfigDescription("Keybinds have a special rebind helper"));

        //    file.Bind(new ConfigDefinition(ctg2, "Enum dropdown"), CameraClearFlags.SolidColor, new ConfigDescription("Enums use a dropdown"));
        //    file.Bind(new ConfigDefinition(ctg2, "Color picker"), Color.magenta, new ConfigDescription("Colors use a special color picker"));
        //    file.Bind(new ConfigDefinition(ctg2, "Multiline input"), "Hello,\r\nworld!", new ConfigDescription("Strings use a multi-line input field"));
        //    //file.Bind(new ConfigDefinition(ctg2, "Float structs"), Vector3.up, new ConfigDescription("Float-structs use an editor like this"));
        //    file.Bind(new ConfigDefinition(ctg2, "Flag toggles"), BindingFlags.Public, new ConfigDescription("Enums with [Flags] attribute use a multi-toggle"));
        //    file.Bind(new ConfigDefinition(ctg2, "Custom type"), new TestConfigClass() { myInt1 = 25, myInt2 = 50 }, 
        //        new ConfigDescription("Custom types are supported with a basic Toml input, if a Converter was registered to TypeConverter."));
        //}

        //public struct TestConfigClass
        //{
        //    public int myInt1;
        //    public int myInt2;
        //}

        ////  ~~~~~~~~~~~~~~~~ END TEST CONFIG ~~~~~~~~~~~~~~~~
    }
}
