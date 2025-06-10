using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public TextMeshProUGUI scoreLabelText;
    public TextMeshProUGUI scoreValueText;
    public TextMeshProUGUI multiplierText;
    public Image scoreImage;
    private int _score = 0;
    private Vector3 originalScale;

    [Header("Score Animation Settings")]
    public float scoreScaleDuration = 0.15f;

    [Header("Highlight Animation Settings")]
    public float highlightScaleAmount = 0.1f;
    public float highlightScaleDuration = 0.2f;

    [Header("Multiplier Text Colors")]
    public Color x1Color = Color.white;
    public Color x2Color = Color.yellow;
    public Color x3Color = new Color(0.5f, 0f, 1f);
    public Color x4Color = Color.red;

    [Header("Preview Settings")]
    public RectTransform previewRectTransform; // Ссылка на RectTransform превьюшки
    public float previewOffset = 10f; // Расстояние между текстом очков и превьюшкой

    private int currentMultiplier = 1;
    private List<MergeableObject> highlightedObjects = new List<MergeableObject>();
    private Dictionary<MergeableObject, Vector3> originalObjectScales = new Dictionary<MergeableObject, Vector3>();
    private HashSet<MergeableObject> objectsBeingAnimated = new HashSet<MergeableObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        HideScoreUI();
    }

    void Start()
    {
        if (scoreValueText != null)
        {
            originalScale = scoreValueText.transform.localScale;
        }

        Canvas scoreCanvas = GetComponentInParent<Canvas>();
        if (scoreCanvas != null)
        {
            scoreCanvas.sortingOrder = 20;
        }

        if (multiplierText != null)
        {
            multiplierText.gameObject.SetActive(false);
            multiplierText.text = "x1";
            multiplierText.color = x1Color;
        }

        UpdateScoreText(); // Устанавливаем начальную позицию превьюшки
    }

    public void AddScore(int points)
    {
        if (!gameObject.activeInHierarchy) return;

        int pointsWithMultiplier = points * currentMultiplier;
        _score += pointsWithMultiplier;
        UpdateScoreText();

        StopAllCoroutines();
        StartCoroutine(AnimateScoreAdd(pointsWithMultiplier));

        ResetMultiplier();
    }

    private void UpdateScoreText()
    {
        if (scoreLabelText != null && scoreValueText != null)
        {
            // Убедимся, что UI-элементы активны перед обновлением
            if (!scoreLabelText.gameObject.activeInHierarchy)
            {
                scoreLabelText.gameObject.SetActive(true);
            }
            if (!scoreValueText.gameObject.activeInHierarchy)
            {
                scoreValueText.gameObject.SetActive(true);
            }

            scoreLabelText.text = " ";
            scoreValueText.text = _score.ToString();

            // Обновляем позицию превьюшки
            if (previewRectTransform != null)
            {
                // Принудительно обновляем layout, чтобы получить корректную ширину текста
                LayoutRebuilder.ForceRebuildLayoutImmediate(scoreValueText.GetComponent<RectTransform>());

                // Получаем ширину текста очков
                float textWidth = scoreValueText.preferredWidth;

                // Получаем текущую позицию текста очков
                RectTransform scoreRect = scoreValueText.GetComponent<RectTransform>();
                Vector2 scoreAnchoredPosition = scoreRect.anchoredPosition;

                // Вычисляем новую позицию превьюшки
                // Превьюшка должна быть слева от текста очков
                float previewX = scoreAnchoredPosition.x - textWidth - previewOffset;

                // Устанавливаем новую позицию превьюшки
                previewRectTransform.anchoredPosition = new Vector2(previewX, previewRectTransform.anchoredPosition.y);
            }
        }
    }

    public int GetScore()
    {
        return _score;
    }

    public void ResetScore()
    {
        // Убираем проверку activeInHierarchy, чтобы сброс очков происходил всегда
        _score = 0;
        Debug.Log($"ScoreManager: ResetScore called, _score set to {_score}");
        UpdateScoreText();
        ResetMultiplier();
    }

    public void Initialize()
    {
        if (scoreLabelText != null && scoreValueText != null)
        {
            scoreLabelText.gameObject.SetActive(true);
            scoreValueText.gameObject.SetActive(true);
            if (multiplierText != null)
            {
                // Не включаем multiplierText при инициализации, оставляем его выключенным
                multiplierText.gameObject.SetActive(false);
                multiplierText.text = "x1";
                multiplierText.color = x1Color;
            }
            if (scoreImage != null)
            {
                scoreImage.gameObject.SetActive(true);
            }
        }
        ResetScore();
        UpdateScoreText();
        Debug.Log("ScoreManager: Initialize called");
    }

    public void HideScoreUI()
    {
        if (scoreLabelText != null && scoreValueText != null)
        {
            scoreLabelText.gameObject.SetActive(false);
            scoreValueText.gameObject.SetActive(false);
            if (multiplierText != null)
            {
                multiplierText.gameObject.SetActive(false);
            }
            if (scoreImage != null)
            {
                scoreImage.gameObject.SetActive(false);
            }
        }

        ResetMultiplier();
    }

    public void ShowScoreUI()
    {
        if (scoreValueText != null && gameObject.activeInHierarchy)
        {
            scoreValueText.gameObject.SetActive(true);
            scoreValueText.text = _score.ToString();
            UpdateScoreText(); // Ensure preview position is updated
        }
        if (scoreLabelText != null)
        {
            scoreLabelText.gameObject.SetActive(true);
        }
        if (scoreImage != null)
        {
            scoreImage.gameObject.SetActive(true);
        }
    }

    public void UpdateMultiplier(List<MergeableObject> selectedObjects)
    {
        List<MergeableObject> newObjects = new List<MergeableObject>();

        foreach (var obj in selectedObjects)
        {
            if (!highlightedObjects.Contains(obj) && !objectsBeingAnimated.Contains(obj))
            {
                newObjects.Add(obj);
            }
        }

        highlightedObjects.Clear();
        highlightedObjects.AddRange(selectedObjects);

        int count = highlightedObjects.Count;

        if (count >= 2 && count <= 5)
        {
            currentMultiplier = 1;
            if (multiplierText != null) multiplierText.color = x1Color;
        }
        else if (count >= 6 && count <= 9)
        {
            currentMultiplier = 2;
            if (multiplierText != null) multiplierText.color = x2Color;
        }
        else if (count >= 10 && count <= 15)
        {
            currentMultiplier = 3;
            if (multiplierText != null) multiplierText.color = x3Color;
        }
        else if (count >= 16)
        {
            currentMultiplier = 4;
            if (multiplierText != null) multiplierText.color = x4Color;
        }
        else
        {
            currentMultiplier = 1;
            if (multiplierText != null) multiplierText.color = x1Color;
        }

        HighlightObjects(newObjects);

        if (multiplierText != null)
        {
            multiplierText.text = $"x{currentMultiplier}";
            multiplierText.gameObject.SetActive(currentMultiplier >= 2);
        }
    }

    private void HighlightObjects(List<MergeableObject> objectsToHighlight)
    {
        foreach (var obj in objectsToHighlight)
        {
            if (obj != null)
            {
                SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    if (!originalObjectScales.ContainsKey(obj))
                    {
                        originalObjectScales[obj] = obj.transform.localScale;
                    }

                    if (!objectsBeingAnimated.Contains(obj))
                    {
                        objectsBeingAnimated.Add(obj);
                        StartCoroutine(ScaleObjectOnce(obj));
                    }
                }
            }
        }
    }

    private IEnumerator ScaleObjectOnce(MergeableObject obj)
    {
        if (obj == null || !originalObjectScales.ContainsKey(obj)) yield break;

        Vector3 baseScale = originalObjectScales[obj];

        float elapsed = 0f;
        while (elapsed < highlightScaleDuration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (highlightScaleDuration / 2);
            if (obj != null)
            {
                obj.transform.localScale = Vector3.Lerp(baseScale, baseScale * (1 + highlightScaleAmount), t);
            }
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < highlightScaleDuration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (highlightScaleDuration / 2);
            if (obj != null)
            {
                obj.transform.localScale = Vector3.Lerp(baseScale * (1 + highlightScaleAmount), baseScale, t);
            }
            yield return null;
        }

        if (obj != null)
        {
            obj.transform.localScale = baseScale;
        }

        if (objectsBeingAnimated.Contains(obj))
        {
            objectsBeingAnimated.Remove(obj);
        }
    }

    private void ResetMultiplier()
    {
        currentMultiplier = 1;
        if (multiplierText != null)
        {
            multiplierText.text = "x1";
            multiplierText.color = x1Color;
            multiplierText.gameObject.SetActive(false);
        }

        foreach (var obj in highlightedObjects)
        {
            if (obj != null && originalObjectScales.ContainsKey(obj))
            {
                obj.transform.localScale = originalObjectScales[obj];
            }
        }

        highlightedObjects.Clear();
        originalObjectScales.Clear();
        objectsBeingAnimated.Clear();
    }

    private IEnumerator AnimateScoreAdd(int addedScore)
    {
        Vector3 targetScale = originalScale * 1.2f;
        float elapsedTime = 0f;

        while (elapsedTime < scoreScaleDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (scoreScaleDuration / 2);
            if (scoreValueText != null)
            {
                scoreValueText.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            }
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < scoreScaleDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (scoreScaleDuration / 2);
            if (scoreValueText != null)
            {
                scoreValueText.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            }
            yield return null;
        }

        if (scoreValueText != null)
        {
            scoreValueText.transform.localScale = originalScale;
        }
    }
}