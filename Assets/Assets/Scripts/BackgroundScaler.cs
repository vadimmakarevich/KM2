using UnityEngine;

public class BackgroundScaler : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer не найден на объекте Background!");
            return;
        }

        AdjustBackgroundToScreen();
    }

    void AdjustBackgroundToScreen()
    {
        // Получаем размеры видимой области камеры
        Camera mainCamera = Camera.main;
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        // Получаем размеры спрайта
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        // Масштабируем спрайт, чтобы он соответствовал ширине экрана
        float scaleX = cameraWidth / spriteSize.x;
        float scaleY = cameraHeight / spriteSize.y;
        float scale = Mathf.Max(scaleX, scaleY); // Используем максимальный масштаб, чтобы заполнить экран

        transform.localScale = new Vector3(scale, scale, 1f);

        // Центрируем фон
        transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 0f);

        Debug.Log($"Background scaled: Scale={scale}, CameraWidth={cameraWidth}, CameraHeight={cameraHeight}");
    }
}