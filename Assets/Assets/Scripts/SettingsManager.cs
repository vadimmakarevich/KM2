using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    private AudioVibrationManager audioVibrationManager;

    public Toggle musicToggle;
    public Toggle sfxToggle;
    public Toggle vibrationToggle;
    public Button exitButton;

    void Start()
    {
        Debug.Log("SettingsManager Start() called.");
        audioVibrationManager = AudioVibrationManager.Instance;
        if (audioVibrationManager == null)
        {
            Debug.LogError("AudioVibrationManager not found in scene!");
            return;
        }

        // Проверка UI компонентов
        if (musicToggle == null) Debug.LogError("MusicToggle not assigned!");
        if (sfxToggle == null) Debug.LogError("SFXToggle not assigned!");
        if (vibrationToggle == null) Debug.LogError("VibrationToggle not assigned!");
        if (exitButton == null) Debug.LogError("ExitButton not assigned!");

        // Проверка активности и интерактивности
        if (musicToggle != null)
        {
            if (!musicToggle.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("MusicToggle is not active! Activating it.");
                musicToggle.gameObject.SetActive(true);
            }
            if (!musicToggle.interactable)
            {
                Debug.LogWarning("MusicToggle is not interactable! Enabling it.");
                musicToggle.interactable = true;
            }
            Graphic musicGraphic = musicToggle.GetComponent<Graphic>() ?? musicToggle.GetComponentInChildren<Graphic>();
            if (musicGraphic != null && !musicGraphic.raycastTarget)
            {
                Debug.LogWarning("MusicToggle has no Raycast Target! Enabling it.");
                musicGraphic.raycastTarget = true;
            }
            musicToggle.isOn = audioVibrationManager.IsMusicEnabled();
            Debug.Log($"MusicToggle initial state: {musicToggle.isOn}");
        }

        if (sfxToggle != null)
        {
            if (!sfxToggle.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("SFXToggle is not active! Activating it.");
                sfxToggle.gameObject.SetActive(true);
            }
            if (!sfxToggle.interactable)
            {
                Debug.LogWarning("SFXToggle is not interactable! Enabling it.");
                sfxToggle.interactable = true;
            }
            Graphic sfxGraphic = sfxToggle.GetComponent<Graphic>() ?? sfxToggle.GetComponentInChildren<Graphic>();
            if (sfxGraphic != null && !sfxGraphic.raycastTarget)
            {
                Debug.LogWarning("SFXToggle has no Raycast Target! Enabling it.");
                sfxGraphic.raycastTarget = true;
            }
            sfxToggle.isOn = audioVibrationManager.IsSFXEnabled();
            Debug.Log($"SFXToggle initial state: {sfxToggle.isOn}");
        }

        if (vibrationToggle != null)
        {
            if (!vibrationToggle.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("VibrationToggle is not active! Activating it.");
                vibrationToggle.gameObject.SetActive(true);
            }
            if (!vibrationToggle.interactable)
            {
                Debug.LogWarning("VibrationToggle is not interactable! Enabling it.");
                vibrationToggle.interactable = true;
            }
            Graphic vibrationGraphic = vibrationToggle.GetComponent<Graphic>() ?? vibrationToggle.GetComponentInChildren<Graphic>();
            if (vibrationGraphic != null && !vibrationGraphic.raycastTarget)
            {
                Debug.LogWarning("VibrationToggle has no Raycast Target! Enabling it.");
                vibrationGraphic.raycastTarget = true;
            }
            vibrationToggle.isOn = audioVibrationManager.IsVibrationEnabled();
            Debug.Log($"VibrationToggle initial state: {vibrationToggle.isOn}");
        }

        if (exitButton != null)
        {
            if (!exitButton.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("ExitButton is not active! Activating it.");
                exitButton.gameObject.SetActive(true);
            }
            if (!exitButton.interactable)
            {
                Debug.LogWarning("ExitButton is not interactable! Enabling it.");
                exitButton.interactable = true;
            }
            Graphic exitGraphic = exitButton.GetComponent<Graphic>() ?? exitButton.GetComponentInChildren<Graphic>();
            if (exitGraphic != null && !exitGraphic.raycastTarget)
            {
                Debug.LogWarning("ExitButton has no Raycast Target! Enabling it.");
                exitGraphic.raycastTarget = true;
            }
        }

        // Проверка CanvasGroup родителя
        CanvasGroup canvasGroup = GetComponentInParent<CanvasGroup>();
        if (canvasGroup != null)
        {
            if (!canvasGroup.interactable || !canvasGroup.blocksRaycasts)
            {
                Debug.LogWarning("Parent CanvasGroup is not interactable or blocksRaycasts is false! Fixing it.");
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        // Привязка событий
        if (musicToggle != null)
        {
            musicToggle.onValueChanged.RemoveAllListeners();
            musicToggle.onValueChanged.AddListener((isOn) =>
            {
                Debug.Log($"MusicToggle clicked, isOn: {isOn}");
                OnMusicToggleChanged(isOn);
            });
            Debug.Log("MusicToggle: onValueChanged bound to OnMusicToggleChanged");
        }
        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.RemoveAllListeners();
            sfxToggle.onValueChanged.AddListener((isOn) =>
            {
                Debug.Log($"SFXToggle clicked, isOn: {isOn}");
                OnSFXToggleChanged(isOn);
            });
            Debug.Log("SFXToggle: onValueChanged bound to OnSFXToggleChanged");
        }
        if (vibrationToggle != null)
        {
            vibrationToggle.onValueChanged.RemoveAllListeners();
            vibrationToggle.onValueChanged.AddListener((isOn) =>
            {
                Debug.Log($"VibrationToggle clicked, isOn: {isOn}");
                OnVibrationToggleChanged(isOn);
            });
            Debug.Log("VibrationToggle: onValueChanged bound to OnVibrationToggleChanged");
        }
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(() =>
            {
                Debug.Log("ExitButton clicked!");
                OnExitButtonClicked();
            });
            Debug.Log("ExitButton: onClick bound to OnExitButtonClicked");

            // Проверка сцены для exitButton
            string currentScene = SceneManager.GetActiveScene().name;
            exitButton.gameObject.SetActive(currentScene == "GameScene");
            Debug.Log($"ExitButton active: {exitButton.gameObject.activeInHierarchy}, current scene: {currentScene}");
        }
    }

    private void OnMusicToggleChanged(bool isOn)
    {
        Debug.Log($"OnMusicToggleChanged called, isOn: {isOn}");
        if (audioVibrationManager != null)
        {
            audioVibrationManager.SetMusicEnabled(isOn);
            Debug.Log($"Music toggle changed to: {isOn}");
        }
    }

    private void OnSFXToggleChanged(bool isOn)
    {
        Debug.Log($"OnSFXToggleChanged called, isOn: {isOn}");
        if (audioVibrationManager != null)
        {
            audioVibrationManager.SetSoundEnabled(isOn);
            Debug.Log($"SFX toggle changed to: {isOn}");
        }
    }

    private void OnVibrationToggleChanged(bool isOn)
    {
        Debug.Log($"OnVibrationToggleChanged called, isOn: {isOn}");
        if (audioVibrationManager != null)
        {
            audioVibrationManager.SetVibrationEnabled(isOn);
            Debug.Log($"Vibration toggle changed to: {isOn}");
        }
    }

    private void OnExitButtonClicked()
    {
        Debug.Log("ExitButton pressed!");
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "GameScene")
        {
            Debug.Log("Exiting game in GameScene...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            Debug.Log("Exiting Unity Editor.");
#endif
        }
        else
        {
            Debug.LogWarning($"Attempt to exit game not in GameScene (current scene: {currentScene}). Exit cancelled.");
        }
    }

    public void TestToggleClick()
    {
        Debug.Log("Toggle clicked! (TestToggleClick)");
    }
}