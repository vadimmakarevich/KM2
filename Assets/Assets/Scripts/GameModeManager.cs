using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class GameModeManager : MonoBehaviour
{
    private static GameModeManager _instance;
    public static GameModeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameModeManager>();
            }
            return _instance;
        }
    }

    public GameObject gameModePanel;
    public GameObject survivalPanel;
    public GameObject timerPanel;
    public GameObject gameOverPanel;
    public GameObject homeButtonPanel;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI countdownText;

    [Header("Game Over UI")]
    public Transform scoresContainer;
    public GameObject scoreEntryPrefab;
    public TextMeshProUGUI currentScoreText;
    [SerializeField] private Color topScoreColor = new Color(0.7f, 0.3f, 1f);

    [Header("Settings UI")]
    public GameObject settingsPanel;
    public GameObject exitConfirmationPopup;
    public GameObject exitFinalPopup;
    public Button exitButton;
    public Button closeSettingsButton;
    public Button settingsButton;

    [Header("Performance Settings")]
    [SerializeField] private int targetFrameRate = -1;

    [Header("Merge Mode")]
    public GameObject mergeModePanel;
    [SerializeField] private FloorCollision floorCollision;
    private MergeManager[] mergeManagers;
    public GameObject startPanel;
    public Button startButton;
    public TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI startPanelGoalsText;
    [SerializeField] private TextMeshProUGUI stepsCounterText;

    [Header("Cat Sort Mode")]
    public GameObject catSortPanel;
    public CatSortMode catSortMode;
    public Button catSortStartButton;

    [Header("Level Complete UI")]
    public GameObject levelCompletePanel;
    public TextMeshProUGUI levelCompleteScoreText;
    public Button nextLevelButton;
    public Button exitLevelButton;
    public Button retryLevelButton;
    public Image[] starImages;
    public Image[] superStarImages;

    [System.Serializable]
    public struct GoalUIElement
    {
        public Image previewImage;
        public TextMeshProUGUI goalText;
        public Image backgroundImage;
    }

    [System.Serializable]
    public struct GoalPositions
    {
        public Vector2[] positions;
    }

    [System.Serializable]
    public struct PanelLayout
    {
        public GoalPositions oneGoal;
        public GoalPositions twoGoals;
        public GoalPositions threeGoals;
        public GoalPositions fourGoals;
    }

    [Header("Merge Mode UI Settings")]
    [SerializeField] private GameObject goalDisplayPrefab;
    [SerializeField] private Sprite stepsIcon;
    [SerializeField] private GameObject goalDisplayMergeModePrefab;
    [SerializeField] private PanelLayout startPanelLayout;
    [SerializeField] private PanelLayout mergeModePanelLayout;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080, 1920);
    private List<GoalUIElement> startPanelGoalUIElements = new List<GoalUIElement>();
    private List<GoalUIElement> gameGoalUIElements = new List<GoalUIElement>();

    public enum GameMode
    {
        Survival,
        Timer,
        AutoSpawn,
        MergeMode,
        CatSort
    }

    private GameMode currentMode = GameMode.Survival;
    private float timerDuration = 45f;
    private float remainingTime;

    public GameMode CurrentMode
    {
        get { return currentMode; }
    }

    [Header("AutoSpawn Settings")]
    [SerializeField] private float minSpawnRate = 1f;
    [SerializeField] private float maxSpawnRate = 10f;

    private PointerController pointerController;
    private PreviewManager previewManager;
    private ScoreManager scoreManager;
    private AudioVibrationManager audioVibrationManager;
    private LeaderboardManager leaderboardManager;
    private MergeModeManager mergeModeManager;

    private bool isGameActive = false;

    private int pendingStartScore;
    private int pendingFinalScore;
    private int pendingStar2Threshold;
    private int pendingStar3Threshold;
    private int pendingSuperStarThreshold;

    void Start()
    {
        Debug.Log("GameModeManager Start() called.");
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0;

        CheckCanvasScaler();
        CleanupRemainingMergeModeObjects();

        UpdateMergeManagers();
        leaderboardManager = LeaderboardManager.Instance;
        pointerController = FindObjectOfType<PointerController>();
        previewManager = FindObjectOfType<PreviewManager>();
        scoreManager = FindObjectOfType<ScoreManager>();
        audioVibrationManager = AudioVibrationManager.Instance;
        mergeModeManager = FindObjectOfType<MergeModeManager>();
        catSortMode = FindObjectOfType<CatSortMode>();

        if (floorCollision == null)
        {
            floorCollision = FindObjectOfType<FloorCollision>();
        }

        if (gameModePanel == null) Debug.LogError("GameModePanel not assigned!");
        if (survivalPanel == null) Debug.LogError("SurvivalPanel not assigned!");
        if (timerPanel == null) Debug.LogError("TimerPanel not assigned!");
        if (gameOverPanel == null) Debug.LogError("GameOverPanel not assigned!");
        if (homeButtonPanel == null) Debug.LogError("HomeButtonPanel not assigned!");
        if (timerText == null) Debug.LogError("TimerText not assigned!");
        if (countdownText == null) Debug.LogError("CountdownText not assigned!");
        if (scoresContainer == null) Debug.LogError("ScoresContainer not assigned!");
        if (scoreEntryPrefab == null) Debug.LogError("ScoreEntryPrefab not assigned!");
        if (currentScoreText == null) Debug.LogError("CurrentScoreText not assigned!");
        if (settingsPanel == null) Debug.LogError("SettingsPanel not assigned!");
        if (exitConfirmationPopup == null) Debug.LogError("ExitConfirmationPopup not assigned!");
        if (exitFinalPopup == null) Debug.LogError("ExitFinalPopup not assigned!");
        if (exitButton == null)
        {
            exitButton = GameObject.Find("ExitButton")?.GetComponent<Button>();
            if (exitButton == null)
            {
                Debug.LogError("ExitButton not assigned and not found! Ensure a GameObject named 'ExitButton' exists.");
            }
        }
        if (closeSettingsButton == null) Debug.LogError("CloseSettingsButton not assigned!");
        if (settingsButton == null)
        {
            settingsButton = GameObject.Find("SettingsButton")?.GetComponent<Button>();
            if (settingsButton == null)
            {
                Debug.LogError("SettingsButton not assigned and not found! Ensure a GameObject named 'SettingsButton' exists.");
            }
        }
        if (catSortPanel == null) Debug.LogError("CatSortPanel not assigned!");
        if (catSortStartButton == null) Debug.LogError("CatSortStartButton not assigned!");

        ConfigureButtons();

        survivalPanel?.SetActive(false);
        timerPanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
        countdownText?.gameObject.SetActive(false);
        pointerController?.HidePreview();
        previewManager?.gameObject.SetActive(false);
        pointerController?.LockMovement();
        homeButtonPanel?.SetActive(true);
        settingsPanel?.SetActive(false);
        exitConfirmationPopup?.SetActive(false);
        exitFinalPopup?.SetActive(false);
        mergeModePanel?.SetActive(false);
        startPanel?.SetActive(false);
        levelCompletePanel?.SetActive(false);
        catSortPanel?.SetActive(false);
        if (stepsCounterText != null)
        {
            stepsCounterText.gameObject.SetActive(false);
        }

        if (settingsButton != null && !isGameActive)
        {
            settingsButton.gameObject.SetActive(false);
            Debug.Log("SettingsButton hidden in main menu.");
        }

        if (scoreManager != null)
        {
            scoreManager.gameObject.SetActive(false);
            Debug.Log("ScoreCanvas hidden in main menu.");
        }

        CanvasGroup settingsCanvasGroup = settingsPanel?.GetComponent<CanvasGroup>();
        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = 0f;
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
        }
        else if (settingsPanel != null)
        {
            Debug.LogWarning("SettingsPanel has no CanvasGroup component!");
        }

        int savedMode = PlayerPrefs.GetInt("CurrentMode", -1);
        if (savedMode != -1)
        {
            currentMode = (GameMode)savedMode;
            InitializeGame();
        }
        else
        {
            gameModePanel?.SetActive(true);
            audioVibrationManager?.PlayBackgroundMusic(audioVibrationManager.menuMusic);
        }
    }

    private void ConfigureButtons()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(() => ShowExitConfirmation());
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(() => ShowSettings());
        }
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(() =>
            {
                if (currentMode == GameMode.MergeMode && mergeModeManager != null)
                {
                    mergeModeManager.ProceedToNextLevel();
                }
                else if (currentMode == GameMode.CatSort && catSortMode != null)
                {
                    catSortMode.ResetLevel();
                    catSortMode.GenerateLevel();
                }
                levelCompletePanel?.SetActive(false);
                if (currentMode == GameMode.MergeMode) startPanel?.SetActive(true);
                else if (currentMode == GameMode.CatSort) catSortPanel?.SetActive(true);
                Time.timeScale = 1f;
                isGameActive = true;
                if (currentMode == GameMode.MergeMode) mergeModeManager?.Resume();
                else if (currentMode == GameMode.CatSort) catSortMode?.Resume();
                scoreManager?.ShowScoreUI();
                pointerController?.UnlockMovement();
                Canvas.ForceUpdateCanvases();
            });
        }
        if (exitLevelButton != null)
        {
            exitLevelButton.onClick.RemoveAllListeners();
            exitLevelButton.onClick.AddListener(() => SceneManager.LoadScene("GameScene"));
        }
        if (retryLevelButton != null)
        {
            retryLevelButton.onClick.RemoveAllListeners();
            retryLevelButton.onClick.AddListener(() =>
            {
                if (currentMode == GameMode.MergeMode && mergeModeManager != null) RestartLevel();
                else if (currentMode == GameMode.CatSort && catSortMode != null)
                {
                    catSortMode.ResetLevel();
                    catSortMode.GenerateLevel();
                }
                levelCompletePanel?.SetActive(false);
                if (currentMode == GameMode.MergeMode) startPanel?.SetActive(true);
                else if (currentMode == GameMode.CatSort) catSortPanel?.SetActive(true);
                Time.timeScale = 1f;
                isGameActive = true;
                if (currentMode == GameMode.MergeMode) mergeModeManager?.Resume();
                else if (currentMode == GameMode.CatSort) catSortMode?.Resume();
                scoreManager?.ShowScoreUI();
                pointerController?.UnlockMovement();
                Canvas.ForceUpdateCanvases();
            });
        }
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartMergeMode);
        }
        if (catSortStartButton != null)
        {
            catSortStartButton.onClick.AddListener(StartCatSortMode);
        }
        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveAllListeners();
            closeSettingsButton.onClick.AddListener(CloseSettings);
        }
        ConfigurePopupButtons();
    }

    private void ConfigurePopupButtons()
    {
        Button confirmExitButton = exitConfirmationPopup?.transform.Find("ConfirmButton")?.GetComponent<Button>();
        if (confirmExitButton != null) confirmExitButton.onClick.RemoveAllListeners();
        Button cancelExitButton = exitConfirmationPopup?.transform.Find("CancelButton")?.GetComponent<Button>();
        if (cancelExitButton != null) cancelExitButton.onClick.RemoveAllListeners();
        Button finalConfirmExitButton = exitFinalPopup?.transform.Find("ConfirmButton")?.GetComponent<Button>();
        if (finalConfirmExitButton != null) finalConfirmExitButton.onClick.RemoveAllListeners();
        Button finalCancelExitButton = exitFinalPopup?.transform.Find("CancelButton")?.GetComponent<Button>();
        if (finalCancelExitButton != null) finalCancelExitButton.onClick.RemoveAllListeners();

        if (confirmExitButton != null) confirmExitButton.onClick.AddListener(ConfirmExit);
        if (cancelExitButton != null) cancelExitButton.onClick.AddListener(CancelExit);
        if (finalConfirmExitButton != null) finalConfirmExitButton.onClick.AddListener(FinalConfirmExit);
        if (finalCancelExitButton != null) finalCancelExitButton.onClick.AddListener(FinalCancelExit);
    }

    private void CheckCanvasScaler()
    {
        CanvasScaler scaler = mergeModePanel?.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ConstantPixelSize)
            {
                Debug.LogWarning($"mergeModePanel CanvasScaler has ScaleMode={scaler.uiScaleMode}. Changing to ConstantPixelSize.");
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
            }
            Debug.Log($"mergeModePanel CanvasScaler: ScaleMode={scaler.uiScaleMode}, ScaleFactor={scaler.scaleFactor}");
        }
        else
        {
            Debug.Log("mergeModePanel has no CanvasScaler component.");
        }
    }

    private void CleanupRemainingMergeModeObjects()
    {
        MergeableObject[] remainingObjects = FindObjectsOfType<MergeableObject>();
        foreach (var obj in remainingObjects)
        {
            if (obj != null)
            {
                Destroy(obj.gameObject);
            }
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.StartsWith("Cell_"))
            {
                Destroy(obj);
            }
        }
    }

    private void UpdateMergeManagers()
    {
        mergeManagers = FindObjectsOfType<MergeManager>();
    }

    public void SelectSurvivalMode()
    {
        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();
        if (currentMode == GameMode.MergeMode) mergeModeManager?.EndGame();
        else if (currentMode == GameMode.CatSort) catSortMode?.EndGame();
        currentMode = GameMode.Survival;
        PlayerPrefs.SetInt("CurrentMode", (int)GameMode.Survival);
        gameModePanel?.SetActive(false);
        pointerController?.ResetQueue();
        StartGame();
    }

    public void SelectTimerMode()
    {
        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();
        if (currentMode == GameMode.MergeMode) mergeModeManager?.EndGame();
        else if (currentMode == GameMode.CatSort) catSortMode?.EndGame();
        currentMode = GameMode.Timer;
        PlayerPrefs.SetInt("CurrentMode", (int)GameMode.Timer);
        gameModePanel?.SetActive(false);
        pointerController?.ResetQueue();
        StartGame();
    }

    public void SelectAutoSpawnMode()
    {
        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();
        if (currentMode == GameMode.MergeMode) mergeModeManager?.EndGame();
        else if (currentMode == GameMode.CatSort) catSortMode?.EndGame();
        currentMode = GameMode.AutoSpawn;
        PlayerPrefs.SetInt("CurrentMode", (int)GameMode.AutoSpawn);
        gameModePanel?.SetActive(false);
        pointerController?.ResetQueue();
        StartGame();
    }

    public void SelectMergeMode()
    {
        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();
        if (currentMode == GameMode.MergeMode) mergeModeManager?.EndGame();
        else if (currentMode == GameMode.CatSort) catSortMode?.EndGame();
        currentMode = GameMode.MergeMode;
        PlayerPrefs.SetInt("CurrentMode", (int)GameMode.MergeMode);
        gameModePanel?.SetActive(false);
        StartGame();
    }

    public void SelectCatSortMode()
    {
        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();
        if (currentMode == GameMode.MergeMode) mergeModeManager?.EndGame();
        else if (currentMode == GameMode.CatSort) catSortMode?.EndGame();
        currentMode = GameMode.CatSort;
        PlayerPrefs.SetInt("CurrentMode", (int)GameMode.CatSort);
        gameModePanel?.SetActive(false);
        StartGame();
    }

    private void InitializeGame()
    {
        if (currentMode == GameMode.MergeMode)
        {
            mergeModeManager?.InitializeMergeMode();
            if (previewManager != null) previewManager.gameObject.SetActive(false);
            return;
        }
        else if (currentMode == GameMode.CatSort)
        {
            catSortMode?.Initialize();
            if (previewManager != null) previewManager.gameObject.SetActive(false);
            return;
        }

        if (pointerController != null)
        {
            pointerController.gameObject.SetActive(true);
            pointerController.EnableObjectSpawning();
        }

        if (previewManager != null)
        {
            previewManager.gameObject.SetActive(true);
            Canvas previewCanvas = previewManager.GetComponentInParent<Canvas>();
            if (previewCanvas != null) previewCanvas.sortingOrder = 10;
        }

        if (pointerController != null && previewManager != null && pointerController.prefabQueue != null)
        {
            previewManager.InitializePreviews(pointerController, pointerController.prefabQueue);
            pointerController.ShowPreview();
        }
    }

    private void StartGame()
    {
        if (!isGameActive)
        {
            UpdateMergeManagers();

            if (floorCollision != null)
            {
                floorCollision.enabled = (currentMode != GameMode.MergeMode && currentMode != GameMode.CatSort);
            }

            if (pointerController != null)
            {
                pointerController.gameObject.SetActive(currentMode != GameMode.MergeMode && currentMode != GameMode.CatSort);
                pointerController.UnlockMovement();
            }

            if (previewManager != null)
            {
                previewManager.gameObject.SetActive(currentMode != GameMode.MergeMode && currentMode != GameMode.CatSort);
            }

            foreach (var mergeManager in mergeManagers)
            {
                if (mergeManager != null)
                {
                    mergeManager.enabled = (currentMode != GameMode.MergeMode && currentMode != GameMode.CatSort);
                }
            }

            InitializeGame();

            if (scoreManager != null)
            {
                scoreManager.gameObject.SetActive(true);
                scoreManager.Initialize();
                Debug.Log("ScoreCanvas shown during game start.");
            }

            homeButtonPanel?.SetActive(true);
            isGameActive = true;

            if (settingsButton != null)
            {
                settingsButton.gameObject.SetActive(true);
                settingsButton.interactable = true;
                Debug.Log("SettingsButton shown during game start.");
            }

            if (audioVibrationManager != null)
            {
                switch (currentMode)
                {
                    case GameMode.Survival:
                        audioVibrationManager.PlayBackgroundMusic(audioVibrationManager.survivalModeMusic);
                        break;
                    case GameMode.Timer:
                    case GameMode.AutoSpawn:
                        audioVibrationManager.PlayBackgroundMusic(audioVibrationManager.timerModeMusic);
                        break;
                    case GameMode.MergeMode:
                        audioVibrationManager.PlayBackgroundMusic(audioVibrationManager.timerModeMusic);
                        startPanel?.SetActive(true);
                        if (startButton != null) startButton.interactable = true;
                        break;
                    case GameMode.CatSort:
                        audioVibrationManager.PlayBackgroundMusic(audioVibrationManager.timerModeMusic);
                        catSortPanel?.SetActive(true);
                        if (catSortStartButton != null) catSortStartButton.interactable = true;
                        break;
                }
            }
        }

        switch (currentMode)
        {
            case GameMode.Survival:
                survivalPanel?.SetActive(true);
                Canvas survivalCanvas = survivalPanel?.GetComponent<Canvas>();
                if (survivalCanvas != null) survivalCanvas.sortingOrder = 5;
                break;
            case GameMode.Timer:
                timerPanel?.SetActive(true);
                remainingTime = timerDuration;
                Canvas timerCanvas = timerPanel?.GetComponent<Canvas>();
                if (timerCanvas != null) timerCanvas.sortingOrder = 5;
                StartCoroutine(CountdownTimer());
                break;
            case GameMode.AutoSpawn:
                timerPanel?.SetActive(true);
                remainingTime = timerDuration;
                Canvas autoSpawnCanvas = timerPanel?.GetComponent<Canvas>();
                if (autoSpawnCanvas != null) autoSpawnCanvas.sortingOrder = 5;
                StartCoroutine(AutoSpawnMode());
                break;
            case GameMode.MergeMode:
                break;
            case GameMode.CatSort:
                break;
        }
    }

    private IEnumerator CountdownTimer()
    {
        float elapsedTime = 0f;
        while (elapsedTime < timerDuration && isGameActive)
        {
            elapsedTime += Time.deltaTime;
            remainingTime = timerDuration - elapsedTime;
            UpdateTimerText();
            yield return null;
        }
        if (isGameActive) GameOver("Timer");
    }

    private IEnumerator AutoSpawnMode()
    {
        timerText?.gameObject.SetActive(false);
        countdownText?.gameObject.SetActive(true);

        pointerController?.LockMovement();

        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            audioVibrationManager?.PlaySFX(audioVibrationManager.timerTickSound);
            audioVibrationManager?.Vibrate();
            yield return new WaitForSeconds(1f);
        }
        countdownText.text = "GO!";
        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        yield return new WaitForSeconds(0.5f);
        countdownText?.gameObject.SetActive(false);
        timerText?.gameObject.SetActive(true);

        pointerController?.UnlockMovement();

        pointerController?.StartAutoSpawn(timerDuration, minSpawnRate, maxSpawnRate);

        float elapsedTime = 0f;
        while (elapsedTime < timerDuration && isGameActive)
        {
            elapsedTime += Time.deltaTime;
            remainingTime = timerDuration - elapsedTime;
            UpdateTimerText();
            yield return null;
        }
        if (isGameActive) GameOver("AutoSpawn");
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            int secondsLeft = Mathf.CeilToInt(remainingTime);
            timerText.text = $"{secondsLeft}";
            timerText.color = remainingTime <= 10f ? Color.red : Color.white;
        }
    }

    public void SetupLevelGoalsUI(int levelIndex, MergeModeManager.LevelGoal[] goals, GameObject[] prefabs)
    {
        Debug.Log($"SetupLevelGoalsUI called for level {levelIndex}, goals count: {(goals != null ? goals.Length : 0)}, prefabs count: {(prefabs != null ? prefabs.Length : 0)}");
        ClearLevelGoalsUI();

        if (goals == null || goals.Length == 0 || prefabs == null || prefabs.Length == 0 || goalDisplayPrefab == null || goalDisplayMergeModePrefab == null)
        {
            Debug.LogWarning("Invalid input: goals, prefabs, goalDisplayPrefab, or goalDisplayMergeModePrefab is null or empty.");
            return;
        }

        if (levelText != null)
        {
            levelText.text = $"Level {levelIndex + 1}";
            Debug.Log($"Updated levelText to: Level {levelIndex + 1}");
        }
        else
        {
            Debug.LogWarning("levelText is not assigned in GameModeManager!");
        }

        int moveLimit = mergeModeManager != null ? mergeModeManager.GetMoveLimit(levelIndex) : 0;
        if (stepsCounterText != null)
        {
            if (moveLimit > 0)
            {
                stepsCounterText.text = $"Moves: {moveLimit}";
                stepsCounterText.gameObject.SetActive(true);
                Debug.Log($"Updated stepsCounterText to: Moves: {moveLimit}");
            }
            else
            {
                stepsCounterText.gameObject.SetActive(false);
                Debug.Log("stepsCounterText deactivated (moveLimit <= 0).");
            }
        }
        else
        {
            Debug.LogWarning("stepsCounterText is not assigned in GameModeManager!");
        }

        if (startPanelGoalsText != null)
        {
            string goalsText = "Level Goals:\n";
            for (int i = 0; i < goals.Length; i++)
            {
                var goal = goals[i];
                goalsText += $"- {(goal.isMoveLimit ? "Use" : "Collect")} {goal.targetCount} of {(goal.isMoveLimit ? "moves" : $"Level {goal.targetLevel}")}\n";
            }
            startPanelGoalsText.text = goalsText;
            startPanelGoalsText.alignment = TextAlignmentOptions.Center;
            Debug.Log($"Updated startPanelGoalsText to: {goalsText}");
        }
        else
        {
            Debug.LogWarning("startPanelGoalsText is not assigned in GameModeManager!");
        }

        if (startPanel != null)
        {
            CreateGoalsForPanel(startPanel, startPanelLayout, goals, prefabs, startPanelGoalUIElements, goalDisplayPrefab, true, moveLimit);
        }
        else
        {
            Debug.LogWarning("startPanel is not assigned in GameModeManager!");
        }

        if (mergeModePanel != null)
        {
            bool wasActive = mergeModePanel.activeSelf;
            mergeModePanel.SetActive(true);
            CreateGoalsForPanel(mergeModePanel, mergeModePanelLayout, goals, prefabs, gameGoalUIElements, goalDisplayMergeModePrefab, false, moveLimit);
            mergeModePanel.SetActive(wasActive);
        }
        else
        {
            Debug.LogWarning("mergeModePanel is not assigned in GameModeManager!");
        }
    }

    private void CreateGoalsForPanel(GameObject panel, PanelLayout layout, MergeModeManager.LevelGoal[] goals, GameObject[] prefabs, List<GoalUIElement> goalUIElements, GameObject goalPrefab, bool isStartPanel, int moveLimit)
    {
        if (panel == null || goalPrefab == null)
        {
            Debug.LogWarning($"CreateGoalsForPanel: panel or goalPrefab is null. panel={panel}, goalPrefab={goalPrefab}");
            return;
        }

        List<MergeModeManager.LevelGoal> filteredGoals = new List<MergeModeManager.LevelGoal>();
        foreach (var goal in goals)
        {
            if (isStartPanel || !goal.isMoveLimit)
            {
                filteredGoals.Add(goal);
            }
        }

        int goalCount = filteredGoals.Count;
        if (goalCount == 0)
        {
            Debug.LogWarning("No valid goals to display in CreateGoalsForPanel.");
            return;
        }

        GoalPositions selectedPositions;
        switch (goalCount)
        {
            case 1: selectedPositions = layout.oneGoal; break;
            case 2: selectedPositions = layout.twoGoals; break;
            case 3: selectedPositions = layout.threeGoals; break;
            case 4: selectedPositions = layout.fourGoals; break;
            default: selectedPositions = layout.fourGoals; break;
        }

        if (selectedPositions.positions == null || selectedPositions.positions.Length < goalCount)
        {
            Debug.LogWarning($"Invalid layout positions for {goalCount} goals. positions={selectedPositions.positions?.Length}");
            return;
        }

        for (int i = 0; i < goalCount; i++)
        {
            var goal = filteredGoals[i];
            GameObject displayObj = Instantiate(goalPrefab, panel.transform);
            if (displayObj == null)
            {
                Debug.LogWarning("Failed to instantiate goalPrefab.");
                continue;
            }

            RectTransform displayRect = displayObj.GetComponent<RectTransform>();
            if (displayRect != null)
            {
                displayRect.anchoredPosition = selectedPositions.positions[i];
            }
            else
            {
                Debug.LogWarning($"Goal prefab {goalPrefab.name} has no RectTransform.");
            }

            string previewImageName = isStartPanel ? "GoalObject" : "GoalPreviewImage";
            string goalTextName = isStartPanel ? "GoalText" : "goalTextPrefab";
            string backgroundImageName = "GoalCircle";

            Image previewImage = displayObj.transform.Find(previewImageName)?.GetComponent<Image>();
            TextMeshProUGUI goalText = displayObj.transform.Find(goalTextName)?.GetComponent<TextMeshProUGUI>();
            Image backgroundImage = isStartPanel ? displayObj.transform.Find(backgroundImageName)?.GetComponent<Image>() : null;

            if (previewImage == null)
            {
                Debug.LogWarning($"Preview image '{previewImageName}' not found in goal prefab {goalPrefab.name}.");
            }
            if (goalText == null)
            {
                Debug.LogWarning($"Goal text '{goalTextName}' not found in goal prefab {goalPrefab.name}.");
            }
            if (isStartPanel && backgroundImage == null)
            {
                Debug.LogWarning($"Background image '{backgroundImageName}' not found in goal prefab {goalPrefab.name} for start panel.");
            }

            if (goalText != null)
            {
                goalText.text = $"{goal.targetCount}";
                goalText.alignment = TextAlignmentOptions.Center;
            }

            if (previewImage != null)
            {
                if (goal.isMoveLimit)
                {
                    if (stepsIcon != null)
                    {
                        previewImage.sprite = stepsIcon;
                        previewImage.color = Color.white;
                    }
                    else
                    {
                        Debug.LogWarning("stepsIcon is not assigned in GameModeManager!");
                    }
                }
                else if (prefabs.Length > goal.targetLevel - 1 && prefabs[goal.targetLevel - 1] != null)
                {
                    SpriteRenderer spriteRenderer = prefabs[goal.targetLevel - 1].GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null && spriteRenderer.sprite != null)
                    {
                        previewImage.sprite = spriteRenderer.sprite;
                        previewImage.color = Color.white;
                    }
                    else
                    {
                        Debug.LogWarning($"No valid SpriteRenderer or sprite in prefab at index {goal.targetLevel - 1} for goal level {goal.targetLevel}.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Invalid prefab index {goal.targetLevel - 1} for goal level {goal.targetLevel}. Prefabs length: {prefabs.Length}");
                }
            }

            goalUIElements.Add(new GoalUIElement
            {
                previewImage = previewImage,
                goalText = goalText,
                backgroundImage = backgroundImage
            });
        }
    }

    public void UpdateLevelGoalsUI(int levelIndex, MergeModeManager.LevelGoal[] goals, Dictionary<int, int> levelCounts)
    {
        if (goals == null || levelCounts == null)
        {
            Debug.LogWarning("UpdateLevelGoalsUI: goals or levelCounts is null.");
            return;
        }

        for (int i = 0; i < goals.Length && i < gameGoalUIElements.Count; i++)
        {
            var goal = goals[i];
            int remaining = goal.isMoveLimit ? Mathf.Max(0, goal.targetCount - mergeModeManager.GetMoveCount()) : Mathf.Max(0, goal.targetCount - (levelCounts.ContainsKey(goal.targetLevel) ? levelCounts[goal.targetLevel] : 0));
            if (gameGoalUIElements[i].goalText != null)
            {
                gameGoalUIElements[i].goalText.text = $"{remaining}";
                Debug.Log($"Updated goalText[{i}] to: {remaining}");
            }
            else
            {
                Debug.LogWarning($"goalText is null for goalUIElement at index {i} in UpdateLevelGoalsUI.");
            }
        }

        if (mergeModeManager != null)
        {
            int moveLimit = mergeModeManager.GetMoveLimit(levelIndex);
            int movesMade = mergeModeManager.GetMoveCount();
            UpdateStepsCounter(levelIndex, movesMade);
        }
    }

    public void UpdateStepsCounter(int levelIndex, int movesMade)
    {
        if (stepsCounterText == null)
        {
            Debug.LogWarning("stepsCounterText is not assigned in GameModeManager!");
            return;
        }

        int moveLimit = mergeModeManager != null ? mergeModeManager.GetMoveLimit(levelIndex) : 0;
        if (moveLimit > 0)
        {
            int movesLeft = moveLimit - movesMade;
            stepsCounterText.text = $"Moves: {movesLeft}";
            stepsCounterText.gameObject.SetActive(true);
            Debug.Log($"Updated stepsCounterText to: Moves: {movesLeft}");
        }
        else
        {
            stepsCounterText.gameObject.SetActive(false);
            Debug.Log("stepsCounterText deactivated (moveLimit <= 0).");
        }
    }

    private void ClearLevelGoalsUI()
    {
        foreach (var element in startPanelGoalUIElements)
        {
            if (element.previewImage != null) Destroy(element.previewImage.gameObject);
            if (element.goalText != null) Destroy(element.goalText.gameObject);
            if (element.backgroundImage != null) Destroy(element.backgroundImage.gameObject);
        }
        startPanelGoalUIElements.Clear();

        foreach (var element in gameGoalUIElements)
        {
            if (element.previewImage != null) Destroy(element.previewImage.gameObject);
            if (element.goalText != null) Destroy(element.goalText.gameObject);
            if (element.backgroundImage != null) Destroy(element.backgroundImage.gameObject);
        }
        gameGoalUIElements.Clear();
    }

    public void ShowLevelCompletePanel(int rawScore, int optimalMoves, int actualMoves)
    {
        Debug.Log($"ShowLevelCompletePanel called with rawScore: {rawScore}, optimalMoves: {optimalMoves}, actualMoves: {actualMoves}");
        if (!isGameActive) return;

        isGameActive = false;
        if (currentMode == GameMode.MergeMode) mergeModePanel?.SetActive(false);
        else if (currentMode == GameMode.CatSort) catSortPanel?.SetActive(false);
        if (stepsCounterText != null)
        {
            stepsCounterText.gameObject.SetActive(false);
            Debug.Log("stepsCounterText deactivated in ShowLevelCompletePanel.");
        }
        ClearLevelGoalsUI();
        levelCompletePanel?.SetActive(true);

        if (currentMode == GameMode.MergeMode && mergeModeManager != null)
        {
            int newLevelIndex = mergeModeManager.CurrentLevelIndex + 1;
            if (newLevelIndex >= mergeModeManager.Levels.Length) newLevelIndex = 0;
            PlayerPrefs.SetInt("MergeModeLevel", newLevelIndex);
            PlayerPrefs.Save();
            Debug.Log($"ShowLevelCompletePanel: Progress saved. Next level index set to {newLevelIndex}");
        }
        else if (currentMode == GameMode.CatSort && catSortMode != null)
        {
            int newLevelIndex = PlayerPrefs.GetInt("CatSortLevel", 0) + 1;
            PlayerPrefs.SetInt("CatSortLevel", newLevelIndex);
            PlayerPrefs.Save();
            Debug.Log($"ShowLevelCompletePanel: CatSort progress saved. Next level index set to {newLevelIndex}");
        }

        if (starImages != null)
        {
            foreach (var star in starImages)
            {
                if (star != null)
                {
                    star.gameObject.SetActive(false);
                    star.transform.localScale = Vector3.zero;
                    Color starColor = star.color;
                    star.color = new Color(starColor.r, starColor.g, starColor.b, 0f);
                }
            }
        }

        if (superStarImages != null)
        {
            foreach (var superStar in superStarImages)
            {
                if (superStar != null)
                {
                    superStar.gameObject.SetActive(false);
                    superStar.transform.localScale = Vector3.zero;
                    Color starColor = superStar.color;
                    superStar.color = new Color(starColor.r, starColor.g, starColor.b, 0f);
                }
            }
        }

        int baseScore = rawScore; // Простая логика для CatSort, можно расширить
        int minScoreForOneStar = baseScore;
        int star2Threshold = Mathf.RoundToInt(minScoreForOneStar * 1.5f);
        int star3Threshold = Mathf.RoundToInt(minScoreForOneStar * 2f);
        int superStarThreshold = Mathf.RoundToInt(minScoreForOneStar * 3f);

        int finalScore = rawScore;
        Debug.Log($"finalScore set to: {finalScore}");

        pendingStartScore = minScoreForOneStar;
        pendingFinalScore = finalScore;
        pendingStar2Threshold = star2Threshold;
        pendingStar3Threshold = star3Threshold;
        pendingSuperStarThreshold = superStarThreshold;

        levelCompleteScoreText.text = $"Score: {minScoreForOneStar}";

        Debug.Log("Attempting to show interstitial ad in ShowLevelCompletePanel.");
        if (AdManager.Instance != null && PlayerPrefs.GetInt("AdsDisabled", 0) == 0)
        {
            AdManager.Instance.SetOnAdCompletedHandler(() =>
            {
                Debug.Log("Ad completed, starting score and stars animation.");
                StartCoroutine(AnimateScoreAndStars(pendingStartScore, pendingFinalScore, pendingStar2Threshold, pendingStar3Threshold, pendingSuperStarThreshold));
            });
            AdManager.Instance.ShowInterstitialAd();
        }
        else
        {
            Debug.LogWarning("AdManager is null or ads are disabled, starting score and stars animation immediately.");
            StartCoroutine(AnimateScoreAndStars(minScoreForOneStar, finalScore, star2Threshold, star3Threshold, superStarThreshold));
        }

        if (settingsButton != null)
        {
            settingsButton.gameObject.SetActive(false);
            Debug.Log("SettingsButton hidden during Level Complete.");
        }
        if (scoreManager != null)
        {
            scoreManager.gameObject.SetActive(false);
            Debug.Log("ScoreCanvas hidden during Level Complete.");
        }

        Time.timeScale = 0f;
    }

    private IEnumerator AnimateScoreAndStars(int startScore, int targetScore, int star2Threshold, int star3Threshold, int superStarThreshold)
    {
        float duration = 2f;
        float elapsed = 0f;
        int currentScore = startScore;
        List<int> activeStars = new List<int>();

        levelCompleteScoreText.text = $"Score: {startScore}";

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            currentScore = (int)Mathf.Lerp(startScore, targetScore, t);
            levelCompleteScoreText.text = $"Score: {currentScore}";

            if (currentScore >= star2Threshold && activeStars.Count < 2 && starImages.Length > 1)
            {
                if (!activeStars.Contains(1))
                {
                    activeStars.Add(1);
                    starImages[1].gameObject.SetActive(true);
                    StartCoroutine(AnimateStar(1, false));
                }
            }
            if (currentScore >= star3Threshold && activeStars.Count < 3 && starImages.Length > 2)
            {
                if (!activeStars.Contains(2))
                {
                    activeStars.Add(2);
                    starImages[2].gameObject.SetActive(true);
                    StartCoroutine(AnimateStar(2, false));
                }
            }
            if (currentScore >= startScore && activeStars.Count < 1 && starImages.Length > 0)
            {
                if (!activeStars.Contains(0))
                {
                    activeStars.Add(0);
                    starImages[0].gameObject.SetActive(true);
                    StartCoroutine(AnimateStar(0, false));
                }
            }

            yield return null;
        }

        levelCompleteScoreText.text = $"Score: {targetScore}";

        if (targetScore >= superStarThreshold && starImages.Length >= 3 && superStarImages.Length >= 3)
        {
            yield return StartCoroutine(TransformToSuperStars());
        }
    }

    private IEnumerator AnimateStar(int index, bool isSuperStar)
    {
        float animationDuration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale;

        Image[] targetImages = isSuperStar ? superStarImages : starImages;

        if (targetImages.Length > index && targetImages[index] != null)
        {
            switch (index)
            {
                case 0:
                    targetScale = Vector3.one;
                    break;
                case 1:
                    targetScale = new Vector3(1.5f, 1.5f, 1.5f);
                    break;
                case 2:
                    targetScale = Vector3.one;
                    break;
                default:
                    targetScale = Vector3.one;
                    break;
            }

            targetImages[index].transform.localScale = startScale;
            Color starColor = targetImages[index].color;
            float startAlpha = 0f;
            float targetAlpha = 1f;
            targetImages[index].color = new Color(starColor.r, starColor.g, starColor.b, startAlpha);

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / animationDuration;
                targetImages[index].transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                starColor = targetImages[index].color;
                targetImages[index].color = new Color(starColor.r, starColor.g, starColor.b, Mathf.Lerp(startAlpha, targetAlpha, t));
                yield return null;
            }

            targetImages[index].transform.localScale = targetScale;
            targetImages[index].color = new Color(starColor.r, starColor.g, starColor.b, targetAlpha);
        }
    }

    private IEnumerator TransformToSuperStars()
    {
        Debug.Log("TransformToSuperStars: Starting transformation to 3 super stars.");

        for (int i = 0; i < starImages.Length && i < 3; i++)
        {
            if (starImages[i] != null)
            {
                float animationDuration = 0.5f;
                float elapsed = 0f;
                Vector3 startScale = starImages[i].transform.localScale;
                float startAlpha = starImages[i].color.a;

                while (elapsed < animationDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / animationDuration;
                    starImages[i].transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                    Color starColor = starImages[i].color;
                    starImages[i].color = new Color(starColor.r, starColor.g, starColor.b, Mathf.Lerp(startAlpha, 0f, t));
                    yield return null;
                }
                starImages[i].gameObject.SetActive(false);
                Debug.Log($"Regular star {i} faded out.");
            }
        }

        for (int i = 0; i < superStarImages.Length && i < 3; i++)
        {
            if (superStarImages[i] != null)
            {
                superStarImages[i].gameObject.SetActive(true);
                StartCoroutine(AnimateStar(i, true));
                Debug.Log($"Super star {i} animation started.");
            }
        }
    }

    public void GameOver(string source = "unknown")
    {
        if (!isGameActive) return;

        StopAllCoroutines();
        isGameActive = false;

        if (currentMode == GameMode.MergeMode)
        {
            Debug.Log("GameOver in Merge Mode: Pausing game.");
            mergeModeManager?.Pause();
            mergeModePanel?.SetActive(false);
            if (stepsCounterText != null) stepsCounterText.gameObject.SetActive(false);
            ClearLevelGoalsUI();
        }
        else if (currentMode == GameMode.CatSort)
        {
            Debug.Log("GameOver in CatSort Mode: Pausing game.");
            catSortMode?.Pause();
            catSortPanel?.SetActive(false);
            if (stepsCounterText != null) stepsCounterText.gameObject.SetActive(false);
            ClearLevelGoalsUI();
        }
        else
        {
            pointerController?.DisableObjectSpawning();
            pointerController?.HidePreview();
            pointerController?.ResetQueue();
            previewManager?.gameObject.SetActive(false);
            pointerController?.LockMovement();
            survivalPanel?.SetActive(false);
            timerPanel?.SetActive(false);
        }

        UpdateMergeManagers();
        foreach (var mergeManager in mergeManagers)
        {
            if (mergeManager != null) mergeManager.enabled = true;
        }
        if (floorCollision != null) floorCollision.enabled = true;
        if (pointerController != null) pointerController.gameObject.SetActive(true);

        SaveScore();
        homeButtonPanel?.SetActive(false);
        gameOverPanel?.SetActive(true);

        if (settingsButton != null)
        {
            settingsButton.gameObject.SetActive(false);
            Debug.Log("SettingsButton hidden after GameOver.");
        }

        if (scoreManager != null)
        {
            scoreManager.gameObject.SetActive(false);
            scoreManager.HideScoreUI();
            Debug.Log("ScoreCanvas hidden after GameOver.");
        }

        Time.timeScale = 0f;
        audioVibrationManager?.StopBackgroundMusic();

        Debug.Log($"Attempting to show interstitial ad in GameOver (Mode: {source}).");
        if (currentMode == GameMode.MergeMode || currentMode == GameMode.CatSort)
        {
            Debug.Log($"GameOver in {currentMode} Mode: Showing interstitial ad.");
        }
        if (AdManager.Instance != null && PlayerPrefs.GetInt("AdsDisabled", 0) == 0)
        {
            AdManager.Instance.ShowInterstitialAd();
        }
        else
        {
            Debug.LogWarning($"AdManager is null or ads are disabled (PlayerPrefs), skipping interstitial ad in GameOver (Mode: {source}).");
        }

        StartCoroutine(DisplayTopScoresWithAnimation());
    }

    private void SaveScore()
    {
        if (scoreManager == null || leaderboardManager == null) return;
        int finalScore = scoreManager.GetScore();
        leaderboardManager.AddScore(finalScore, currentMode);
    }

    private IEnumerator DisplayTopScoresWithAnimation()
    {
        if (leaderboardManager == null) yield break;

        foreach (Transform child in scoresContainer) Destroy(child.gameObject);

        List<int> topScores = leaderboardManager.GetTopScores(currentMode, 5);
        int currentScore = scoreManager != null ? scoreManager.GetScore() : 0;

        if (currentScoreText != null)
        {
            currentScoreText.text = $"Your score: {currentScore}";
            currentScoreText.alpha = 0f;
        }

        List<TextMeshProUGUI> scoreTexts = new List<TextMeshProUGUI>();
        for (int i = 0; i < 5; i++)
        {
            GameObject entry = Instantiate(scoreEntryPrefab, scoresContainer);
            TextMeshProUGUI text = entry.GetComponent<TextMeshProUGUI>();
            if (i < topScores.Count)
            {
                text.text = $"#{i + 1}: {topScores[i]}";
                text.color = topScores[i] == currentScore ? topScoreColor : Color.white;
            }
            else
            {
                text.text = $"#{i + 1}: -";
                text.color = Color.white;
            }
            text.alpha = 0f;
            scoreTexts.Add(text);
        }

        if (currentScoreText != null)
        {
            audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
            audioVibrationManager?.Vibrate();
            yield return StartCoroutine(FadeInText(currentScoreText));
            yield return new WaitForSecondsRealtime(0.5f);
        }

        for (int i = scoreTexts.Count - 1; i >= 0; i--)
        {
            audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
            audioVibrationManager?.Vibrate();
            yield return StartCoroutine(FadeInText(scoreTexts[i]));
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    private IEnumerator FadeInText(TextMeshProUGUI text)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 originalScale = text.transform.localScale;
        text.transform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            text.alpha = Mathf.Lerp(0f, 1f, t);
            text.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            yield return null;
        }
        text.alpha = 1f;
        text.transform.localScale = originalScale;
    }

    public void StartMergeMode()
    {
        if (currentMode != GameMode.MergeMode || !isGameActive) return;

        startPanel?.SetActive(false);
        mergeModePanel?.SetActive(true);
        mergeModeManager?.Resume();
        scoreManager?.ShowScoreUI();
        pointerController?.UnlockMovement();
        Canvas.ForceUpdateCanvases();
        Debug.Log($"StartMergeMode: mergeModePanel active: {mergeModePanel.activeSelf}, startPanel active: {startPanel.activeSelf}");

        if (settingsButton != null)
        {
            settingsButton.gameObject.SetActive(true);
            settingsButton.interactable = true;
            Debug.Log("SettingsButton shown during Merge Mode start.");
        }

        if (scoreManager != null)
        {
            scoreManager.gameObject.SetActive(true);
            scoreManager.ShowScoreUI();
            Debug.Log("ScoreCanvas shown during Merge Mode start.");
        }
    }

    public void StartCatSortMode()
    {
        if (currentMode != GameMode.CatSort || !isGameActive) return;

        catSortPanel?.SetActive(true);
        if (catSortMode != null)
        {
            catSortMode.Initialize(); // Вызываем инициализацию
        }
        catSortMode?.Resume();
        scoreManager?.ShowScoreUI();
        pointerController?.UnlockMovement();
        Canvas.ForceUpdateCanvases();
        Debug.Log($"StartCatSortMode: catSortPanel active: {catSortPanel.activeSelf}");

        if (settingsButton != null)
        {
            settingsButton.gameObject.SetActive(true);
            settingsButton.interactable = true;
            Debug.Log("SettingsButton shown during CatSort Mode start.");
        }

        if (scoreManager != null)
        {
            scoreManager.gameObject.SetActive(true);
            scoreManager.ShowScoreUI();
            Debug.Log("ScoreCanvas shown during CatSort Mode start.");
        }
    }

    public void ShowExitConfirmation()
    {
        Debug.Log("ShowExitConfirmation called.");
        if (exitConfirmationPopup == null)
        {
            Debug.LogError("exitConfirmationPopup is not assigned in GameModeManager!");
            return;
        }

        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();
        exitConfirmationPopup.SetActive(true);

        Canvas popupCanvas = exitConfirmationPopup.GetComponentInParent<Canvas>();
        if (popupCanvas != null)
        {
            popupCanvas.sortingOrder = 200;
            popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log($"exitConfirmationPopup Canvas: sortingOrder={popupCanvas.sortingOrder}, renderMode={popupCanvas.renderMode}");
        }

        Time.timeScale = 0f;

        if (currentMode == GameMode.MergeMode) mergeModeManager?.Pause();
        else if (currentMode == GameMode.CatSort) catSortMode?.Pause();
        else pointerController?.LockMovement();
    }

    public void ConfirmExit()
    {
        Debug.Log("ConfirmExit called.");
        if (exitConfirmationPopup == null || exitFinalPopup == null) return;

        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();
        exitConfirmationPopup.SetActive(false);
        exitFinalPopup.SetActive(true);
    }

    public void CancelExit()
    {
        Debug.Log("CancelExit called.");
        if (exitConfirmationPopup == null) return;

        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();
        exitConfirmationPopup.SetActive(false);
        Time.timeScale = 1f;

        if (currentMode == GameMode.MergeMode) mergeModeManager?.Resume();
        else if (currentMode == GameMode.CatSort) catSortMode?.Resume();
        else pointerController?.UnlockMovement();
    }

    public void FinalConfirmExit()
    {
        Debug.Log("FinalConfirmExit called.");
        Debug.Log("Attempting to show interstitial ad in FinalConfirmExit.");
        if (AdManager.Instance != null && PlayerPrefs.GetInt("AdsDisabled", 0) == 0)
        {
            AdManager.Instance.ShowInterstitialAd();
        }
        else
        {
            Debug.LogWarning("AdManager is null or ads are disabled (PlayerPrefs), skipping interstitial ad in FinalConfirmExit.");
        }

        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();
        Time.timeScale = 1f;

        if (currentMode == GameMode.MergeMode) mergeModeManager?.EndGame();
        else if (currentMode == GameMode.CatSort) catSortMode?.EndGame();

        SceneManager.LoadScene("GameScene");
    }

    public void FinalCancelExit()
    {
        Debug.Log("FinalCancelExit called.");
        if (exitFinalPopup == null) return;

        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();
        exitFinalPopup.SetActive(false);
        Time.timeScale = 1f;

        if (currentMode == GameMode.MergeMode) mergeModeManager?.Resume();
        else if (currentMode == GameMode.CatSort) catSortMode?.Resume();
        else pointerController?.UnlockMovement();
    }

    public void ShowSettings()
    {
        Debug.Log("ShowSettings called.");
        if (settingsPanel == null)
        {
            Debug.LogError("SettingsPanel is not assigned in GameModeManager!");
            return;
        }

        Canvas canvas = settingsPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("SettingsPanel has no parent Canvas!");
        }
        else
        {
            canvas.sortingOrder = 100;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log($"SettingsPanel Canvas: sortingOrder={canvas.sortingOrder}, renderMode={canvas.renderMode}");
        }

        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("No EventSystem found in the scene! Adding one.");
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
        else
        {
            Debug.Log($"EventSystem found: {eventSystem.gameObject.name}, enabled: {eventSystem.enabled}");
        }

        if (currentMode == GameMode.MergeMode || currentMode == GameMode.CatSort)
        {
            if (currentMode == GameMode.MergeMode)
            {
                Debug.Log($"Merge Mode UI state: mergeModePanel active: {mergeModePanel?.activeSelf}, startPanel active: {startPanel?.activeSelf}");
                if (mergeModePanel != null && mergeModePanel.activeSelf)
                {
                    Debug.LogWarning("mergeModePanel is active in ShowSettings! Deactivating to avoid overlap.");
                    mergeModePanel.SetActive(false);
                }
            }
            else if (currentMode == GameMode.CatSort)
            {
                Debug.Log($"CatSort Mode UI state: catSortPanel active: {catSortPanel?.activeSelf}");
                if (catSortPanel != null && catSortPanel.activeSelf)
                {
                    Debug.LogWarning("catSortPanel is active in ShowSettings! Deactivating to avoid overlap.");
                    catSortPanel.SetActive(false);
                }
            }
        }

        if (homeButtonPanel != null)
        {
            if (!homeButtonPanel.activeSelf)
            {
                Debug.LogWarning("homeButtonPanel is not active in ShowSettings! Activating it.");
                homeButtonPanel.SetActive(true);
            }
            CanvasGroup homeCanvasGroup = homeButtonPanel.GetComponent<CanvasGroup>();
            if (homeCanvasGroup != null && (!homeCanvasGroup.interactable || !homeCanvasGroup.blocksRaycasts))
            {
                Debug.LogWarning("homeButtonPanel CanvasGroup is not interactable or blocksRaycasts is false! Fixing it.");
                homeCanvasGroup.interactable = true;
                homeCanvasGroup.blocksRaycasts = true;
            }
            Canvas homeCanvas = homeButtonPanel.GetComponentInParent<Canvas>();
            if (homeCanvas != null)
            {
                Debug.Log($"homeButtonPanel Canvas: sortingOrder={homeCanvas.sortingOrder}, renderMode={homeCanvas.renderMode}");
            }
            Debug.Log($"homeButtonPanel active: {homeButtonPanel.activeSelf} in ShowSettings");
        }

        if (exitButton != null)
        {
            if (!exitButton.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("ExitButton is not active in ShowSettings! Activating it.");
                exitButton.gameObject.SetActive(true);
            }
            if (!exitButton.interactable)
            {
                Debug.LogWarning("ExitButton is not interactable in ShowSettings! Enabling it.");
                exitButton.interactable = true;
            }
            Graphic raycastTarget = exitButton.GetComponent<Graphic>() ?? exitButton.GetComponentInChildren<Graphic>();
            if (raycastTarget != null && !raycastTarget.raycastTarget)
            {
                Debug.LogWarning("ExitButton Raycast Target is disabled! Enabling it.");
                raycastTarget.raycastTarget = true;
            }
            Debug.Log($"ExitButton active: {exitButton.gameObject.activeInHierarchy}, interactable: {exitButton.interactable} in ShowSettings");
        }

        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();
        settingsPanel.SetActive(true);

        CanvasGroup settingsCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = 1f;
            settingsCanvasGroup.interactable = true;
            settingsCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            Debug.LogWarning("SettingsPanel has no CanvasGroup component! Adding one.");
            settingsCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();
            settingsCanvasGroup.alpha = 1f;
            settingsCanvasGroup.interactable = true;
            settingsCanvasGroup.blocksRaycasts = true;
        }

        foreach (CanvasGroup cg in settingsPanel.GetComponentsInChildren<CanvasGroup>())
        {
            if (!cg.interactable || !cg.blocksRaycasts)
            {
                Debug.LogWarning($"Child CanvasGroup on {cg.gameObject.name} is not interactable or blocksRaycasts is false! Fixing it.");
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        SettingsManager settingsManager = settingsPanel.GetComponentInChildren<SettingsManager>();
        if (settingsManager != null)
        {
            Debug.Log($"SettingsManager found on settingsPanel. Checking toggles...");
            Debug.Log($"MusicToggle: {(settingsManager.musicToggle != null ? $"active={settingsManager.musicToggle.gameObject.activeInHierarchy}, interactable={settingsManager.musicToggle.interactable}" : "null")}");
            Debug.Log($"SFXToggle: {(settingsManager.sfxToggle != null ? $"active={settingsManager.sfxToggle.gameObject.activeInHierarchy}, interactable={settingsManager.sfxToggle.interactable}" : "null")}");
            Debug.Log($"VibrationToggle: {(settingsManager.vibrationToggle != null ? $"active={settingsManager.vibrationToggle.gameObject.activeInHierarchy}, interactable={settingsManager.vibrationToggle.interactable}" : "null")}");
            Debug.Log($"ExitButton (Settings): {(settingsManager.exitButton != null ? $"active={settingsManager.exitButton.gameObject.activeInHierarchy}, interactable={settingsManager.exitButton.interactable}" : "null")}");
        }
        else
        {
            Debug.LogWarning("SettingsManager not found on settingsPanel!");
        }

        Time.timeScale = 0f;

        if (currentMode == GameMode.MergeMode) mergeModeManager?.Pause();
        else if (currentMode == GameMode.CatSort) catSortMode?.Pause();
        else pointerController?.LockMovement();

        Canvas.ForceUpdateCanvases();
    }

    public void CloseSettings()
    {
        Debug.Log("CloseSettings called.");
        if (settingsPanel == null)
        {
            Debug.LogError("SettingsPanel is not assigned in GameModeManager!");
            return;
        }

        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();
        settingsPanel.SetActive(false);
        CanvasGroup settingsCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = 0f;
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
        }

        if (homeButtonPanel != null)
        {
            if (!homeButtonPanel.activeSelf)
            {
                Debug.LogWarning("homeButtonPanel is not active in CloseSettings! Activating it.");
                homeButtonPanel.SetActive(true);
            }
            CanvasGroup homeCanvasGroup = homeButtonPanel.GetComponent<CanvasGroup>();
            if (homeCanvasGroup != null && (!homeCanvasGroup.interactable || !homeCanvasGroup.blocksRaycasts))
            {
                Debug.LogWarning("homeButtonPanel CanvasGroup is not interactable or blocksRaycasts is false! Fixing it.");
                homeCanvasGroup.interactable = true;
                homeCanvasGroup.blocksRaycasts = true;
            }
            Debug.Log($"homeButtonPanel active: {homeButtonPanel.activeSelf} in CloseSettings");
        }

        if (exitButton != null)
        {
            if (!exitButton.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("ExitButton is not active in CloseSettings! Activating it.");
                exitButton.gameObject.SetActive(true);
            }
            if (!exitButton.interactable)
            {
                Debug.LogWarning("ExitButton is not interactable in CloseSettings! Enabling it.");
                exitButton.interactable = true;
            }
            Debug.Log($"ExitButton active: {exitButton.gameObject.activeInHierarchy}, interactable: {exitButton.interactable} in CloseSettings");
        }

        Time.timeScale = 1f;

        if (currentMode == GameMode.MergeMode && mergeModeManager != null)
        {
            if (!mergeModePanel.activeSelf)
            {
                mergeModePanel.SetActive(true);
                Debug.Log("mergeModePanel reactivated in CloseSettings.");
            }
            mergeModeManager.Resume();
        }
        else if (currentMode == GameMode.CatSort && catSortMode != null)
        {
            if (!catSortPanel.activeSelf)
            {
                catSortPanel.SetActive(true);
                Debug.Log("catSortPanel reactivated in CloseSettings.");
            }
            catSortMode.Resume();
        }
        else if (pointerController != null)
        {
            pointerController.UnlockMovement();
        }

        Canvas.ForceUpdateCanvases();
    }

    private void RestartLevel()
    {
        if (mergeModeManager != null)
        {
            mergeModeManager.RestartLevel();
            mergeModePanel?.SetActive(true);
            startPanel?.SetActive(false);
            isGameActive = true;
            mergeModeManager.Resume();
            scoreManager?.ShowScoreUI();
            pointerController?.UnlockMovement();
            Canvas.ForceUpdateCanvases();
        }
    }
}