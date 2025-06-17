using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AlertBlink : MonoBehaviour
{
    public int blinkCount = 3;
    public float blinkDuration = 0.2f;

    public UnityEvent OnBlinkComplete;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            Debug.LogWarning("AlertBlink: No se encontró SpriteRenderer.");
    }

    private void OnEnable()
    {
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        for (int i = 0; i < blinkCount; i++)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(blinkDuration);
            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(blinkDuration);
        }

        OnBlinkComplete?.Invoke();

        Destroy(gameObject);
    }
}
