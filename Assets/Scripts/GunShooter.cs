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

    private IWeaponInput weaponInput;
    private float nextAllowedShootTime;
    private float bufferedShotExpireTime = -1f;

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
        }

        if (weaponInput.RemoveMagazinePressedThisFrame)
        {
            pistolState.RemoveMagazine();
        }

        if (weaponInput.SlidePulledThisFrame)
        {
            pistolState.PullSlide();
        }

        if (weaponInput.SlideReleasedThisFrame)
        {
            pistolState.ReleaseSlide();
        }

        if (weaponInput.SlideLockReleasedThisFrame)
        {
            pistolState.ReleaseSlideLock();
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
    }

    private bool CanShootByFireRate()
    {
        return Time.time >= nextAllowedShootTime;
    }

    public string GetWeaponStatusText()
    {
        if (pistolState == null)
        {
            return "Weapon offline";
        }

        return pistolState.GetStatusLine();
    }

    public void ResetWeaponState()
    {
        ClearBufferedShot();
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
        if (gunAudioSource == null || dryFireClip == null)
        {
            return;
        }

        gunAudioSource.Stop();
        gunAudioSource.volume = 1f;
        gunAudioSource.mute = false;
        gunAudioSource.spatialBlend = 0f;
        gunAudioSource.PlayOneShot(dryFireClip, 1f);
    }
}
