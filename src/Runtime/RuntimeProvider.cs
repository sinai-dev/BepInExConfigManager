using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ConfigManager.Runtime
{
    public abstract class RuntimeProvider
    {
        public static RuntimeProvider Instance;

        public ReflectionProvider Reflection;

        public RuntimeProvider()
        {
            Initialize();
        }

        public static void Init() =>
#if CPP
            Instance = new Il2Cpp.Il2CppProvider();
#else
            Instance = new Mono.MonoProvider();
#endif

        public abstract void Initialize();

        // Unity API handlers

        public abstract void SetColorBlock(Selectable selectable, ColorBlock colors);

        public abstract void SetColorBlock(Selectable selectable, Color? normal = null, Color? highlighted = null, Color? pressed = null,
            Color? disabled = null);

        public abstract T AddComponent<T>(GameObject obj, Type type) where T : Component;

        public abstract ScriptableObject CreateScriptable(Type type);
    }
}
