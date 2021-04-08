using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ConfigManager.UI;
using System.Reflection;

namespace ConfigManager.UI.InteractiveValues
{
    // Class for supporting any "float struct" (ie Vector, Quaternion, etc).
    // Supports any struct where all the instance fields are floats

    public class InteractiveFloatStruct : InteractiveValue
    {
        // StructInfo is a helper class for using reflection on the current value's type
        public class StructInfo
        {
            public string[] FieldNames { get; }
            private readonly FieldInfo[] m_fields;

            public StructInfo(Type type)
            {
                m_fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                               .Where(it => !it.IsLiteral)
                               .ToArray();

                FieldNames = m_fields.Select(it => it.Name)
                                     .ToArray();
            }

            public object SetValue(ref object instance, int fieldIndex, float val)
            {
                m_fields[fieldIndex].SetValue(instance, val);
                return instance;
            }

            public float GetValue(object instance, int fieldIndex)
                => (float)m_fields[fieldIndex].GetValue(instance);

            public void RefreshUI(InputField[] inputs, object instance)
            {
                try
                {
                    for (int i = 0; i < m_fields.Length; i++)
                    {
                        var field = m_fields[i];
                        float val = (float)field.GetValue(instance);
                        inputs[i].text = val.ToString();
                    }
                }
                catch (Exception ex)
                {
                    ConfigMngrPlugin.Logger.LogMessage(ex);
                }
            }
        }

        private static readonly Dictionary<string, bool> _typeSupportCache = new Dictionary<string, bool>();
        
        public static bool IsTypeSupported(Type type)
        {
            if (!type.IsValueType)
                return false;

            if (_typeSupportCache.TryGetValue(type.AssemblyQualifiedName, out bool ret))
                return ret;

            ret = true;
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.IsLiteral)
                    continue;

                if (!typeof(float).IsAssignableFrom(field.FieldType))
                {
                    ret = false;
                    break;
                }
            }
            _typeSupportCache.Add(type.AssemblyQualifiedName, ret);
            return ret;
        }

        //~~~~~~~~~ Instance ~~~~~~~~~~

        public InteractiveFloatStruct(object value, Type valueType) : base(value, valueType) { }

        public override bool SupportsType(Type type) => IsTypeSupported(type);

        public StructInfo structInfo;

        public override void RefreshUIForValue()
        {
            InitializeStructInfo();

            base.RefreshUIForValue();

            structInfo.RefreshUI(m_inputs, this.Value);
        }

        internal Type m_lastStructType;

        internal void InitializeStructInfo()
        {
            var type = Value?.GetType() ?? FallbackType;

            if (structInfo != null && type == m_lastStructType)
                return;

            m_lastStructType = type;

            structInfo = new StructInfo(type);
        }

        internal InputField[] m_inputs;

        public override void ConstructUI(GameObject parent)
        {
            try
            {
                InitializeStructInfo();

                base.ConstructUI(parent);

                var editorContainer = UIFactory.CreateGridGroup(m_mainContent, "EditorContent", new Vector2(150f, 25f), new Vector2(5f, 5f),
                    new Color(1,1,1,0));
                UIFactory.SetLayoutElement(editorContainer, minWidth: 300, flexibleWidth: 9999);

                m_inputs = new InputField[structInfo.FieldNames.Length];

                for (int i = 0; i < structInfo.FieldNames.Length; i++)
                    AddEditorRow(i, editorContainer);

                RefreshUIForValue();
            }
            catch (Exception ex)
            {
                ConfigMngrPlugin.Logger.LogMessage(ex);
            }
        }

        internal void AddEditorRow(int index, GameObject groupObj)
        {
            try
            {
                var row = UIFactory.CreateHorizontalGroup(groupObj, "EditorRow", false, true, true, true, 5, default, new Color(1, 1, 1, 0));

                string name = structInfo.FieldNames[index];
                if (name.StartsWith("m_"))
                    name = name.Substring(2, name.Length - 2);
                if (name.Length > 1)
                    name = name.Substring(0, 1);

                var label = UIFactory.CreateLabel(row, "RowLabel", $"{name}:", TextAnchor.MiddleRight, Color.cyan);
                UIFactory.SetLayoutElement(label.gameObject, minWidth: 30, flexibleWidth: 0, minHeight: 25);

                var inputFieldObj = UIFactory.CreateInputField(row, "InputField", "...", 14, 3, 1);
                UIFactory.SetLayoutElement(inputFieldObj, minWidth: 120, minHeight: 25, flexibleWidth: 0);

                var inputField = inputFieldObj.GetComponent<InputField>();
                m_inputs[index] = inputField;

                inputField.onValueChanged.AddListener((string val) =>
                {
                    try
                    {
                        float f = float.Parse(val);
                        Value = structInfo.SetValue(ref this.Value, index, f);
                        Owner.SetValueFromIValue();
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                ConfigMngrPlugin.Logger.LogMessage(ex);
            }
        }
    }
}
