/*
 * IceClash runtime mobile-control view builder.
 * Creates a safe-area-aware fixed joystick and circular action visuals with
 * generous independent hit regions, then returns shared input bindings.
 * Recent changes: anchored the stick lower-left and enlarged the unified action
 * hit/visual sizes with safe non-overlapping right-thumb spacing.
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace IceClash.UI
{
    public readonly struct MobileControlBindings
    {
        public MobileControlBindings(GameObject canvasRoot, VirtualJoystick joystick,
            MobileActionButton pass, MobileActionButton deke, MobileActionButton shoot)
        {
            CanvasRoot = canvasRoot;
            Joystick = joystick;
            Pass = pass;
            Deke = deke;
            Shoot = shoot;
        }

        public GameObject CanvasRoot { get; }
        public VirtualJoystick Joystick { get; }
        public MobileActionButton Pass { get; }
        public MobileActionButton Deke { get; }
        public MobileActionButton Shoot { get; }
    }

    public static class MobileControlsBuilder
    {
        private const int CircleTextureSize = 128;
        private static readonly Vector2 ActionButtonSize = new(220f, 220f);
        private const float ActionVisualDiameter = 200f;
        private static readonly Vector2 JoystickZoneSize = new(360f, 360f);
        private static readonly Vector2 JoystickZoneCenter = new(390f, 430f);
        private static readonly Color JoystickRingColor = new(0.04f, 0.12f, 0.2f, 0.82f);
        private static readonly Color JoystickHandleColor = new(0.76f, 0.91f, 1f, 0.96f);
        private static readonly Color ActionColor = new(0.04f, 0.11f, 0.19f, 0.84f);
        private static readonly Color HighlightedColor = new(0.12f, 0.35f, 0.54f, 0.92f);
        private static readonly Color PressedColor = new(0.18f, 0.68f, 1f, 0.98f);
        private static Sprite ringSprite;
        private static Sprite filledCircleSprite;

        public static MobileControlBindings Build(Transform parent)
        {
            EnsureEventSystem(parent);

            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform mobileControls = CreateRect("MobileControls", canvasObject.transform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            mobileControls.gameObject.AddComponent<SafeAreaFitter>();

            RectTransform joystickArea = CreateRect("JoystickArea", mobileControls,
                Vector2.zero, Vector2.zero, JoystickZoneSize, JoystickZoneCenter);
            joystickArea.pivot = new Vector2(0.5f, 0.5f);
            Image joystickHitArea = joystickArea.gameObject.AddComponent<Image>();
            joystickHitArea.color = Color.clear;

            RectTransform joystickBackground = CreateRect("JoystickBackground", joystickArea,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(260f, 260f), Vector2.zero);
            joystickBackground.pivot = new Vector2(0.5f, 0.5f);
            Image backgroundImage = joystickBackground.gameObject.AddComponent<Image>();
            backgroundImage.sprite = GetRingSprite();
            backgroundImage.color = JoystickRingColor;
            backgroundImage.preserveAspect = true;
            backgroundImage.raycastTarget = false;

            RectTransform joystickHandle = CreateRect("JoystickHandle", joystickBackground,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(110f, 110f), Vector2.zero);
            Image handleImage = joystickHandle.gameObject.AddComponent<Image>();
            handleImage.sprite = GetFilledCircleSprite();
            handleImage.color = JoystickHandleColor;
            handleImage.preserveAspect = true;
            handleImage.raycastTarget = false;

            VirtualJoystick joystick = joystickArea.gameObject.AddComponent<VirtualJoystick>();
            joystick.Configure(joystickArea, joystickBackground, joystickHandle, 130f, 0.12f);

            RectTransform actionButtons = CreateRect("ActionButtons", mobileControls,
                new Vector2(0.58f, 0f), new Vector2(1f, 0.62f), Vector2.zero, Vector2.zero);
            MobileActionButton pass = CreateButton("PassButton", "PASS", actionButtons,
                new Vector2(-400f, 360f));
            MobileActionButton deke = CreateButton("DekeButton", "DEKE", actionButtons,
                new Vector2(-410f, 120f));
            MobileActionButton shoot = CreateButton("ShootButton", "SHOOT", actionButtons,
                new Vector2(-150f, 150f));

            return new MobileControlBindings(canvasObject, joystick, pass, deke, shoot);
        }

        private static MobileActionButton CreateButton(string objectName, string label, Transform parent,
            Vector2 anchoredPosition)
        {
            RectTransform rect = CreateRect(objectName, parent,
                Vector2.right, Vector2.right, ActionButtonSize, anchoredPosition);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image hitArea = rect.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;

            RectTransform visualRect = CreateRect("Visual", rect,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(ActionVisualDiameter, ActionVisualDiameter), Vector2.zero);
            Image visual = visualRect.gameObject.AddComponent<Image>();
            visual.sprite = GetRingSprite();
            visual.color = ActionColor;
            visual.preserveAspect = true;
            visual.raycastTarget = false;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = visual;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = ActionColor;
            colors.highlightedColor = HighlightedColor;
            colors.pressedColor = PressedColor;
            colors.selectedColor = ActionColor;
            colors.disabledColor = new Color(ActionColor.r, ActionColor.g, ActionColor.b, 0.35f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            RectTransform textRect = CreateRect("Label", visualRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text text = textRect.gameObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 40;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.raycastTarget = false;

            MobileActionButton input = rect.gameObject.AddComponent<MobileActionButton>();
            input.Configure(label, text);
            return input;
        }

        private static Sprite GetRingSprite()
        {
            if (ringSprite == null) ringSprite = CreateCircleSprite("Mobile Control Ring", 54, 0.7f, 0.86f);
            return ringSprite;
        }

        private static Sprite GetFilledCircleSprite()
        {
            if (filledCircleSprite == null) filledCircleSprite = CreateCircleSprite("Mobile Control Fill", 255, 0.78f, 0.9f);
            return filledCircleSprite;
        }

        private static Sprite CreateCircleSprite(string spriteName, byte innerAlpha, float borderStart, float edgeStart)
        {
            Texture2D texture = new(CircleTextureSize, CircleTextureSize, TextureFormat.RGBA32, false, true)
            {
                name = spriteName + " Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color32[] pixels = new Color32[CircleTextureSize * CircleTextureSize];
            float center = (CircleTextureSize - 1f) * 0.5f;
            float radius = CircleTextureSize * 0.5f;
            for (int y = 0; y < CircleTextureSize; y++)
            {
                for (int x = 0; x < CircleTextureSize; x++)
                {
                    float distance = new Vector2(x - center, y - center).magnitude / radius;
                    byte alpha = EvaluateCircleAlpha(distance, innerAlpha, borderStart, edgeStart);
                    pixels[y * CircleTextureSize + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, CircleTextureSize, CircleTextureSize),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = spriteName;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static byte EvaluateCircleAlpha(float distance, byte innerAlpha, float borderStart, float edgeStart)
        {
            if (distance >= 1f) return 0;
            if (distance <= borderStart) return innerAlpha;
            if (distance <= edgeStart)
            {
                float borderBlend = Mathf.InverseLerp(borderStart, edgeStart, distance);
                return (byte)Mathf.RoundToInt(Mathf.Lerp(innerAlpha, 255f, borderBlend));
            }

            float edgeBlend = Mathf.InverseLerp(edgeStart, 1f, distance);
            return (byte)Mathf.RoundToInt(Mathf.Lerp(255f, 0f, edgeBlend));
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 size, Vector2 anchoredPosition)
        {
            GameObject child = new(objectName, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return rect;
        }

        private static void EnsureEventSystem(Transform parent)
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
            eventSystemObject.transform.SetParent(parent, false);
            InputSystemUIInputModule module = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
    }
}
