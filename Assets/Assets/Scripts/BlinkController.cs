using UnityEngine;

public class BlinkController : MonoBehaviour
{
    public Sprite openEyesSprite;
    public Sprite closedEyesSprite;
    public float minBlinkInterval = 5f;
    public float maxBlinkInterval = 30f;
    public float blinkDuration = 1.5f;

    private SpriteRenderer spriteRenderer;
    private bool isBlinking = false;
    private Coroutine blinkCoroutine;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("BlinkController: SpriteRenderer not found on GameObject!");
            enabled = false;
        }
    }

    void OnEnable()
    {
        StartBlinking();
    }

    void OnDisable()
    {
        StopBlinking();
    }

    public void StartBlinking()
    {
        if (isBlinking || spriteRenderer == null || openEyesSprite == null || closedEyesSprite == null) return;

        isBlinking = true;
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    public void StopBlinking()
    {
        if (!isBlinking) return;

        isBlinking = false;
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        if (spriteRenderer != null && openEyesSprite != null)
        {
            spriteRenderer.sprite = openEyesSprite;
        }
    }

    private System.Collections.IEnumerator BlinkRoutine()
    {
        while (isBlinking)
        {
            // Ждём случайный интервал перед морганием
            float waitTime = Random.Range(minBlinkInterval, maxBlinkInterval);
            yield return new WaitForSeconds(waitTime);

            if (!isBlinking || spriteRenderer == null) yield break;

            // Моргание: переключаем на закрытые глаза
            spriteRenderer.sprite = closedEyesSprite;
            yield return new WaitForSeconds(blinkDuration);

            if (!isBlinking || spriteRenderer == null) yield break;

            // Возвращаем открытые глаза
            spriteRenderer.sprite = openEyesSprite;
        }
    }
}