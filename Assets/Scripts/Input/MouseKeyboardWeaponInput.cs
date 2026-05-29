using UnityEngine;

public class MouseKeyboardWeaponInput : MonoBehaviour, IWeaponInput
{
    [Header("Trigger")]
    public int triggerMouseButton = 0;

    [Header("Keyboard Controls")]
    public KeyCode insertMagazineKey = KeyCode.R;
    public KeyCode removeMagazineKey = KeyCode.T;
    public KeyCode pullSlideKey = KeyCode.F;
    public KeyCode releaseSlideKey = KeyCode.G;
    public KeyCode releaseSlideLockKey = KeyCode.V;

    public bool TriggerPressedThisFrame
    {
        get { return Input.GetMouseButtonDown(triggerMouseButton); }
    }

    public bool TriggerHeld
    {
        get { return Input.GetMouseButton(triggerMouseButton); }
    }

    public bool InsertMagazinePressedThisFrame
    {
        get { return Input.GetKeyDown(insertMagazineKey); }
    }

    public bool RemoveMagazinePressedThisFrame
    {
        get { return Input.GetKeyDown(removeMagazineKey); }
    }

    public bool SlidePulledThisFrame
    {
        get { return Input.GetKeyDown(pullSlideKey); }
    }

    public bool SlideReleasedThisFrame
    {
        get { return Input.GetKeyDown(releaseSlideKey); }
    }

    public bool SlideLockReleasedThisFrame
    {
        get { return Input.GetKeyDown(releaseSlideLockKey); }
    }
}
