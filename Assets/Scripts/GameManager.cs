using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

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

    private enum GameState
    {
        Idle,
        WaitingBeep,
        Shooting,
        Result
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

    private GameState state = GameState.Idle;
    private AudioClip generatedBeepClip;
    private Coroutine roundCoroutine;
    private float startShootTime;
    private int score;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitAudio();
        InitWeaponUI();
        HideAllTargets();
        ApplyUITheme();
        ShowStartUI();

        StartCoroutine(WarmUpBeepAudio());
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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartQuickReactionDemo();
        }

        if (Input.GetKeyDown(testBeepKey))
        {
            PlayBeep();
        }

        if (Input.GetKeyDown(testHitSoundKey))
        {
            PlayMetalHit();
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
        }
    }

    private IEnumerator WaitThenBeep()
    {
        float delay = Random.Range(minBeepDelay, maxBeepDelay);
        yield return new WaitForSeconds(delay);

        PlayBeep();

        startShootTime = Time.time;
        state = GameState.Shooting;
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

        if (shotRecords.Count >= bulletCount)
        {
            EndDemo();
        }
    }

    private void EndDemo()
    {
        state = GameState.Result;
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
            bulletText.text = "已射击: " + shotRecords.Count + "/" + bulletCount;
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
        builder.AppendLine("命中次数    " + GetHitCount() + "/" + bulletCount);
        builder.AppendLine("命中率      " + GetHitRate().ToString("F1") + "%");
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
}
