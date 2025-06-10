using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Пространство имён для TextMeshPro

public class ElectricCat : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Rigidbody2D rb;
    private Collider2D _collider; // Ссылка на коллайдер
    private TMP_Text timerText; // Используем TMP_Text
    private GameObject timerObj; // Отдельный объект для текста таймера
    private bool hasCollided = false;
    private bool isAnimating = false; // Флаг для анимации
    private Vector2 originalSpriteSize; // Исходный размер спрайта
    private List<GameObject> objectsToDestroy; // Список объектов для уничтожения
    private AudioVibrationManager audioVibrationManager;

    [Header("Explosion Settings")]
    public float explosionRadius = 2f; // Радиус взрыва

    [Header("Timer Settings")]
    public float countdownTime = 3f; // Длительность таймера (по умолчанию 3 секунды)

    [Header("Timer Text Settings")]
    public int timerFontSize = 20; // Размер шрифта таймера
    public Color timerColor = Color.red; // Цвет текста таймера
    public TMP_FontAsset timerFont; // Используем TMP_FontAsset
    public Vector3 timerOffset = Vector3.up * 0.5f; // Смещение текста относительно объекта
    public string timerSortingLayer = "Foreground"; // Слой сортировки текста
    public int timerSortingOrder = 10; // Порядок сортировки текста
    public float scaleAnimationDuration = 0.2f; // Длительность анимации масштабирования текста
    public float scaleAnimationMax = 1.2f; // Максимальный масштаб текста

    [Header("Explosion Animation Settings")]
    public Sprite[] explosionSprites; // Массив спрайтов для анимации взрыва (spritesheet)
    public float animationFrameRate = 0.1f; // Время между кадрами анимации (в секундах)
    public float startScaleFactor = 1f; // Начальный коэффициент масштаба спрайта
    public float endScaleFactor = 1.5f; // Конечный коэффициент масштаба спрайта

    void Start()
    {
        // Инициализация компонентов
        _renderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>(); // Получаем коллайдер
        if (_renderer == null) Debug.LogError($"[{gameObject.name}] SpriteRenderer не найден на ElectricCat!", gameObject);
        if (rb == null) Debug.LogError($"[{gameObject.name}] Rigidbody2D не найден на ElectricCat!", gameObject);
        if (_collider == null) Debug.LogError($"[{gameObject.name}] Collider2D не найден на ElectricCat!", gameObject);

        audioVibrationManager = FindObjectOfType<AudioVibrationManager>();
        if (audioVibrationManager == null) Debug.LogError("AudioVibrationManager не найден в сцене!");

        // Настройка физики: объект падает и взаимодействует по физике, как другие префабы
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.mass = 1f;

        // Сохраняем исходный размер спрайта
        _renderer.drawMode = SpriteDrawMode.Sliced; // Устанавливаем режим для изменения размера
        originalSpriteSize = _renderer.size; // Сохраняем исходный размер

        // Создаём объект для текста таймера как отдельный объект в сцене (не дочерний)
        timerObj = new GameObject("TimerText");
        timerText = timerObj.AddComponent<TextMeshPro>(); // Используем TextMeshPro
        timerText.fontSize = timerFontSize;
        timerText.color = timerColor;
        timerText.alignment = TextAlignmentOptions.Center; // Аналог TextAnchor.MiddleCenter
        timerText.enableAutoSizing = false;

        if (timerFont != null)
        {
            timerText.font = timerFont;
            if (timerFont.material == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Материал шрифта не назначен для timerFont: {timerFont.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Шрифт для таймера не задан в ElectricCat.");
        }

        // Настраиваем сортировку через Renderer
        Renderer timerRenderer = timerText.GetComponent<Renderer>();
        if (timerRenderer != null)
        {
            timerRenderer.sortingLayerName = timerSortingLayer;
            timerRenderer.sortingOrder = timerSortingOrder;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Renderer не найден на TimerText!");
        }

        timerText.gameObject.SetActive(false);

        // Проверяем наличие спрайтов для анимации
        if (explosionSprites == null || explosionSprites.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] Explosion Sprites не назначены в ElectricCat! Анимация взрыва не будет проигрываться.");
        }

        // Инициализируем список объектов для уничтожения
        objectsToDestroy = new List<GameObject>();

        Debug.Log($"[{gameObject.name}] ElectricCat Start: Time.timeScale = {Time.timeScale}");
    }

    void Update()
    {
        // Обновляем позицию текста таймера, чтобы он следовал за ElectricCat
        if (timerObj != null)
        {
            timerObj.transform.position = transform.position + timerOffset;
        }

        // Отладка: проверяем, изменяется ли transform.localScale
        if (transform.localScale != Vector3.one)
        {
            Debug.LogWarning($"[{gameObject.name}] transform.localScale изменился: {transform.localScale}");
        }
    }

    void OnEnable()
    {
        // Сбрасываем состояние при активации объекта (например, из пула объектов)
        hasCollided = false;
        isAnimating = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints2D.None;
        if (_collider != null) _collider.enabled = true; // Включаем коллайдер
        if (_renderer != null)
        {
            _renderer.drawMode = SpriteDrawMode.Sliced;
            _renderer.size = originalSpriteSize; // Сбрасываем размер спрайта
            _renderer.color = new Color(_renderer.color.r, _renderer.color.g, _renderer.color.b, 1f); // Сбрасываем прозрачность
        }
        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
            timerText.fontSize = timerFontSize;
            timerText.color = timerColor;
            timerText.alpha = 1f; // Сбрасываем прозрачность
            if (timerFont != null) timerText.font = timerFont;

            // Настраиваем сортировку через Renderer
            Renderer timerRenderer = timerText.GetComponent<Renderer>();
            if (timerRenderer != null)
            {
                timerRenderer.sortingLayerName = timerSortingLayer;
                timerRenderer.sortingOrder = timerSortingOrder;
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] Renderer не найден на TimerText при OnEnable!");
            }

            timerObj.transform.localScale = Vector3.one; // Сбрасываем масштаб текста
        }

        // Очищаем список объектов для уничтожения
        objectsToDestroy.Clear();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasCollided) return;

        // Пропускаем столкновение с другим ElectricCat
        if (collision.gameObject.GetComponent<ElectricCat>() != null)
        {
            Debug.Log($"[{gameObject.name}] Столкновение с другим ElectricCat — пропускаем.");
            return;
        }

        Debug.Log($"[{gameObject.name}] Столкновение с объектом: {collision.gameObject.name}, Тег: {collision.gameObject.tag}");

        try
        {
            if (collision.gameObject.CompareTag("GameObject") || collision.gameObject.CompareTag("BottomWall"))
            {
                hasCollided = true;

                // НЕ замораживаем физику — объект продолжает двигаться по физике
                Debug.Log($"[{gameObject.name}] Столкновение с {collision.gameObject.tag} — запускаем таймер");

                StartCoroutine(CountdownAndExplode());
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Объект не имеет ожидаемого тега: {collision.gameObject.tag}");
            }
        }
        catch (UnityException e)
        {
            Debug.LogError($"[{gameObject.name}] Ошибка при проверке тега: {e.Message}");
        }
    }

    private IEnumerator CountdownAndExplode()
    {
        float timer = countdownTime;
        timerText.gameObject.SetActive(true);
        int lastDisplayedValue = Mathf.CeilToInt(timer);

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            int currentDisplayedValue = Mathf.CeilToInt(timer);

            // Обновляем текст и запускаем анимацию масштабирования при изменении значения
            if (currentDisplayedValue != lastDisplayedValue)
            {
                timerText.text = currentDisplayedValue.ToString();
                StartCoroutine(ScaleTextAnimation());
                lastDisplayedValue = currentDisplayedValue;
            }

            // Запускаем анимацию и затухание, когда таймер достигает 1
            if (timer <= 1f && !isAnimating)
            {
                isAnimating = true;

                // Запускаем затухание текста
                StartCoroutine(FadeOutTimerText());

                // Отключаем физику и коллайдер у ElectricCat
                rb.isKinematic = true;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                if (_collider != null) _collider.enabled = false;

                // Собираем объекты для уничтожения
                Explode();

                if (explosionSprites != null && explosionSprites.Length > 0)
                {
                    StartCoroutine(PlayExplosionAnimation());
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] Нет спрайтов для анимации взрыва.");
                    StartCoroutine(FadeOutAndDestroy()); // Если нет анимации, просто затухаем
                }
            }

            yield return null;
        }

        // На всякий случай гарантируем, что таймер скрыт
        timerText.gameObject.SetActive(false);
    }

    private IEnumerator FadeOutTimerText()
    {
        float fadeDuration = 0.3f; // Длительность затухания (можно настроить)
        float elapsedTime = 0f;
        float startAlpha = timerText.alpha;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            timerText.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        timerText.alpha = 0f;
        timerText.gameObject.SetActive(false);
    }

    private IEnumerator ScaleTextAnimation()
    {
        float elapsedTime = 0f;
        Vector3 initialScale = Vector3.one;
        Vector3 maxScale = Vector3.one * scaleAnimationMax;

        // Увеличиваем масштаб
        while (elapsedTime < scaleAnimationDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (scaleAnimationDuration / 2);
            timerObj.transform.localScale = Vector3.Lerp(initialScale, maxScale, t);
            yield return null;
        }

        // Уменьшаем масштаб
        elapsedTime = 0f;
        while (elapsedTime < scaleAnimationDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (scaleAnimationDuration / 2);
            timerObj.transform.localScale = Vector3.Lerp(maxScale, initialScale, t);
            yield return null;
        }

        // Устанавливаем масштаб точно в исходное значение
        timerObj.transform.localScale = initialScale;
    }

    private IEnumerator PlayExplosionAnimation()
    {
        Debug.Log($"[{gameObject.name}] Проигрываю анимацию взрыва: {explosionSprites.Length} кадров, frameRate = {animationFrameRate}");

        float totalAnimationTime = explosionSprites.Length * animationFrameRate;
        float elapsedTime = 0f;

        // Собираем SpriteRenderer'ы всех объектов для затухания
        List<SpriteRenderer> renderersToFade = new List<SpriteRenderer>();
        renderersToFade.Add(_renderer); // Добавляем renderer ElectricCat
        foreach (GameObject obj in objectsToDestroy)
        {
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                renderersToFade.Add(sr);
            }
        }

        // Вычисляем шаг затухания
        float fadeDuration = totalAnimationTime * 0.5f; // Затухание начинается на последних 50% анимации
        float fadeStartTime = totalAnimationTime - fadeDuration; // Время начала затухания
        float alphaStep = _renderer.color.a / fadeDuration;

        // Анимация и затухание
        for (int i = 0; i < explosionSprites.Length; i++)
        {
            if (_renderer != null)
            {
                _renderer.sprite = explosionSprites[i];

                // Линейно изменяем размер спрайта через _renderer.size
                elapsedTime += animationFrameRate;
                float t = elapsedTime / totalAnimationTime;
                float currentScaleFactor = Mathf.Lerp(startScaleFactor, endScaleFactor, t);
                _renderer.size = originalSpriteSize * currentScaleFactor;

                // Затухание начинается только после fadeStartTime
                if (elapsedTime >= fadeStartTime)
                {
                    foreach (SpriteRenderer sr in renderersToFade)
                    {
                        if (sr != null)
                        {
                            float newAlpha = sr.color.a - alphaStep * animationFrameRate;
                            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, Mathf.Max(0f, newAlpha));
                        }
                    }
                }
            }

            yield return new WaitForSeconds(animationFrameRate);
        }

        Debug.Log($"[{gameObject.name}] Анимация взрыва завершена.");

        // Уничтожаем все объекты после анимации и затухания
        StartCoroutine(FadeOutAndDestroy());
    }

    private void Explode()
    {
        // Проигрываем звук и вибрацию при взрыве
        if (audioVibrationManager != null)
        {
            audioVibrationManager.PlaySFX(audioVibrationManager.explosionSound);
            audioVibrationManager.Vibrate();
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();

        // Собираем объекты для уничтожения
        objectsToDestroy.Clear();
        foreach (Collider2D hit in hitColliders)
        {
            if (hit.CompareTag("GameObject") && hit.gameObject != gameObject)
            {
                ObjectLevel levelComponent = hit.GetComponent<ObjectLevel>();
                if (levelComponent != null)
                {
                    int level = levelComponent.level;
                    int score = level * 2; // Очки как при слиянии (level * 2)

                    if (scoreManager != null)
                    {
                        scoreManager.AddScore(score);
                        Debug.Log($"[{gameObject.name}] Добавлено {score} очков за уничтожение объекта уровня {level}.");
                    }
                }

                // Отключаем физику и коллайдер у объекта, чтобы он не двигался
                Rigidbody2D hitRb = hit.GetComponent<Rigidbody2D>();
                if (hitRb != null)
                {
                    hitRb.isKinematic = true;
                    hitRb.velocity = Vector2.zero;
                    hitRb.angularVelocity = 0f;
                }
                Collider2D hitCollider = hit.GetComponent<Collider2D>();
                if (hitCollider != null) hitCollider.enabled = false;

                // Добавляем объект в список для уничтожения
                objectsToDestroy.Add(hit.gameObject);
            }
        }
    }

    private IEnumerator FadeOutAndDestroy()
    {
        // Уничтожаем объект таймера
        if (timerObj != null)
        {
            Destroy(timerObj);
        }

        // Уничтожаем все объекты одновременно
        foreach (GameObject obj in objectsToDestroy)
        {
            if (obj != null) Destroy(obj);
        }

        Destroy(gameObject);
        yield return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}