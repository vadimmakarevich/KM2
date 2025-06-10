using UnityEngine;

public class WallPositioner : MonoBehaviour
{
    [SerializeField] private bool isLeftWall = true;  // Указывает, является ли это левой стеной (иначе — правая)
    [SerializeField] private float edgeOffset = 0.5f; // Отступ от края экрана
    [SerializeField] private bool scaleHeightToScreen = true; // Масштабировать высоту стены по высоте экрана

    private BoxCollider2D wallCollider;

    void Start()
    {
        wallCollider = GetComponent<BoxCollider2D>();
        if (wallCollider == null)
        {
            Debug.LogError($"[{gameObject.name}] BoxCollider2D не найден на объекте!");
            return;
        }

        PositionWall();
    }

    private void PositionWall()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError($"[{gameObject.name}] Main Camera не найдена!");
            return;
        }

        // Вычисляем размеры видимой области камеры
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        // Определяем края экрана
        float cameraLeftEdge = mainCamera.transform.position.x - cameraWidth / 2f;
        float cameraRightEdge = mainCamera.transform.position.x + cameraWidth / 2f;

        // Позиционируем стену
        float wallX = isLeftWall ? (cameraLeftEdge + edgeOffset) : (cameraRightEdge - edgeOffset);
        transform.position = new Vector3(wallX, transform.position.y, transform.position.z);

        // Масштабируем высоту стены, если включена опция
        if (scaleHeightToScreen)
        {
            wallCollider.size = new Vector2(wallCollider.size.x, cameraHeight);
            // Если стена — это SpriteRenderer, масштабируем его тоже
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float spriteHeight = sr.sprite.bounds.size.y * transform.localScale.y;
                float scaleFactor = cameraHeight / spriteHeight;
                transform.localScale = new Vector3(transform.localScale.x, scaleFactor, transform.localScale.z);
            }
        }

        Debug.Log($"[{gameObject.name}] Positioned: X={wallX}, Height={(scaleHeightToScreen ? cameraHeight : wallCollider.size.y)}");
    }
}