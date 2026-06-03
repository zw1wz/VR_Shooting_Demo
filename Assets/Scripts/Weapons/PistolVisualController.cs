using UnityEngine;

public class PistolVisualController : MonoBehaviour
{
    public PistolStateMachine pistolState;

    [Header("Visual Parts")]
    public bool createPrototypeVisuals = true;
    public bool createStyledPrototypeModel = true;
    public bool hideLegacyBlockoutParts = true;
    public Transform slideTransform;
    public Transform magazineTransform;
    public Transform triggerTransform;
    public Transform ejectionPoint;

    [Header("Recoil")]
    public bool enableRecoilAnimation = true;
    public float recoilKickBackDistance = 0.055f;
    public float recoilMuzzleRiseAngle = 6f;
    public float recoilDuration = 0.16f;
    public float recoilRecoverySpeed = 28f;

    [Header("Slide")]
    public float slideTravel = 0.1f;
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
    public float casingRandomForce = 0.35f;
    public float casingTorque = 6f;
    public float casingRandomTorque = 2f;

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
    private Transform recoilTransform;
    private Vector3 recoilRestPosition;
    private Quaternion recoilRestRotation;
    private float shotAnimationStartTime = -1f;
    private float recoilAnimationStartTime = -1f;
    private bool initialized;

    private const string StyledModelRootName = "G17StylePrototype";
    private const float GripRakeAngle = 13f;

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        Initialize();
        UpdateRecoilVisual();
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

        if (recoilTransform != null)
        {
            recoilRestPosition = recoilTransform.localPosition;
            recoilRestRotation = recoilTransform.localRotation;
        }

        initialized = true;
        ResetVisualState();
    }

    public void ResetVisualState()
    {
        shotAnimationStartTime = -1f;
        recoilAnimationStartTime = -1f;

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

        if (recoilTransform != null)
        {
            recoilTransform.localPosition = recoilRestPosition;
            recoilTransform.localRotation = recoilRestRotation;
        }
    }

    public void NotifyShotFired()
    {
        Initialize();
        shotAnimationStartTime = Time.time;
        recoilAnimationStartTime = Time.time;
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

    private void UpdateRecoilVisual()
    {
        if (!enableRecoilAnimation || recoilTransform == null)
        {
            return;
        }

        float recoilAmount = 0f;
        if (recoilAnimationStartTime >= 0f)
        {
            float duration = Mathf.Max(0.01f, recoilDuration);
            float progress = (Time.time - recoilAnimationStartTime) / duration;

            if (progress >= 1f)
            {
                recoilAnimationStartTime = -1f;
            }
            else
            {
                recoilAmount = GetRecoilCurve(progress);
            }
        }

        Vector3 targetPosition = recoilRestPosition
            + Vector3.back * (recoilKickBackDistance * recoilAmount);
        Quaternion targetRotation = recoilRestRotation
            * Quaternion.Euler(-recoilMuzzleRiseAngle * recoilAmount, 0f, 0f);

        recoilTransform.localPosition = DampVector3(
            recoilTransform.localPosition,
            targetPosition,
            recoilRecoverySpeed
        );

        recoilTransform.localRotation = Quaternion.Slerp(
            recoilTransform.localRotation,
            targetRotation,
            GetDampFactor(recoilRecoverySpeed)
        );
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
        if (createStyledPrototypeModel)
        {
            HideLegacyBlockoutParts();
            EnsureStyledPrototypeModel();
            return;
        }

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

    private void EnsureStyledPrototypeModel()
    {
        Transform modelRoot = EnsureEmptyChild(
            transform,
            StyledModelRootName,
            Vector3.zero,
            Quaternion.identity
        );
        recoilTransform = modelRoot;

        Transform frameRoot = EnsureEmptyChild(
            modelRoot,
            "G17_Frame",
            Vector3.zero,
            Quaternion.identity
        );

        slideTransform = EnsureEmptyChild(
            modelRoot,
            "G17_Slide",
            new Vector3(0f, 1.04f, 0.015f),
            Quaternion.identity
        );

        magazineTransform = EnsureEmptyChild(
            modelRoot,
            "G17_Magazine",
            new Vector3(0f, 0.56f, -0.245f),
            Quaternion.Euler(GripRakeAngle, 0f, 0f)
        );

        triggerTransform = EnsureEmptyChild(
            modelRoot,
            "G17_Trigger",
            new Vector3(0f, 0.845f, 0.025f),
            Quaternion.identity
        );

        BuildStyledSlide(slideTransform);
        BuildStyledFrame(frameRoot);
        BuildStyledMagazine(magazineTransform);
        BuildStyledTrigger(triggerTransform);
        EnsureStyledEjectionPoint(slideTransform);
    }

    private void BuildStyledSlide(Transform slideRoot)
    {
        Color slideColor = new Color(0.055f, 0.06f, 0.065f);
        Color slideEdgeColor = new Color(0.025f, 0.027f, 0.03f);
        Color sightColor = new Color(0.01f, 0.012f, 0.014f);
        Color markingColor = new Color(0.82f, 0.84f, 0.78f);
        Color portColor = new Color(0.012f, 0.013f, 0.015f);
        Color chamberColor = new Color(0.62f, 0.56f, 0.42f);

        EnsureBox(
            slideRoot,
            "SlideBlock",
            Vector3.zero,
            new Vector3(0.29f, 0.118f, 0.74f),
            slideColor
        );

        EnsureBox(
            slideRoot,
            "SlideLowerLip",
            new Vector3(0f, -0.069f, 0f),
            new Vector3(0.252f, 0.034f, 0.68f),
            slideEdgeColor
        );

        EnsureBox(
            slideRoot,
            "SlideTopFlat",
            new Vector3(0f, 0.069f, 0f),
            new Vector3(0.22f, 0.022f, 0.66f),
            new Color(0.075f, 0.08f, 0.085f)
        );

        EnsureBox(
            slideRoot,
            "FrontFace",
            new Vector3(0f, 0f, 0.382f),
            new Vector3(0.255f, 0.105f, 0.022f),
            slideEdgeColor
        );

        EnsureBox(
            slideRoot,
            "RearFace",
            new Vector3(0f, 0f, -0.382f),
            new Vector3(0.255f, 0.105f, 0.022f),
            slideEdgeColor
        );

        EnsureCylinder(
            slideRoot,
            "VisibleBarrel",
            new Vector3(0f, -0.018f, 0.335f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(0.04f, 0.13f, 0.04f),
            new Color(0.18f, 0.18f, 0.17f)
        );

        EnsureBox(
            slideRoot,
            "EjectionPortCutout",
            new Vector3(0.149f, 0.022f, 0.1f),
            new Vector3(0.018f, 0.055f, 0.17f),
            portColor
        );

        EnsureBox(
            slideRoot,
            "EjectionPortChamber",
            new Vector3(0.16f, 0.018f, 0.095f),
            new Vector3(0.012f, 0.038f, 0.095f),
            chamberColor
        );

        EnsureBox(
            slideRoot,
            "FrontSight",
            new Vector3(0f, 0.091f, 0.315f),
            new Vector3(0.052f, 0.03f, 0.038f),
            sightColor
        );

        EnsureBox(
            slideRoot,
            "FrontSightDot",
            new Vector3(0f, 0.108f, 0.323f),
            new Vector3(0.018f, 0.006f, 0.01f),
            markingColor
        );

        EnsureBox(
            slideRoot,
            "RearSight",
            new Vector3(0f, 0.093f, -0.31f),
            new Vector3(0.11f, 0.032f, 0.045f),
            sightColor
        );

        EnsureBox(
            slideRoot,
            "RearSightNotch",
            new Vector3(0f, 0.112f, -0.302f),
            new Vector3(0.035f, 0.006f, 0.012f),
            markingColor
        );

        for (int i = 0; i < 5; i++)
        {
            float z = -0.31f + i * 0.026f;
            EnsureSerration(slideRoot, "RearSerrationR_" + i, 0.151f, z);
            EnsureSerration(slideRoot, "RearSerrationL_" + i, -0.151f, z);
        }

        for (int i = 0; i < 4; i++)
        {
            float z = 0.205f + i * 0.026f;
            EnsureSerration(slideRoot, "FrontSerrationR_" + i, 0.151f, z);
            EnsureSerration(slideRoot, "FrontSerrationL_" + i, -0.151f, z);
        }
    }

    private void BuildStyledFrame(Transform frameRoot)
    {
        Color polymerColor = new Color(0.035f, 0.04f, 0.045f);
        Color polymerEdgeColor = new Color(0.015f, 0.017f, 0.02f);
        Color gripTextureColor = new Color(0.08f, 0.085f, 0.08f);

        EnsureBox(
            frameRoot,
            "DustCover",
            new Vector3(0f, 0.965f, 0.08f),
            new Vector3(0.255f, 0.12f, 0.55f),
            polymerColor
        );

        EnsureBox(
            frameRoot,
            "FrameBeavertail",
            new Vector3(0f, 0.955f, -0.29f),
            new Vector3(0.235f, 0.105f, 0.18f),
            polymerColor
        );

        EnsureBox(
            frameRoot,
            "FrameUpperRail",
            new Vector3(0f, 1.015f, 0.005f),
            new Vector3(0.232f, 0.04f, 0.64f),
            polymerEdgeColor
        );

        EnsureBox(
            frameRoot,
            "FrameSlideShadow",
            new Vector3(0f, 0.996f, 0.015f),
            new Vector3(0.265f, 0.018f, 0.68f),
            new Color(0.012f, 0.014f, 0.016f)
        );

        EnsureBox(
            frameRoot,
            "Grip",
            new Vector3(0f, 0.675f, -0.245f),
            Quaternion.Euler(GripRakeAngle, 0f, 0f),
            new Vector3(0.225f, 0.58f, 0.195f),
            polymerColor
        );

        EnsureBox(
            frameRoot,
            "GripBackstrap",
            new Vector3(0f, 0.675f, -0.345f),
            Quaternion.Euler(GripRakeAngle, 0f, 0f),
            new Vector3(0.19f, 0.54f, 0.035f),
            polymerEdgeColor
        );

        EnsureBox(
            frameRoot,
            "GripTangBridge",
            new Vector3(0f, 0.88f, -0.245f),
            Quaternion.Euler(GripRakeAngle, 0f, 0f),
            new Vector3(0.22f, 0.19f, 0.2f),
            polymerColor
        );

        EnsureBox(
            frameRoot,
            "FrontStrapBlend",
            new Vector3(0f, 0.775f, -0.105f),
            Quaternion.Euler(GripRakeAngle, 0f, 0f),
            new Vector3(0.19f, 0.18f, 0.055f),
            polymerColor
        );

        EnsureBox(
            frameRoot,
            "MagazineWellLip",
            new Vector3(0f, 0.42f, -0.245f),
            Quaternion.Euler(GripRakeAngle, 0f, 0f),
            new Vector3(0.255f, 0.052f, 0.23f),
            polymerEdgeColor
        );

        EnsureBox(
            frameRoot,
            "AccessoryRail",
            new Vector3(0f, 0.9f, 0.18f),
            new Vector3(0.22f, 0.032f, 0.28f),
            polymerEdgeColor
        );

        for (int i = 0; i < 4; i++)
        {
            EnsureBox(
                frameRoot,
                "RailSlot_" + i,
                new Vector3(0f, 0.875f, 0.065f + i * 0.065f),
                new Vector3(0.235f, 0.011f, 0.018f),
                new Color(0.09f, 0.095f, 0.09f)
            );
        }

        BuildTriggerGuard(frameRoot, polymerColor);
        BuildGripTexture(frameRoot, gripTextureColor);
        BuildControls(frameRoot, polymerEdgeColor);
    }

    private void BuildTriggerGuard(Transform frameRoot, Color color)
    {
        EnsureBox(
            frameRoot,
            "TriggerGuardFront",
            new Vector3(0f, 0.805f, 0.105f),
            new Vector3(0.18f, 0.2f, 0.04f),
            color
        );

        EnsureBox(
            frameRoot,
            "TriggerGuardBottom",
            new Vector3(0f, 0.725f, 0.005f),
            new Vector3(0.18f, 0.052f, 0.22f),
            color
        );

        EnsureBox(
            frameRoot,
            "TriggerGuardRear",
            new Vector3(0f, 0.802f, -0.085f),
            new Vector3(0.18f, 0.18f, 0.04f),
            color
        );

        EnsureBox(
            frameRoot,
            "TriggerGuardOpening",
            new Vector3(0f, 0.805f, 0.005f),
            new Vector3(0.185f, 0.11f, 0.125f),
            new Color(0.01f, 0.011f, 0.012f)
        );
    }

    private void BuildGripTexture(Transform frameRoot, Color color)
    {
        for (int row = 0; row < 5; row++)
        {
            float y = 0.49f + row * 0.075f;
            EnsureBox(
                frameRoot,
                "GripFrontRib_" + row,
                new Vector3(0f, y, -0.145f),
                Quaternion.Euler(GripRakeAngle, 0f, 0f),
                new Vector3(0.19f, 0.012f, 0.012f),
                color
            );
        }

        for (int side = 0; side < 2; side++)
        {
            float x = side == 0 ? 0.116f : -0.116f;
            for (int row = 0; row < 4; row++)
            {
                EnsureBox(
                    frameRoot,
                    "GripSidePatch_" + side + "_" + row,
                    new Vector3(x, 0.5f + row * 0.085f, -0.245f),
                    Quaternion.Euler(GripRakeAngle, 0f, 0f),
                    new Vector3(0.012f, 0.045f, 0.12f),
                    color
                );
            }
        }
    }

    private void BuildControls(Transform frameRoot, Color color)
    {
        EnsureBox(
            frameRoot,
            "SlideStopLever",
            new Vector3(0.136f, 0.98f, -0.09f),
            new Vector3(0.02f, 0.035f, 0.11f),
            color
        );

        EnsureBox(
            frameRoot,
            "MagazineRelease",
            new Vector3(0.128f, 0.825f, -0.13f),
            new Vector3(0.025f, 0.045f, 0.04f),
            color
        );
    }

    private void BuildStyledMagazine(Transform magazineRoot)
    {
        EnsureBox(
            magazineRoot,
            "MagazineBody",
            Vector3.zero,
            new Vector3(0.15f, 0.42f, 0.13f),
            new Color(0.08f, 0.085f, 0.09f)
        );

        EnsureBox(
            magazineRoot,
            "MagazineBasePlate",
            new Vector3(0f, -0.225f, 0f),
            new Vector3(0.21f, 0.055f, 0.18f),
            new Color(0.025f, 0.028f, 0.032f)
        );

        for (int i = 0; i < 4; i++)
        {
            EnsureBox(
                magazineRoot,
                "MagazineRib_" + i,
                new Vector3(0.078f, 0.13f - i * 0.085f, 0f),
                new Vector3(0.012f, 0.035f, 0.105f),
                new Color(0.12f, 0.125f, 0.13f)
            );
        }
    }

    private void BuildStyledTrigger(Transform triggerRoot)
    {
        EnsureBox(
            triggerRoot,
            "TriggerBlade",
            new Vector3(0f, -0.055f, 0f),
            Quaternion.Euler(10f, 0f, 0f),
            new Vector3(0.06f, 0.13f, 0.04f),
            new Color(0.015f, 0.017f, 0.02f)
        );

        EnsureBox(
            triggerRoot,
            "TriggerSafetyTab",
            new Vector3(0f, -0.055f, 0.022f),
            Quaternion.Euler(10f, 0f, 0f),
            new Vector3(0.022f, 0.1f, 0.012f),
            new Color(0.08f, 0.085f, 0.09f)
        );
    }

    private void EnsureStyledEjectionPoint(Transform slideRoot)
    {
        if (ejectionPoint != null)
        {
            return;
        }

        Transform existing = slideRoot.Find("G17_EjectionPoint");
        if (existing != null)
        {
            ejectionPoint = existing;
            return;
        }

        GameObject point = new GameObject("G17_EjectionPoint");
        point.transform.SetParent(slideRoot, false);
        point.transform.localPosition = new Vector3(0.17f, 0.03f, 0.1f);
        point.transform.localRotation = Quaternion.identity;
        ejectionPoint = point.transform;
    }

    private void EnsureSerration(Transform parent, string name, float x, float z)
    {
        EnsureBox(
            parent,
            name,
            new Vector3(x, 0.008f, z),
            Quaternion.Euler(0f, 0f, 18f * Mathf.Sign(x)),
            new Vector3(0.012f, 0.095f, 0.012f),
            new Color(0.018f, 0.02f, 0.023f)
        );
    }

    private void HideLegacyBlockoutParts()
    {
        if (!hideLegacyBlockoutParts)
        {
            return;
        }

        SetChildActive("GunBody", false);
        SetChildActive("GunHandle", false);
    }

    private void SetChildActive(string objectName, bool active)
    {
        Transform child = transform.Find(objectName);
        if (child != null)
        {
            child.gameObject.SetActive(active);
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

    private Transform EnsureEmptyChild(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        Transform child = parent.Find(objectName);
        if (child == null)
        {
            GameObject gameObject = new GameObject(objectName);
            child = gameObject.transform;
            child.SetParent(parent, false);
        }

        child.localPosition = localPosition;
        child.localRotation = localRotation;
        child.localScale = Vector3.one;
        child.gameObject.SetActive(true);
        return child;
    }

    private Transform EnsureBox(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        Color color)
    {
        return EnsureBox(parent, objectName, localPosition, Quaternion.identity, localScale, color);
    }

    private Transform EnsureBox(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Color color)
    {
        return EnsurePrimitivePart(
            parent,
            objectName,
            PrimitiveType.Cube,
            localPosition,
            localRotation,
            localScale,
            color
        );
    }

    private Transform EnsureCylinder(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Color color)
    {
        return EnsurePrimitivePart(
            parent,
            objectName,
            PrimitiveType.Cylinder,
            localPosition,
            localRotation,
            localScale,
            color
        );
    }

    private Transform EnsurePrimitivePart(
        Transform parent,
        string objectName,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Color color)
    {
        Transform existing = parent.Find(objectName);
        GameObject gameObject;

        if (existing == null)
        {
            gameObject = GameObject.CreatePrimitive(primitiveType);
            gameObject.name = objectName;
            gameObject.transform.SetParent(parent, false);
            RemoveCollider(gameObject);
        }
        else
        {
            gameObject = existing.gameObject;
        }

        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localRotation = localRotation;
        gameObject.transform.localScale = localScale;
        gameObject.SetActive(true);
        ApplyPrototypeColor(gameObject, color);
        return gameObject.transform;
    }

    private static void RemoveCollider(GameObject gameObject)
    {
        Collider collider = gameObject.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }
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
        body.mass = 0.012f;
        body.drag = 0.02f;
        body.angularDrag = 0.04f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        float randomForce = Mathf.Max(0f, casingRandomForce);
        Vector3 force = transform.right * Random.Range(
                Mathf.Max(0f, casingRightForce - randomForce),
                casingRightForce + randomForce
            )
            + transform.up * Random.Range(
                Mathf.Max(0f, casingUpForce - randomForce),
                casingUpForce + randomForce
            )
            + transform.forward * Random.Range(
                casingForwardForce - randomForce,
                casingForwardForce + randomForce
            );

        body.AddForce(force, ForceMode.Impulse);
        body.AddTorque(
            Random.onUnitSphere * (casingTorque + Random.Range(0f, Mathf.Max(0f, casingRandomTorque))),
            ForceMode.Impulse
        );
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

    private static float GetRecoilCurve(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        if (clampedProgress < 0.18f)
        {
            return clampedProgress / 0.18f;
        }

        float recoveryProgress = (clampedProgress - 0.18f) / 0.82f;
        return Mathf.Pow(1f - recoveryProgress, 1.75f);
    }

    private static float GetDampFactor(float speed)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0f, speed) * Time.deltaTime);
    }
}
