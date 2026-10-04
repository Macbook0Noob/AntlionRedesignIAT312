using System.Collections;
using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public TMP_Text textComponent;
    public float moveSpeed = 1.8f;
    public float fadeDuration = 0.8f;

    public void Setup(string message, Color color)
    {
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = message;
            textComponent.color = color;
        }

        StartCoroutine(FloatAndFadeRoutine());
    }

    private IEnumerator FloatAndFadeRoutine()
    {
        float elapsed = 0f;
        Color initialColor = textComponent != null ? textComponent.color : Color.yellow;
        Vector3 startPos = transform.position;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            // Float upward
            transform.position = startPos + new Vector3(0, t * moveSpeed, 0);

            // Fade alpha out
            if (textComponent != null)
            {
                Color c = initialColor;
                c.a = Mathf.Lerp(1f, 0f, t * t); // Accelerate fade near end
                textComponent.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}