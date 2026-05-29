public interface IWeaponInput
{
    bool TriggerPressedThisFrame { get; }
    bool TriggerHeld { get; }
    bool InsertMagazinePressedThisFrame { get; }
    bool RemoveMagazinePressedThisFrame { get; }
    bool SlidePulledThisFrame { get; }
    bool SlideReleasedThisFrame { get; }
    bool SlideLockReleasedThisFrame { get; }
}
