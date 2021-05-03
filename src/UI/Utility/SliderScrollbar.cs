using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ConfigManager.UI
{
    // A Slider Scrollbar which automatically resizes for the content size (no pooling).
    // Currently just used for the C# Console input field.

    public class AutoSliderScrollbar
    {
        internal static void UpdateInstances()
        {
            foreach (var instance in Instances)
            {
                if (!instance.Enabled)
                    continue;

                instance.Update();
            }
        }

        internal static readonly List<AutoSliderScrollbar> Instances = new List<AutoSliderScrollbar>();

        public GameObject UIRoot
        {
            get
            {
                if (Slider)
                    return Slider.gameObject;
                return null;
            }
        }

        public bool Enabled => UIRoot.activeInHierarchy;

        //public event Action<float> OnValueChanged;

        internal readonly Scrollbar Scrollbar;
        internal readonly Slider Slider;
        internal RectTransform ContentRect;
        internal RectTransform ViewportRect;

        //internal InputFieldScroller m_parentInputScroller;

        public AutoSliderScrollbar(Scrollbar scrollbar, Slider slider, RectTransform contentRect, RectTransform viewportRect)
        {
            Instances.Add(this);

            this.Scrollbar = scrollbar;
            this.Slider = slider;
            this.ContentRect = contentRect;
            this.ViewportRect = viewportRect;

            this.Scrollbar.onValueChanged.AddListener(this.OnScrollbarValueChanged);
            this.Slider.onValueChanged.AddListener(this.OnSliderValueChanged);

            this.Slider.Set(0f, false);
        }

        private float lastAnchorPosition;
        private float lastContentHeight;
        private float lastViewportHeight;
        private bool _refreshWanted;

        public void Update()
        {
            if (!Enabled)
                return;

            _refreshWanted = false;
            if (ContentRect.localPosition.y != lastAnchorPosition)
            {
                lastAnchorPosition = ContentRect.localPosition.y;
                _refreshWanted = true;
            }
            if (ContentRect.rect.height != lastContentHeight)
            {
                lastContentHeight = ContentRect.rect.height;
                _refreshWanted = true;
            }
            if (ViewportRect.rect.height != lastViewportHeight)
            {
                lastViewportHeight = ViewportRect.rect.height;
                _refreshWanted = true;
            }

            if (_refreshWanted)
                UpdateSliderHandle();
        }

        public void UpdateSliderHandle()
        {
            // calculate handle size based on viewport / total data height
            var totalHeight = ContentRect.rect.height;
            var viewportHeight = ViewportRect.rect.height;

            if (totalHeight <= viewportHeight)
            {
                Slider.handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
                Slider.value = 0f;
                Slider.interactable = false;
                return;
            }

            var handleHeight = viewportHeight * Math.Min(1, viewportHeight / totalHeight);
            handleHeight = Math.Max(15f, handleHeight);

            // resize the handle container area for the size of the handle (bigger handle = smaller container)
            var container = Slider.m_HandleContainerRect;
            container.offsetMax = new Vector2(container.offsetMax.x, -(handleHeight * 0.5f));
            container.offsetMin = new Vector2(container.offsetMin.x, handleHeight * 0.5f);

            // set handle size
            Slider.handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, handleHeight);

            // if slider is 100% height then make it not interactable
            Slider.interactable = !Mathf.Approximately(handleHeight, viewportHeight);

            float val = 0f;
            if (totalHeight > 0f)
                val = (float)((decimal)ContentRect.localPosition.y / (decimal)(totalHeight - ViewportRect.rect.height));

            Slider.value = val;
        }

        public void OnScrollbarValueChanged(float value)
        {
            value = 1f - value;
            if (this.Slider.value != value)
                this.Slider.Set(value, false);
            //OnValueChanged?.Invoke(value);
        }

        public void OnSliderValueChanged(float value)
        {
            value = 1f - value;
            this.Scrollbar.value = value;
            //OnValueChanged?.Invoke(value);
        }
    }
}








//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.UI;
//using MelonPrefManager;
//using MelonPrefManager.UI;

//namespace MelonPrefManager.UI.Utility
//{
//    // Basically just to fix an issue with Scrollbars, instead we use a Slider as the scrollbar.
//    public class SliderScrollbar
//    {
//        internal static readonly List<SliderScrollbar> Instances = new List<SliderScrollbar>();

//        public static void UpdateInstances()
//        {
//            if (!Instances.Any())
//                return;

//            for (int i = 0; i < Instances.Count; i++)
//            {
//                var slider = Instances[i];

//                if (slider.CheckDestroyed())
//                    i--;
//                else
//                    slider.Update();
//            }
//        }

//        public bool IsActive { get; private set; }

//        internal readonly Scrollbar m_scrollbar;
//        internal readonly Slider m_slider;
//        internal readonly RectTransform m_scrollRect;

//        public SliderScrollbar(Scrollbar scrollbar, Slider slider)
//        {
//            Instances.Add(this);

//            this.m_scrollbar = scrollbar;
//            this.m_slider = slider;
//            this.m_scrollRect = scrollbar.transform.parent.GetComponent<RectTransform>();

//            this.m_scrollbar.onValueChanged.AddListener(this.OnScrollbarValueChanged);
//            this.m_slider.onValueChanged.AddListener(this.OnSliderValueChanged);

//            this.RefreshVisibility();
//            this.m_slider.Set(1f, false);
//        }

//        internal bool CheckDestroyed()
//        {
//            if (!m_slider || !m_scrollbar)
//            {
//                Instances.Remove(this);
//                return true;
//            }

//            return false;
//        }

//        internal void Update()
//        {
//            this.RefreshVisibility();
//        }

//        internal void RefreshVisibility()
//        {
//            if (!m_slider.gameObject.activeInHierarchy)
//            {
//                IsActive = false;
//                return;
//            }

//            bool shouldShow = !Mathf.Approximately(this.m_scrollbar.size, 1);
//            var obj = this.m_slider.handleRect.gameObject;

//            if (IsActive != shouldShow)
//            {
//                IsActive = shouldShow;
//                obj.SetActive(IsActive);

//                if (IsActive)
//                    this.m_slider.Set(this.m_scrollbar.value, false);
//                else
//                    m_slider.Set(1f, false);
//            }
//        }

//        public void OnScrollbarValueChanged(float _value)
//        {
//            if (this.m_slider.value != _value)
//                this.m_slider.Set(_value, false);
//        }

//        public void OnSliderValueChanged(float _value)
//        {
//            this.m_scrollbar.value = _value;
//        }

//        #region UI CONSTRUCTION

//        public static GameObject CreateSliderScrollbar(GameObject parent, out Slider slider)
//        {
//            GameObject sliderObj = UIFactory.CreateUIObject("SliderScrollbar", parent, UIFactory._smallElementSize);

//            GameObject bgObj = UIFactory.CreateUIObject("Background", sliderObj);
//            GameObject fillAreaObj = UIFactory.CreateUIObject("Fill Area", sliderObj);
//            GameObject fillObj = UIFactory.CreateUIObject("Fill", fillAreaObj);
//            GameObject handleSlideAreaObj = UIFactory.CreateUIObject("Handle Slide Area", sliderObj);
//            GameObject handleObj = UIFactory.CreateUIObject("Handle", handleSlideAreaObj);

//            Image bgImage = bgObj.AddComponent<Image>();
//            bgImage.type = Image.Type.Sliced;
//            bgImage.color = new Color(0.05f, 0.05f, 0.05f, 1.0f);

//            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
//            bgRect.anchorMin = Vector2.zero;
//            bgRect.anchorMax = Vector2.one;
//            bgRect.sizeDelta = Vector2.zero;
//            bgRect.offsetMax = new Vector2(-10f, 0f);

//            RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
//            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
//            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
//            fillAreaRect.anchoredPosition = new Vector2(-5f, 0f);
//            fillAreaRect.sizeDelta = new Vector2(-20f, 0f);

//            Image fillImage = fillObj.AddComponent<Image>();
//            fillImage.type = Image.Type.Sliced;
//            fillImage.color = Color.clear;

//            fillObj.GetComponent<RectTransform>().sizeDelta = new Vector2(10f, 0f);

//            RectTransform handleSlideRect = handleSlideAreaObj.GetComponent<RectTransform>();
//            handleSlideRect.anchorMin = new Vector2(0f, 0f);
//            handleSlideRect.anchorMax = new Vector2(1f, 1f);
//            handleSlideRect.offsetMin = new Vector2(15f, 30f);
//            handleSlideRect.offsetMax = new Vector2(-15f, 0f);
//            handleSlideRect.sizeDelta = new Vector2(-30f, -30f);

//            Image handleImage = handleObj.AddComponent<Image>();
//            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

//            var handleRect = handleObj.GetComponent<RectTransform>();
//            handleRect.sizeDelta = new Vector2(15f, 30f);
//            handleRect.offsetMin = new Vector2(-13f, -28f);
//            handleRect.offsetMax = new Vector2(3f, -2f);

//            var sliderBarLayout = sliderObj.AddComponent<LayoutElement>();
//            sliderBarLayout.minWidth = 25;
//            sliderBarLayout.flexibleWidth = 0;
//            sliderBarLayout.minHeight = 30;
//            sliderBarLayout.flexibleHeight = 5000;

//            slider = sliderObj.AddComponent<Slider>();
//            slider.fillRect = fillObj.GetComponent<RectTransform>();
//            slider.handleRect = handleObj.GetComponent<RectTransform>();
//            slider.targetGraphic = handleImage;
//            slider.direction = Slider.Direction.BottomToTop;
//            UIFactory.SetDefaultSelectableColors(slider);

//            return sliderObj;
//        }

//        #endregion
//    }

//#if MONO
//public static class SliderExtensions
//{
//	// il2cpp can just use the orig method directly (forced public)

//	private static MethodInfo m_setMethod;
//	private static MethodInfo SetMethod
//    {
//		get
//        {
//			if (m_setMethod == null)
//            {
//				m_setMethod = typeof(Slider).GetMethod("Set", ReflectionUtility.AllFlags, null, new[] { typeof(float), typeof(bool) }, null);
//            }
//			return m_setMethod;
//        }
//	}

//	public static void Set(this Slider slider, float value, bool invokeCallback)
//	{
//		SetMethod.Invoke(slider, new object[] { value, invokeCallback });
//	}
//}
//#endif
//}