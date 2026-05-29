using System;
using UnityEngine;

[Serializable]
public class PistolConfig
{
    public WeaponProfile weaponProfile = new WeaponProfile();
    public bool startWithMagazine = true;
    public bool startChambered = true;
    public bool slideStartsLocked = false;
    public bool autoCycleSlideAfterShot = true;
    public bool requireTriggerReset = true;

    public int MagazineCapacity
    {
        get { return weaponProfile != null ? weaponProfile.magazineCapacity : 0; }
    }

    public bool LockSlideWhenEmpty
    {
        get { return weaponProfile == null || weaponProfile.locksOpenOnEmpty; }
    }

    public float FireRateRoundsPerMinute
    {
        get { return weaponProfile != null ? weaponProfile.fireRateRoundsPerMinute : 0f; }
    }

    public int NormalizedMagazineCapacity
    {
        get { return Mathf.Max(0, MagazineCapacity); }
    }

    public string WeaponName
    {
        get
        {
            return weaponProfile != null && !string.IsNullOrEmpty(weaponProfile.weaponName)
                ? weaponProfile.weaponName
                : "Weapon";
        }
    }
}
