using UnityEngine;

public class GunMouseAim : MonoBehaviour
{
    public float mouseSensitivity = 3f;

    public float minYaw = -45f;
    public float maxYaw = 45f;

    public float minPitch = -25f;
    public float maxPitch = 25f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        SetCursorLocked(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
        }

        if (Input.GetMouseButtonDown(0))
        {
            SetCursorLocked(true);
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        UpdateRotation();
    }

    private void UpdateRotation()
    {
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw = Mathf.Clamp(yaw, minYaw, maxYaw);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
