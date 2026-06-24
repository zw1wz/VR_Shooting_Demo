using UnityEngine;

public class GunsmithAnimationEventRelay : MonoBehaviour
{
    public PistolVisualController visualController;

    public void OnFire()
    {
        if (visualController != null)
        {
            visualController.NotifyExternalAnimationFireEvent();
        }
    }

    public void OnReloaded()
    {
        if (visualController != null)
        {
            visualController.NotifyExternalAnimationReloadedEvent();
        }
    }

    public void OnHammerStayCocked()
    {
    }

    public void OnHammerReleased()
    {
    }

    public void OnTableSound(string eventPath)
    {
        if (visualController != null)
        {
            visualController.PlayExternalAnimationSound(eventPath);
        }
    }

    public void PlayCaseSound(string eventPath)
    {
        OnTableSound(eventPath);
    }
}
