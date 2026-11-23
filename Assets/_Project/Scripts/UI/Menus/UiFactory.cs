using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SquishyBump.UI.Menus
{
    /// <summary>
    /// Builds the uGUI screens in code (no prefabs), so menus work in any scene. Skin = the team's UI kit:
    /// sprites in Art/UI/Resources/UI and the Sleeping font (credits in CREDITS.md). A missing sprite falls back to a plain look.
    /// </summary>
    public static class UiFactory
    {
        // Kit palette: deep-purple ink on the mint panels.
        public static readonly Color Ink = new(0.23f, 0.06f, 0.6f);
        static readonly Color Dim = new(0.45f, 0.4f, 0.6f);
        const float SliceBorder = 3f; // kit panels are cut at 4x: divide the 180 px border down to ~60 canvas px
        static readonly System.Collections.Generic.Dictionary<string, Sprite> sprites = new();
        static Font font;

        static Font Font => font != null ? font : font = Resources.Load<Font>("Fonts/Sleeping")
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Kit sprite by file name (no extension), or null. Cached.</summary>
        public static Sprite Kit(string name)
        {
            if (sprites.TryGetValue(name, out var cached) && cached != null) return cached;
            return sprites[name] = Resources.Load<Sprite>("UI/" + name);
        }

        /// <summary>"Exit Game" -> "exit_game".</summary>
        public static string Slug(string text) =>
            System.Text.RegularExpressions.Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');

        /// <summary>An image child showing a kit sprite, kept in proportion.</summary>
        public static Image Icon(Transform parent, string spriteName, Vector2 size)
        {
            var image = Child(parent, spriteName, Color.white).GetComponent<Image>();
            image.sprite = Kit(spriteName);
            image.preserveAspect = true;
            image.raycastTarget = false;
            ((RectTransform)image.transform).sizeDelta = size;
            return image;
        }

        public static void Slice(Image image, string spriteName)
        {
            image.sprite = Kit(spriteName);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = SliceBorder;
        }

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
            rect.sizeDelta = new Vector2(700, 900);
            Slice(go.GetComponent<Image>(), "panel_box");
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        /// <summary>Page heading: the kit's banner image when `title_<text>` exists, else a big label.</summary>
        public static void Title(Transform parent, string text, int size = 44)
        {
            if (Kit("title_" + Slug(text)) == null) { Label(parent, text, size); return; }
            var icon = Icon(parent, "title_" + Slug(text), new Vector2(0, 120));
            icon.gameObject.AddComponent<LayoutElement>().minHeight = 120;
        }

        public static Text Label(Transform parent, string text, int size = 34)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = Font;
            label.fontSize = size;
            label.color = Ink;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = text;
            go.GetComponent<LayoutElement>().minHeight = size * 1.6f;
            return label;
        }

        public static Button Button(Transform parent, string text, Action onClick)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            var art = Kit("btn_" + Slug(text));
            var label = Label(go.transform, text, 32);
            Stretch((RectTransform)label.transform);
            if (art != null)
            {
                // The kit button has its text baked in; the label stays only as an accessible name.
                image.sprite = art;
                image.preserveAspect = true;
                label.enabled = false;
                go.GetComponent<LayoutElement>().minHeight = go.GetComponent<LayoutElement>().preferredHeight = 88;
            }
            else
            {
                Slice(image, "panel_box");
                go.GetComponent<LayoutElement>().minHeight = 72;
            }
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => onClick());
            return button;
        }

        public static Slider Slider(Transform parent, string text, float min, float max, float value, Action<float> onChange)
        {
            Label(parent, text, 26);
            var go = new GameObject(text + " Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().minHeight = 44;
            var track = Child(go.transform, "Track", Color.white);
            Slice(track.GetComponent<Image>(), "slider_track");
            track.GetComponent<Image>().pixelsPerUnitMultiplier = 160f / 44f; // keep the round caps round at 44 px
            Stretch(track);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).transform as RectTransform;
            fillArea.SetParent(go.transform, false);
            Stretch(fillArea);
            var fill = Child(fillArea, "Fill", Color.white);
            Slice(fill.GetComponent<Image>(), "slider_fill");
            fill.GetComponent<Image>().pixelsPerUnitMultiplier = 148f / 44f;
            Stretch(fill);
            var handleArea = new GameObject("Handle Area", typeof(RectTransform)).transform as RectTransform;
            handleArea.SetParent(go.transform, false);
            Stretch(handleArea);
            var handle = Child(handleArea, "Handle", Color.white);
            handle.GetComponent<Image>().sprite = Kit("slider_knob");
            handle.sizeDelta = new Vector2(44, 0);
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
            go.GetComponent<LayoutElement>().minHeight = 56;
            var row = go.GetComponent<HorizontalLayoutGroup>();
            row.spacing = 16;
            row.childControlWidth = row.childControlHeight = false;
            row.childAlignment = TextAnchor.MiddleCenter;
            var box = Child(go.transform, "Box", Color.white);
            var boxImage = box.GetComponent<Image>();
            boxImage.sprite = Kit("toggle");
            box.sizeDelta = new Vector2(97, 52);
            var label = Label(go.transform, text, 26);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            ((RectTransform)label.transform).sizeDelta = new Vector2(440, 48);
            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.transition = Selectable.Transition.None;
            // The kit art is the ON pill (knob right); OFF is the same art mirrored and dimmed.
            void Look(bool on)
            {
                box.localScale = new Vector3(on ? 1f : -1f, 1f, 1f);
                boxImage.color = on ? Color.white : Dim;
            }
            toggle.isOn = value;
            Look(value);
            toggle.onValueChanged.AddListener(v => { Look(v); onChange(v); });
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
