using System;
using UnityEngine;

[Serializable]
public class PistolMagazine
{
    [SerializeField] private int capacity = 17;
    [SerializeField] private int ammoCount = 17;

    public int Capacity
    {
        get { return capacity; }
    }

    public int AmmoCount
    {
        get { return ammoCount; }
    }

    public bool HasAmmo
    {
        get { return ammoCount > 0; }
    }

    public void Configure(int magazineCapacity, bool fillMagazine)
    {
        capacity = Mathf.Max(0, magazineCapacity);
        ammoCount = fillMagazine ? capacity : Mathf.Clamp(ammoCount, 0, capacity);
    }

    public void LoadFull()
    {
        ammoCount = capacity;
    }

    public void SetAmmo(int value)
    {
        ammoCount = Mathf.Clamp(value, 0, capacity);
    }

    public bool TryConsumeRound()
    {
        if (ammoCount <= 0)
        {
            return false;
        }

        ammoCount--;
        return true;
    }
}
