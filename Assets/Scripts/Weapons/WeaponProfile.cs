using System;

[Serializable]
public class WeaponProfile
{
    public string weaponName = "G17";
    public WeaponFireMode fireMode = WeaponFireMode.SemiAutomatic;
    public string caliber = "9x19mm";
    public int magazineCapacity = 17;
    public float fireRateRoundsPerMinute = 300f;
    public bool usesDetachableMagazine = true;
    public bool locksOpenOnEmpty = true;
}
