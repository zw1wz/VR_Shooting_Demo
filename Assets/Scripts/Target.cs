using UnityEngine;

public class Target : MonoBehaviour
{
    public int score = 10;
    public Color hitFeedbackColor = new Color(1f, 0.72f, 0.18f);
    public float hitFeedbackLifeTime = 0.18f;
    public float hitFeedbackStartScale = 0.12f;
    public float hitFeedbackEndScale = 0.34f;

    public void PlayHitFeedback(Vector3 hitPoint, Vector3 hitNormal)
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "TargetHitFeedback";
        flash.transform.position = hitPoint + hitNormal.normalized * 0.035f;
        flash.transform.localScale = Vector3.one * Mathf.Max(0.01f, hitFeedbackStartScale);

        Collider collider = flash.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }

        Renderer renderer = flash.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = hitFeedbackColor;
        }

        TargetHitFeedback feedback = flash.AddComponent<TargetHitFeedback>();
        feedback.Initialize(
            renderer,
            hitFeedbackColor,
            Mathf.Max(0.03f, hitFeedbackLifeTime),
            Mathf.Max(0.01f, hitFeedbackStartScale),
            Mathf.Max(hitFeedbackStartScale, hitFeedbackEndScale)
        );
    }
}

public class TargetHitFeedback : MonoBehaviour
{
    private Renderer targetRenderer;
    private Color startColor;
    private float lifeTime = 0.18f;
    private float startScale = 0.12f;
    private float endScale = 0.34f;
    private float startTime;

    public void Initialize(
        Renderer renderer,
        Color color,
        float duration,
        float initialScale,
        float finalScale)
    {
        targetRenderer = renderer;
        startColor = color;
        lifeTime = duration;
        startScale = initialScale;
        endScale = finalScale;
        startTime = Time.time;
    }

    private void Update()
    {
        float progress = Mathf.Clamp01((Time.time - startTime) / Mathf.Max(0.01f, lifeTime));
        float scale = Mathf.Lerp(startScale, endScale, progress);
        transform.localScale = Vector3.one * scale;

        if (targetRenderer != null)
        {
            Color color = startColor;
            color.a = 1f - progress;
            targetRenderer.material.color = color;
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
