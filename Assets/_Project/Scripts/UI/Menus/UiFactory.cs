using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SquishyBump.UI.Menus
{
    /// <summary>
    /// Builds the greybox uGUI screens in code (no prefab or font assets needed), so menus work in any scene.
    /// Art pass later swaps the colours and font here.
    /// </summary>
    public static class UiFactory
    {
        static readonly Color PanelColor = new(0.12f, 0.1f, 0.18f, 0.92f);
        static readonly Color ButtonColor = new(1f, 0.55f, 0.35f);
        static readonly Color TextColor = Color.white;
        static Font font;

        static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas Canvas(string name, int sortingOrder)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            return canvas;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        /// <summary>A centred vertical page.</summary>
        public static RectTransform Page(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620, 760);
            go.GetComponent<Image>().color = PanelColor;
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.spacing = 18;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        public static Text Label(Transform parent, string text, int size = 34)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = Font;
            label.fontSize = size;
            label.color = TextColor;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = text;
            go.GetComponent<LayoutElement>().minHeight = size * 1.6f;
            return label;
        }

        public static Button Button(Transform parent, string text, Action onClick)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = ButtonColor;
            go.GetComponent<LayoutElement>().minHeight = 72;
            var label = Label(go.transform, text, 32);
            Stretch((RectTransform)label.transform);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => onClick());
            return button;
        }

        public static Slider Slider(Transform parent, string text, float min, float max, float value, Action<float> onChange)
        {
            Label(parent, text, 26);
            var go = new GameObject(text + " Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().minHeight = 36;
            var track = Child(go.transform, "Track", new Color(1f, 1f, 1f, 0.25f));
            Stretch(track);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).transform as RectTransform;
            fillArea.SetParent(go.transform, false);
            Stretch(fillArea);
            var fill = Child(fillArea, "Fill", ButtonColor);
            Stretch(fill);
            var handleArea = new GameObject("Handle Area", typeof(RectTransform)).transform as RectTransform;
            handleArea.SetParent(go.transform, false);
            Stretch(handleArea);
            var handle = Child(handleArea, "Handle", Color.white);
            handle.sizeDelta = new Vector2(28, 0);
            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }

        public static Toggle Toggle(Transform parent, string text, bool value, Action<bool> onChange)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Toggle), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().minHeight = 48;
            var row = go.GetComponent<HorizontalLayoutGroup>();
            row.spacing = 16;
            row.childControlWidth = row.childControlHeight = false;
            row.childAlignment = TextAnchor.MiddleCenter;
            var box = Child(go.transform, "Box", new Color(1f, 1f, 1f, 0.25f));
            box.sizeDelta = new Vector2(40, 40);
            var check = Child(box, "Check", ButtonColor);
            Stretch(check, 6);
            var label = Label(go.transform, text, 28);
            ((RectTransform)label.transform).sizeDelta = new Vector2(380, 48);
            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(v => onChange(v));
            return toggle;
        }

        public static InputField InputField(Transform parent, string value)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
            go.GetComponent<LayoutElement>().minHeight = 64;
            var text = Label(go.transform, "", 30);
            text.color = Color.black;
            text.supportRichText = false;
            Stretch((RectTransform)text.transform, 8);
            var field = go.GetComponent<InputField>();
            field.textComponent = text;
            field.text = value;
            return field;
        }

        static RectTransform Child(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
