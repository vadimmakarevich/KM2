using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MergeModeManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private Vector2Int minGridSize = new Vector2Int(4, 4);
    [SerializeField] private Vector2Int maxGridSize = new Vector2Int(6, 10);
    [SerializeField] private float bottomOffset = 2f;
    [SerializeField] private bool centerGrid = true;
    [SerializeField] private float spacing = 0.5f;

    [Header("Visual Grid Settings")]
    [SerializeField] private Sprite cellSprite;
    [SerializeField] private Color cellColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private float cellSortingOrder = -1f;
    [SerializeField] private float fixedScale = 0.1f;

    [Header("Swipe Line Settings")]
    [SerializeField] private Color lineColor = new Color(1f, 1f, 0f, 1f);
    [SerializeField] private float lineWidth = 0.1f;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.0025f;
    [SerializeField] private float moveDuration = 0.025f;
    [SerializeField] private float fadeDelayBetweenObjects = 0.0025f;
    [SerializeField] private float moveDelayPerCell = 0.0075f;
    [SerializeField] private float[] scaleKeyframes = { 0.5f, 1.1f, 0.95f, 1f };
    [SerializeField] private float[] scaleDurations = { 0.1f, 0.075f, 0.05f, 0.025f };
    [SerializeField] private float resetFadeOutDuration = 0.01f;
    [SerializeField] private float resetScaleUpDuration = 0.03f;
    [SerializeField] private float splashScale = 1.2f;
    [SerializeField] private float spawnDelay = 0.02f;

    [Header("Prefab Settings")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private int maxInitialLevel = 4;
    [SerializeField] private int[] pointsPerLevel = { 2, 4, 6, 8, 10 };
    private const int MAX_PREFAB_LEVEL = 11;
    private const int TARGET_LEVEL = 11;

    [Header("Blink Controller Settings")]
    [SerializeField] private Sprite[] openEyesSprites;
    [SerializeField] private Sprite[] closedEyesSprites;
    [SerializeField] private bool[] canBlink;

    [Header("Level Progression Settings")]
    [SerializeField] private int movesToIncreaseLevel = 10;


    [Header("Level Settings")]
    [SerializeField] private LevelDefinition[] levels;
    private int currentLevelIndex = 0;
    private Dictionary<int, int> levelCounts;
    private int moveCount = 0;
    private int currentMinLevel = 1;
    private int currentMaxLevel = 4;

    private Vector2Int gridSize;
    private float cellSize;
    private Vector2 gridBottomLeft;
    private GameObject[,] grid;
    private List<Vector2Int> selectedCells = new List<Vector2Int>();
    private List<MergeableObject> selectedObjects = new List<MergeableObject>();
    private ScoreManager scoreManager;
    private AudioVibrationManager audioVibrationManager;
    private GameModeManager gameModeManager;
    private GameObject[,] visualGrid;
    private bool isDragging = false;
    private int currentLevel = -1;
    private Vector2Int lastGridPos;
    private bool isMerging = false;
    private LineRenderer swipeLine;
    private bool isPaused = false;

    public int CurrentLevelIndex
    {
        get { return currentLevelIndex; }
    }

    public LevelDefinition[] Levels
    {
        get { return levels; }
    }

    public GameObject[] Prefabs
    {
        get { return prefabs; }
    }

    void Awake()
    {
        scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager не найден в сцене! Убедитесь, что объект ScoreManager присутствует и активен.");
        }
        else
        {
            Debug.Log("ScoreManager успешно инициализирован.");
        }

        audioVibrationManager = AudioVibrationManager.Instance;
        gameModeManager = FindObjectOfType<GameModeManager>();

        if (pointsPerLevel.Length < prefabs.Length)
        {
            int[] newPointsPerLevel = new int[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
            {
                newPointsPerLevel[i] = i < pointsPerLevel.Length ? pointsPerLevel[i] : pointsPerLevel[pointsPerLevel.Length - 1];
            }
            pointsPerLevel = newPointsPerLevel;
        }

        levelCounts = new Dictionary<int, int>();
    }

    void Start()
    {
        currentLevelIndex = ProgressManager.LoadInt("MergeModeLevel", 0);
        if (currentLevelIndex >= levels.Length)
        {
            currentLevelIndex = 0;
            ProgressManager.SaveInt("MergeModeLevel", currentLevelIndex);
        }

        foreach (var goal in levels[currentLevelIndex].goals)
        {
            levelCounts[goal.targetLevel] = 0;
        }

        List<LevelGoal> goalsWithSteps = new List<LevelGoal>(levels[currentLevelIndex].goals);
        if (levels[currentLevelIndex].moveLimit > 0)
        {
            goalsWithSteps.Add(new LevelGoal { targetLevel = 0, targetCount = levels[currentLevelIndex].moveLimit, isMoveLimit = true });
        }

        gameModeManager?.SetupLevelGoalsUI(currentLevelIndex, goalsWithSteps.ToArray(), prefabs);
        UpdateLevelGoalsUI();
        ResetObjectScales();
    }

    public void InitializeMergeMode()
    {
        GenerateGridSize();
        CalculateCellSize();
        SetupGrid();
        FillGrid();
        isPaused = false;
        moveCount = 0;
        currentMinLevel = 1;
        currentMaxLevel = maxInitialLevel;

        if (scoreManager != null)
        {
            scoreManager.Initialize();
        }

        UpdateLevelGoalsUI();
        ResetObjectScales();
        Debug.Log($"InitializeMergeMode: Grid initialized with size {gridSize}, fixedScale={fixedScale}");
    }

    private void GenerateGridSize()
    {
        float screenWidth = Camera.main.orthographicSize * 2 * Camera.main.aspect;
        float screenHeight = Camera.main.orthographicSize * 2;

        float maxGridWidth = screenWidth * 0.8f;
        float maxGridHeight = screenHeight * 0.6f;

        cellSize = spacing;

        int maxCellsX = Mathf.FloorToInt(maxGridWidth / cellSize);
        int maxCellsY = Mathf.FloorToInt(maxGridHeight / cellSize);

        int width = Mathf.Min(Random.Range(minGridSize.x, maxGridSize.x + 1), maxCellsX);
        int height = Mathf.Min(Random.Range(minGridSize.y, maxGridSize.y + 1), maxCellsY);
        gridSize = new Vector2Int(width, height);
    }

    private void CalculateCellSize()
    {
        float screenWidth = Camera.main.orthographicSize * 2 * Camera.main.aspect;
        float screenHeight = Camera.main.orthographicSize * 2;

        cellSize = spacing;

        float gridWidth = gridSize.x * cellSize;
        float gridHeight = gridSize.y * cellSize;

        if (centerGrid)
        {
            gridBottomLeft = new Vector2(-gridWidth / 2, -gridHeight / 2);
        }
        else
        {
            gridBottomLeft = new Vector2(-gridWidth / 2, -screenHeight / 2 + bottomOffset);
        }
    }

    private void SetupGrid()
    {
        grid = new GameObject[gridSize.x, gridSize.y];
        visualGrid = new GameObject[gridSize.x, gridSize.y];

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2 position = gridBottomLeft + new Vector2(x * cellSize + cellSize / 2, y * cellSize + cellSize / 2);
                GameObject cellVisual = new GameObject($"Cell_{x}_{y}");
                cellVisual.transform.position = position;
                cellVisual.transform.localScale = Vector3.one * fixedScale;
                cellVisual.transform.SetParent(null);

                SpriteRenderer sr = cellVisual.AddComponent<SpriteRenderer>();
                sr.sprite = cellSprite;
                sr.color = cellColor;
                sr.sortingOrder = Mathf.RoundToInt(cellSortingOrder);

                visualGrid[x, y] = cellVisual;
                Debug.Log($"SetupGrid: Cell ({x},{y}) created with localScale={cellVisual.transform.localScale}, lossyScale={cellVisual.transform.lossyScale}");
            }
        }
    }

    private void FillGrid()
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (grid[x, y] == null)
                {
                    int level = levels[currentLevelIndex].fillFieldWithAllLevels ? WeightedRandomLevel(levels[currentLevelIndex].spawnWeights) : GetRandomLevel();
                    SpawnPrefab(x, y, level);
                }
            }
        }
        UpdateLevelGoalsUI();
        ResetObjectScales();
    }

    private int GetRandomLevel()
    {
        UpdateLevelRange();
        return Mathf.Max(1, Random.Range(currentMinLevel, currentMaxLevel + 1));
    }

    private int WeightedRandomLevel(float[] weights)
    {
        if (weights == null || weights.Length != prefabs.Length)
        {
            return Mathf.Max(1, Random.Range(1, prefabs.Length + 1));
        }

        int maxWeightedLevel = 1;
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] > 0)
            {
                maxWeightedLevel = i + 1;
            }
        }

        float totalWeight = 0f;
        for (int i = 0; i < maxWeightedLevel; i++)
        {
            totalWeight += weights[i];
        }

        if (totalWeight <= 0)
        {
            return 1;
        }

        float randomValue = Random.Range(0f, totalWeight);
        for (int i = 0; i < maxWeightedLevel; i++)
        {
            randomValue -= weights[i];
            if (randomValue <= 0f)
            {
                return Mathf.Max(1, i + 1);
            }
        }
        return 1;
    }

    private void UpdateLevelRange()
    {
        int levelStep = moveCount / movesToIncreaseLevel;

        switch (levelStep)
        {
            case 0:
                currentMinLevel = 1;
                currentMaxLevel = maxInitialLevel;
                break;
            case 1:
                currentMinLevel = 2;
                currentMaxLevel = Mathf.Min(5, prefabs.Length);
                break;
            case 2:
                currentMinLevel = 3;
                currentMaxLevel = Mathf.Min(6, prefabs.Length);
                break;
            case 3:
                currentMinLevel = 5;
                currentMaxLevel = Mathf.Min(7, prefabs.Length);
                break;
            case 4:
                currentMinLevel = 6;
                currentMaxLevel = Mathf.Min(8, prefabs.Length);
                break;
            default:
                currentMinLevel = 7;
                currentMaxLevel = Mathf.Min(9, prefabs.Length);
                break;
        }
    }

    private void SpawnPrefab(int x, int y, int level)
    {
        if (level <= 0 || level > prefabs.Length)
        {
            Debug.LogWarning($"Недопустимый уровень {level} для спавна. Используется уровень 1.");
            level = 1;
        }

        GameObject prefab = prefabs[level - 1];
        if (prefab == null)
        {
            Debug.LogError($"Префаб для уровня {level} не установлен в массиве prefabs!");
            return;
        }

        Vector2 position = gridBottomLeft + new Vector2(x * cellSize + cellSize / 2, y * cellSize + cellSize / 2);
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        instance.transform.localScale = Vector3.one * fixedScale;
        instance.transform.SetParent(null);
        Debug.Log($"SpawnPrefab: Object at ({x},{y}), level={level}, localScale={instance.transform.localScale}, lossyScale={instance.transform.lossyScale}");

        Rigidbody2D rb = instance.GetComponent<Rigidbody2D>();
        if (rb != null) Destroy(rb);
        instance.AddComponent<MergeableObject>().Initialize(level, x, y);

        SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError($"SpriteRenderer отсутствует на префабе уровня {level}!");
            Destroy(instance);
            return;
        }

        int spriteIndex = level - 1;
        if (canBlink != null && canBlink.Length > spriteIndex && canBlink[spriteIndex])
        {
            if (spriteIndex >= 0 && spriteIndex < openEyesSprites.Length && spriteIndex < closedEyesSprites.Length)
            {
                BlinkController blinkController = instance.AddComponent<BlinkController>();
                blinkController.openEyesSprite = openEyesSprites[spriteIndex];
                blinkController.closedEyesSprite = closedEyesSprites[spriteIndex];
                blinkController.minBlinkInterval = 5f;
                blinkController.maxBlinkInterval = 20f;
                blinkController.blinkDuration = 0.2f;
            }
        }

        grid[x, y] = instance;

        Color color = sr.color;
        color.a = 1f;
        sr.color = color;

        sr.sortingOrder = 2;

        if (canBlink != null && canBlink.Length > spriteIndex && canBlink[spriteIndex] && openEyesSprites.Length > spriteIndex)
        {
            sr.sprite = openEyesSprites[spriteIndex];
        }

        StartCoroutine(ScaleAnimation(instance.transform));
        UpdateLevelGoalsUI();
    }

    private IEnumerator ScaleAnimation(Transform target)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 baseScale = Vector3.one * fixedScale;

        Vector3 startScale = baseScale * scaleKeyframes[0];
        target.localScale = startScale;
        yield return AnimateScale(target, startScale, baseScale * scaleKeyframes[1], scaleDurations[0]);

        yield return AnimateScale(target, baseScale * scaleKeyframes[1], baseScale * scaleKeyframes[2], scaleDurations[1]);

        yield return AnimateScale(target, baseScale * scaleKeyframes[2], baseScale * scaleKeyframes[3], scaleDurations[2]);

        target.localScale = baseScale;
        Debug.Log($"ScaleAnimation completed for {target.gameObject.name}, final localScale={target.localScale}");
    }

    private IEnumerator AnimateScale(Transform target, Vector3 fromScale, Vector3 toScale, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.localScale = Vector3.Lerp(fromScale, toScale, t);
            yield return null;
        }
        target.localScale = toScale;
    }

    void Update()
    {
        if (gameModeManager.startPanel.activeSelf) return;
        if (isMerging || isPaused) return;

        if (levels[currentLevelIndex].moveLimit > 0 && moveCount >= levels[currentLevelIndex].moveLimit)
        {
            gameModeManager?.GameOver("MergeMode - Out of Moves");
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = WorldToGridPosition(worldPos);
            if (IsValidGridPosition(gridPos) && grid[gridPos.x, gridPos.y] != null)
            {
                MergeableObject obj = grid[gridPos.x, gridPos.y].GetComponent<MergeableObject>();
                isDragging = true;
                selectedCells.Clear();
                selectedObjects.Clear();
                currentLevel = obj.Level;
                selectedCells.Add(gridPos);
                selectedObjects.Add(obj);
                lastGridPos = gridPos;
                SetupSwipeLine(gridPos);

                if (scoreManager != null)
                {
                    scoreManager.UpdateMultiplier(selectedObjects);
                }
            }
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = WorldToGridPosition(worldPos);
            if (IsValidGridPosition(gridPos) && gridPos != lastGridPos)
            {
                if (selectedCells.Count > 1 && gridPos == selectedCells[selectedCells.Count - 2])
                {
                    selectedCells.RemoveAt(selectedCells.Count - 1);
                    selectedObjects.RemoveAt(selectedCells.Count - 1);
                    lastGridPos = gridPos;
                    UpdateSwipeLine();

                    if (selectedCells.Count == 0)
                    {
                        ClearSwipeLine();
                        isDragging = false;
                        currentLevel = -1;
                        selectedObjects.Clear();
                    }

                    if (scoreManager != null)
                    {
                        scoreManager.UpdateMultiplier(selectedObjects);
                    }
                }
                else if (grid[gridPos.x, gridPos.y] != null)
                {
                    MergeableObject obj = grid[gridPos.x, gridPos.y].GetComponent<MergeableObject>();
                    if (obj.Level == currentLevel && !selectedCells.Contains(gridPos))
                    {
                        Vector2Int diff = gridPos - lastGridPos;
                        if (Mathf.Abs(diff.x) <= 1 && Mathf.Abs(diff.y) <= 1)
                        {
                            selectedCells.Add(gridPos);
                            selectedObjects.Add(obj);
                            lastGridPos = gridPos;
                            UpdateSwipeLine();

                            if (scoreManager != null)
                            {
                                scoreManager.UpdateMultiplier(selectedObjects);
                            }
                        }
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            Debug.Log($"Mouse button up, selectedCells.Count: {selectedCells.Count}");
            isDragging = false;
            if (selectedCells.Count >= 2)
            {
                Debug.Log("Starting MergeCellsWithAnimation...");
                StartCoroutine(MergeCellsWithAnimation());
            }
            else
            {
                Debug.Log("Not enough cells selected for merge.");
                selectedCells.Clear();
                selectedObjects.Clear();
                if (scoreManager != null)
                {
                    scoreManager.UpdateMultiplier(selectedObjects);
                }
            }
            ClearSwipeLine();
            currentLevel = -1;
        }
    }

    private void SetupSwipeLine(Vector2Int startPos)
    {
        GameObject lineObject = new GameObject("SwipeLine");
        swipeLine = lineObject.AddComponent<LineRenderer>();
        swipeLine.positionCount = 0;
        swipeLine.startWidth = lineWidth;
        swipeLine.endWidth = lineWidth;
        swipeLine.material = new Material(Shader.Find("Sprites/Default"));
        swipeLine.startColor = lineColor;
        swipeLine.endColor = lineColor;
        swipeLine.sortingOrder = 10;
        UpdateSwipeLine();
    }

    private void UpdateSwipeLine()
    {
        if (swipeLine == null) return;

        swipeLine.positionCount = selectedCells.Count;
        for (int i = 0; i < selectedCells.Count; i++)
        {
            Vector2Int pos = selectedCells[i];
            Vector3 worldPos = (Vector3)(gridBottomLeft + new Vector2(pos.x * cellSize + cellSize / 2, pos.y * cellSize + cellSize / 2));
            worldPos.z = 0;
            swipeLine.SetPosition(i, worldPos);
        }
    }

    private void ClearSwipeLine()
    {
        if (swipeLine != null)
        {
            Destroy(swipeLine.gameObject);
            swipeLine = null;
        }
    }

    private Vector2Int WorldToGridPosition(Vector2 worldPos)
    {
        Vector2 localPos = worldPos - gridBottomLeft;
        int x = Mathf.FloorToInt(localPos.x / cellSize);
        int y = Mathf.FloorToInt(localPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    private bool IsValidGridPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridSize.x && pos.y >= 0 && pos.y < gridSize.y;
    }

    private IEnumerator MergeCellsWithAnimation()
    {
        isMerging = true;
        moveCount++;

        Vector2Int lastSelectedPos = selectedCells[selectedCells.Count - 1];
        int level = grid[lastSelectedPos.x, lastSelectedPos.y].GetComponent<MergeableObject>().Level;

        int pointsPerPrefab = pointsPerLevel[Mathf.Min(level - 1, pointsPerLevel.Length - 1)];
        int totalBasePoints = pointsPerPrefab * selectedCells.Count;

        float[] spawnWeights = levels[currentLevelIndex].spawnWeights;
        int maxWeightedLevel = 1;
        if (spawnWeights != null && spawnWeights.Length == prefabs.Length)
        {
            for (int i = 0; i < spawnWeights.Length; i++)
            {
                if (spawnWeights[i] > 0)
                {
                    maxWeightedLevel = i + 1;
                }
            }
        }

        Debug.Log($"Merging level {level} objects. MaxWeightedLevel: {maxWeightedLevel}, Next Level: {level + 1}");

        List<Coroutine> fadeCoroutines = new List<Coroutine>();
        for (int i = 0; i < selectedCells.Count - 1; i++)
        {
            Vector2Int pos = selectedCells[i];
            GameObject obj = grid[pos.x, pos.y];
            if (obj != null)
            {
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                Transform transform = obj.transform;
                if (sr != null && transform != null)
                {
                    fadeCoroutines.Add(StartCoroutine(FadeOutObject(sr, transform, obj)));
                }
                grid[pos.x, pos.y] = null;
            }
            yield return new WaitForSeconds(fadeDelayBetweenObjects);
        }

        GameObject lastObj = grid[lastSelectedPos.x, lastSelectedPos.y];
        if (lastObj != null)
        {
            SpriteRenderer lastSr = lastObj.GetComponent<SpriteRenderer>();
            Transform lastTransform = lastObj.transform;
            if (lastSr != null && lastTransform != null)
            {
                fadeCoroutines.Add(StartCoroutine(FadeOutObject(lastSr, lastTransform, lastObj)));
            }
            grid[lastSelectedPos.x, lastSelectedPos.y] = null;
        }

        foreach (var coroutine in fadeCoroutines)
        {
            yield return coroutine;
        }

        GameObject newPrefab = null;
        if (level + 1 <= maxWeightedLevel && level + 1 <= prefabs.Length)
        {
            Debug.Log($"Spawning new prefab at level {level + 1} at position {lastSelectedPos}");
            SpawnPrefab(lastSelectedPos.x, lastSelectedPos.y, level + 1);
            newPrefab = grid[lastSelectedPos.x, lastSelectedPos.y];
            yield return StartCoroutine(FadeInPrefab(newPrefab));

            if (scoreManager != null)
            {
                scoreManager.AddScore(totalBasePoints);
                Debug.Log($"Добавлено {totalBasePoints} очков за слияние {selectedCells.Count} объектов уровня {level}.");
            }
            else
            {
                Debug.LogError("ScoreManager is null, cannot add score for merge!");
            }

            foreach (var goal in levels[currentLevelIndex].goals)
            {
                if (level == goal.targetLevel)
                {
                    if (levelCounts.ContainsKey(level))
                    {
                        levelCounts[level] += selectedCells.Count;
                        UpdateLevelGoalsUI();
                    }
                    break;
                }
            }
        }
        else
        {
            Debug.Log($"Level {level + 1} exceeds maxWeightedLevel {maxWeightedLevel}. Removing all selected objects.");
            if (scoreManager != null)
            {
                scoreManager.AddScore(totalBasePoints);
                Debug.Log($"Добавлено {totalBasePoints} очков за удаление {selectedCells.Count} объектов уровня {level}.");
            }
            else
            {
                Debug.LogError("ScoreManager is null, cannot add score for removal!");
            }
        }

        Debug.Log("Grid state after merge:");
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Debug.Log($"grid[{x},{y}] = {(grid[x, y] != null ? "Occupied" : "Empty")}");
            }
        }

        yield return StartCoroutine(ShiftCellsDownWithAnimation());

        Debug.Log("Grid state after shift:");
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Debug.Log($"grid[{x},{y}] = {(grid[x, y] != null ? "Occupied" : "Empty")}");
            }
        }

        FillEmptyCells();

        if (CheckLevelCompletion())
        {
            int rawScore = scoreManager != null ? scoreManager.GetScore() : 0;
            int optimalMoves = GetOptimalMoves();
            int actualMoves = GetActualMoves();
            gameModeManager?.ShowLevelCompletePanel(rawScore, optimalMoves, actualMoves);
            yield break;
        }

        if (!CanMakeMove())
        {
            StartCoroutine(ResetFieldWithoutMoveReset());
        }

        audioVibrationManager?.PlaySFX(audioVibrationManager.buttonClickSound);
        audioVibrationManager?.Vibrate();

        selectedCells.Clear();
        selectedObjects.Clear();
        if (scoreManager != null)
        {
            scoreManager.UpdateMultiplier(selectedObjects);
        }

        isMerging = false;

        if (gameModeManager != null)
        {
            gameModeManager.UpdateStepsCounter(currentLevelIndex, moveCount);
        }
    }

    private bool CheckLevelCompletion()
    {
        bool allGoalsMet = true;
        foreach (var goal in levels[currentLevelIndex].goals)
        {
            if (levelCounts[goal.targetLevel] < goal.targetCount)
            {
                allGoalsMet = false;
                break;
            }
        }
        return allGoalsMet;
    }

    public void ProceedToNextLevel()
    {
        currentLevelIndex++;
        if (currentLevelIndex >= levels.Length)
        {
            currentLevelIndex = 0;
        }
        ProgressManager.SaveInt("MergeModeLevel", currentLevelIndex);

        levelCounts.Clear();
        foreach (var goal in levels[currentLevelIndex].goals)
        {
            levelCounts[goal.targetLevel] = 0;
        }

        StartCoroutine(ResetFieldForNextLevel());
    }

    private IEnumerator ResetFieldForNextLevel()
    {
        yield return StartCoroutine(FadeOutField());
        EndGame();
        InitializeMergeMode();

        List<LevelGoal> goalsWithSteps = new List<LevelGoal>(levels[currentLevelIndex].goals);
        if (levels[currentLevelIndex].moveLimit > 0)
        {
            goalsWithSteps.Add(new LevelGoal { targetLevel = 0, targetCount = levels[currentLevelIndex].moveLimit, isMoveLimit = true });
        }

        gameModeManager?.SetupLevelGoalsUI(currentLevelIndex, goalsWithSteps.ToArray(), prefabs);
        UpdateLevelGoalsUI();
        ResetObjectScales();
        Debug.Log($"ProceedToNextLevel: Transition to level {currentLevelIndex + 1} completed, fixedScale={fixedScale}");
    }

    private void UpdateLevelGoalsUI()
    {
        List<LevelGoal> goalsWithoutSteps = new List<LevelGoal>();
        foreach (var goal in levels[currentLevelIndex].goals)
        {
            if (!goal.isMoveLimit)
            {
                goalsWithoutSteps.Add(goal);
            }
        }

        if (gameModeManager != null)
        {
            gameModeManager.UpdateLevelGoalsUI(currentLevelIndex, goalsWithoutSteps.ToArray(), levelCounts);
        }
    }

    public int GetLevel11Count()
    {
        return levelCounts.ContainsKey(TARGET_LEVEL) ? levelCounts[TARGET_LEVEL] : 0;
    }

    public int GetMoveLimit(int levelIndex)
    {
        return levelIndex < levels.Length ? levels[levelIndex].moveLimit : 0;
    }

    public int GetMoveCount()
    {
        return moveCount;
    }

    public int GetTargetCount()
    {
        foreach (var goal in levels[currentLevelIndex].goals)
        {
            if (!goal.isMoveLimit && goal.targetCount > 0)
            {
                return goal.targetCount;
            }
        }
        return 20;
    }

    public int GetObjectValue()
    {
        foreach (var goal in levels[currentLevelIndex].goals)
        {
            if (!goal.isMoveLimit && goal.targetLevel > 0 && goal.targetLevel <= pointsPerLevel.Length)
            {
                return pointsPerLevel[goal.targetLevel - 1];
            }
        }
        return 7;
    }

    public int GetOptimalMoves()
    {
        int targetCount = GetTargetCount();
        return Mathf.FloorToInt(targetCount / 2f);
    }

    public int GetActualMoves()
    {
        return moveCount;
    }

    private IEnumerator FadeOutObject(SpriteRenderer sr, Transform transform, GameObject obj)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            if (sr != null)
            {
                Color color = sr.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                sr.color = color;
            }
            if (transform != null)
            {
                float scale = Mathf.Lerp(1f, 0f, t);
                transform.localScale = Vector3.one * fixedScale * scale;
            }
            yield return null;
        }

        if (sr != null)
        {
            Color color = sr.color;
            color.a = 0f;
            sr.color = color;
        }
        if (transform != null)
        {
            transform.localScale = Vector3.one * fixedScale * 0f;
        }

        if (obj != null)
        {
            Destroy(obj);
        }
    }

    private IEnumerator FadeInPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("FadeInPrefab: Prefab is null.");
            yield break;
        }

        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        Transform transform = prefab.transform;
        if (sr == null || transform == null)
        {
            Debug.LogWarning("FadeInPrefab: SpriteRenderer or Transform is null.");
            yield break;
        }

        Color color = sr.color;
        color.a = 0f;
        sr.color = color;
        transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            color.a = Mathf.Lerp(0f, 1f, t);
            sr.color = color;

            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * fixedScale, t);

            yield return null;
        }

        color.a = 1f;
        sr.color = color;
        transform.localScale = Vector3.one * fixedScale;
        Debug.Log($"FadeInPrefab: {prefab.name} faded in, final localScale={transform.localScale}");
    }

    private IEnumerator ShiftCellsDownWithAnimation()
    {
        List<(GameObject obj, Vector2 startPos, Vector2 targetPos, float delay)> movingObjects = new List<(GameObject, Vector2, Vector2, float)>();

        for (int x = 0; x < gridSize.x; x++)
        {
            int emptyY = 0;
            for (int y = 0; y < gridSize.y; y++)
            {
                if (grid[x, y] == null)
                {
                    emptyY++;
                }
                else if (emptyY > 0)
                {
                    grid[x, y - emptyY] = grid[x, y];
                    grid[x, y] = null;
                    MergeableObject mergeable = grid[x, y - emptyY].GetComponent<MergeableObject>();
                    if (mergeable != null)
                    {
                        mergeable.UpdatePosition(x, y - emptyY);
                    }
                    Vector2 targetPos = gridBottomLeft + new Vector2(x * cellSize + cellSize / 2, (y - emptyY) * cellSize + cellSize / 2);
                    float delay = x * moveDelayPerCell;
                    movingObjects.Add((grid[x, y - emptyY], grid[x, y - emptyY].transform.position, targetPos, delay));
                }
            }
        }

        float elapsed = 0f;
        while (elapsed < moveDuration + (gridSize.x - 1) * moveDelayPerCell)
        {
            elapsed += Time.deltaTime;
            foreach (var (obj, startPos, targetPos, delay) in movingObjects)
            {
                if (obj != null)
                {
                    float adjustedTime = Mathf.Max(0, elapsed - delay);
                    float t = Mathf.Clamp01(adjustedTime / moveDuration);
                    obj.transform.position = Vector2.Lerp(startPos, targetPos, t);
                }
            }
            yield return null;
        }

        foreach (var (obj, _, targetPos, _) in movingObjects)
        {
            if (obj != null)
            {
                obj.transform.position = targetPos;
                obj.transform.localScale = Vector3.one * fixedScale;
            }
        }
        UpdateLevelGoalsUI();
    }

    private void FillEmptyCells()
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (grid[x, y] == null)
                {
                    int level = levels[currentLevelIndex].fillFieldWithAllLevels ? WeightedRandomLevel(levels[currentLevelIndex].spawnWeights) : GetRandomLevel();
                    SpawnPrefab(x, y, level);
                }
            }
        }
        ResetObjectScales();
    }

    private bool CanMakeMove()
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (grid[x, y] == null) continue;
                int level = grid[x, y].GetComponent<MergeableObject>().Level;

                Vector2Int[] directions = new Vector2Int[]
                {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
                };
                foreach (var dir in directions)
                {
                    Vector2Int neighbor = new Vector2Int(x, y) + dir;
                    if (IsValidGridPosition(neighbor) && grid[neighbor.x, neighbor.y] != null)
                    {
                        int neighborLevel = grid[neighbor.x, neighbor.y].GetComponent<MergeableObject>().Level;
                        if (neighborLevel == level)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    public void Pause()
    {
        isPaused = true;
        ClearSwipeLine();
        isDragging = false;
        selectedCells.Clear();
        selectedObjects.Clear();
        currentLevel = -1;

        if (scoreManager != null)
        {
            scoreManager.UpdateMultiplier(selectedObjects);
        }

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (grid[x, y] != null)
                {
                    BlinkController blinkController = grid[x, y].GetComponent<BlinkController>();
                    if (blinkController != null)
                    {
                        blinkController.StopBlinking();
                    }
                    grid[x, y].SetActive(false);
                }
                if (visualGrid[x, y] != null)
                {
                    visualGrid[x, y].SetActive(false);
                }
            }
        }
    }

    public void Resume()
    {
        isPaused = false;
        RestoreMergeModeState();
        UpdateLevelGoalsUI();
        ResetObjectScales();
    }

    public void EndGame()
    {
        StopAllCoroutines();

        if (scoreManager != null)
        {
            var emptyList = new List<MergeableObject>();
            scoreManager.UpdateMultiplier(emptyList);
            scoreManager.ResetScore(); // Добавляем сброс очков при завершении игры
        }

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y]);
                    grid[x, y] = null;
                }
            }
        }

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (visualGrid[x, y] != null)
                {
                    Destroy(visualGrid[x, y]);
                    visualGrid[x, y] = null;
                }
            }
        }

        ClearSwipeLine();

        MergeableObject[] remainingObjects = FindObjectsOfType<MergeableObject>();
        foreach (var obj in remainingObjects)
        {
            if (obj != null)
            {
                Destroy(obj.gameObject);
            }
        }

        grid = null;
        visualGrid = null;
        selectedCells.Clear();
        selectedObjects.Clear();
        gridSize = Vector2Int.zero;
        isMerging = false;
        isDragging = false;
        isPaused = false;
        currentLevel = -1;
        moveCount = 0;
        Debug.Log("EndGame: Field fully cleared");
    }

    public void RestartLevel()
    {
        EndGame();
        InitializeMergeMode();
        ResetLevelCounts();    // if level counts should reset
    }

    public void RestoreMergeModeState()
    {
        if (gridSize == Vector2Int.zero || grid == null || visualGrid == null)
        {
            return;
        }

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (visualGrid[x, y] != null)
                {
                    visualGrid[x, y].SetActive(true);
                    visualGrid[x, y].transform.localScale = Vector3.one * fixedScale;
                }
                if (grid[x, y] != null)
                {
                    grid[x, y].SetActive(true);
                    grid[x, y].transform.localScale = Vector3.one * fixedScale;
                    BlinkController blinkController = grid[x, y].GetComponent<BlinkController>();
                    if (blinkController != null)
                    {
                        blinkController.StartBlinking();
                    }
                }
            }
        }
        ResetObjectScales();
        Debug.Log("RestoreMergeModeState: Scales reset to fixedScale=" + fixedScale);
    }

    private void ResetObjectScales()
    {
        if (grid == null || visualGrid == null) return;

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (grid[x, y] != null)
                {
                    grid[x, y].transform.localScale = Vector3.one * fixedScale;
                    grid[x, y].transform.SetParent(null);
                    Debug.Log($"ResetObjectScales: Object at ({x},{y}), localScale={grid[x, y].transform.localScale}, lossyScale={grid[x, y].transform.lossyScale}");
                }
                if (visualGrid[x, y] != null)
                {
                    visualGrid[x, y].transform.localScale = Vector3.one * fixedScale;
                    visualGrid[x, y].transform.SetParent(null);
                    Debug.Log($"ResetObjectScales: Visual cell at ({x},{y}), localScale={visualGrid[x, y].transform.localScale}, lossyScale={visualGrid[x, y].transform.lossyScale}");
                }
            }
        }
    }

    public IEnumerator ResetFieldWithoutMoveReset()
    {
        int currentMoveCount = moveCount;
        Vector2Int currentGridSize = gridSize;

        yield return StartCoroutine(FadeOutField());

        EndGame();

        gridSize = currentGridSize;
        CalculateCellSize();
        SetupGrid();
        yield return StartCoroutine(SpawnFieldWithAnimation());

        moveCount = currentMoveCount;
        UpdateLevelGoalsUI();
        ResetObjectScales();
    }

    private IEnumerator FadeOutField()
    {
        Debug.Log("FadeOutField: Starting chaotic fade out");

        List<(SpriteRenderer sr, Transform transform, GameObject obj)> objectsToFade = new List<(SpriteRenderer, Transform, GameObject)>();
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (grid[x, y] != null)
                {
                    SpriteRenderer sr = grid[x, y].GetComponent<SpriteRenderer>();
                    Transform transform = grid[x, y].transform;
                    GameObject obj = grid[x, y];
                    if (sr != null && transform != null)
                    {
                        objectsToFade.Add((sr, transform, obj));
                    }
                }
            }
        }

        for (int i = 0; i < objectsToFade.Count; i++)
        {
            int randomIndex = Random.Range(i, objectsToFade.Count);
            var temp = objectsToFade[i];
            objectsToFade[i] = objectsToFade[randomIndex];
            objectsToFade[randomIndex] = temp;
        }

        List<Coroutine> fadeOutCoroutines = new List<Coroutine>();
        foreach (var (sr, transform, obj) in objectsToFade)
        {
            fadeOutCoroutines.Add(StartCoroutine(FadeOutObjectForReset(sr, transform)));
        }

        foreach (var coroutine in fadeOutCoroutines)
        {
            yield return coroutine;
        }

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y]);
                    grid[x, y] = null;
                }
            }
        }

        Debug.Log("FadeOutField: All objects faded out chaotically");
    }

    private IEnumerator FadeOutObjectForReset(SpriteRenderer sr, Transform transform)
    {
        float elapsed = 0f;
        while (elapsed < resetFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / resetFadeOutDuration;
            Color color = sr.color;
            color.a = Mathf.Lerp(1f, 0f, t);
            sr.color = color;
            transform.localScale = Vector3.one * fixedScale * Mathf.Lerp(1f, 0.5f, t);
            yield return null;
        }
        Color finalColor = sr.color;
        finalColor.a = 0f;
        sr.color = finalColor;
        transform.localScale = Vector3.one * fixedScale * 0.5f;
    }

    private IEnumerator SpawnFieldWithAnimation()
    {
        List<Coroutine> spawnCoroutines = new List<Coroutine>();

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                int level = levels[currentLevelIndex].fillFieldWithAllLevels ? WeightedRandomLevel(levels[currentLevelIndex].spawnWeights) : GetRandomLevel();
                SpawnPrefab(x, y, level);

                if (grid[x, y] != null)
                {
                    SpriteRenderer sr = grid[x, y].GetComponent<SpriteRenderer>();
                    Transform transform = grid[x, y].transform;
                    if (sr != null && transform != null)
                    {
                        Color color = sr.color;
                        color.a = 0f;
                        sr.color = color;
                        transform.localScale = Vector3.zero;

                        yield return new WaitForSeconds(spawnDelay);
                        spawnCoroutines.Add(StartCoroutine(SpawnObjectWithAnimation(sr, transform)));
                    }
                }
            }
        }

        foreach (var coroutine in spawnCoroutines)
        {
            yield return coroutine;
        }
        ResetObjectScales();
    }

    private IEnumerator SpawnObjectWithAnimation(SpriteRenderer sr, Transform transform)
    {
        float elapsed = 0f;
        while (elapsed < resetScaleUpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / resetScaleUpDuration;
            Color color = sr.color;
            color.a = Mathf.Lerp(0f, 1f, t);
            sr.color = color;
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * fixedScale, t);
            yield return null;
        }
        Color colorFinal = sr.color;
        colorFinal.a = 1f;
        sr.color = colorFinal;
        transform.localScale = Vector3.one * fixedScale;

        elapsed = 0f;
        float splashDuration = resetScaleUpDuration * 0.5f;
        while (elapsed < splashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / splashDuration;
            transform.localScale = Vector3.Lerp(Vector3.one * fixedScale, Vector3.one * fixedScale * splashScale, t);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < splashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / splashDuration;
            transform.localScale = Vector3.Lerp(Vector3.one * fixedScale * splashScale, Vector3.one * fixedScale, t);
            yield return null;
        }
        transform.localScale = Vector3.one * fixedScale;
        Debug.Log($"SpawnObjectWithAnimation: {transform.gameObject.name}, final localScale={transform.localScale}");
    }

    public void ResetLevelCounts()
    {
        moveCount = 0;
        levelCounts.Clear();
        foreach (var goal in levels[currentLevelIndex].goals)
        {
            levelCounts[goal.targetLevel] = 0;
        }
        Debug.Log($"ResetLevelCounts: moveCount set to {moveCount}, levelCounts cleared and reinitialized for level {currentLevelIndex + 1}");
    }
}

[System.Serializable]
public class MergeableObject : MonoBehaviour
{
    public int Level { get; private set; }
    private int gridX;
    private int gridY;

    public void Initialize(int level, int x, int y)
    {
        Level = level;
        gridX = x;
        gridY = y;
    }

    public void UpdatePosition(int x, int y)
    {
        gridX = x;
        gridY = y;
    }
}