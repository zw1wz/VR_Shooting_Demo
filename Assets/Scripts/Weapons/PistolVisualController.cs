using UnityEngine;

public class PistolVisualController : MonoBehaviour
{
    public PistolStateMachine pistolState;

    [Header("Visual Parts")]
    public bool createPrototypeVisuals = true;
    public Transform slideTransform;
    public Transform magazineTransform;
    public Transform triggerTransform;
    public Transform ejectionPoint;

    [Header("Slide")]
    public float slideTravel = 0.18f;
    public float slideMoveSpeed = 24f;
    public float shotSlideDuration = 0.12f;

    [Header("Magazine")]
    public float magazineDropDistance = 0.34f;
    public float magazineMoveSpeed = 9f;

    [Header("Trigger")]
    public float triggerPullAngle = 16f;
    public float triggerMoveSpeed = 18f;

    [Header("Casing")]
    public bool ejectCasings = true;
    public float casingLifetime = 3f;
    public float casingRightForce = 1.8f;
    public float casingUpForce = 1.5f;
    public float casingForwardForce = 0.4f;
    public float casingTorque = 6f;

    [Header("Optional Audio")]
    public AudioSource actionAudioSource;
    public AudioClip slidePulledClip;
    public AudioClip slideReleasedClip;
    public AudioClip magazineInsertedClip;
    public AudioClip magazineRemovedClip;
    public AudioClip slideLockedClip;

    private Vector3 slideRestPosition;
    private Vector3 magazineInsertedPosition;
    private Vector3 magazineRemovedPosition;
    private Quaternion triggerRestRotation;
    private float shotAnimationStartTime = -1f;
    private bool initialized;

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        Initialize();
        UpdateSlideVisual();
        UpdateMagazineVisual();
        UpdateTriggerVisual();
    }

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        if (pistolState == null)
        {
            pistolState = GetComponent<PistolStateMachine>();
        }

        if (createPrototypeVisuals)
        {
            EnsurePrototypeVisuals();
        }

        if (slideTransform != null)
        {
            slideRestPosition = slideTransform.localPosition;
        }

        if (magazineTransform != null)
        {
            magazineInsertedPosition = magazineTransform.localPosition;
            magazineRemovedPosition = magazineInsertedPosition + Vector3.down * magazineDropDistance;
        }

        if (triggerTransform != null)
        {
            triggerRestRotation = triggerTransform.localRotation;
        }

        initialized = true;
        ResetVisualState();
    }

    public void ResetVisualState()
    {
        shotAnimationStartTime = -1f;

        if (!initialized)
        {
            return;
        }

        if (slideTransform != null)
        {
            slideTransform.localPosition = slideRestPosition + Vector3.back * GetStableSlideOffset();
        }

        if (magazineTransform != null)
        {
            bool magazineInserted = pistolState == null || pistolState.MagazineInserted;
            magazineTransform.gameObject.SetActive(magazineInserted);
            magazineTransform.localPosition = magazineInserted
                ? magazineInsertedPosition
                : magazineRemovedPosition;
        }

        if (triggerTransform != null)
        {
            triggerTransform.localRotation = GetTargetTriggerRotation();
        }
    }

    public void NotifyShotFired()
    {
        Initialize();
        shotAnimationStartTime = Time.time;
        EjectCasing();

        if (pistolState != null && pistolState.SlideLocked)
        {
            PlayActionClip(slideLockedClip);
        }
    }

    public void NotifyMagazineInserted()
    {
        Initialize();

        if (magazineTransform != null)
        {
            magazineTransform.gameObject.SetActive(true);
            magazineTransform.localPosition = magazineRemovedPosition;
        }

        PlayActionClip(magazineInsertedClip);
    }

    public void NotifyMagazineRemoved()
    {
        Initialize();
        PlayActionClip(magazineRemovedClip);
    }

    public void NotifySlidePulled(bool roundEjected)
    {
        Initialize();

        if (roundEjected)
        {
            EjectCasing();
        }

        PlayActionClip(slidePulledClip);
    }

    public void NotifySlideReleased()
    {
        Initialize();
        PlayActionClip(slideReleasedClip);
    }

    public void NotifySlideLockReleased()
    {
        Initialize();
        PlayActionClip(slideReleasedClip);
    }

    private void UpdateSlideVisual()
    {
        if (slideTransform == null)
        {
            return;
        }

        float backOffset = GetStableSlideOffset();
        if (backOffset <= 0f && shotAnimationStartTime >= 0f)
        {
            float duration = Mathf.Max(0.01f, shotSlideDuration);
            float progress = (Time.time - shotAnimationStartTime) / duration;

            if (progress >= 1f)
            {
                shotAnimationStartTime = -1f;
            }
            else
            {
                backOffset = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * slideTravel;
            }
        }

        Vector3 targetPosition = slideRestPosition + Vector3.back * backOffset;
        slideTransform.localPosition = DampVector3(
            slideTransform.localPosition,
            targetPosition,
            slideMoveSpeed
        );
    }

    private void UpdateMagazineVisual()
    {
        if (magazineTransform == null)
        {
            return;
        }

        bool magazineInserted = pistolState == null || pistolState.MagazineInserted;
        if (magazineInserted && !magazineTransform.gameObject.activeSelf)
        {
            magazineTransform.gameObject.SetActive(true);
            magazineTransform.localPosition = magazineRemovedPosition;
        }

        Vector3 targetPosition = magazineInserted
            ? magazineInsertedPosition
            : magazineRemovedPosition;

        magazineTransform.localPosition = DampVector3(
            magazineTransform.localPosition,
            targetPosition,
            magazineMoveSpeed
        );

        if (!magazineInserted
            && Vector3.Distance(magazineTransform.localPosition, magazineRemovedPosition) < 0.01f)
        {
            magazineTransform.gameObject.SetActive(false);
        }
    }

    private void UpdateTriggerVisual()
    {
        if (triggerTransform == null)
        {
            return;
        }

        triggerTransform.localRotation = Quaternion.Slerp(
            triggerTransform.localRotation,
            GetTargetTriggerRotation(),
            GetDampFactor(triggerMoveSpeed)
        );
    }

    private float GetStableSlideOffset()
    {
        if (pistolState == null)
        {
            return 0f;
        }

        return pistolState.SlidePulled || pistolState.SlideLocked
            ? slideTravel
            : 0f;
    }

    private Quaternion GetTargetTriggerRotation()
    {
        bool triggerHeld = pistolState != null && pistolState.TriggerHeld;
        return triggerHeld
            ? triggerRestRotation * Quaternion.Euler(triggerPullAngle, 0f, 0f)
            : triggerRestRotation;
    }

    private void EnsurePrototypeVisuals()
    {
        if (slideTransform == null)
        {
            slideTransform = EnsurePrototypeCube(
                "PrototypeSlide",
                new Vector3(0f, 1.08f, 0f),
                new Vector3(0.23f, 0.08f, 0.68f),
                new Color(0.07f, 0.08f, 0.1f)
            );
        }

        if (magazineTransform == null)
        {
            magazineTransform = EnsurePrototypeCube(
                "PrototypeMagazine",
                new Vector3(0f, 0.54f, -0.14f),
                new Vector3(0.13f, 0.34f, 0.11f),
                new Color(0.12f, 0.14f, 0.17f)
            );
        }

        if (triggerTransform == null)
        {
            triggerTransform = EnsurePrototypeCube(
                "PrototypeTrigger",
                new Vector3(0f, 0.79f, 0.02f),
                new Vector3(0.07f, 0.1f, 0.04f),
                new Color(0.18f, 0.2f, 0.22f)
            );
        }

        if (ejectionPoint == null)
        {
            Transform existing = transform.Find("PrototypeEjectionPoint");
            if (existing != null)
            {
                ejectionPoint = existing;
            }
            else
            {
                GameObject point = new GameObject("PrototypeEjectionPoint");
                point.transform.SetParent(transform, false);
                point.transform.localPosition = new Vector3(0.15f, 1.08f, 0.02f);
                ejectionPoint = point.transform;
            }
        }
    }

    private Transform EnsurePrototypeCube(
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        Color color)
    {
        Transform existing = transform.Find(objectName);
        if (existing != null)
        {
            return existing;
        }

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(transform, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }

        ApplyPrototypeColor(cube, color);
        return cube.transform;
    }

    private void EjectCasing()
    {
        if (!ejectCasings || ejectionPoint == null)
        {
            return;
        }

        GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        casing.name = "PrototypeCasing";
        casing.transform.position = ejectionPoint.position;
        casing.transform.rotation = ejectionPoint.rotation * Quaternion.Euler(0f, 0f, 90f);
        casing.transform.localScale = new Vector3(0.025f, 0.055f, 0.025f);

        ApplyPrototypeColor(casing, new Color(0.78f, 0.56f, 0.16f));
        IgnoreWeaponCollisions(casing.GetComponent<Collider>());

        Rigidbody body = casing.AddComponent<Rigidbody>();
        Vector3 force = transform.right * casingRightForce
            + transform.up * casingUpForce
            + transform.forward * casingForwardForce;

        body.AddForce(force, ForceMode.Impulse);
        body.AddTorque(Random.onUnitSphere * casingTorque, ForceMode.Impulse);
        Destroy(casing, Mathf.Max(0.1f, casingLifetime));
    }

    private void IgnoreWeaponCollisions(Collider casingCollider)
    {
        if (casingCollider == null)
        {
            return;
        }

        Collider[] weaponColliders = GetComponentsInChildren<Collider>();
        foreach (Collider weaponCollider in weaponColliders)
        {
            if (weaponCollider != null && weaponCollider != casingCollider)
            {
                Physics.IgnoreCollision(casingCollider, weaponCollider);
            }
        }
    }

    private void PlayActionClip(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (actionAudioSource == null)
        {
            actionAudioSource = gameObject.AddComponent<AudioSource>();
            actionAudioSource.playOnAwake = false;
            actionAudioSource.loop = false;
            actionAudioSource.spatialBlend = 0f;
        }

        actionAudioSource.PlayOneShot(clip, 1f);
    }

    private static void ApplyPrototypeColor(GameObject gameObject, Color color)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private static Vector3 DampVector3(Vector3 current, Vector3 target, float speed)
    {
        return Vector3.Lerp(current, target, GetDampFactor(speed));
    }

    private static float GetDampFactor(float speed)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0f, speed) * Time.deltaTime);
    }
}
