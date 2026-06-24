using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class SelectionPanelBuilder
{
    private static readonly Color OverlayColor = new Color(0.02f, 0.03f, 0.04f, 1f);
    private static readonly Color CardColor = new Color(0.08f, 0.095f, 0.115f, 0.92f);
    private static readonly Color AccentColor = new Color(0.12f, 0.82f, 0.72f, 1f);
    private static readonly Color TextColor = new Color(0.94f, 0.97f, 1f, 1f);
    private static readonly Color MutedTextColor = new Color(0.72f, 0.78f, 0.84f, 1f);
    private static readonly Color ButtonColor = new Color(0.09f, 0.72f, 0.65f, 1f);
    private static readonly Color ButtonHoverColor = new Color(0.14f, 0.86f, 0.78f, 1f);
    private static readonly Color ButtonPressedColor = new Color(0.05f, 0.48f, 0.44f, 1f);
    private static readonly Color ItemNormalColor = new Color(0.1f, 0.12f, 0.15f, 0.9f);
    private static readonly Color ItemSelectedColor = new Color(0.12f, 0.48f, 0.44f, 0.95f);
    private static readonly Color DisabledColor = new Color(0.26f, 0.3f, 0.32f, 0.65f);

    private static TMP_FontAsset _fontAsset;

    public class ModeSelectionResult
    {
        public GameObject panel;
        public Action<GameMode> onModeSelected;
        public Action onConfirm;
        public Action onBack;
    }

    public class WeaponSelectionResult
    {
        public GameObject panel;
        public Action<WeaponDefinition> onWeaponSelected;
        public Action onConfirm;
        public Action onBack;
    }

    public static ModeSelectionResult CreateModeSelectionPanel(
        Transform parent,
        List<GameMode> modes)
    {
        GameObject panel = CreatePanel(parent, "ModeSelectionPanel");
        GameObject card = CreateCard(panel.transform, new Vector2(840f, 520f));

        CreateTitle(card.transform, "选择游戏模式", new Vector2(0f, 200f), 42f);

        var result = new ModeSelectionResult();
        var buttons = new List<(Button btn, Image img, GameMode mode)>();
        GameMode selectedMode = null;

        Button confirmBtn = CreateButton(card.transform, "开始", new Vector2(0f, -215f), new Vector2(300f, 56f));
        confirmBtn.onClick.AddListener(() => result.onConfirm?.Invoke());
        SetButtonInteractable(confirmBtn, false);

        for (int i = 0; i < modes.Count; i++)
        {
            GameMode mode = modes[i];
            float y = 100f - i * 110f;

            var item = CreateSelectionItem(
                card.transform,
                mode.displayName,
                mode.description,
                new Vector2(0f, y),
                new Vector2(720f, 92f)
            );

            int capturedIndex = i;
            item.btn.onClick.AddListener(() =>
            {
                selectedMode = mode;
                SetButtonInteractable(confirmBtn, true);
                result.onModeSelected?.Invoke(mode);
                foreach (var b in buttons)
                {
                    SetItemColor(b.img, b.mode == selectedMode);
                }
            });

            buttons.Add((item.btn, item.img, mode));
        }

        result.panel = panel;

        return result;
    }

    public static WeaponSelectionResult CreateWeaponSelectionPanel(
        Transform parent,
        List<WeaponDefinition> weapons)
    {
        GameObject panel = CreatePanel(parent, "WeaponSelectionPanel");
        GameObject card = CreateCard(panel.transform, new Vector2(840f, 520f));

        CreateTitle(card.transform, "选择武器", new Vector2(0f, 200f), 42f);

        var result = new WeaponSelectionResult();
        var buttons = new List<(Button btn, Image img, WeaponDefinition weapon)>();
        WeaponDefinition selectedWeapon = null;

        Button confirmBtn = CreateButton(card.transform, "确认", new Vector2(140f, -215f), new Vector2(220f, 56f));
        confirmBtn.onClick.AddListener(() => result.onConfirm?.Invoke());
        SetButtonInteractable(confirmBtn, false);

        Button backBtn = CreateButton(card.transform, "返回", new Vector2(-140f, -215f), new Vector2(220f, 56f));
        backBtn.onClick.AddListener(() => result.onBack?.Invoke());

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponDefinition weapon = weapons[i];
            float y = 100f - i * 110f;

            var item = CreateSelectionItem(
                card.transform,
                weapon.displayName,
                weapon.description,
                new Vector2(0f, y),
                new Vector2(720f, 92f)
            );

            item.btn.onClick.AddListener(() =>
            {
                selectedWeapon = weapon;
                SetButtonInteractable(confirmBtn, true);
                result.onWeaponSelected?.Invoke(weapon);
                foreach (var b in buttons)
                {
                    SetItemColor(b.img, b.weapon == selectedWeapon);
                }
            });

            buttons.Add((item.btn, item.img, weapon));
        }

        result.panel = panel;

        return result;
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = parent.gameObject.layer;
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        panel.GetComponent<Image>().color = OverlayColor;
        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateCard(Transform parent, Vector2 size)
    {
        GameObject card = new GameObject("UI_Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.layer = parent.gameObject.layer;
        card.transform.SetParent(parent, false);

        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        card.GetComponent<Image>().color = CardColor;

        GameObject accent = new GameObject("UI_Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accent.layer = parent.gameObject.layer;
        accent.transform.SetParent(card.transform, false);

        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 5f);
        accent.GetComponent<Image>().color = AccentColor;

        return card;
    }

    private static void CreateTitle(Transform parent, string text, Vector2 position, float fontSize)
    {
        GameObject obj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);

        TMP_Text tmp = obj.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TextColor;
        tmp.raycastTarget = false;
        ApplyFont(tmp);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(780f, 80f);
    }

    private static (Button btn, Image img) CreateSelectionItem(
        Transform parent,
        string title,
        string subtitle,
        Vector2 position,
        Vector2 size)
    {
        GameObject obj = new GameObject("Item_" + title, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image bg = obj.GetComponent<Image>();
        bg.color = ItemNormalColor;

        Button btn = obj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = ItemNormalColor;
        colors.highlightedColor = new Color(0.14f, 0.2f, 0.24f, 0.95f);
        colors.pressedColor = new Color(0.08f, 0.12f, 0.15f, 0.9f);
        colors.selectedColor = ItemSelectedColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        GameObject titleObj = new GameObject("ItemTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.layer = parent.gameObject.layer;
        titleObj.transform.SetParent(obj.transform, false);

        TMP_Text titleText = titleObj.GetComponent<TMP_Text>();
        titleText.text = title;
        titleText.fontSize = 26f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = TextColor;
        titleText.raycastTarget = false;
        ApplyFont(titleText);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 0.5f);
        titleRect.anchoredPosition = new Vector2(24f, -4f);
        titleRect.sizeDelta = new Vector2(-48f, 0f);

        GameObject subObj = new GameObject("ItemSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subObj.layer = parent.gameObject.layer;
        subObj.transform.SetParent(obj.transform, false);

        TMP_Text subText = subObj.GetComponent<TMP_Text>();
        subText.text = subtitle;
        subText.fontSize = 18f;
        subText.alignment = TextAlignmentOptions.MidlineLeft;
        subText.color = MutedTextColor;
        subText.raycastTarget = false;
        ApplyFont(subText);

        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0f);
        subRect.anchorMax = new Vector2(1f, 0.5f);
        subRect.pivot = new Vector2(0f, 0.5f);
        subRect.anchoredPosition = new Vector2(24f, 4f);
        subRect.sizeDelta = new Vector2(-48f, 0f);

        return (btn, bg);
    }

    private static Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject("Btn_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.GetComponent<Image>();
        image.color = ButtonColor;

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHoverColor;
        colors.pressedColor = ButtonPressedColor;
        colors.selectedColor = ButtonHoverColor;
        colors.disabledColor = DisabledColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.layer = parent.gameObject.layer;
        textObj.transform.SetParent(obj.transform, false);

        TMP_Text text = textObj.GetComponent<TMP_Text>();
        text.text = label;
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.02f, 0.04f, 0.045f, 1f);
        text.raycastTarget = false;
        ApplyFont(text);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;

        return button;
    }

    private static void SetItemColor(Image img, bool selected)
    {
        if (selected)
        {
            img.color = ItemSelectedColor;
        }
        else
        {
            img.color = ItemNormalColor;
        }
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        button.interactable = interactable;
    }

    private static void ApplyFont(TMP_Text text)
    {
        if (text == null) return;

        if (_fontAsset == null)
        {
            Font bundledFont = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (bundledFont != null)
            {
                _fontAsset = TMP_FontAsset.CreateFontAsset(bundledFont);
            }
        }

        if (_fontAsset != null)
        {
            text.font = _fontAsset;
        }
    }
}
