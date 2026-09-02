using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameDevStudio.UI
{
    public static class UiFactory
    {
        public static readonly Color PanelColor = new Color(0.16f, 0.12f, 0.15f, 0.94f);
        public static readonly Color PanelInner = new Color(0.22f, 0.17f, 0.2f, 0.96f);
        public static readonly Color Accent = new Color(0.36f, 0.72f, 0.48f, 1f);
        public static readonly Color Danger = new Color(0.78f, 0.32f, 0.28f, 1f);
        public static readonly Color ButtonColor = new Color(0.32f, 0.24f, 0.3f, 1f);
        public static readonly Color TextColor = new Color(0.95f, 0.92f, 0.88f, 1f);
        public static readonly Color Muted = new Color(0.72f, 0.66f, 0.62f, 1f);

        static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null)
                    {
                        _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }
                }

                return _font;
            }
        }

        public static Canvas CreateCanvas(Transform parent)
        {
            var go = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static UnityEngine.UI.Image MakeImage(Transform parent, Color color, string name = "Image")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = true;
            Stretch(go.GetComponent<RectTransform>());
            return image;
        }

        public static Text Label(Transform parent, string text, int size, TextAnchor anchor, Color? color = null, string name = "Label")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.alignment = anchor;
            label.color = color ?? TextColor;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            Stretch(go.GetComponent<RectTransform>());
            return label;
        }

        public static UnityEngine.UI.Button ButtonWidget(Transform parent, string caption, UnityAction onClick, Color? color = null)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color ?? ButtonColor;
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            Label(go.transform, caption, 22, TextAnchor.MiddleCenter);
            return button;
        }

        public static UnityEngine.UI.Image FillBar(Transform parent, Color fill)
        {
            UnityEngine.UI.Image bg = MakeImage(parent, new Color(0.08f, 0.07f, 0.08f, 1f), "Bar");
            bg.raycastTarget = false;
            UnityEngine.UI.Image value = MakeImage(bg.transform, fill, "Fill");
            value.raycastTarget = false;
            value.type = UnityEngine.UI.Image.Type.Filled;
            value.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            value.fillAmount = 0f;
            var rect = value.rectTransform;
            rect.offsetMin = new Vector2(3f, 3f);
            rect.offsetMax = new Vector2(-3f, -3f);
            return value;
        }

        public static RectTransform Panel(Transform parent, string name, Color? color = null)
        {
            return MakeImage(parent, color ?? PanelColor, name).rectTransform;
        }

        public static ScrollRect ScrollColumn(Transform parent, out RectTransform content)
        {
            var root = new GameObject("Scroll", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(ScrollRect));
            root.transform.SetParent(parent, false);
            var image = root.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(0f, 0f, 0f, 0.08f);
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(Mask));
            viewportGo.transform.SetParent(root.transform, false);
            viewportGo.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;
            Stretch(viewportGo.GetComponent<RectTransform>());

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 8f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            scroll.content = contentRect;
            Stretch(root.GetComponent<RectTransform>());
            content = contentRect;
            return scroll;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
