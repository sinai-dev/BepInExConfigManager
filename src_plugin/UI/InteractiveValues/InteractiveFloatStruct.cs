using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ConfigManager.UI;
using System.Reflection;
using UniverseLib.UI;

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
            private readonly FieldInfo[] fields;

            public StructInfo(Type type)
            {
                fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                               .Where(it => !it.IsLiteral)
                               .ToArray();

                FieldNames = fields.Select(it => it.Name)
                                     .ToArray();
            }

            public object SetValue(ref object instance, int fieldIndex, float val)
            {
                fields[fieldIndex].SetValue(instance, val);
                return instance;
            }

            public float GetValue(object instance, int fieldIndex)
                => (float)fields[fieldIndex].GetValue(instance);

            public void RefreshUI(InputField[] inputs, object instance)
            {
                try
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        FieldInfo field = fields[i];
                        float val = (float)field.GetValue(instance);
                        inputs[i].text = val.ToString(CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception ex)
                {
                    ConfigManager.LogSource.LogMessage(ex);
                }
            }
        }

        private static readonly Dictionary<string, bool> _typeSupportCache = new();
        
        public static bool IsTypeSupported(Type type)
        {
            if (!type.IsValueType)
                return false;

            if (_typeSupportCache.TryGetValue(type.AssemblyQualifiedName, out bool ret))
                return ret;

            ret = true;
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (FieldInfo field in fields)
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

            structInfo.RefreshUI(inputs, this.Value);
        }

        internal Type lastStructType;

        internal void InitializeStructInfo()
        {
            Type type = Value?.GetType() ?? FallbackType;

            if (structInfo != null && type == lastStructType)
                return;

            lastStructType = type;

            structInfo = new StructInfo(type);
        }

        internal InputField[] inputs;

        public override void ConstructUI(GameObject parent)
        {
            try
            {
                InitializeStructInfo();

                base.ConstructUI(parent);

                GameObject editorContainer = UIFactory.CreateGridGroup(mainContent, "EditorContent", new Vector2(150f, 25f), new Vector2(5f, 5f),
                    new Color(1,1,1,0));
                UIFactory.SetLayoutElement(editorContainer, minWidth: 300, flexibleWidth: 9999);

                inputs = new InputField[structInfo.FieldNames.Length];

                for (int i = 0; i < structInfo.FieldNames.Length; i++)
                    AddEditorRow(i, editorContainer);

                RefreshUIForValue();
            }
            catch (Exception ex)
            {
                ConfigManager.LogSource.LogMessage(ex);
            }
        }

        internal void AddEditorRow(int index, GameObject groupObj)
        {
            try
            {
                GameObject row = UIFactory.CreateHorizontalGroup(groupObj, "EditorRow", false, true, true, true, 5, default, new Color(1, 1, 1, 0));

                string name = structInfo.FieldNames[index];
                if (name.StartsWith("m_"))
                    name = name.Substring(2, name.Length - 2);
                if (name.Length > 1)
                    name = name.Substring(0, 1);

                Text label = UIFactory.CreateLabel(row, "RowLabel", $"{name}:", TextAnchor.MiddleRight, Color.cyan);
                UIFactory.SetLayoutElement(label.gameObject, minWidth: 30, flexibleWidth: 0, minHeight: 25);

                UniverseLib.UI.Models.InputFieldRef inputField = UIFactory.CreateInputField(row, "InputField", "...");
                UIFactory.SetLayoutElement(inputField.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 0);

                inputs[index] = inputField.Component;

                inputField.OnValueChanged += (string val) =>
                {
                    try
                    {
                        float f = float.Parse(val, CultureInfo.InvariantCulture);
                        Value = structInfo.SetValue(ref this.Value, index, f);
                        Owner.SetValueFromIValue();
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                ConfigManager.LogSource.LogMessage(ex);
            }
        }
    }
}
