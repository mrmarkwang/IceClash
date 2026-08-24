/*
 * IceClash runtime mobile-control view builder.
 * Creates the safe-area-aware Unity UI hierarchy and placeholder visuals while
 * returning focused joystick/button bindings to the shared input controller.
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
        private static readonly Color PanelColor = new(0.05f, 0.12f, 0.2f, 0.72f);
        private static readonly Color PressedColor = new(0.2f, 0.68f, 1f, 0.96f);

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
                Vector2.zero, new Vector2(0.48f, 0.62f), Vector2.zero, Vector2.zero);
            joystickArea.offsetMin = new Vector2(130f, 130f);
            joystickArea.offsetMax = new Vector2(-130f, -130f);
            Image joystickHitArea = joystickArea.gameObject.AddComponent<Image>();
            joystickHitArea.color = Color.clear;

            RectTransform joystickBackground = CreateRect("JoystickBackground", joystickArea,
                Vector2.zero, Vector2.zero, new Vector2(260f, 260f), Vector2.zero);
            joystickBackground.pivot = new Vector2(0.5f, 0.5f);
            Image backgroundImage = joystickBackground.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.06f, 0.14f, 0.24f, 0.7f);
            backgroundImage.raycastTarget = false;

            RectTransform joystickHandle = CreateRect("JoystickHandle", joystickBackground,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(110f, 110f), Vector2.zero);
            Image handleImage = joystickHandle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(0.75f, 0.9f, 1f, 0.92f);
            handleImage.raycastTarget = false;

            VirtualJoystick joystick = joystickArea.gameObject.AddComponent<VirtualJoystick>();
            joystick.Configure(joystickArea, joystickBackground, joystickHandle, 130f, 0.12f);

            RectTransform actionButtons = CreateRect("ActionButtons", mobileControls,
                new Vector2(0.58f, 0f), new Vector2(1f, 0.62f), Vector2.zero, Vector2.zero);
            MobileActionButton pass = CreateButton("PassButton", "PASS", actionButtons,
                new Vector2(180f, 130f), new Vector2(-365f, 250f));
            MobileActionButton deke = CreateButton("DekeButton", "DEKE", actionButtons,
                new Vector2(190f, 140f), new Vector2(-390f, 90f));
            MobileActionButton shoot = CreateButton("ShootButton", "SHOOT", actionButtons,
                new Vector2(260f, 230f), new Vector2(-135f, 120f));

            return new MobileControlBindings(canvasObject, joystick, pass, deke, shoot);
        }

        private static MobileActionButton CreateButton(string objectName, string label, Transform parent,
            Vector2 size, Vector2 anchoredPosition)
        {
            RectTransform rect = CreateRect(objectName, parent,
                Vector2.right, Vector2.right, size, anchoredPosition);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = PanelColor;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = PanelColor;
            colors.highlightedColor = new Color(0.12f, 0.32f, 0.5f, 0.9f);
            colors.pressedColor = PressedColor;
            colors.selectedColor = PanelColor;
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            RectTransform textRect = CreateRect("Label", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text text = textRect.gameObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = label == "SHOOT" ? 44 : 36;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.raycastTarget = false;

            MobileActionButton input = rect.gameObject.AddComponent<MobileActionButton>();
            input.Configure(label, text);
            return input;
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
