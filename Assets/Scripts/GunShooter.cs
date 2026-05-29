using UnityEngine;

public class GunShooter : MonoBehaviour
{
    public Transform muzzlePoint;
    public float shootDistance = 100f;

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

    private void Start()
    {
        InitGunAudio();
    }

    private void Update()
    {
        UpdateAimDot();

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
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

    private void Shoot()
    {
        if (GameManager.Instance == null || !GameManager.Instance.CanShoot())
        {
            return;
        }

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
}
