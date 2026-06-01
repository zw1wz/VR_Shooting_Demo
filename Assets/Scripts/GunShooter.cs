using UnityEngine;

public class GunShooter : MonoBehaviour
{
    public Transform muzzlePoint;
    public float shootDistance = 100f;
    public PistolStateMachine pistolState;
    public MonoBehaviour weaponInputSource;

    [Header("Fire Rate")]
    public float fireRateRoundsPerMinute = 300f;
    public float shotInputBufferTime = 0.15f;

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlash;

    [Header("Hit Effect")]
    public GameObject hitEffectPrefab;
    public float hitEffectLifeTime = 0.5f;

    [Header("3D Aim Dot")]
    public GameObject aimDot;
    public float aimDotOffset = 0.03f;

    [Header("Gun Audio")]
    public AudioSource gunAudioSource;
    public AudioClip gunShotClip;
    public AudioClip dryFireClip;

    [Header("Weapon Feedback")]
    public float feedbackMessageDuration = 1.4f;

    private IWeaponInput weaponInput;
    private AudioClip generatedDryFireClip;
    private float nextAllowedShootTime;
    private float bufferedShotExpireTime = -1f;
    private string weaponFeedbackText = string.Empty;
    private float weaponFeedbackExpireTime = -1f;

    private void Start()
    {
        InitPistolState();
        InitWeaponInput();
        InitGunAudio();
    }

    private void Update()
    {
        UpdateAimDot();
        HandleWeaponOperationInput();

        if (weaponInput != null && weaponInput.TriggerPressedThisFrame)
        {
            BufferShotInput();
        }

        TryConsumeBufferedShot();

        if (weaponInput == null || !weaponInput.TriggerHeld)
        {
            ReleaseTrigger();
        }
    }

    private void InitPistolState()
    {
        if (pistolState == null)
        {
            pistolState = GetComponent<PistolStateMachine>();
        }

        if (pistolState == null)
        {
            pistolState = gameObject.AddComponent<PistolStateMachine>();
        }
    }

    private void InitWeaponInput()
    {
        if (weaponInputSource == null)
        {
            weaponInputSource = GetComponent<MouseKeyboardWeaponInput>();
        }

        if (weaponInputSource == null)
        {
            weaponInputSource = gameObject.AddComponent<MouseKeyboardWeaponInput>();
        }

        weaponInput = weaponInputSource as IWeaponInput;
        if (weaponInput == null)
        {
            Debug.LogError("Weapon input source must implement IWeaponInput.");
        }
    }

    private void InitGunAudio()
    {
        if (gunAudioSource == null)
        {
            gunAudioSource = GetComponent<AudioSource>();
        }

        if (gunAudioSource == null)
        {
            gunAudioSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureAudioSource(gunAudioSource);

        if (gunShotClip != null)
        {
            gunShotClip.LoadAudioData();
            gunAudioSource.clip = gunShotClip;
        }

        if (dryFireClip != null)
        {
            dryFireClip.LoadAudioData();
        }
        else
        {
            generatedDryFireClip = CreateDryFireClip();
        }
    }

    private void HandleWeaponOperationInput()
    {
        if (pistolState == null || weaponInput == null)
        {
            return;
        }

        if (weaponInput.InsertMagazinePressedThisFrame)
        {
            pistolState.InsertFullMagazine();
            ShowWeaponFeedback("已插入满弹匣");
        }

        if (weaponInput.RemoveMagazinePressedThisFrame)
        {
            ShowWeaponFeedback(
                pistolState.RemoveMagazine()
                    ? "已拔出弹匣"
                    : "当前没有可拔出的弹匣"
            );
        }

        if (weaponInput.SlidePulledThisFrame)
        {
            bool roundEjected = pistolState.RoundInChamber;
            ShowWeaponFeedback(
                pistolState.PullSlide()
                    ? roundEjected
                        ? "已拉动枪机，退出膛内弹"
                        : "已拉动枪机"
                    : "枪机已经处于后拉状态"
            );
        }

        if (weaponInput.SlideReleasedThisFrame)
        {
            ShowWeaponFeedback(
                pistolState.ReleaseSlide()
                    ? pistolState.RoundInChamber
                        ? "已释放枪机，子弹上膛"
                        : "已释放枪机，膛内无弹"
                    : "枪机尚未后拉"
            );
        }

        if (weaponInput.SlideLockReleasedThisFrame)
        {
            ShowWeaponFeedback(
                pistolState.ReleaseSlideLock()
                    ? pistolState.RoundInChamber
                        ? "已解除空仓挂机，子弹上膛"
                        : "已解除空仓挂机，膛内无弹"
                    : "当前没有空仓挂机"
            );
        }
    }

    private static void ConfigureAudioSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.volume = 1f;
        source.mute = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.priority = 0;
    }

    private Ray GetMuzzleRay()
    {
        Transform raySource = muzzlePoint != null ? muzzlePoint : transform;
        return new Ray(raySource.position, raySource.forward);
    }

    private void UpdateAimDot()
    {
        if (aimDot == null)
        {
            return;
        }

        Ray ray = GetMuzzleRay();
        aimDot.SetActive(true);

        if (Physics.Raycast(ray, out RaycastHit hit, shootDistance))
        {
            aimDot.transform.position = hit.point + hit.normal * aimDotOffset;

            if (Camera.main != null)
            {
                aimDot.transform.LookAt(Camera.main.transform);
            }

            return;
        }

        aimDot.transform.position = ray.origin + ray.direction * shootDistance;
    }

    private void BufferShotInput()
    {
        bufferedShotExpireTime = Time.time + Mathf.Max(0f, shotInputBufferTime);
    }

    private void TryConsumeBufferedShot()
    {
        if (bufferedShotExpireTime < 0f)
        {
            return;
        }

        if (Time.time > bufferedShotExpireTime)
        {
            ClearBufferedShot();
            return;
        }

        if (GameManager.Instance == null || !GameManager.Instance.CanShoot())
        {
            ClearBufferedShot();
            return;
        }

        if (!CanShootByFireRate())
        {
            return;
        }

        if (pistolState == null)
        {
            InitPistolState();
        }

        ClearBufferedShot();

        PistolTriggerResult result = pistolState.PressTrigger();
        if (result == PistolTriggerResult.Fired)
        {
            Shoot();
            return;
        }

        if (result == PistolTriggerResult.DryFire)
        {
            PlayDryFire();
        }

        ShowWeaponFeedback(pistolState.GetTriggerFeedbackText(result));
    }

    private void ClearBufferedShot()
    {
        bufferedShotExpireTime = -1f;
    }

    private void ReleaseTrigger()
    {
        if (pistolState != null)
        {
            pistolState.ReleaseTrigger();
        }
    }

    private void Shoot()
    {
        nextAllowedShootTime = Time.time + GetShotCooldown();

        PlayGunShot();

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        Target hitTarget = null;
        float hitDistance = 0f;
        Ray ray = GetMuzzleRay();

        if (Physics.Raycast(ray, out RaycastHit hit, shootDistance))
        {
            hitTarget = hit.collider.GetComponentInParent<Target>();
            hitDistance = hit.distance;
            SpawnHitEffect(hit);
        }

        GameManager.Instance.RecordShot(hitTarget, hitDistance);

        if (pistolState != null && pistolState.SlideLocked)
        {
            ShowWeaponFeedback("弹匣已空，进入空仓挂机");
        }
    }

    private bool CanShootByFireRate()
    {
        return Time.time >= nextAllowedShootTime;
    }

    public string GetWeaponStatusText()
    {
        if (pistolState == null)
        {
            return "武器未连接";
        }

        return pistolState.GetStatusLine();
    }

    public string GetWeaponHudText()
    {
        if (pistolState == null)
        {
            return "武器未连接";
        }

        return pistolState.GetHudText();
    }

    public string GetWeaponFeedbackText()
    {
        if (Time.time > weaponFeedbackExpireTime)
        {
            return string.Empty;
        }

        return weaponFeedbackText;
    }

    public void ResetWeaponState()
    {
        ClearBufferedShot();
        ClearWeaponFeedback();
        nextAllowedShootTime = 0f;

        if (pistolState == null)
        {
            InitPistolState();
        }

        if (pistolState != null)
        {
            pistolState.ResetToConfiguredState();
        }
    }

    private float GetShotCooldown()
    {
        float roundsPerMinute = GetEffectiveFireRate();
        if (roundsPerMinute <= 0f)
        {
            return 0f;
        }

        return 60f / roundsPerMinute;
    }

    private float GetEffectiveFireRate()
    {
        if (pistolState != null
            && pistolState.config != null
            && pistolState.config.FireRateRoundsPerMinute > 0f)
        {
            return pistolState.config.FireRateRoundsPerMinute;
        }

        return fireRateRoundsPerMinute;
    }

    private void SpawnHitEffect(RaycastHit hit)
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(
            hitEffectPrefab,
            hit.point,
            Quaternion.LookRotation(hit.normal)
        );

        Destroy(effect, Mathf.Max(0.01f, hitEffectLifeTime));
    }

    private void PlayGunShot()
    {
        if (gunAudioSource == null || gunShotClip == null)
        {
            return;
        }

        gunAudioSource.Stop();
        gunAudioSource.volume = 1f;
        gunAudioSource.mute = false;
        gunAudioSource.spatialBlend = 0f;
        gunAudioSource.PlayOneShot(gunShotClip, 1f);
    }

    private void PlayDryFire()
    {
        AudioClip clip = dryFireClip != null ? dryFireClip : generatedDryFireClip;
        if (gunAudioSource == null || clip == null)
        {
            return;
        }

        gunAudioSource.Stop();
        gunAudioSource.volume = 1f;
        gunAudioSource.mute = false;
        gunAudioSource.spatialBlend = 0f;
        gunAudioSource.PlayOneShot(clip, 1f);
    }

    private void ShowWeaponFeedback(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        weaponFeedbackText = message;
        weaponFeedbackExpireTime = Time.time + Mathf.Max(0f, feedbackMessageDuration);
    }

    private void ClearWeaponFeedback()
    {
        weaponFeedbackText = string.Empty;
        weaponFeedbackExpireTime = -1f;
    }

    private static AudioClip CreateDryFireClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.11f;

        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float firstClick = Mathf.Exp(-t * 125f);
            float secondClick = t >= 0.032f
                ? Mathf.Exp(-(t - 0.032f) * 165f) * 0.48f
                : 0f;
            float noise = Mathf.Sin(i * 12.9898f) * 43758.5453f;
            noise = (noise - Mathf.Floor(noise)) * 2f - 1f;
            float click = Mathf.Sin(2f * Mathf.PI * 1650f * t) * 0.45f + noise * 0.55f;
            float body = Mathf.Sin(2f * Mathf.PI * 310f * t) * Mathf.Exp(-t * 55f) * 0.22f;
            samples[i] = Mathf.Clamp(click * (firstClick + secondClick) * 0.82f + body, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("Generated_DryFire", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);

        return clip;
    }
}
