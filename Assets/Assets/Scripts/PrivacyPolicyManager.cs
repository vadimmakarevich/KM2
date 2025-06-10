using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class PrivacyPolicyManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Elements")]
    public Button termsAndPrivacyButton; // Кнопка для открытия политики
    public GameObject termsPanel; // Панель с текстом политики
    public TextMeshProUGUI termsText; // Текст политики (для проверки)
    public ScrollRect scrollRect; // Ссылка на ScrollRect для управления прокруткой

    private Vector2 lastDragPosition;
    private bool isDragging;

    void Start()
    {
        if (termsAndPrivacyButton == null) Debug.LogError("TermsAndPrivacyButton not assigned!");
        if (termsPanel == null) Debug.LogError("TermsPanel not assigned!");
        if (termsText == null) Debug.LogError("TermsText not assigned!");
        if (scrollRect == null) Debug.LogError("ScrollRect not assigned!");

        if (termsAndPrivacyButton != null)
        {
            termsAndPrivacyButton.onClick.AddListener(ShowTermsPanel);
        }
        if (termsPanel != null)
        {
            termsPanel.SetActive(false);
        }
    }

    private void ShowTermsPanel()
    {
        if (termsPanel != null)
        {
            termsPanel.SetActive(true);
            CanvasGroup canvasGroup = termsPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                StartCoroutine(FadeInTermsPanel(canvasGroup));
            }
        }
    }

    private IEnumerator FadeInTermsPanel(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void CloseTermsPanel()
    {
        if (termsPanel != null)
        {
            termsPanel.SetActive(false);
        }
    }

    // Обработка свайпа
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        lastDragPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || scrollRect == null) return;

        Vector2 currentPosition = eventData.position;
        float deltaY = (currentPosition.y - lastDragPosition.y) / Screen.height;
        lastDragPosition = currentPosition;

        // Обновляем позицию прокрутки
        float newNormalizedPosition = scrollRect.normalizedPosition.y - (deltaY * scrollRect.scrollSensitivity * 0.01f);
        scrollRect.normalizedPosition = new Vector2(scrollRect.normalizedPosition.x, Mathf.Clamp01(newNormalizedPosition));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
}