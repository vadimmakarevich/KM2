using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel; // Ссылка на панель Game Over
    public GameModeManager gameModeManager; // Ссылка на GameModeManager

    // Перезапуск текущего режима
    public void RestartGame()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Возобновляем время
        Time.timeScale = 1f;

        // Перезагружаем GameScene
        SceneManager.LoadScene("GameScene");
    }

    // Возвращение на главное меню
    public void ReturnToMainMenu()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Возобновляем время
        Time.timeScale = 1f;

        // Загружаем главное меню
        SceneManager.LoadScene("MainMenu");
    }
}