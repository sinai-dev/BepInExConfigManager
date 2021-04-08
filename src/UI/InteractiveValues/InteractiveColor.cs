using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ConfigManager.UI.InteractiveValues
{
    public class InteractiveColor : InteractiveValue
    {
        private Image m_colorImage;
        private readonly InputField[] m_inputs = new InputField[4];
        private readonly Slider[] m_sliders = new Slider[4];

        private static readonly string[] s_fieldNames = new[] { "R", "G", "B", "A" };

        public InteractiveColor(object value, Type valueType) : base(value, valueType) { }

        public override bool SupportsType(Type type) => type == typeof(Color) || type == typeof(Color32);

        public override void RefreshUIForValue()
        {
            base.RefreshUIForValue();

            RefreshColorUI();
        }

        private void RefreshColorUI()
        {
            if (this.Value is Color32 c32)
            {
                m_inputs[0].text = c32.r.ToString();
                m_inputs[1].text = c32.g.ToString();
                m_inputs[2].text = c32.b.ToString();
                m_inputs[3].text = c32.a.ToString();

                if (m_colorImage)
                    m_colorImage.color = c32;
            }
            else if (this.Value is Color color)
            {
                m_inputs[0].text = color.r.ToString();
                m_inputs[1].text = color.g.ToString();
                m_inputs[2].text = color.b.ToString();
                m_inputs[3].text = color.a.ToString();

                if (m_colorImage)
                    m_colorImage.color = color;
            }
        }

        protected internal override void OnToggleSubcontent(bool toggle)
        {
            base.OnToggleSubcontent(toggle);

            RefreshColorUI();
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            // hori group

            var baseHoriGroup = UIFactory.CreateHorizontalGroup(m_mainContent, "ColorEditor", false, false, true, true, 5,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);

            var imgHolder = UIFactory.CreateVerticalGroup(baseHoriGroup, "ImgHolder", true, true, true, true, 0, new Vector4(1, 1, 1, 1),
                new Color(0.08f, 0.08f, 0.08f));
            UIFactory.SetLayoutElement(imgHolder, minWidth: 50, minHeight: 25, flexibleWidth: 999, flexibleHeight: 0);

            var imgObj = UIFactory.CreateUIObject("ColorImageHelper", imgHolder, new Vector2(100, 25));
            m_colorImage = imgObj.AddComponent<Image>();
            m_colorImage.color = Value is Color ? (Color)this.Value : (Color)(Color32)this.Value;

            // sliders / inputs

            var editorGroup = UIFactory.CreateVerticalGroup(baseHoriGroup, "EditorsGroup", false, false, true, true, 3, new Vector4(3, 3, 3, 3),
                new Color(1, 1, 1, 0));

            var grid = UIFactory.CreateGridGroup(editorGroup, "Grid", new Vector2(140, 25), new Vector2(2, 2), new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(grid, minWidth: 580, minHeight: 25, flexibleWidth: 900);

            for (int i = 0; i < 4; i++)
                AddEditorRow(i, grid);

            RefreshUIForValue();
        }

        internal void AddEditorRow(int index, GameObject groupObj)
        {
            var row = UIFactory.CreateHorizontalGroup(groupObj, "EditorRow_" + s_fieldNames[index],
                false, true, true, true, 5, default, new Color(1, 1, 1, 0));

            var label = UIFactory.CreateLabel(row, "RowLabel", $"{s_fieldNames[index]}:", TextAnchor.MiddleRight, Color.cyan);
            UIFactory.SetLayoutElement(label.gameObject, minWidth: 17, flexibleWidth: 0, minHeight: 25);

            var inputFieldObj = UIFactory.CreateInputField(row, "InputField", "...", 14, 3, 1);
            UIFactory.SetLayoutElement(inputFieldObj, minWidth: 40, minHeight: 25, flexibleWidth: 0);

            var inputField = inputFieldObj.GetComponent<InputField>();
            m_inputs[index] = inputField;
            inputField.characterValidation = Value is Color
                                             ? InputField.CharacterValidation.Decimal
                                             : InputField.CharacterValidation.Integer; // color32 uses byte

            inputField.onValueChanged.AddListener((string value) =>
            {
                if (Value is Color)
                {
                    float val = float.Parse(value);
                    SetValueToColor(val);
                    m_sliders[index].value = val;
                }
                else
                {
                    byte val = byte.Parse(value);
                    SetValueToColor32(val);
                    m_sliders[index].value = val;
                }
            });

            var sliderObj = UIFactory.CreateSlider(row, "Slider", out Slider slider);
            m_sliders[index] = slider;
            UIFactory.SetLayoutElement(sliderObj, minHeight: 25, minWidth: 70, flexibleWidth: 999, flexibleHeight: 0);
            slider.minValue = 0;
            if (Value is Color)
            {
                slider.maxValue = 1;
                slider.value = GetValueFromColor();
            }
            else
            {
                slider.maxValue = 255;
                slider.value = GetValueFromColor32();
            }

            slider.onValueChanged.AddListener((float value) =>
            {
                try
                {
                    if (Value is Color32)
                    {
                        var val = ((byte)value).ToString();
                        inputField.text = val;
                        SetValueToColor32((byte)value);
                        m_inputs[index].text = val;
                    }
                    else
                    {
                        inputField.text = value.ToString();
                        SetValueToColor(value);
                        m_inputs[index].text = value.ToString();
                    }
                }
                catch (Exception ex)
                {
                    ConfigManager.Logger.LogMessage(ex.ToString());
                }
            });

            // methods for working with Color

            void SetValueToColor(float floatValue)
            {
                Color _color = (Color)Value;
                switch (index)
                {
                    case 0: _color.r = floatValue; break;
                    case 1: _color.g = floatValue; break;
                    case 2: _color.b = floatValue; break;
                    case 3: _color.a = floatValue; break;
                }
                Value = _color;
                m_colorImage.color = _color;
                Owner.SetValueFromIValue();
            }

            float GetValueFromColor()
            {
                Color _color = (Color)Value;
                return index switch
                {
                    0 => _color.r,
                    1 => _color.g,
                    2 => _color.b,
                    3 => _color.a,
                    _ => throw new NotImplementedException(),
                };
            }

            // methods for working with Color32

            void SetValueToColor32(byte byteValue)
            {
                Color32 _color = (Color32)Value;
                switch (index)
                {
                    case 0: _color.r = byteValue; break;
                    case 1: _color.g = byteValue; break;
                    case 2: _color.b = byteValue; break;
                    case 3: _color.a = byteValue; break;
                }
                Value = _color;
                m_colorImage.color = _color;
                Owner.SetValueFromIValue();
            }

            byte GetValueFromColor32()
            {
                Color32 _color = (Color32)Value;
                return index switch
                {
                    0 => _color.r,
                    1 => _color.g,
                    2 => _color.b,
                    3 => _color.a,
                    _ => throw new NotImplementedException(),
                };
            }
        }
    }
}
