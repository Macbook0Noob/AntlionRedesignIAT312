using System.Collections;
using UnityEngine;

public class SonarPulseRing : MonoBehaviour
{
    private SpriteRenderer sr;

    public void AnimatePulse(float maxRadius, float duration)
    {
        sr = GetComponent<SpriteRenderer>();
        StartCoroutine(PulseRoutine(maxRadius, duration));
    }

    private IEnumerator PulseRoutine(float maxRadius, float duration)
    {
        float elapsed = 0f;
        Vector3 initialScale = Vector3.zero;
        Vector3 targetScale = new Vector3(maxRadius * 2f, maxRadius * 2f, 1f);

        // Read the base color directly from the SpriteRenderer component
        Color baseColor = (sr != null) ? sr.color : Color.cyan;
        float initialAlpha = baseColor.a > 0f ? baseColor.a : 0.8f;

        transform.localScale = initialScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Expansion easing curve
            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.localScale = Vector3.Lerp(initialScale, targetScale, smoothT);

            // Fade ONLY the alpha, preserving the Inspector's RGB color
            if (sr != null)
            {
                Color c = baseColor;
                c.a = Mathf.Lerp(initialAlpha, 0f, t * t);
                sr.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}