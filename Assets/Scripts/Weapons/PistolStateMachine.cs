using UnityEngine;

public enum PistolTriggerResult
{
    None,
    Fired,
    DryFire,
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

    public void InsertFullMagazine()
    {
        magazine.Configure(config.NormalizedMagazineCapacity, true);
        magazine.LoadFull();
        magazineInserted = true;
    }

    public void RemoveMagazine()
    {
        magazineInserted = false;
    }

    public void PullSlide()
    {
        slidePulled = true;
        slideLocked = false;

        if (roundInChamber)
        {
            roundInChamber = false;
        }
    }

    public void ReleaseSlide()
    {
        if (!slidePulled && !slideLocked)
        {
            return;
        }

        slidePulled = false;
        ChamberRoundOrCloseEmpty();
    }

    public void ReleaseSlideLock()
    {
        if (!slideLocked)
        {
            return;
        }

        slideLocked = false;
        ChamberRoundOrCloseEmpty();
    }

    public PistolTriggerResult PressTrigger()
    {
        if (config.requireTriggerReset && triggerHeld)
        {
            LastTriggerResult = PistolTriggerResult.TriggerHeld;
            return LastTriggerResult;
        }

        triggerHeld = true;

        if (slideLocked || slidePulled)
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
            : "OUT";

        string chamberText = roundInChamber ? "+1" : "+0";
        return config.WeaponName + " | Ammo " + magazineText + " " + chamberText + " | " + GetStateLabel();
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

    private void ChamberRoundOrCloseEmpty()
    {
        if (magazineInserted && magazine.TryConsumeRound())
        {
            roundInChamber = true;
            slideLocked = false;
            return;
        }

        roundInChamber = false;
        slideLocked = false;
    }

    private string GetStateLabel()
    {
        if (slidePulled)
        {
            return "SLIDE PULLED";
        }

        if (slideLocked)
        {
            return "SLIDE LOCK";
        }

        if (!magazineInserted)
        {
            return roundInChamber ? "CHAMBERED" : "NO MAG";
        }

        if (roundInChamber)
        {
            return "READY";
        }

        return "EMPTY";
    }
}
