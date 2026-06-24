using UnityEngine;

public enum PistolTriggerResult
{
    None,
    Fired,
    DryFire,
    SlidePulled,
    SlideLocked,
    TriggerHeld
}

public class PistolStateMachine : MonoBehaviour
{
    public PistolConfig config = new PistolConfig();
    public PistolMagazine magazine = new PistolMagazine();

    [SerializeField] private bool magazineInserted;
    [SerializeField] private bool roundInChamber;
    [SerializeField] private bool slidePulled;
    [SerializeField] private bool slideLocked;
    [SerializeField] private bool triggerHeld;

    public bool MagazineInserted
    {
        get { return magazineInserted; }
    }

    public bool RoundInChamber
    {
        get { return roundInChamber; }
    }

    public bool SlidePulled
    {
        get { return slidePulled; }
    }

    public bool SlideLocked
    {
        get { return slideLocked; }
    }

    public bool TriggerHeld
    {
        get { return triggerHeld; }
    }

    public bool CanFire
    {
        get { return roundInChamber && !slidePulled && !slideLocked; }
    }

    public PistolTriggerResult LastTriggerResult { get; private set; }

    private void Awake()
    {
        ResetToConfiguredState();
    }

    private void OnValidate()
    {
        if (config == null)
        {
            config = new PistolConfig();
        }

        if (magazine == null)
        {
            magazine = new PistolMagazine();
        }

        magazine.Configure(config.NormalizedMagazineCapacity, false);
    }

    public void ResetToConfiguredState()
    {
        if (config == null)
        {
            config = new PistolConfig();
        }

        if (magazine == null)
        {
            magazine = new PistolMagazine();
        }

        magazine.Configure(config.NormalizedMagazineCapacity, true);
        magazineInserted = config.startWithMagazine;
        roundInChamber = config.startChambered;
        slidePulled = false;
        slideLocked = config.slideStartsLocked;
        triggerHeld = false;
        LastTriggerResult = PistolTriggerResult.None;
    }

    public void ApplyConfig(PistolConfig newConfig)
    {
        config = newConfig != null ? newConfig : new PistolConfig();
        ResetToConfiguredState();
    }

    public bool InsertFullMagazine()
    {
        if (magazineInserted)
        {
            return false;
        }

        magazine.Configure(config.NormalizedMagazineCapacity, true);
        magazine.LoadFull();
        magazineInserted = true;
        return true;
    }

    public bool RemoveMagazine()
    {
        if (!magazineInserted)
        {
            return false;
        }

        magazineInserted = false;
        return true;
    }

    public bool PullSlide()
    {
        if (slidePulled)
        {
            return true;
        }

        slideLocked = false;

        if (roundInChamber)
        {
            roundInChamber = false;
        }

        if (ShouldLockOpenOnEmptyMagazine())
        {
            slidePulled = false;
            slideLocked = true;
            return true;
        }

        slidePulled = true;
        return true;
    }

    public bool ReleaseSlide()
    {
        if (!slidePulled && !slideLocked)
        {
            return false;
        }

        bool shouldLockOpenOnEmptyMagazine = slidePulled || slideLocked;
        slidePulled = false;
        slideLocked = false;
        ChamberRoundOrCloseEmpty(shouldLockOpenOnEmptyMagazine);
        return true;
    }

    public bool ReleaseSlideLock()
    {
        if (!slideLocked)
        {
            return false;
        }

        slideLocked = false;
        ChamberRoundOrCloseEmpty(false);
        return true;
    }

    public PistolTriggerResult PressTrigger()
    {
        if (config.requireTriggerReset && triggerHeld)
        {
            LastTriggerResult = PistolTriggerResult.TriggerHeld;
            return LastTriggerResult;
        }

        triggerHeld = true;

        if (slidePulled)
        {
            LastTriggerResult = PistolTriggerResult.SlidePulled;
            return LastTriggerResult;
        }

        if (slideLocked)
        {
            LastTriggerResult = PistolTriggerResult.SlideLocked;
            return LastTriggerResult;
        }

        if (!roundInChamber)
        {
            LastTriggerResult = PistolTriggerResult.DryFire;
            return LastTriggerResult;
        }

        roundInChamber = false;

        if (config.autoCycleSlideAfterShot)
        {
            CycleSlideAfterShot();
        }

        LastTriggerResult = PistolTriggerResult.Fired;
        return LastTriggerResult;
    }

    public void ReleaseTrigger()
    {
        triggerHeld = false;
    }

    public string GetStatusLine()
    {
        string magazineText = magazineInserted
            ? magazine.AmmoCount + "/" + magazine.Capacity
            : "已拔出";

        string chamberText = roundInChamber ? "+1" : "+0";
        return config.WeaponName + " | 弹匣 " + magazineText + " " + chamberText + " | " + GetStateLabel();
    }

    public string GetHudText()
    {
        string magazineText = magazineInserted
            ? magazine.AmmoCount + "/" + magazine.Capacity
            : "已拔出";

        return config.WeaponName
            + "\n弹匣    " + magazineText
            + "\n膛内    " + (roundInChamber ? "有弹" : "无弹")
            + "\n枪机    " + GetSlideLabel();
    }

    public string GetTriggerFeedbackText(PistolTriggerResult result)
    {
        switch (result)
        {
            case PistolTriggerResult.DryFire:
                return magazineInserted
                    ? "空击：膛内无弹"
                    : "空击：未插入弹匣";
            case PistolTriggerResult.SlidePulled:
                return "无法射击：请先释放枪机";
            case PistolTriggerResult.SlideLocked:
                return "无法射击：当前为空仓挂机";
            case PistolTriggerResult.TriggerHeld:
                return "请先松开扳机，再重新扣动";
            default:
                return string.Empty;
        }
    }

    private void CycleSlideAfterShot()
    {
        if (magazineInserted && magazine.TryConsumeRound())
        {
            roundInChamber = true;
            slideLocked = false;
            return;
        }

        if (config.LockSlideWhenEmpty)
        {
            slideLocked = true;
        }
    }

    private void ChamberRoundOrCloseEmpty(bool lockOpenOnEmptyMagazine)
    {
        if (magazineInserted && magazine.TryConsumeRound())
        {
            roundInChamber = true;
            slideLocked = false;
            return;
        }

        roundInChamber = false;
        slideLocked = lockOpenOnEmptyMagazine
            && magazineInserted
            && config.LockSlideWhenEmpty;
    }

    private bool ShouldLockOpenOnEmptyMagazine()
    {
        return magazineInserted
            && magazine != null
            && !magazine.HasAmmo
            && (config == null || config.LockSlideWhenEmpty);
    }

    private string GetStateLabel()
    {
        if (slidePulled)
        {
            return "枪机后拉";
        }

        if (slideLocked)
        {
            return "空仓挂机";
        }

        if (!magazineInserted)
        {
            return roundInChamber ? "膛内有弹" : "无弹匣";
        }

        if (roundInChamber)
        {
            return "可射击";
        }

        return "膛内无弹";
    }

    private string GetSlideLabel()
    {
        if (slidePulled)
        {
            return "后拉";
        }

        return slideLocked ? "空仓挂机" : "就绪";
    }
}
