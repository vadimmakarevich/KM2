using UnityEngine;

public class SpawnDelay : MonoBehaviour
{
    public float delayTime = 0.2f; // Задержка перед следующим спавном
    private PointerController pointerController;

    void Start()
    {
        // Находим ссылку на PointerController
        pointerController = FindObjectOfType<PointerController>();

        // Если найден, блокируем спавн
        if (pointerController != null)
        {
            pointerController.isSpawning = true;

            // Через заданное время сбрасываем флаг
            Destroy(this, delayTime);
        }
    }

    void OnDestroy()
    {
        // Сбрасываем флаг при удалении компонента
        if (pointerController != null)
        {
            pointerController.isSpawning = false;
        }
    }
}