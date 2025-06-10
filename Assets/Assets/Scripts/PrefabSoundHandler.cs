using UnityEngine;

public class PrefabSoundHandler : MonoBehaviour
{
    [Header("Prefab Settings")]
    public int prefabIndex; // Индекс префаба (должен совпадать с индексом в массиве префабов)

    private AudioVibrationManager audioVibrationManager;
    private bool hasCollided = false; // Флаг для отслеживания первого столкновения

    void Start()
    {
        audioVibrationManager = AudioVibrationManager.Instance;
        if (audioVibrationManager == null)
        {
            Debug.LogError("AudioVibrationManager не найден в сцене!");
        }

        // Настройка Rigidbody2D для более точного обнаружения столкновений
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Предотвращает вращение (можно убрать, если нужно)
        }
    }

    public void SetIndex(int index)
    {
        prefabIndex = index;
        Debug.Log($"[{gameObject.name}] Установлен prefabIndex: {prefabIndex}");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[{gameObject.name}] OnCollisionEnter2D вызван, hasCollided: {hasCollided}, столкнулся с: {collision.gameObject.name}");

        // Проверяем, является ли столкнувшийся объект другим префабом
        PrefabSoundHandler otherPrefab = collision.gameObject.GetComponent<PrefabSoundHandler>();
        if (otherPrefab != null)
        {
            // Если индексы совпадают, это мерж
            if (otherPrefab.prefabIndex == prefabIndex)
            {
                Debug.Log($"[{gameObject.name}] Обнаружен мерж с {collision.gameObject.name}, индекс: {prefabIndex}");
                HandleMerge(otherPrefab);
                return;
            }
        }

        // Обрабатываем столкновение (с любым объектом)
        HandleCollision(collision);
    }

    private void HandleCollision(Collision2D collision)
    {
        // Пропускаем, если уже было столкновение
        if (hasCollided)
        {
            Debug.Log($"[{gameObject.name}] Пропускаем, уже было столкновение ранее (с любым объектом)");
            return;
        }

        // Устанавливаем флаг, чтобы звук больше не проигрывался
        hasCollided = true;

        // Проигрываем звук столкновения
        if (audioVibrationManager != null)
        {
            AudioClip collisionSound = audioVibrationManager.GetPrefabCollisionSound(prefabIndex);
            if (collisionSound != null)
            {
                audioVibrationManager.PlaySFX(collisionSound);
                audioVibrationManager.Vibrate();
                Debug.Log($"[{gameObject.name}] Проигран звук столкновения для префаба с индексом {prefabIndex} при столкновении с {collision.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Звук столкновения для префаба с индексом {prefabIndex} не найден!");
            }
        }
    }

    private void HandleMerge(PrefabSoundHandler otherPrefab)
    {
        // Проигрываем звук мержа
        if (audioVibrationManager != null)
        {
            AudioClip mergeSound = audioVibrationManager.GetPrefabMergeSound(prefabIndex);
            if (mergeSound != null)
            {
                audioVibrationManager.PlaySFX(mergeSound);
                audioVibrationManager.Vibrate();
                Debug.Log($"[{gameObject.name}] Проигран звук мержа для префаба с индексом {prefabIndex}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Звук мержа для префаба с индексом {prefabIndex} не найден!");
            }
        }

        // Логика мержа: уничтожаем другой префаб и "апгрейдим" текущий
        Destroy(otherPrefab.gameObject);
        UpgradePrefab();
    }

    private void UpgradePrefab()
    {
        int newIndex = prefabIndex + 1;
        if (newIndex >= 13) // Предполагаем, что у вас 13 префабов
        {
            Debug.Log($"[{gameObject.name}] Достигнут максимальный уровень префаба!");
            return;
        }

        Debug.Log($"[{gameObject.name}] Префаб апгрейдится с индекса {prefabIndex} на {newIndex}");
        prefabIndex = newIndex;

        // Не сбрасываем hasCollided, чтобы звук столкновения больше не проигрывался
        // hasCollided = false; // Убрали сброс
    }
}