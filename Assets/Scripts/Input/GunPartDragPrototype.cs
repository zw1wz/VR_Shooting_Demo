using UnityEngine;

/// <summary>
/// 桌面原型：用鼠标拖拽模拟 AR 动捕手套的手部位置，驱动枪械零件。
/// 挂在 Gun 对象上，运行时自动禁用外部 Animator，让零件完全由鼠标位置控制。
///
/// 操作方式：
///   鼠标左键点击+拖拽弹匣 → 弹匣跟随鼠标移动（向下拔出/向上插入）
///   鼠标左键点击+拖拽枪机 → 枪机沿枪管轴向跟随鼠标移动（向后拉/向前推）
///   鼠标左键点击+拖拽扳机 → 扳机跟随鼠标旋转（扣动/松开）
///   松开鼠标 → 根据零件位置自动吸附到位并触发对应状态
/// </summary>
public class GunPartDragPrototype : MonoBehaviour
{
    public PistolVisualController vc;
    public PistolStateMachine state;
    public GunShooter shooter;

    [Header("Drag Settings")]
    public float magazineSnapThreshold = 0.04f;
    public float triggerFireThreshold = 0.7f;

    [Header("Slide")]
    public float slideMaxTravel = 0.075f;

    [Header("Magazine")]
    public float magazineDropDistance = 0.34f;

    [Header("Trigger")]
    public float triggerMaxAngle = 16f;
    public float triggerDragRange = 0.05f;

    [Header("Collider Size")]
    public Vector3 magazineColliderSize = new Vector3(0.06f, 0.12f, 0.05f);
    public Vector3 slideColliderSize = new Vector3(0.06f, 0.05f, 0.3f);
    public Vector3 triggerColliderSize = new Vector3(0.03f, 0.04f, 0.02f);

    private enum DragMode { None, Magazine, Slide, Trigger }

    private DragMode currentDrag = DragMode.None;
    private Vector3 dragStartMouseWorld;
    private Vector3 slideRestPos;
    private Vector3 magazineRestPos;
    private Quaternion triggerRestRot;
    private BoxCollider magazineCollider;
    private BoxCollider slideCollider;
    private BoxCollider triggerCollider;
    private bool initialized;
    private float triggerPull01;

    private void Start()
    {
        if (vc == null) vc = GetComponent<PistolVisualController>();
        if (state == null) state = GetComponent<PistolStateMachine>();
        if (shooter == null) shooter = GetComponent<GunShooter>();
    }

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            return;
        }

        HandleInput();
    }

    private GunMouseAim mouseAim;
    private MouseKeyboardWeaponInput keyboardInput;

    private void TryInitialize()
    {
        if (vc == null || state == null) return;
        if (vc.slideTransform == null || vc.magazineTransform == null || vc.triggerTransform == null) return;

        vc.useExternalModelAnimator = false;
        vc.externalAnimatorControlsMovingParts = false;
        vc.externalAnimatorControlsRecoil = false;

        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (var a in animators) a.enabled = false;

        // 禁用鼠标瞄准和键鼠输入，避免和拖拽冲突
        mouseAim = GetComponent<GunMouseAim>();
        if (mouseAim != null) mouseAim.enabled = false;
        keyboardInput = GetComponent<MouseKeyboardWeaponInput>();
        if (keyboardInput != null) keyboardInput.enabled = false;

        // 解锁鼠标光标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        slideRestPos = vc.slideTransform.localPosition;
        magazineRestPos = vc.magazineTransform.localPosition;
        triggerRestRot = vc.triggerTransform.localRotation;

        magazineCollider = vc.magazineTransform.gameObject.AddComponent<BoxCollider>();
        magazineCollider.size = magazineColliderSize;
        magazineCollider.isTrigger = true;

        slideCollider = vc.slideTransform.gameObject.AddComponent<BoxCollider>();
        slideCollider.size = slideColliderSize;
        slideCollider.isTrigger = true;

        triggerCollider = vc.triggerTransform.gameObject.AddComponent<BoxCollider>();
        triggerCollider.size = triggerColliderSize;
        triggerCollider.isTrigger = true;

        initialized = true;
    }

    private void HandleInput()
    {
        if (currentDrag == DragMode.None)
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryStartDrag();
            }
        }
        else
        {
            if (Input.GetMouseButton(0))
            {
                UpdateDrag();
            }

            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }
    }

    private void TryStartDrag()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 10f);

        // 优先检测小零件（扳机 > 枪机 > 弹匣），避免大碰撞器遮挡小零件
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == triggerCollider)
            {
                currentDrag = DragMode.Trigger;
                dragStartMouseWorld = GetMouseWorldOnPlane(vc.triggerTransform.position);
                return;
            }
        }

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == slideCollider)
            {
                currentDrag = DragMode.Slide;
                dragStartMouseWorld = GetMouseWorldOnPlane(slideRestPos);
                return;
            }
        }

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == magazineCollider)
            {
                currentDrag = DragMode.Magazine;
                dragStartMouseWorld = GetMouseWorldOnPlane(magazineRestPos);
                return;
            }
        }
    }

    private void UpdateDrag()
    {
        switch (currentDrag)
        {
            case DragMode.Magazine:
                DragMagazine();
                break;
            case DragMode.Slide:
                DragSlide();
                break;
            case DragMode.Trigger:
                DragTrigger();
                break;
        }
    }

    private void DragMagazine()
    {
        Vector3 gunDown = -transform.up;
        Vector3 mouseWorld = GetMouseWorldOnPlane(magazineRestPos);

        Vector3 delta = mouseWorld - dragStartMouseWorld;
        float downAmount = Vector3.Dot(delta, gunDown);
        downAmount = Mathf.Clamp(downAmount, 0f, magazineDropDistance);

        Vector3 localDown = vc.magazineTransform.parent.InverseTransformDirection(gunDown);
        vc.magazineTransform.localPosition = magazineRestPos + localDown.normalized * downAmount;
        vc.magazineTransform.gameObject.SetActive(true);
    }

    private void DragSlide()
    {
        Vector3 gunBack = -transform.forward;
        Vector3 mouseWorld = GetMouseWorldOnPlane(slideRestPos);

        Vector3 delta = mouseWorld - dragStartMouseWorld;
        float backAmount = Vector3.Dot(delta, gunBack);
        backAmount = Mathf.Clamp(backAmount, 0f, slideMaxTravel);

        Vector3 localBack = vc.slideTransform.parent.InverseTransformDirection(gunBack);
        vc.slideTransform.localPosition = slideRestPos + localBack.normalized * backAmount;
    }

    private void DragTrigger()
    {
        Vector3 gunDown = -transform.up;
        Vector3 mouseWorld = GetMouseWorldOnPlane(vc.triggerTransform.position);

        Vector3 delta = mouseWorld - dragStartMouseWorld;
        float pullAmount = Vector3.Dot(delta, gunDown);
        triggerPull01 = Mathf.Clamp01(pullAmount / triggerDragRange);

        float angle = triggerMaxAngle * triggerPull01;
        vc.triggerTransform.localRotation = triggerRestRot * Quaternion.Euler(angle, 0f, 0f);
    }

    private void EndDrag()
    {
        switch (currentDrag)
        {
            case DragMode.Magazine:
                EndMagazineDrag();
                break;
            case DragMode.Slide:
                EndSlideDrag();
                break;
            case DragMode.Trigger:
                EndTriggerDrag();
                break;
        }

        currentDrag = DragMode.None;
    }

    private void EndMagazineDrag()
    {
        float distFromRest = Vector3.Distance(vc.magazineTransform.localPosition, magazineRestPos);

        if (distFromRest < magazineSnapThreshold)
        {
            if (!state.MagazineInserted)
            {
                state.InsertFullMagazine();
                vc.NotifyMagazineInserted();
            }
            vc.magazineTransform.localPosition = magazineRestPos;
        }
        else
        {
            if (state.MagazineInserted)
            {
                state.RemoveMagazine();
                vc.NotifyMagazineRemoved();
            }
        }
    }

    private void EndSlideDrag()
    {
        float backAmount = Vector3.Distance(vc.slideTransform.localPosition, slideRestPos);

        if (backAmount > slideMaxTravel * 0.6f)
        {
            if (!state.SlidePulled)
            {
                bool hadRound = state.RoundInChamber;
                state.PullSlide();
                vc.NotifySlidePulled(hadRound);
            }
        }
        else
        {
            if (state.SlidePulled || state.SlideLocked)
            {
                if (state.SlideLocked)
                {
                    state.ReleaseSlideLock();
                    vc.NotifySlideLockReleased();
                }
                else
                {
                    state.ReleaseSlide();
                    vc.NotifySlideReleased();
                }
            }
            vc.slideTransform.localPosition = slideRestPos;
        }
    }

    private void EndTriggerDrag()
    {
        if (triggerPull01 >= triggerFireThreshold)
        {
            if (GameManager.Instance != null && GameManager.Instance.CanShoot())
            {
                // 训练中：通过 GunShooter 的公开输入缓冲触发
                // GunShooter 在 Update 中读取 IWeaponInput.TriggerPressedThisFrame
                // 这里通过状态机直接处理
                var result = state.PressTrigger();
                if (result == PistolTriggerResult.Fired)
                {
                    vc.NotifyShotFired();
                    // 通过 GunShooter 的公开方法处理射击逻辑
                    Ray ray = new Ray(shooter.muzzlePoint != null ? shooter.muzzlePoint.position : transform.position,
                                      shooter.muzzlePoint != null ? shooter.muzzlePoint.forward : transform.forward);
                    ShootRay(ray);
                }
                else
                {
                    vc.NotifyDryFire();
                }
            }
            else
            {
                var result = state.PressTrigger();
                if (result == PistolTriggerResult.Fired)
                {
                    vc.NotifyShotFired();
                    Ray ray = new Ray(shooter.muzzlePoint != null ? shooter.muzzlePoint.position : transform.position,
                                      shooter.muzzlePoint != null ? shooter.muzzlePoint.forward : transform.forward);
                    ShootRay(ray);
                }
                else
                {
                    vc.NotifyDryFire();
                }
            }
        }

        triggerPull01 = 0f;
        vc.triggerTransform.localRotation = triggerRestRot;
        state.ReleaseTrigger();
    }

    private void ShootRay(Ray ray)
    {
        if (GameManager.Instance == null) return;

        float shootDistance = shooter.shootDistance;
        Target hitTarget = null;
        float hitDistance = 0f;

        if (Physics.Raycast(ray, out RaycastHit hit, shootDistance))
        {
            hitTarget = hit.collider.GetComponentInParent<Target>();
            hitDistance = hit.distance;
            if (hitTarget != null)
            {
                hitTarget.PlayHitFeedback(hit.point, hit.normal);
            }
        }

        GameManager.Instance.RecordShot(hitTarget, hitDistance);
    }

    private Vector3 GetMouseWorldOnPlane(Vector3 planePoint)
    {
        Camera cam = Camera.main;
        if (cam == null) return planePoint;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(cam.transform.forward, planePoint);

        if (plane.Raycast(ray, out float dist))
        {
            return ray.GetPoint(dist);
        }

        return planePoint;
    }

    private void OnGUI()
    {
        if (!initialized) return;

        GUILayout.BeginArea(new Rect(10, 10, 380, 200));
        GUIStyle rich = new GUIStyle(GUI.skin.label);
        rich.richText = true;
        rich.fontSize = 14;
        GUILayout.Label("<b>枪械零件拖拽原型</b>", rich);

        string mode = currentDrag switch
        {
            DragMode.Magazine => "拖拽中: 弹匣",
            DragMode.Slide => "拖拽中: 枪机",
            DragMode.Trigger => "拖拽中: 扳机",
            _ => "空闲",
        };
        GUILayout.Label($"状态: {mode}");
        GUILayout.Label($"弹匣: {(state.MagazineInserted ? "已插入" : "已拔出")} | 枪机: {(state.SlidePulled ? "后拉" : state.SlideLocked ? "空仓挂机" : "就绪")} | 膛内: {(state.RoundInChamber ? "有弹" : "无弹")}");
        GUILayout.Label("鼠标点击+拖拽: 弹匣/枪机/扳机");
        GUILayout.Label("兜底按键: T拔 R插 F拉 G放 V空仓");
        GUILayout.EndArea();
    }
}
