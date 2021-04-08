using System;
using UnityEngine;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;

namespace ConfigManager.Input
{
    public enum InputType
    {
        InputSystem,
        Legacy,
        None
    }

    public static class InputManager
    {
        public static InputType CurrentType { get; private set; }

        private static IHandleInput m_inputModule;

        public static Vector3 MousePosition => m_inputModule.MousePosition;

        public static bool Rebinding { get; internal set; }
        public static KeyCode? LastRebindKey { get; internal set; }

        internal static IEnumerable<KeyCode> allKeycodes;
        internal static Action<KeyCode> onRebindPressed;
        internal static Action<KeyCode?> onRebindFinished;

        public static BaseInputModule UIInput => m_inputModule.UIModule;

        public static void Init()
        {
            if (InputSystem.TKeyboard != null || (ReflectionUtility.LoadModule("Unity.InputSystem") && InputSystem.TKeyboard != null))
            {
                m_inputModule = new InputSystem();
                CurrentType = InputType.InputSystem;
            }
            else if (LegacyInput.TInput != null || (ReflectionUtility.LoadModule("UnityEngine.InputLegacyModule") && LegacyInput.TInput != null))
            {
                m_inputModule = new LegacyInput();
                CurrentType = InputType.Legacy;
            }

            if (m_inputModule == null)
            {
                ConfigManager.Logger.LogWarning("Could not find any Input module!");
                m_inputModule = new NoInput();
                CurrentType = InputType.None;
                return;
            }

            var keycodes = Enum.GetValues(typeof(KeyCode));
            var list = new List<KeyCode>();
            foreach (KeyCode kc in keycodes)
            {
                string s = kc.ToString();
                if (!s.Contains("Mouse") && !s.Contains("Joystick"))
                    list.Add(kc);
            }
            allKeycodes = list.ToArray();

            CursorUnlocker.Init();
        }

        public static bool GetKeyDown(KeyCode key)
            => !Rebinding && m_inputModule.GetKeyDown(key);

        public static bool GetKey(KeyCode key)
            => !Rebinding && m_inputModule.GetKey(key);

        public static bool GetMouseButtonDown(int btn)
            => m_inputModule.GetMouseButtonDown(btn);

        public static bool GetMouseButton(int btn)
            => m_inputModule.GetMouseButton(btn);

        public static void BeginRebind(Action<KeyCode> onSelection, Action<KeyCode?> onFinished)
        {
            if (Rebinding)
                return;

            onRebindPressed = onSelection;
            onRebindFinished = onFinished;

            Rebinding = true;
            LastRebindKey = null;
        }

        public static void Update()
        {
            if (Rebinding)
            {
                var kc = GetCurrentKeyDown();
                if (kc != null)
                {
                    LastRebindKey = kc;
                    onRebindPressed?.Invoke((KeyCode)kc);
                }
            }
        }

        public static KeyCode? GetCurrentKeyDown()
        {
            foreach (var kc in allKeycodes)
            {
                if (m_inputModule.GetKeyDown(kc))
                    return kc;
            }

            return null;
        }

        public static void EndRebind()
        {
            if (!Rebinding)
                return;

            Rebinding = false;
            onRebindFinished?.Invoke(LastRebindKey);

            onRebindFinished = null;
            onRebindPressed = null;
        }

        public static void ActivateUIModule() => m_inputModule.ActivateModule();

        public static void AddUIModule()
        {
            m_inputModule.AddUIInputModule();
            ActivateUIModule();
        }
    }
}