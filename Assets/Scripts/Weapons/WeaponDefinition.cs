using UnityEngine;

[CreateAssetMenu(fileName = "Weapon_", menuName = "Shooting Range/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    public string weaponId = "G17";
    public string displayName = "Glock 17";
    [TextArea] public string description = "9mm 半自动手枪，17发弹匣";

    [Header("Weapon Profile")]
    public WeaponFireMode fireMode = WeaponFireMode.SemiAutomatic;
    public string caliber = "9x19mm";
    public int magazineCapacity = 17;
    public float fireRateRoundsPerMinute = 300f;
    public bool usesDetachableMagazine = true;
    public bool locksOpenOnEmpty = true;

    [Header("Pistol Behavior")]
    public bool startWithMagazine = true;
    public bool startChambered = true;
    public bool slideStartsLocked = false;
    public bool autoCycleSlideAfterShot = true;
    public bool requireTriggerReset = true;

    [Header("Audio Resources")]
    public string gunShotClipPath = "GunsmithSimulator/Glock17/Audio/17_Glock_17_Shot_OUT-001";
    public string dryFireClipPath = "GunsmithSimulator/Glock17/Audio/586_Glock_17_DryFire-001";
    public string slidePulledClipPath = "GunsmithSimulator/Glock17/Audio/307_Glock_17_Cock";
    public string slideReleasedClipPath = "GunsmithSimulator/Glock17/Audio/345_Case_Glock_17_Close";
    public string magazineInsertedClipPath = "GunsmithSimulator/Glock17/Audio/58_Glock_17_Mag_In";
    public string magazineRemovedClipPath = "GunsmithSimulator/Glock17/Audio/327_Glock_17_Mag_Out";
    public string slideLockedClipPath = "GunsmithSimulator/Glock17/Audio/697_Glock_17_ShotLast";

    [Header("Visual Resources")]
    public string externalModelResourcePath = "GunsmithSimulator/Glock17/Imported/Resources/cases/CaseWithGlock17";
    public string externalModelChildName = "Glock17";
    public string muzzlePointName = "MuzzleLocator";

    public WeaponProfile CreateWeaponProfile()
    {
        return new WeaponProfile
        {
            weaponName = displayName,
            fireMode = fireMode,
            caliber = caliber,
            magazineCapacity = magazineCapacity,
            fireRateRoundsPerMinute = fireRateRoundsPerMinute,
            usesDetachableMagazine = usesDetachableMagazine,
            locksOpenOnEmpty = locksOpenOnEmpty,
        };
    }

    public PistolConfig CreatePistolConfig()
    {
        return new PistolConfig
        {
            weaponProfile = CreateWeaponProfile(),
            startWithMagazine = startWithMagazine,
            startChambered = startChambered,
            slideStartsLocked = slideStartsLocked,
            autoCycleSlideAfterShot = autoCycleSlideAfterShot,
            requireTriggerReset = requireTriggerReset,
        };
    }
}
