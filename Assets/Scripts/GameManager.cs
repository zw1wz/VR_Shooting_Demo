using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Target Settings")]
    public Target[] targets;
    public int targetCount = 5;
    public int bulletCount = 5;

    [Header("Beep Delay")]
    public float minBeepDelay = 1f;
    public float maxBeepDelay = 3f;

    [Header("Target Spawn Range")]
    public float minX = -3f;
    public float maxX = 3f;
    public float minY = 1.2f;
    public float maxY = 2.5f;
    public float targetZ = 8f;

    [Header("Target Spacing")]
    public float minTargetDistance = 1.3f;
    public float minViewportTargetDistance = 0.18f;
    public Vector2 viewportPadding = new Vector2(0.08f, 0.08f);
    public Camera targetViewCamera;
    public int maxPositionTryCount = 120;

    [Header("Beep Audio")]
    public AudioSource beepSource;
    public AudioClip beepAudioClip;

    [Header("Hit Audio")]
    public AudioSource hitAudioSource;
    public AudioClip metalHitClip;

    [Header("Hit Audio Delay")]
    public bool useDistanceHitSoundDelay = true;
    public float soundSpeed = 343f;
    public float hitSoundDelayMultiplier = 3f;
    public float maxHitSoundDelay = 0.3f;

    [Header("Debug")]
    public KeyCode testBeepKey = KeyCode.B;
    public KeyCode testHitSoundKey = KeyCode.N;

    [Header("UI Panels")]
    public GameObject startPanel;
    public GameObject waitingPanel;
    public GameObject shootingPanel;
    public GameObject resultPanel;

    [Header("UI Text")]
    public TMP_Text timeText;
    public TMP_Text bulletText;
    public TMP_Text scoreText;
    public TMP_Text resultText;

    [Header("UI Theme")]
    public bool applyRuntimeUITheme = true;

    [Header("Weapon UI")]
    public GunShooter gunShooter;
    public bool showWeaponStatusInHud = true;
    public TMP_Text weaponStatusText;
    public TMP_Text weaponFeedbackText;

    [Header("Weapon Selection")]
    public WeaponDefinition[] availableWeapons;

    private enum GameState
    {
        ModeSelection,
        WeaponSelection,
        WaitingBeep,
        Shooting,
        Result,
    }

    private struct ShotRecord
    {
        public readonly int Index;
        public readonly float Time;
        public readonly bool Hit;
        public readonly string TargetName;

        public ShotRecord(int index, float time, bool hit, string targetName)
        {
            Index = index;
            Time = time;
            Hit = hit;
            TargetName = targetName;
        }
    }

    private readonly List<Target> activeTargets = new List<Target>();
    private readonly List<ShotRecord> shotRecords = new List<ShotRecord>();

    private GameState state = GameState.ModeSelection;
    private AudioClip generatedBeepClip;
    private Coroutine roundCoroutine;
    private float startShootTime;
    private int score;

    private GameMode selectedGameMode;
    private WeaponDefinition selectedWeapon;
    private GameObject modeSelectionPanel;
    private GameObject weaponSelectionPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!Application.isPlaying)
        {
            CreateEditorOverlay();
            return;
        }

        HideAllPanels();
        CreateSelectionPanels();
        SetPanelActive(modeSelectionPanel, true);
    }

    private void Start()
    {
        HideEditorOverlay();
        InitAudio();
        InitWeaponUI();
        HideAllTargets();
        RebindSceneButtons();
        ApplyUITheme();
        ShowModeSelectionUI();

        StartCoroutine(WarmUpBeepAudio());
    }

    private void HideEditorOverlay()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        if (canvas != null)
        {
            Transform overlay = canvas.transform.Find("EditorOverlay");
            if (overlay != null)
            {
                Destroy(overlay.gameObject);
            }
        }
    }

    private void CreateEditorOverlay()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("EditorOverlay");
        if (existing != null) return;

        GameObject overlay = new GameObject("EditorOverlay",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);

        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        Image img = overlay.GetComponent<Image>();
        img.color = new Color(0.03f, 0.05f, 0.09f, 1f);
        img.raycastTarget = false;

        GameObject accent = new GameObject("AccentBar",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accent.transform.SetParent(overlay.transform, false);

        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 5f);

        Image accentImg = accent.GetComponent<Image>();
        accentImg.color = new Color(0.12f, 0.82f, 0.72f, 1f);
        accentImg.raycastTarget = false;

        GameObject titleObj = new GameObject("EditorTitle",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
        titleObj.transform.SetParent(overlay.transform, false);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 0f);
        titleRect.sizeDelta = new Vector2(800f, 100f);

        UnityEngine.UI.Text titleText = titleObj.GetComponent<UnityEngine.UI.Text>();
        titleText.text = "虚拟射击训练系统";
        titleText.font = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
        if (titleText.font == null)
        {
            titleText.font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 48);
        }
        titleText.fontSize = 48;
        titleText.fontStyle = UnityEngine.FontStyle.Bold;
        titleText.alignment = UnityEngine.TextAnchor.MiddleCenter;
        titleText.color = new Color(0.94f, 0.97f, 1f, 1f);
        titleText.raycastTarget = false;
    }

    private void Update()
    {
        HandleDebugInput();
        UpdateWeaponUI();

        if (state == GameState.Shooting)
        {
            UpdateShootingUI();
        }
    }

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(testBeepKey))
        {
            PlayBeep();
        }

        if (Input.GetKeyDown(testHitSoundKey))
        {
            PlayMetalHit();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && state == GameState.Shooting)
        {
            EndDemo();
        }
    }

    private void InitAudio()
    {
        beepSource = EnsureAudioSource(beepSource);
        hitAudioSource = EnsureAudioSource(hitAudioSource);

        AudioListener.volume = 1f;
        AudioListener.pause = false;

        LoadClip(beepAudioClip, beepSource);
        LoadClip(metalHitClip, hitAudioSource);

        generatedBeepClip = CreateBeepClip();
    }

    private void InitWeaponUI()
    {
        if (gunShooter == null)
        {
            gunShooter = FindObjectOfType<GunShooter>();
        }

        weaponStatusText = EnsureWeaponHudText(weaponStatusText, "WeaponStatusText");
        weaponFeedbackText = EnsureWeaponHudText(weaponFeedbackText, "WeaponFeedbackText");
        UpdateWeaponUI();
    }

    private void CreateSelectionPanels()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var modeResult = SelectionPanelBuilder.CreateModeSelectionPanel(
            canvas.transform, GameModes.All);
        modeSelectionPanel = modeResult.panel;
        modeResult.onModeSelected = mode => { selectedGameMode = mode; };
        modeResult.onConfirm = ConfirmModeSelection;

        var weaponList = new List<WeaponDefinition>();
        if (availableWeapons != null)
        {
            weaponList.AddRange(availableWeapons);
        }
        if (weaponList.Count == 0)
        {
            Debug.LogWarning("No weapons assigned to GameManager.availableWeapons.");
        }

        var weaponResult = SelectionPanelBuilder.CreateWeaponSelectionPanel(
            canvas.transform, weaponList);
        weaponSelectionPanel = weaponResult.panel;
        weaponResult.onWeaponSelected = weapon => { selectedWeapon = weapon; };
        weaponResult.onConfirm = ConfirmWeaponSelection;
        weaponResult.onBack = ShowModeSelectionUI;
    }

    private void RebindSceneButtons()
    {
        if (startPanel != null)
        {
            Button startBtn = startPanel.GetComponentInChildren<Button>(true);
            if (startBtn != null)
            {
                startBtn.onClick.RemoveAllListeners();
                startBtn.onClick.AddListener(ShowModeSelectionUI);
            }
        }

        if (resultPanel != null)
        {
            Transform resultCard = resultPanel.transform.Find("UI_Card");
            Transform resultParent = resultCard != null ? resultCard : resultPanel.transform;

            // Configure existing RestartButton → 返回菜单 (right)
            Button restartBtn = null;
            Transform restartTransform = resultParent.Find("RestartButton");
            if (restartTransform == null)
            {
                restartTransform = resultPanel.transform.Find("RestartButton");
            }
            if (restartTransform != null)
            {
                restartTransform.SetParent(resultParent, false);
                restartBtn = restartTransform.GetComponent<Button>();
                RectTransform rect = restartTransform.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(130f, -365f);
                rect.sizeDelta = new Vector2(240f, 56f);
            }

            if (restartBtn != null)
            {
                restartBtn.onClick.RemoveAllListeners();
                restartBtn.onClick.AddListener(ReturnToModeSelection);

                TMP_Text label = restartBtn.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = "返回菜单";
                    ApplyChineseFontToText(label);
                }
            }

            // Create or configure RetryButton → 重新开始 (left)
            Transform retryTransform = resultParent.Find("RetryButton");
            if (retryTransform == null)
            {
                GameObject retryObj = new GameObject("RetryButton",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                retryObj.transform.SetParent(resultParent, false);
                retryObj.layer = resultPanel.layer;

                RectTransform retryRect = retryObj.GetComponent<RectTransform>();
                retryRect.anchorMin = new Vector2(0.5f, 0.5f);
                retryRect.anchorMax = new Vector2(0.5f, 0.5f);
                retryRect.pivot = new Vector2(0.5f, 0.5f);
                retryRect.anchoredPosition = new Vector2(-130f, -365f);
                retryRect.sizeDelta = new Vector2(240f, 56f);

                Image retryImg = retryObj.GetComponent<Image>();
                retryImg.color = new Color(0.09f, 0.72f, 0.65f, 1f);

                Button retryButton = retryObj.AddComponent<Button>();
                ColorBlock colors = retryButton.colors;
                colors.normalColor = new Color(0.09f, 0.72f, 0.65f, 1f);
                colors.highlightedColor = new Color(0.14f, 0.86f, 0.78f, 1f);
                colors.pressedColor = new Color(0.05f, 0.48f, 0.44f, 1f);
                colors.selectedColor = new Color(0.14f, 0.86f, 0.78f, 1f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.08f;
                retryButton.colors = colors;

                GameObject retryLabel = new GameObject("Label",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                retryLabel.transform.SetParent(retryObj.transform, false);
                retryLabel.layer = resultPanel.layer;

                TMP_Text retryText = retryLabel.GetComponent<TMP_Text>();
                retryText.text = "重新开始";
                retryText.fontSize = 24f;
                retryText.fontStyle = FontStyles.Bold;
                retryText.alignment = TextAlignmentOptions.Center;
                retryText.color = new Color(0.02f, 0.04f, 0.045f, 1f);
                retryText.raycastTarget = false;
                ApplyChineseFontToText(retryText);

                RectTransform retryLabelRect = retryLabel.GetComponent<RectTransform>();
                retryLabelRect.anchorMin = Vector2.zero;
                retryLabelRect.anchorMax = Vector2.one;
                retryLabelRect.pivot = new Vector2(0.5f, 0.5f);
                retryLabelRect.anchoredPosition = Vector2.zero;
                retryLabelRect.sizeDelta = Vector2.zero;

                retryButton.onClick.AddListener(RestartCurrentMode);

                // Copy sprite from RestartButton for visual consistency
                if (restartBtn != null)
                {
                    Image sourceImg = restartBtn.GetComponent<Image>();
                    Image newRetryImg = retryObj.GetComponent<Image>();
                    if (sourceImg != null && sourceImg.sprite != null)
                    {
                        newRetryImg.sprite = sourceImg.sprite;
                        newRetryImg.type = sourceImg.type;
                    }
                }
            }
            else
            {
                // Already exists — just fix position and label
                RectTransform retryRect = retryTransform.GetComponent<RectTransform>();
                retryRect.SetParent(resultParent, false);
                retryRect.anchoredPosition = new Vector2(-130f, -365f);
                retryRect.sizeDelta = new Vector2(240f, 56f);

                Button retryButton = retryTransform.GetComponent<Button>();
                if (retryButton != null)
                {
                    retryButton.onClick.RemoveAllListeners();
                    retryButton.onClick.AddListener(RestartCurrentMode);
                }

                TMP_Text retryText = retryTransform.GetComponentInChildren<TMP_Text>(true);
                if (retryText != null)
                {
                    retryText.text = "重新开始";
                    ApplyChineseFontToText(retryText);
                }

                if (restartBtn != null)
                {
                    Image sourceImg = restartBtn.GetComponent<Image>();
                    Image existingRetryImg = retryTransform.GetComponent<Image>();
                    if (sourceImg != null && sourceImg.sprite != null && existingRetryImg != null)
                    {
                        existingRetryImg.sprite = sourceImg.sprite;
                        existingRetryImg.type = sourceImg.type;
                    }
                }
            }
        }
    }

    private void ShowModeSelectionUI()
    {
        if (roundCoroutine != null)
        {
            StopCoroutine(roundCoroutine);
            roundCoroutine = null;
        }

        HideAllTargets();
        state = GameState.ModeSelection;
        HideAllPanels();
        ResetSelectionPanel(modeSelectionPanel);
        SetPanelActive(modeSelectionPanel, true);
    }

    private void ShowWeaponSelectionUI()
    {
        state = GameState.WeaponSelection;
        HideAllPanels();
        ResetSelectionPanel(weaponSelectionPanel);
        SetPanelActive(weaponSelectionPanel, true);
    }

    private static void ResetSelectionPanel(GameObject panel)
    {
        if (panel == null) return;

        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn.gameObject.name.StartsWith("Btn_"))
            {
                btn.interactable = false;
            }
            else if (btn.gameObject.name.StartsWith("Item_"))
            {
                Image img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.1f, 0.12f, 0.15f, 0.9f);
                }
            }
        }
    }

    private void ConfirmModeSelection()
    {
        if (selectedGameMode == null)
        {
            return;
        }
        ShowWeaponSelectionUI();
    }

    private void ConfirmWeaponSelection()
    {
        if (selectedWeapon == null)
        {
            return;
        }

        ApplySelectedWeapon();
        StartGameWithMode();
    }

    private void ApplySelectedWeapon()
    {
        if (selectedWeapon != null && gunShooter != null)
        {
            gunShooter.LoadWeapon(selectedWeapon);
        }
    }

    private void StartGameWithMode()
    {
        if (selectedGameMode == null)
        {
            return;
        }

        bulletCount = selectedGameMode.bulletCount;
        targetCount = selectedGameMode.targetCount;

        if (selectedGameMode.useBeepCountdown)
        {
            StartQuickReactionDemo();
        }
        else
        {
            StartFreeShoot();
        }
    }

    private void StartFreeShoot()
    {
        if (roundCoroutine != null)
        {
            StopCoroutine(roundCoroutine);
            roundCoroutine = null;
        }

        ResetRound();
        HideAllTargets();
        ShowRandomTargets();

        state = GameState.Shooting;
        startShootTime = Time.time;

        if (gunShooter != null)
        {
            gunShooter.BeginTrainingMetrics();
        }

        ShowShootingUI();
    }

    private void ReturnToModeSelection()
    {
        if (roundCoroutine != null)
        {
            StopCoroutine(roundCoroutine);
            roundCoroutine = null;
        }

        ResetRound();
        HideAllTargets();

        if (gunShooter != null)
        {
            gunShooter.UnloadWeapon();
        }

        ShowModeSelectionUI();
    }

    private void RestartCurrentMode()
    {
        if (selectedGameMode == null || selectedWeapon == null)
        {
            ReturnToModeSelection();
            return;
        }

        ApplySelectedWeapon();
        StartGameWithMode();
    }

    private static TMP_FontAsset _chineseFont;

    private static void ApplyChineseFontToText(TMP_Text text)
    {
        if (text == null) return;

        if (_chineseFont == null)
        {
            Font bundledFont = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (bundledFont != null)
            {
                _chineseFont = TMP_FontAsset.CreateFontAsset(bundledFont);
            }
        }

        if (_chineseFont != null)
        {
            text.font = _chineseFont;
        }
    }

    private TMP_Text EnsureWeaponHudText(TMP_Text currentText, string objectName)
    {
        if (currentText != null || shootingPanel == null)
        {
            return currentText;
        }

        Transform existing = shootingPanel.transform.Find(objectName);
        if (existing != null)
        {
            return existing.GetComponent<TMP_Text>();
        }

        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        textObject.layer = shootingPanel.layer;
        textObject.transform.SetParent(shootingPanel.transform, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.raycastTarget = false;
        return text;
    }

    private AudioSource EnsureAudioSource(AudioSource source)
    {
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = false;
        source.volume = 1f;
        source.mute = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.priority = 0;

        return source;
    }

    private static void LoadClip(AudioClip clip, AudioSource source)
    {
        if (clip == null || source == null)
        {
            return;
        }

        clip.LoadAudioData();
        source.clip = clip;
    }

    private IEnumerator WarmUpBeepAudio()
    {
        yield return new WaitForSecondsRealtime(0.2f);

        AudioClip clip = GetBeepClip();
        if (beepSource == null || clip == null)
        {
            yield break;
        }

        beepSource.Stop();
        beepSource.volume = 0.01f;
        beepSource.PlayOneShot(clip, 0.01f);

        yield return new WaitForSecondsRealtime(0.15f);

        beepSource.Stop();
        beepSource.volume = 1f;
    }

    public bool CanShoot()
    {
        return state == GameState.Shooting;
    }

    public bool CanControlWeapon()
    {
        return state == GameState.WaitingBeep || state == GameState.Shooting;
    }

    public void StartQuickReactionDemo()
    {
        if (roundCoroutine != null)
        {
            StopCoroutine(roundCoroutine);
            roundCoroutine = null;
        }

        ResetRound();
        HideAllTargets();
        ShowRandomTargets();

        state = GameState.WaitingBeep;
        ShowWaitingUI();

        roundCoroutine = StartCoroutine(WaitThenBeep());
    }

    private void ResetRound()
    {
        score = 0;
        activeTargets.Clear();
        shotRecords.Clear();

        if (gunShooter != null)
        {
            gunShooter.ResetWeaponState();
            gunShooter.ResetTrainingMetrics();
        }
    }

    private IEnumerator WaitThenBeep()
    {
        float delay = Random.Range(minBeepDelay, maxBeepDelay);
        yield return new WaitForSeconds(delay);

        PlayBeep();

        startShootTime = Time.time;
        state = GameState.Shooting;

        if (gunShooter != null)
        {
            gunShooter.BeginTrainingMetrics();
        }

        ShowShootingUI();
        roundCoroutine = null;
    }

    private void ShowRandomTargets()
    {
        if (targets == null || targets.Length == 0)
        {
            Debug.LogError("Targets are not assigned.");
            return;
        }

        List<Target> targetPool = new List<Target>(targets.Length);
        foreach (Target target in targets)
        {
            if (target != null)
            {
                targetPool.Add(target);
            }
        }

        int count = Mathf.Min(targetCount, targetPool.Count);
        List<Vector3> usedPositions = new List<Vector3>(count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, targetPool.Count);
            Target target = targetPool[randomIndex];
            targetPool.RemoveAt(randomIndex);

            Vector3 position = GetNonOverlapPosition(usedPositions);
            target.transform.position = position;
            target.gameObject.SetActive(true);

            usedPositions.Add(position);
            activeTargets.Add(target);
        }
    }

    private Vector3 GetNonOverlapPosition(List<Vector3> usedPositions)
    {
        Vector3 bestCandidate = GetRandomTargetPosition();
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < maxPositionTryCount; i++)
        {
            Vector3 candidate = GetRandomTargetPosition();
            float candidateScore = GetPositionScore(candidate, usedPositions);

            if (candidateScore > bestScore)
            {
                bestScore = candidateScore;
                bestCandidate = candidate;
            }

            if (IsValidTargetPosition(candidate, usedPositions))
            {
                return candidate;
            }
        }

        return bestCandidate;
    }

    private Vector3 GetRandomTargetPosition()
    {
        return new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            targetZ
        );
    }

    private bool IsValidTargetPosition(Vector3 candidate, List<Vector3> usedPositions)
    {
        return !IsTooCloseInWorld(candidate, usedPositions)
            && !IsTooCloseInViewport(candidate, usedPositions);
    }

    private bool IsTooCloseInWorld(Vector3 candidate, List<Vector3> usedPositions)
    {
        Vector2 candidatePosition = new Vector2(candidate.x, candidate.y);

        foreach (Vector3 usedPosition in usedPositions)
        {
            Vector2 usedPosition2D = new Vector2(usedPosition.x, usedPosition.y);
            if (Vector2.Distance(candidatePosition, usedPosition2D) < minTargetDistance)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsTooCloseInViewport(Vector3 candidate, List<Vector3> usedPositions)
    {
        Camera viewCamera = GetTargetViewCamera();
        if (viewCamera == null)
        {
            return false;
        }

        Vector3 candidateViewportPosition = viewCamera.WorldToViewportPoint(candidate);
        if (!IsInsideSafeViewport(candidateViewportPosition))
        {
            return true;
        }

        foreach (Vector3 usedPosition in usedPositions)
        {
            Vector3 usedViewportPosition = viewCamera.WorldToViewportPoint(usedPosition);
            Vector2 candidatePoint = new Vector2(candidateViewportPosition.x, candidateViewportPosition.y);
            Vector2 usedPoint = new Vector2(usedViewportPosition.x, usedViewportPosition.y);

            if (Vector2.Distance(candidatePoint, usedPoint) < minViewportTargetDistance)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInsideSafeViewport(Vector3 viewportPosition)
    {
        return viewportPosition.z > 0f
            && viewportPosition.x >= viewportPadding.x
            && viewportPosition.x <= 1f - viewportPadding.x
            && viewportPosition.y >= viewportPadding.y
            && viewportPosition.y <= 1f - viewportPadding.y;
    }

    private float GetPositionScore(Vector3 candidate, List<Vector3> usedPositions)
    {
        Camera viewCamera = GetTargetViewCamera();

        if (usedPositions.Count == 0)
        {
            if (viewCamera == null)
            {
                return 1f;
            }

            Vector3 firstViewportPosition = viewCamera.WorldToViewportPoint(candidate);
            return IsInsideSafeViewport(firstViewportPosition) ? 1f : -1f;
        }

        float nearestWorldDistance = float.PositiveInfinity;
        foreach (Vector3 usedPosition in usedPositions)
        {
            float distance = Vector2.Distance(
                new Vector2(candidate.x, candidate.y),
                new Vector2(usedPosition.x, usedPosition.y)
            );

            nearestWorldDistance = Mathf.Min(nearestWorldDistance, distance);
        }

        if (viewCamera == null)
        {
            return nearestWorldDistance;
        }

        Vector3 candidateViewportPosition = viewCamera.WorldToViewportPoint(candidate);
        float viewportScore = IsInsideSafeViewport(candidateViewportPosition) ? 1f : -1f;
        float nearestViewportDistance = float.PositiveInfinity;

        foreach (Vector3 usedPosition in usedPositions)
        {
            Vector3 usedViewportPosition = viewCamera.WorldToViewportPoint(usedPosition);
            Vector2 candidatePoint = new Vector2(candidateViewportPosition.x, candidateViewportPosition.y);
            Vector2 usedPoint = new Vector2(usedViewportPosition.x, usedViewportPosition.y);

            nearestViewportDistance = Mathf.Min(
                nearestViewportDistance,
                Vector2.Distance(candidatePoint, usedPoint)
            );
        }

        return nearestWorldDistance + viewportScore + nearestViewportDistance * 10f;
    }

    private Camera GetTargetViewCamera()
    {
        return targetViewCamera != null ? targetViewCamera : Camera.main;
    }

    private void HideAllTargets()
    {
        if (targets == null)
        {
            return;
        }

        foreach (Target target in targets)
        {
            if (target != null)
            {
                target.gameObject.SetActive(false);
            }
        }
    }

    public void RecordShot(Target hitTarget, float hitDistance)
    {
        if (state != GameState.Shooting)
        {
            return;
        }

        float shotTime = Time.time - startShootTime;
        bool hitActiveTarget = hitTarget != null
            && hitTarget.gameObject.activeSelf
            && activeTargets.Contains(hitTarget);

        string hitName = "None";

        if (hitActiveTarget)
        {
            hitName = hitTarget.gameObject.name;
            score += hitTarget.score;

            PlayMetalHitByDistance(hitDistance);

            activeTargets.Remove(hitTarget);
            hitTarget.gameObject.SetActive(false);
        }

        shotRecords.Add(new ShotRecord(
            shotRecords.Count + 1,
            shotTime,
            hitActiveTarget,
            hitName
        ));

        if (bulletCount > 0 && shotRecords.Count >= bulletCount)
        {
            EndDemo();
        }
    }

    private void EndDemo()
    {
        state = GameState.Result;

        if (gunShooter != null)
        {
            gunShooter.EndTrainingMetrics();
        }

        HideAllTargets();
        ShowResultUI();
    }

    private AudioClip GetBeepClip()
    {
        return beepAudioClip != null ? beepAudioClip : generatedBeepClip;
    }

    private void PlayBeep()
    {
        AudioClip clip = GetBeepClip();
        if (beepSource == null || clip == null)
        {
            Debug.LogWarning("Beep audio is not ready.");
            return;
        }

        PlayOneShot(beepSource, clip);
    }

    private void PlayMetalHitByDistance(float distance)
    {
        if (!useDistanceHitSoundDelay)
        {
            PlayMetalHit();
            return;
        }

        float delay = soundSpeed > 0f ? distance / soundSpeed : 0f;
        delay *= hitSoundDelayMultiplier;
        delay = Mathf.Clamp(delay, 0f, maxHitSoundDelay);

        StartCoroutine(PlayMetalHitAfterDelay(delay));
    }

    private IEnumerator PlayMetalHitAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        PlayMetalHit();
    }

    private void PlayMetalHit()
    {
        if (hitAudioSource == null || metalHitClip == null)
        {
            Debug.LogWarning("Hit audio is not ready.");
            return;
        }

        PlayOneShot(hitAudioSource, metalHitClip);
    }

    private static void PlayOneShot(AudioSource source, AudioClip clip)
    {
        source.Stop();
        source.volume = 1f;
        source.mute = false;
        source.spatialBlend = 0f;
        source.PlayOneShot(clip, 1f);
    }

    private AudioClip CreateBeepClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.45f;
        const float frequency = 1200f;

        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float sample = Mathf.Sin(2 * Mathf.PI * frequency * t);
            float fadeIn = Mathf.Clamp01(t / 0.02f);
            float fadeOut = Mathf.Clamp01((duration - t) / 0.08f);
            float envelope = Mathf.Min(fadeIn, fadeOut);

            samples[i] = sample * 0.9f * envelope;
        }

        AudioClip clip = AudioClip.Create("Generated_Beep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);

        return clip;
    }

    private int GetHitCount()
    {
        int hitCount = 0;

        foreach (ShotRecord record in shotRecords)
        {
            if (record.Hit)
            {
                hitCount++;
            }
        }

        return hitCount;
    }

    private float GetHitRate()
    {
        if (shotRecords.Count == 0)
        {
            return 0f;
        }

        return (float)GetHitCount() / shotRecords.Count * 100f;
    }

    private void HideAllPanels()
    {
        SetPanelActive(startPanel, false);
        SetPanelActive(waitingPanel, false);
        SetPanelActive(shootingPanel, false);
        SetPanelActive(resultPanel, false);
        SetPanelActive(modeSelectionPanel, false);
        SetPanelActive(weaponSelectionPanel, false);
    }

    private void ApplyUITheme()
    {
        if (!applyRuntimeUITheme)
        {
            return;
        }

        UIThemeController themeController = GetComponent<UIThemeController>();
        if (themeController == null)
        {
            themeController = gameObject.AddComponent<UIThemeController>();
        }

        themeController.ApplyTheme(startPanel, waitingPanel, shootingPanel, resultPanel);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private void ShowStartUI()
    {
        HideAllPanels();
        SetPanelActive(startPanel, true);
    }

    private void ShowWaitingUI()
    {
        HideAllPanels();
        SetPanelActive(waitingPanel, true);
    }

    private void ShowShootingUI()
    {
        HideAllPanels();
        SetPanelActive(shootingPanel, true);
        UpdateShootingUI();
    }

    private void ShowResultUI()
    {
        HideAllPanels();
        SetPanelActive(resultPanel, true);
        UpdateResultUI();
    }

    private void UpdateShootingUI()
    {
        float currentTime = Time.time - startShootTime;

        if (timeText != null)
        {
            timeText.text = "时间: " + currentTime.ToString("F3") + " 秒";
        }

        if (bulletText != null)
        {
            string bulletDisplay = bulletCount > 0
                ? shotRecords.Count + "/" + bulletCount
                : shotRecords.Count.ToString();
            bulletText.text = "已射击: " + bulletDisplay;
        }

        if (scoreText != null)
        {
            scoreText.text = GetScoreStatusText();
        }
    }

    private string GetScoreStatusText()
    {
        if (!showWeaponStatusInHud || gunShooter == null || weaponStatusText != null)
        {
            return "得分: " + score;
        }

        return "得分: " + score + "\n" + gunShooter.GetWeaponStatusText();
    }

    private void UpdateWeaponUI()
    {
        if (weaponStatusText != null)
        {
            bool showWeaponStatus = showWeaponStatusInHud && gunShooter != null;
            weaponStatusText.text = showWeaponStatus
                ? gunShooter.GetWeaponHudText()
                : string.Empty;
            SetHudTextActive(weaponStatusText, showWeaponStatus);
        }

        if (weaponFeedbackText != null)
        {
            weaponFeedbackText.text = showWeaponStatusInHud && gunShooter != null
                ? gunShooter.GetWeaponFeedbackText()
                : string.Empty;
            SetHudTextActive(weaponFeedbackText, !string.IsNullOrEmpty(weaponFeedbackText.text));
        }
    }

    private static void SetHudTextActive(TMP_Text text, bool active)
    {
        text.gameObject.SetActive(active);

        Transform backplate = text.transform.parent.Find(text.gameObject.name + "_Backplate");
        if (backplate != null)
        {
            backplate.gameObject.SetActive(active);
        }
    }

    private void UpdateResultUI()
    {
        if (resultText == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("最终得分    " + score);
        builder.AppendLine("命中次数    " + GetHitCount() + (bulletCount > 0 ? "/" + bulletCount : ""));
        builder.AppendLine("命中率      " + GetHitRate().ToString("F1") + "%");
        AppendWeaponTrainingMetrics(builder);
        builder.AppendLine();
        builder.AppendLine("射击记录");
        builder.AppendLine("------------------------------------------------");

        foreach (ShotRecord record in shotRecords)
        {
            string result = record.Hit ? "命中 " + record.TargetName : "未命中";
            builder
                .Append("#").Append(record.Index.ToString("00"))
                .Append("    ").Append(record.Time.ToString("F3")).Append("s")
                .Append("    ").AppendLine(result);
        }

        resultText.text = builder.ToString();
    }

    private void AppendWeaponTrainingMetrics(StringBuilder builder)
    {
        if (gunShooter == null)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("枪械操作");
        builder.AppendLine("有效射击    " + gunShooter.FiredShotCount);
        builder.AppendLine("空击次数    " + gunShooter.DryFireCount);
        builder.AppendLine("误操作次数  " + gunShooter.OperationErrorCount);
        builder.AppendLine("最快换弹    " + FormatMetricDuration(gunShooter.BestReloadTime));
        builder.AppendLine("空仓处理    " + FormatMetricDuration(gunShooter.BestSlideLockRecoveryTime));
    }

    private static string FormatMetricDuration(float duration)
    {
        return duration >= 0f ? duration.ToString("F3") + " 秒" : "--";
    }
}
