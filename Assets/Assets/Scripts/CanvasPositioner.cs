using UnityEngine;

public class CanvasPositioner : MonoBehaviour
{
    [SerializeField] private bool alignToRight = true;  // Привязка к правому краю
    [SerializeField] private bool alignToTop = true;    // Привязка к верхнему краю
    [SerializeField] private Vector2 offsetFromEdge = new Vector2(50f, 50f); // Отступ от края в пикселях (x — от правого/левого, y — от верхнего/нижнего)

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"[{gameObject.name}] RectTransform не найден!");
            return;
        }

        PositionCanvasElement();
    }

    private void PositionCanvasElement()
    {
        // Настраиваем якоря
        Vector2 anchorMin = new Vector2(alignToRight ? 1f : 0f, alignToTop ? 1f : 0f);
        Vector2 anchorMax = anchorMin; // Якоря совпадают, чтобы позиция была фиксированной относительно края
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        // Устанавливаем pivot (точку отсчёта позиции)
        rectTransform.pivot = new Vector2(alignToRight ? 1f : 0f, alignToTop ? 1f : 0f);

        // Устанавливаем отступы от края
        Vector2 anchoredPosition = new Vector2(
            alignToRight ? -offsetFromEdge.x : offsetFromEdge.x,
            alignToTop ? -offsetFromEdge.y : offsetFromEdge.y
        );
        rectTransform.anchoredPosition = anchoredPosition;

        Debug.Log($"[{gameObject.name}] Positioned: AnchorMin={rectTransform.anchorMin}, AnchorMax={rectTransform.anchorMax}, AnchoredPosition={rectTransform.anchoredPosition}");
    }
}