using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Кнопка "Start" в главном меню
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    // Возврат в главное меню
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}