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
        // Kit palette (Drive "Mã màu"): #410E9E ink, #FD5FB0 pink, #3ED5D0 teal.
        public static readonly Color Ink = new(0.255f, 0.055f, 0.62f);
        public static readonly Color Pink = new(0.992f, 0.373f, 0.69f);
        public static readonly Color Teal = new(0.243f, 0.835f, 0.816f);
        static readonly Color Dim = new(0.45f, 0.4f, 0.6f);
        const float SliceBorder = 3f; // slice_kit panels are cut at 4x: divide the 180 px border down to ~60 canvas px
        const float ButtonHeight = 84f;
        // The console art's screen, in console pixels from its centre (import_uxui: 1576x770, screen 760x430).
        static readonly Vector2 ConsoleScreenOffset = new(18, -1), ConsoleScreenSize = new(760, 430);
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

        /// <summary>9-sliced kit sprite. <paramref name="scale"/> 3 for the 4x slice_kit art, 1 for the 1x UX/UI art.</summary>
        public static void Slice(Image image, string spriteName, float scale = SliceBorder)
        {
            image.sprite = Kit(spriteName);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = scale;
        }

        /// <summary>Kit art placed by hand (no layout): pivot = anchor, native size unless <paramref name="size"/> is given.</summary>
        public static Image Place(Transform parent, string spriteName, Vector2 anchor, Vector2 position, Vector2? size = null)
        {
            var image = Child(parent, spriteName, Color.white).GetComponent<Image>();
            image.sprite = Kit(spriteName);
            image.raycastTarget = false;
            var rect = (RectTransform)image.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            if (size is { } s) { rect.sizeDelta = s; image.preserveAspect = true; }
            else if (image.sprite != null) image.SetNativeSize();
            return image;
        }

        /// <summary>A transparent rect filling its parent: the root of a hand-placed screen.</summary>
        public static RectTransform Layer(Transform parent, string name)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            return rect;
        }

        /// <summary>Kit art stretched over its parent (backgrounds, full-screen frames).</summary>
        public static Image FullScreen(Transform parent, string spriteName)
        {
            var image = Child(parent, spriteName, Color.white).GetComponent<Image>();
            image.sprite = Kit(spriteName);
            image.raycastTarget = false;
            Stretch((RectTransform)image.transform);
            return image;
        }

        /// <summary>Round home icon at the screen's top-left corner (as in every kit mockup).</summary>
        public static Button HomeButton(Transform screen, Action onClick)
        {
            var image = Place(screen, "btn_home", Vector2.up, new Vector2(40, -40));
            image.raycastTarget = true;
            image.gameObject.name = "Home";
            var button = image.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>
        /// Handheld-console page (Settings/Pause mockups): full-screen root with the console art centred and a home
        /// icon top-left. Returns the root; <paramref name="content"/> is the vertical layout over the console's screen.
        /// </summary>
        public static RectTransform ConsolePage(Transform canvas, string name, string spriteName, Action home, out RectTransform content)
        {
            var root = Layer(canvas, name);
            var console = Place(root, spriteName, new Vector2(0.5f, 0.5f), Vector2.zero);
            var go = new GameObject("Screen", typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(console.transform, false);
            content = (RectTransform)go.transform;
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = ConsoleScreenOffset;
            content.sizeDelta = ConsoleScreenSize;
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 28, 28);
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            HomeButton(root, home);
            return root;
        }

        /// <summary>White text with an ink outline: readable on the console screen and over the 3D scene.</summary>
        public static Text Outlined(Text text, float width = 2f)
        {
            text.color = Color.white;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(width, -width);
            return text;
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
            scaler.matchWidthOrHeight = 0.5f; // the kit screens are laid out for 16:9; wide or tall windows scale on both axes
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
            layout.childForceExpandWidth = layout.childForceExpandHeight = false; // pills keep their art width
            return rect;
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
            var size = go.GetComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = ButtonHeight;
            if (art != null)
            {
                // The kit button has its text baked in; the label stays only as an accessible name.
                image.sprite = art;
                image.preserveAspect = true;
                label.enabled = false;
                size.preferredWidth = ButtonHeight * art.rect.width / art.rect.height;
            }
            else
            {
                // Blank kit pill with live text, stretched to fit the caption.
                Slice(image, "btn_blank", 1f);
                size.preferredWidth = Mathf.Max(300f, label.preferredWidth + 110f);
            }
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => onClick());
            return button;
        }

        public static Slider Slider(Transform parent, string text, float min, float max, float value, Action<float> onChange)
        {
            var row = Row(parent, text);
            var slider = BareSlider(row, text + " Slider", 44, min, max, value, onChange);
            slider.gameObject.AddComponent<LayoutElement>();
            var size = slider.GetComponent<LayoutElement>();
            size.minHeight = 44;
            size.flexibleWidth = 1;
            return slider;
        }

        /// <summary>Kit slider with no caption or layout: the caller sizes and places it. Knob and track scale with <paramref name="height"/>.</summary>
        public static Slider BareSlider(Transform parent, string name, float height, float min, float max, float value, Action<float> onChange)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var track = Child(go.transform, "Track", Color.white);
            Slice(track.GetComponent<Image>(), "slider_track");
            track.GetComponent<Image>().pixelsPerUnitMultiplier = 160f / height; // keep the round caps round
            Stretch(track);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).transform as RectTransform;
            fillArea.SetParent(go.transform, false);
            Stretch(fillArea);
            var fill = Child(fillArea, "Fill", Color.white);
            Slice(fill.GetComponent<Image>(), "slider_fill");
            fill.GetComponent<Image>().pixelsPerUnitMultiplier = 148f / height;
            Stretch(fill);
            var handleArea = new GameObject("Handle Area", typeof(RectTransform)).transform as RectTransform;
            handleArea.SetParent(go.transform, false);
            Stretch(handleArea);
            var handle = Child(handleArea, "Handle", Color.white);
            handle.GetComponent<Image>().sprite = Kit("slider_knob");
            handle.sizeDelta = new Vector2(height, height);
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
            var go = Row(parent, text).gameObject;
            go.AddComponent<Toggle>();
            var box = Child(go.transform, "Box", Color.white);
            var boxImage = box.GetComponent<Image>();
            boxImage.sprite = Kit("toggle");
            var boxSize = box.gameObject.AddComponent<LayoutElement>();
            boxSize.preferredWidth = boxSize.minWidth = 97;
            boxSize.preferredHeight = 52;
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

        /// <summary>Settings row: right-aligned caption on the left, the control (added by the caller) on the right.</summary>
        static RectTransform Row(Transform parent, string text)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var size = go.GetComponent<LayoutElement>();
            size.minHeight = 52;
            size.flexibleWidth = 1;
            var row = go.GetComponent<HorizontalLayoutGroup>();
            row.spacing = 18;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var label = Outlined(Label(go.transform, text, 26));
            label.alignment = TextAnchor.MiddleRight;
            label.GetComponent<LayoutElement>().preferredWidth = Mathf.Max(320f, label.preferredWidth);
            return (RectTransform)go.transform;
        }

        public static InputField InputField(Transform parent, string value)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
            go.GetComponent<LayoutElement>().minHeight = 64;
            go.GetComponent<LayoutElement>().flexibleWidth = 1;
            var text = Label(go.transform, "", 30);
            text.color = Color.black;
            text.supportRichText = false;
            Stretch((RectTransform)text.transform, 8);
            var field = go.GetComponent<InputField>();
            field.textComponent = text;
            field.text = value;
            return field;
        }

        /// <summary>
        /// A rect placed by pixel box (x0,y0 top-left, x1,y1 bottom-right) of a hand-drawn sheet of size <paramref name="sheet"/>:
        /// anchors are fractions of the parent, so it follows the sprite at any scale. The parent must show the sheet.
        /// </summary>
        public static RectTransform PxRect(Transform parent, string name, Vector2 sheet, float x0, float y0, float x1, float y1)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x0 / sheet.x, 1f - y1 / sheet.y);
            rect.anchorMax = new Vector2(x1 / sheet.x, 1f - y0 / sheet.y);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>Invisible button over a baked part of the art (a tab, an arrow). Named so tests and screenshots can find it.</summary>
        public static Button Hotspot(Transform parent, string name, Vector2 sheet, float x0, float y0, float x1, float y1, Action onClick)
        {
            var rect = PxRect(parent, name, sheet, x0, y0, x1, y1);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f); // alpha 0 still takes clicks
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>
        /// Makes an input field fit a thin baked bar: text overflows instead of being cut by the bar's height, and a
        /// grey hint shows while it is empty.
        /// </summary>
        public static void FitBar(InputField field, string hint, int size)
        {
            var fieldRect = (RectTransform)field.transform;
            fieldRect.anchorMin = Vector2.zero; fieldRect.anchorMax = Vector2.one; fieldRect.offsetMin = fieldRect.offsetMax = Vector2.zero;
            field.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f); // the baked bar shows through
            foreach (var text in new[] { field.textComponent, MakeHint(field, hint) })
            {
                text.fontSize = size;
                text.alignment = TextAnchor.MiddleLeft;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.raycastTarget = false;
            }
        }

        static Text MakeHint(InputField field, string hint)
        {
            var text = Label(field.transform, hint, 28);
            text.color = new Color(Ink.r, Ink.g, Ink.b, 0.45f);
            text.supportRichText = false;
            Stretch((RectTransform)text.transform, 8);
            Destroy(text.GetComponent<LayoutElement>());
            field.placeholder = text;
            return text;
        }

        /// <summary>A flat tinted overlay on a baked part of the art (selected tab, dimmed control).</summary>
        public static Image Tint(Transform parent, string name, Vector2 sheet, float x0, float y0, float x1, float y1, Color color)
        {
            var image = PxRect(parent, name, sheet, x0, y0, x1, y1).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Text filling a pixel box of the sheet, ink colour unless <paramref name="outlined"/>.</summary>
        public static Text SheetText(Transform parent, string name, Vector2 sheet, float x0, float y0, float x1, float y1,
            string text, int size, TextAnchor anchor = TextAnchor.MiddleCenter, bool outlined = false)
        {
            var label = Label(PxRect(parent, name, sheet, x0, y0, x1, y1), text, size);
            label.alignment = anchor;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch((RectTransform)label.transform);
            Destroy(label.GetComponent<LayoutElement>());
            return outlined ? Outlined(label) : label;
        }

        static void Destroy(UnityEngine.Object o) => UnityEngine.Object.Destroy(o);

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
