using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ShelfCompletionChecker : MonoBehaviour
{
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button exitButton;

    public bool AreAllShelvesEmpty(List<CatSortMode.Shelf> shelves)
    {
        foreach (var shelf in shelves)
        {
            if (shelf.cats.Count > 0)
            {
                return false;
            }
        }
        return true;
    }

    public void CheckAllShelvesEmpty(List<CatSortMode.Shelf> shelves)
    {
        if (!AreAllShelvesEmpty(shelves))
        {
            return;
        }

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetGameActive(false);
        }

        ShowLevelCompletePanel(shelves.Count * 4);
    }

    private void ShowLevelCompletePanel(int rawScore)
    {
        int current = PlayerPrefs.GetInt("CatSortLevel", 0);
        PlayerPrefs.SetInt("CatSortLevel", current + 1);
        PlayerPrefs.Save();

        Time.timeScale = 0f;
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                if (levelCompletePanel != null)
                {
                    levelCompletePanel.SetActive(false);
                }
                if (GameModeManager.Instance != null && GameModeManager.Instance.catSortMode != null)
                {
                    GameModeManager.Instance.catSortMode.ResetLevel();
                    GameModeManager.Instance.catSortMode.GenerateLevel();
                }
            });
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                if (levelCompletePanel != null)
                {
                    levelCompletePanel.SetActive(false);
                }
                SceneManager.LoadScene("ModeSelectScene");
            });
        }

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.ShowLevelCompletePanel(rawScore, 0, 0);
        }
    }
}
