using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using ConfigManager.UI;
using System.Linq;
#if CPP
using UnhollowerRuntimeLib;
#endif

namespace ConfigManager.Input
{
    public class InputSystem : IHandleInput
    {
        public InputSystem()
        {
            m_kbCurrentProp = TKeyboard.GetProperty("current");
            m_kbIndexer = TKeyboard.GetProperty("Item", new Type[] { TKey });

            var btnControl = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl");
            m_btnIsPressedProp = btnControl.GetProperty("isPressed");
            m_btnWasPressedProp = btnControl.GetProperty("wasPressedThisFrame");

            m_mouseCurrentProp = TMouse.GetProperty("current");
            m_leftButtonProp = TMouse.GetProperty("leftButton");
            m_rightButtonProp = TMouse.GetProperty("rightButton");

            m_positionProp = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Pointer")
                            .GetProperty("position");

            m_readVector2InputMethod = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputControl`1")
                                      .MakeGenericType(typeof(Vector2))
                                      .GetMethod("ReadValue");
        }

        public static Type TKeyboard => m_tKeyboard ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Keyboard");
        private static Type m_tKeyboard;

        public static Type TMouse => m_tMouse ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Mouse");
        private static Type m_tMouse;

        public static Type TKey => m_tKey ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Key");
        private static Type m_tKey;

        private static PropertyInfo m_btnIsPressedProp;
        private static PropertyInfo m_btnWasPressedProp;

        private static object CurrentKeyboard => m_currentKeyboard ??= m_kbCurrentProp.GetValue(null, null);
        private static object m_currentKeyboard;
        private static PropertyInfo m_kbCurrentProp;
        private static PropertyInfo m_kbIndexer;

        private static object CurrentMouse => m_currentMouse ??= m_mouseCurrentProp.GetValue(null, null);
        private static object m_currentMouse;
        private static PropertyInfo m_mouseCurrentProp;

        private static object LeftMouseButton => m_lmb ??= m_leftButtonProp.GetValue(CurrentMouse, null);
        private static object m_lmb;
        private static PropertyInfo m_leftButtonProp;

        private static object RightMouseButton => m_rmb ??= m_rightButtonProp.GetValue(CurrentMouse, null);
        private static object m_rmb;
        private static PropertyInfo m_rightButtonProp;

        private static object MousePositionInfo => m_pos ??= m_positionProp.GetValue(CurrentMouse, null);
        private static object m_pos;
        private static PropertyInfo m_positionProp;
        private static MethodInfo m_readVector2InputMethod;

        public Vector2 MousePosition
        {
            get
            {
                try
                {
                    return (Vector2)m_readVector2InputMethod.Invoke(MousePositionInfo, new object[0]);
                }
                catch
                {
                    return Vector2.zero;
                }
            }
        }

        public bool GetKeyDown(KeyCode key)
        {
            var obj = KeyCodeToActualKey(key);
            if (obj == default)
                return false;
            return (bool)m_btnWasPressedProp.GetValue(KeyCodeToActualKey(key), null);
        }

        public bool GetKey(KeyCode key)
        {
            var obj = KeyCodeToActualKey(key);
            if (obj == default)
                return false;
            return (bool)m_btnIsPressedProp.GetValue(KeyCodeToActualKey(key), null);
        }
        public bool GetMouseButtonDown(int btn)
        {
            return btn switch
            {
                0 => (bool)m_btnWasPressedProp.GetValue(LeftMouseButton, null),
                1 => (bool)m_btnWasPressedProp.GetValue(RightMouseButton, null),
                // case 2: return (bool)_btnWasPressedProp.GetValue(MiddleMouseButton, null);
                _ => throw new NotImplementedException(),
            };
        }

        public bool GetMouseButton(int btn)
        {
            return btn switch
            {
                0 => (bool)m_btnIsPressedProp.GetValue(LeftMouseButton, null),
                1 => (bool)m_btnIsPressedProp.GetValue(RightMouseButton, null),
                // case 2: return (bool)_btnIsPressedProp.GetValue(MiddleMouseButton, null);
                _ => throw new NotImplementedException(),
            };
        }

        internal static Dictionary<KeyCode, object> KeyCodeToKeyDict = new Dictionary<KeyCode, object>();
        internal static Dictionary<KeyCode, object> KeyCodeToKeyEnumDict = new Dictionary<KeyCode, object>();

        internal static Dictionary<string, string> keycodeToKeyFixes = new Dictionary<string, string>
        {
            { "Control", "Ctrl" },
            { "Return", "Enter" },
            { "Alpha", "Digit" },
            { "Keypad", "Numpad" },
            { "Numlock", "NumLock" },
            { "Print", "PrintScreen" },
            { "BackQuote", "Backquote" }
        };

        internal object KeyCodeToActualKey(KeyCode key)
        {
            if (!KeyCodeToKeyDict.ContainsKey(key)) 
            {
                try
                {
                    var parsed = KeyCodeToKeyEnum(key);
                    var actualKey = m_kbIndexer.GetValue(CurrentKeyboard, new object[] { parsed });
                    KeyCodeToKeyDict.Add(key, actualKey);
                }
                catch
                {
                    KeyCodeToKeyDict.Add(key, default);
                }
            }

            return KeyCodeToKeyDict[key];
        }

        internal object KeyCodeToKeyEnum(KeyCode key)
        {
            if (!KeyCodeToKeyEnumDict.ContainsKey(key))
            {
                var s = key.ToString();
                try
                {
                    if (keycodeToKeyFixes.First(it => s.Contains(it.Key)) is KeyValuePair<string, string> entry)
                        s = s.Replace(entry.Key, entry.Value);
                }
                catch { }

                try
                {
                    var parsed = Enum.Parse(TKey, s);
                    KeyCodeToKeyEnumDict.Add(key, parsed);
                }
                catch
                {
                    KeyCodeToKeyEnumDict.Add(key, default);
                }
            }

            return KeyCodeToKeyEnumDict[key];
        }

        // UI Input

        public Type TInputSystemUIInputModule
            => m_tUIInputModule ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
        internal Type m_tUIInputModule;

        public BaseInputModule UIModule => m_newInputModule;
        internal BaseInputModule m_newInputModule;

        public void AddUIInputModule()
        {
            if (TInputSystemUIInputModule == null)
            {
                ConfigManager.Logger.LogWarning("Unable to find UI Input Module Type, Input will not work!");
                return;
            }

            var assetType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionAsset");
#if CPP
            m_newInputModule = UIManager.CanvasRoot.AddComponent(Il2CppType.From(TInputSystemUIInputModule)).TryCast<BaseInputModule>();
            var asset = ScriptableObject.CreateInstance(Il2CppType.From(assetType));
#else
            m_newInputModule = (BaseInputModule)UIManager.CanvasRoot.AddComponent(TInputSystemUIInputModule);
            var asset = ScriptableObject.CreateInstance(assetType);
#endif
            inputExtensions = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionSetupExtensions");

            var addMap = inputExtensions.GetMethod("AddActionMap", new Type[] { assetType, typeof(string) });
            var map = addMap.Invoke(null, new object[] { asset, "UI" });

            CreateAction(map, "point", new[] { "<Mouse>/position" }, "point");
            CreateAction(map, "click", new[] { "<Mouse>/leftButton" }, "leftClick");
            CreateAction(map, "rightClick", new[] { "<Mouse>/rightButton" }, "rightClick");
            CreateAction(map, "scrollWheel", new[] { "<Mouse>/scroll" }, "scrollWheel");

            UI_Enable = map.GetType().GetMethod("Enable");
            UI_Enable.Invoke(map, new object[0]);
            UI_ActionMap = map;
        }

        private Type inputExtensions;
        private object UI_ActionMap;
        private MethodInfo UI_Enable;

        private void CreateAction(object map, string actionName, string[] bindings, string propertyName)
        {
            var addAction = inputExtensions.GetMethod("AddAction");
            var pointAction = addAction.Invoke(null, new object[] { map, actionName, default, null, null, null, null, null });

            var inputActionType = pointAction.GetType();
            var addBinding = inputExtensions.GetMethod("AddBinding",
                new Type[] { inputActionType, typeof(string), typeof(string), typeof(string), typeof(string) });

            foreach (string binding in bindings)
                addBinding.Invoke(null, new object[] { pointAction, binding, null, null, null });

            var inputRef = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionReference")
                            .GetMethod("Create")
                            .Invoke(null, new object[] { pointAction });

            TInputSystemUIInputModule
                .GetProperty(propertyName)
                .SetValue(m_newInputModule, inputRef, null);
        }

        public void ActivateModule()
        {
            m_newInputModule.ActivateModule();
            UI_Enable.Invoke(UI_ActionMap, new object[0]);
        }
    }
}
