using UnityEngine;

public class SpriteScaler : MonoBehaviour
{
    [SerializeField] private bool preserveAspectRatio = false; // Сохранять пропорции спрайта (true) или растягивать полностью (false)
    [SerializeField] private bool scaleByWidth = true; // Масштабировать по ширине (true) или по высоте (false)

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer не найден на объекте!");
            return;
        }

        AdjustSpriteToScreen();
    }

    void AdjustSpriteToScreen()
    {
        // Получаем размеры видимой области камеры
        Camera mainCamera = Camera.main;
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        // Получаем размеры спрайта (без учёта текущего масштаба)
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        // Рассчитываем масштаб
        float scaleX = cameraWidth / spriteSize.x;
        float scaleY = cameraHeight / spriteSize.y;

        if (preserveAspectRatio)
        {
            // Если сохраняем пропорции, используем минимальный масштаб (чтобы спрайт полностью помещался)
            float scale = scaleByWidth ? scaleX : scaleY;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            // Если не сохраняем пропорции, растягиваем по ширине и высоте
            transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        // Центрируем спрайт относительно камеры
        transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, transform.position.z);

        Debug.Log($"Sprite scaled: ScaleX={transform.localScale.x}, ScaleY={transform.localScale.y}, CameraWidth={cameraWidth}, CameraHeight={cameraHeight}");
    }
}