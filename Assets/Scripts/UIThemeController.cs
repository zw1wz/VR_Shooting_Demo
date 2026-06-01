using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIThemeController : MonoBehaviour
{
    private static readonly Color OverlayColor = new Color(0.02f, 0.03f, 0.04f, 0.78f);
    private static readonly Color CardColor = new Color(0.08f, 0.095f, 0.115f, 0.92f);
    private static readonly Color AccentColor = new Color(0.12f, 0.82f, 0.72f, 1f);
    private static readonly Color WarningColor = new Color(1f, 0.72f, 0.28f, 1f);
    private static readonly Color TextColor = new Color(0.94f, 0.97f, 1f, 1f);
    private static readonly Color MutedTextColor = new Color(0.72f, 0.78f, 0.84f, 1f);
    private static readonly Color ButtonColor = new Color(0.09f, 0.72f, 0.65f, 1f);
    private static readonly Color ButtonHoverColor = new Color(0.14f, 0.86f, 0.78f, 1f);
    private static readonly Color ButtonPressedColor = new Color(0.05f, 0.48f, 0.44f, 1f);
    private static readonly Color HudPlateColor = new Color(0.03f, 0.04f, 0.05f, 0.64f);
    private static TMP_FontAsset chineseFontAsset;

    public void ApplyTheme(
        GameObject startPanel,
        GameObject waitingPanel,
        GameObject shootingPanel,
        GameObject resultPanel)
    {
        StylePanel(startPanel, new Vector2(840f, 460f));
        StylePanel(waitingPanel, new Vector2(760f, 330f));
        StylePanel(resultPanel, new Vector2(900f, 840f));
        StyleShootingPanel(shootingPanel);

        StyleStartPanel(startPanel);
        StyleWaitingPanel(waitingPanel);
        StyleResultPanel(resultPanel);
    }

    private static void StylePanel(GameObject panel, Vector2 cardSize)
    {
        if (panel == null)
        {
            return;
        }

        Image panelImage = GetOrAdd<Image>(panel);
        panelImage.color = OverlayColor;
        panelImage.raycastTarget = true;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        StretchToParent(panelRect);

        GameObject card = EnsureChild(panel.transform, "UI_Card");
        RectTransform cardRect = card.GetComponent<RectTransform>();
        CenterRect(cardRect, Vector2.zero, cardSize);
        card.transform.SetSiblingIndex(0);

        Image cardImage = GetOrAdd<Image>(card);
        cardImage.color = CardColor;
        cardImage.raycastTarget = false;

        GameObject accent = EnsureChild(card.transform, "UI_Accent");
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 5f);

        Image accentImage = GetOrAdd<Image>(accent);
        accentImage.color = AccentColor;
        accentImage.raycastTarget = false;
    }

    private static void StyleStartPanel(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        TMP_Text titleText = FindText(panel, "TitleText");
        StyleText(
            titleText,
            "快速反应\n射击训练",
            54f,
            TextColor,
            TextAlignmentOptions.Center,
            new Vector2(0f, 128f),
            new Vector2(780f, 130f),
            FontStyles.Bold
        );

        TMP_Text warningText = FindText(panel, "WaringText");
        StyleText(
            warningText,
            "等待信号，完成五发射击",
            24f,
            MutedTextColor,
            TextAlignmentOptions.Center,
            new Vector2(0f, 22f),
            new Vector2(720f, 60f),
            FontStyles.Normal
        );

        Button startButton = FindButton(panel, "StartButton");
        StyleButton(startButton, "开始训练", new Vector2(0f, -104f), new Vector2(300f, 64f));
    }

    private static void StyleWaitingPanel(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        TMP_Text waitingText = FindText(panel, "WaitingText");
        StyleText(
            waitingText,
            "准备\n等待信号",
            50f,
            WarningColor,
            TextAlignmentOptions.Center,
            new Vector2(0f, 12f),
            new Vector2(680f, 170f),
            FontStyles.Bold
        );
    }

    private static void StyleShootingPanel(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        Image panelImage = GetOrAdd<Image>(panel);
        panelImage.color = new Color(0f, 0f, 0f, 0f);
        panelImage.raycastTarget = false;

        StyleHudText(FindText(panel, "TimeText"), 0, AccentColor);
        StyleHudText(FindText(panel, "BulletText"), 1, WarningColor);
        StyleHudText(FindText(panel, "ScoreText"), 2, TextColor);
        StyleWeaponStatusText(FindText(panel, "WeaponStatusText"));
        StyleWeaponFeedbackText(FindText(panel, "WeaponFeedbackText"));
    }

    private static void StyleResultPanel(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        TMP_Text titleText = FindText(panel, "ResultTitleText");
        StyleText(
            titleText,
            "训练结果",
            42f,
            TextColor,
            TextAlignmentOptions.Center,
            new Vector2(0f, 340f),
            new Vector2(760f, 72f),
            FontStyles.Bold
        );

        TMP_Text resultText = FindText(panel, "ResultText");
        StyleText(
            resultText,
            resultText != null ? resultText.text : string.Empty,
            24f,
            TextColor,
            TextAlignmentOptions.TopLeft,
            new Vector2(0f, 52f),
            new Vector2(760f, 560f),
            FontStyles.Normal
        );

        if (resultText != null)
        {
            resultText.lineSpacing = 8f;
            resultText.enableWordWrapping = false;
        }

        Button restartButton = FindButton(panel, "RestartButton");
        StyleButton(restartButton, "重新开始", new Vector2(0f, -365f), new Vector2(280f, 64f));
    }

    private static void StyleHudText(TMP_Text text, int index, Color textColor)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(34f, -32f - index * 54f);
        rect.sizeDelta = new Vector2(290f, 42f);

        text.fontSize = 24f;
        text.enableAutoSizing = false;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = textColor;
        text.lineSpacing = 0f;
        text.raycastTarget = false;
        ApplyChineseFont(text);

        EnsureBackplate(text);
    }

    private static void StyleWeaponStatusText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-34f, -32f);
        rect.sizeDelta = new Vector2(330f, 146f);

        text.fontSize = 20f;
        text.enableAutoSizing = false;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = TextColor;
        text.lineSpacing = 4f;
        text.raycastTarget = false;
        ApplyChineseFont(text);

        EnsureBackplate(text);
    }

    private static void StyleWeaponFeedbackText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 34f);
        rect.sizeDelta = new Vector2(660f, 48f);

        text.fontSize = 21f;
        text.enableAutoSizing = false;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = WarningColor;
        text.lineSpacing = 0f;
        text.raycastTarget = false;
        ApplyChineseFont(text);

        EnsureBackplate(text);
    }

    private static void ApplyChineseFont(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        if (chineseFontAsset == null)
        {
            Font bundledFont = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");

            if (bundledFont != null)
            {
                chineseFontAsset = TMP_FontAsset.CreateFontAsset(bundledFont);
            }
        }

        if (chineseFontAsset != null)
        {
            text.font = chineseFontAsset;
        }
    }

    private static void EnsureBackplate(TMP_Text text)
    {
        Transform parent = text.transform.parent;
        GameObject plate = EnsureChild(parent, text.gameObject.name + "_Backplate");
        RectTransform plateRect = plate.GetComponent<RectTransform>();
        RectTransform textRect = text.GetComponent<RectTransform>();

        plateRect.anchorMin = textRect.anchorMin;
        plateRect.anchorMax = textRect.anchorMax;
        plateRect.pivot = textRect.pivot;
        plateRect.anchoredPosition = textRect.anchoredPosition + new Vector2(-14f, 2f);
        plateRect.sizeDelta = textRect.sizeDelta + new Vector2(28f, 8f);

        Image plateImage = GetOrAdd<Image>(plate);
        plateImage.color = HudPlateColor;
        plateImage.raycastTarget = false;

        plate.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
        plate.SetActive(text.gameObject.activeSelf);
    }

    private static void StyleText(
        TMP_Text text,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 anchoredPosition,
        Vector2 size,
        FontStyles fontStyle)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.GetComponent<RectTransform>();
        CenterRect(rect, anchoredPosition, size);

        text.text = value;
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        ApplyChineseFont(text);
    }

    private static void StyleButton(Button button, string label, Vector2 anchoredPosition, Vector2 size)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        CenterRect(rect, anchoredPosition, size);

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = ButtonColor;
            image.raycastTarget = true;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHoverColor;
        colors.pressedColor = ButtonPressedColor;
        colors.selectedColor = ButtonHoverColor;
        colors.disabledColor = new Color(0.26f, 0.3f, 0.32f, 0.65f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
        {
            buttonText.text = label;
            buttonText.fontSize = 24f;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = new Color(0.02f, 0.04f, 0.045f, 1f);
            buttonText.raycastTarget = false;
            ApplyChineseFont(buttonText);

            RectTransform textRect = buttonText.GetComponent<RectTransform>();
            StretchToParent(textRect);
        }
    }

    private static TMP_Text FindText(GameObject root, string objectName)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text.gameObject.name == objectName)
            {
                return text;
            }
        }

        return null;
    }

    private static Button FindButton(GameObject root, string objectName)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.gameObject.name == objectName)
            {
                return button;
            }
        }

        return null;
    }

    private static GameObject EnsureChild(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);

        return child;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private static void CenterRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void StretchToParent(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
