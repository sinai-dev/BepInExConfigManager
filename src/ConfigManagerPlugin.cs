using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace ConfigManager
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class ConfigManagerPlugin
#if MONO
        : BaseUnityPlugin
#else
        : BepInEx.IL2CPP.BasePlugin
#endif
    {
        public const string GUID = "com.sinai.BepInExConfigManager";
        public const string NAME = "BepInExConfigManager";
        public const string AUTHOR = "Sinai";
        public const string VERSION = "0.5.2";

        public static ConfigManagerPlugin Instance { get; private set; }

        public static ManualLogSource LogSource =>
#if MONO
            Instance.Logger;
#else
            Instance.Log;
#endif

#if MONO
        internal void Awake()
        {
            Instance = this;
            ConfigManager.Init();
        }

        internal void Update()
        {
            ConfigManager.Update();
        }
#else
        public override void Load()
        {
            Instance = this;

            UnhollowerRuntimeLib.ClassInjector.RegisterTypeInIl2Cpp<DummyBehaviour>();
            var obj = new GameObject("ConfigManagerBehaviour");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            obj.AddComponent<DummyBehaviour>();
            ConfigManager.Init();
        }

        public class DummyBehaviour : MonoBehaviour
        {
            public DummyBehaviour(IntPtr ptr) : base(ptr) { }

            internal void Update()
            {
                ConfigManager.Update();
            }
        }
#endif
    }
}