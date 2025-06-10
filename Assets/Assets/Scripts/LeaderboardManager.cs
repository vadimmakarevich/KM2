using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardManager : MonoBehaviour
{
    private const string SurvivalScoresKey = "SurvivalScores";
    private const string TimerScoresKey = "TimerScores";
    private const string AutoSpawnScoresKey = "AutoSpawnScores"; // Новый ключ для AutoSpawn
    private const int MaxScores = 5;

    private static LeaderboardManager instance;

    public static LeaderboardManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<LeaderboardManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("LeaderboardManager");
                    instance = obj.AddComponent<LeaderboardManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void AddScore(int score, GameModeManager.GameMode mode)
    {
        string key = GetKeyForMode(mode);
        List<int> scores = GetScores(key);

        Debug.Log($"Перед добавлением результата: {score}, текущий список: {string.Join(", ", scores)}");
        scores.Add(score);
        Debug.Log($"После добавления результата: {score}, текущий список: {string.Join(", ", scores)}");

        scores = scores.OrderByDescending(s => s).Take(MaxScores).ToList();
        Debug.Log($"После сортировки и обрезки: {string.Join(", ", scores)}");

        SaveScores(key, scores);
    }

    public List<int> GetTopScores(GameModeManager.GameMode mode, int count = MaxScores)
    {
        string key = GetKeyForMode(mode);
        List<int> scores = GetScores(key);
        Debug.Log($"Получены топ результаты для режима {key}: {string.Join(", ", scores)}");
        return scores.OrderByDescending(s => s).Take(count).ToList();
    }

    private string GetKeyForMode(GameModeManager.GameMode mode)
    {
        switch (mode)
        {
            case GameModeManager.GameMode.Survival:
                return SurvivalScoresKey;
            case GameModeManager.GameMode.Timer:
                return TimerScoresKey;
            case GameModeManager.GameMode.AutoSpawn:
                return AutoSpawnScoresKey;
            default:
                Debug.LogError($"Неизвестный режим: {mode}. Используем TimerScoresKey по умолчанию.");
                return TimerScoresKey;
        }
    }

    private List<int> GetScores(string key)
    {
        List<int> scores = new List<int>();
        if (PlayerPrefs.HasKey(key))
        {
            string scoresString = PlayerPrefs.GetString(key);
            Debug.Log($"Загружены данные из PlayerPrefs для ключа {key}: {scoresString}");
            if (string.IsNullOrEmpty(scoresString))
            {
                Debug.LogWarning($"Данные для ключа {key} пусты!");
                return scores;
            }

            string[] scoresArray = scoresString.Split(',');
            foreach (string score in scoresArray)
            {
                if (string.IsNullOrWhiteSpace(score))
                {
                    Debug.LogWarning($"Пропущен пустой элемент в данных: {scoresString}");
                    continue;
                }

                if (int.TryParse(score, out int parsedScore))
                {
                    scores.Add(parsedScore);
                }
                else
                {
                    Debug.LogWarning($"Не удалось распарсить результат: {score}");
                }
            }
        }
        else
        {
            Debug.Log($"Ключ {key} не найден в PlayerPrefs, возвращаем пустой список");
        }
        return scores;
    }

    private void SaveScores(string key, List<int> scores)
    {
        scores = scores.Where(s => s > 0).ToList();
        string scoresString = string.Join(",", scores);
        PlayerPrefs.SetString(key, scoresString);
        PlayerPrefs.Save();
        Debug.Log($"Сохранены результаты для ключа {key}: {scoresString}");
    }
}