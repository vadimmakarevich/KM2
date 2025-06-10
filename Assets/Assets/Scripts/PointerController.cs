using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PointerController : MonoBehaviour
{
    [SerializeField] private float leftEdgeOffset = 0.5f;
    [SerializeField] private float rightEdgeOffset = 0.5f;
    private float minX;
    private float maxX;
    public GameObject[] playerPrefabs;
    public float[] weights;
    public SpriteRenderer currentPreview;
    public Sprite[] closedEyesSprites;
    public bool[] canBlink;

    private bool _isSpawning = false;
    public Queue<int> prefabQueue = new Queue<int>();
    private PreviewManager previewManager;
    private AudioVibrationManager audioVibrationManager;
    private bool canSpawn = false;
    private int spawnOrderCounter = 1;
    public bool canSpawnObjects = false;
    private bool isInputBlocked = false;
    private bool isAutoSpawning = false;
    private bool isMovementLocked = false;

    private Vector2 touchStartPos;
    private float minSwipeDistance = 30f;
    private bool isSwiping = false;

    public bool isSpawning
    {
        get { return _isSpawning; }
        set { _isSpawning = value; }
    }

    void Start()
    {
        CalculateScreenBounds();

        previewManager = FindObjectOfType<PreviewManager>();
        audioVibrationManager = AudioVibrationManager.Instance;

        if (currentPreview == null || playerPrefabs.Length != 13 || weights.Length != 13 || canBlink.Length != 13)
        {
            return;
        }

        int blinkCount = 0;
        foreach (bool b in canBlink) if (b) blinkCount++;
        if (closedEyesSprites.Length != blinkCount)
        {
            return;
        }

        FillQueue();
        HidePreview();
        Invoke("EnableSpawning", 0.1f);
    }

    void OnEnable()
    {
        previewManager = FindObjectOfType<PreviewManager>();
    }

    private void CalculateScreenBounds()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        float cameraLeftEdge = mainCamera.transform.position.x - cameraWidth / 2f;
        float cameraRightEdge = mainCamera.transform.position.x + cameraWidth / 2f;

        minX = cameraLeftEdge + leftEdgeOffset;
        maxX = cameraRightEdge - rightEdgeOffset;
    }

    void Update()
    {
        if (!isMovementLocked)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) MovePointer(-1);
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) MovePointer(1);
            HandleTouchInput();
        }
    }

    void EnableSpawning()
    {
        canSpawn = true;
    }

    void MovePointer(int direction)
    {
        float speed = 5f * Time.deltaTime;
        float newX = Mathf.Clamp(transform.position.x + direction * speed, minX, maxX);
        transform.position = new Vector3(newX, transform.position.y, 0f);
        if (currentPreview != null)
            currentPreview.transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
    }

    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
            float clampedX = Mathf.Clamp(touchPosition.x, minX, maxX);
            transform.position = new Vector3(clampedX, transform.position.y, 0f);
            if (currentPreview != null)
                currentPreview.transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    isSwiping = false;
                    break;
                case TouchPhase.Moved:
                    if (Vector2.Distance(touchStartPos, touch.position) > minSwipeDistance) isSwiping = true;
                    break;
                case TouchPhase.Ended:
                    if (canSpawn && canSpawnObjects && !isInputBlocked && !isSpawning && isSwiping && !isAutoSpawning) SpawnObject();
                    isSwiping = false;
                    break;
                case TouchPhase.Canceled:
                    isSwiping = false;
                    break;
            }
        }
    }

    public int GetNextSortingOrder()
    {
        int nextOrder = spawnOrderCounter++;
        if (nextOrder < 1)
        {
            nextOrder = 1;
            spawnOrderCounter = 2;
        }
        return nextOrder;
    }

    public void FillQueue()
    {
        while (prefabQueue.Count < 4)
        {
            prefabQueue.Enqueue(WeightedRandom(weights));
        }
    }

    public void ResetQueue()
    {
        prefabQueue.Clear();
        FillQueue();
        UpdateCurrentPreview();
        if (previewManager != null && previewManager.gameObject.activeInHierarchy)
        {
            previewManager.OnPrefabSpawned(prefabQueue);
        }
    }

    void SpawnObject()
    {
        if (playerPrefabs.Length != 13 || weights.Length != 13 || canBlink.Length != 13)
        {
            return;
        }

        if (prefabQueue.Count < 4)
        {
            FillQueue();
        }

        int currentIndex = prefabQueue.Dequeue();

        GameObject spawnedObject = Instantiate(playerPrefabs[currentIndex], transform.position + Vector3.up * -0.5f, Quaternion.identity);
        SpriteRenderer sr = spawnedObject.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Destroy(spawnedObject);
            return;
        }

        sr.sortingOrder = GetNextSortingOrder();

        MergeManager mergeManager = spawnedObject.GetComponent<MergeManager>();
        if (mergeManager != null)
        {
            mergeManager.ResetCollisionState();
        }

        PrefabSoundHandler soundHandler = spawnedObject.GetComponent<PrefabSoundHandler>();
        if (soundHandler != null) soundHandler.prefabIndex = currentIndex;

        if (canBlink[currentIndex])
        {
            int blinkSpriteIndex = GetBlinkSpriteIndex(currentIndex);
            Sprite openSprite = playerPrefabs[currentIndex].GetComponent<SpriteRenderer>()?.sprite;
            if (openSprite == null)
            {
                Destroy(spawnedObject);
                return;
            }

            if (blinkSpriteIndex >= 0 && blinkSpriteIndex < closedEyesSprites.Length)
            {
                BlinkController blinkController = spawnedObject.AddComponent<BlinkController>();
                blinkController.openEyesSprite = openSprite;
                blinkController.closedEyesSprite = closedEyesSprites[blinkSpriteIndex];
                blinkController.minBlinkInterval = 5f;
                blinkController.maxBlinkInterval = 20f;
                blinkController.blinkDuration = 0.2f;
            }
        }

        if (audioVibrationManager != null)
        {
            AudioClip spawnSound = audioVibrationManager.GetPrefabSpawnSound(currentIndex);
            if (spawnSound != null) audioVibrationManager.PlaySFX(spawnSound);
            audioVibrationManager.Vibrate();
        }

        prefabQueue.Enqueue(WeightedRandom(weights));

        UpdateCurrentPreview();
        if (prefabQueue.Count < 4)
        {
            FillQueue();
        }

        if (previewManager == null)
        {
            previewManager = FindObjectOfType<PreviewManager>();
            if (previewManager == null)
            {
                return;
            }
        }

        if (previewManager.gameObject.activeInHierarchy)
        {
            previewManager.OnPrefabSpawned(prefabQueue);
        }

        isSpawning = true;
        StartCoroutine(ResetSpawnFlag());
    }

    int WeightedRandom(float[] weights)
    {
        float totalWeight = 0;
        foreach (float weight in weights) totalWeight += weight;
        if (totalWeight <= 0) return 0;

        float randomValue = Random.Range(0f, totalWeight);
        for (int i = 0; i < weights.Length; i++)
        {
            randomValue -= weights[i];
            if (randomValue <= 0) return i;
        }
        return 0;
    }

    void UpdateCurrentPreview()
    {
        if (currentPreview != null && prefabQueue.Count > 0)
        {
            int currentIndex = prefabQueue.Peek();
            currentPreview.sprite = playerPrefabs[currentIndex].GetComponent<SpriteRenderer>().sprite;
        }
    }

    IEnumerator ResetSpawnFlag()
    {
        yield return new WaitForSeconds(0.2f);
        isSpawning = false;
    }

    public int GetBlinkSpriteIndex(int prefabIndex)
    {
        int blinkIndex = 0;
        for (int i = 0; i < prefabIndex; i++) if (canBlink[i]) blinkIndex++;
        return blinkIndex;
    }

    public void EnableObjectSpawning()
    {
        canSpawnObjects = true;
        StartCoroutine(BlockInputTemporarily(1.0f));
    }

    public void DisableObjectSpawning()
    {
        canSpawnObjects = false;
        isInputBlocked = false;
        isAutoSpawning = false;
        isMovementLocked = false;
    }

    private IEnumerator BlockInputTemporarily(float duration)
    {
        isInputBlocked = true;
        yield return new WaitForSeconds(duration);
        isInputBlocked = false;
    }

    public void HidePreview()
    {
        currentPreview?.gameObject.SetActive(false);
        if (previewManager != null && previewManager.gameObject.activeInHierarchy) previewManager.HidePreviews();
    }

    public void ShowPreview()
    {
        GameModeManager gameModeManager = FindObjectOfType<GameModeManager>();
        if (gameModeManager != null && gameModeManager.CurrentMode == GameModeManager.GameMode.MergeMode)
        {
            return;
        }

        if (currentPreview != null)
        {
            currentPreview.gameObject.SetActive(true);
            UpdateCurrentPreview();
        }
        if (prefabQueue.Count < 4)
        {
            FillQueue();
        }
        if (previewManager != null && previewManager.gameObject.activeInHierarchy)
        {
            previewManager.ShowPreviews();
        }
    }

    public void StartAutoSpawn(float duration, float minSpawnRate, float maxSpawnRate)
    {
        if (!canSpawnObjects) EnableObjectSpawning();
        isAutoSpawning = true;
        StartCoroutine(AutoSpawnRoutine(duration, minSpawnRate, maxSpawnRate));
    }

    private IEnumerator AutoSpawnRoutine(float duration, float minSpawnRate, float maxSpawnRate)
    {
        float elapsedTime = 0f;
        while (isAutoSpawning && elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float currentSpawnRate = Mathf.Lerp(minSpawnRate, maxSpawnRate, t);
            float currentInterval = 1f / currentSpawnRate;
            SpawnObject();
            yield return new WaitForSeconds(currentInterval);
        }
        isAutoSpawning = false;
    }

    public void LockMovement()
    {
        isMovementLocked = true;
    }

    public void UnlockMovement()
    {
        isMovementLocked = false;
    }
}