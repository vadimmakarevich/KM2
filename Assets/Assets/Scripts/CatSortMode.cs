using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CatSortMode : MonoBehaviour
{
    [SerializeField] private GameObject shelfPrefab;
    [SerializeField] private GameObject catPrefab;
    [SerializeField] private Sprite[] catSprites; // Массив спрайтов: [type_0, type_0_jump, type_1, type_1_jump, ...]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private int leftShelves = 2;
    [SerializeField] private int rightShelves = 2;

    [System.Serializable]
    public class LevelConfig
    {
        public int leftShelves = 2;
        public int rightShelves = 2;
        public List<int> catTypes = new List<int>();
    }

    [SerializeField] private LevelConfig[] levelConfigs;

    private LevelConfig currentConfig;
    private int currentLevelIndex;
    private List<Shelf> shelves = new List<Shelf>();
    private AudioVibrationManager audioVibrationManager;

    [System.Serializable]
    public class Cat
    {
        public int type; // 0 to 11 для 12 типов котов
        public bool isSelected;
        public Transform transform;
        public SpriteRenderer spriteRenderer;
    }

    [System.Serializable]
    public class Shelf
    {
        public enum ShelfSide { Left, Right }
        public ShelfSide side;
        public List<Cat> cats = new List<Cat>(4);
        public Transform shelfTransform;
        public bool IsFull => cats.Count >= 4;
    }

    void Start()
    {
        Debug.Log("CatSortMode Start called");
        audioVibrationManager = AudioVibrationManager.Instance;
        currentLevelIndex = PlayerPrefs.GetInt("CatSortLevel", 0);
        if (levelConfigs != null && levelConfigs.Length > currentLevelIndex)
        {
            currentConfig = levelConfigs[currentLevelIndex];
        }
        else
        {
            currentConfig = new LevelConfig { leftShelves = leftShelves, rightShelves = rightShelves };
            if (currentConfig.catTypes.Count == 0)
            {
                for (int i = 0; i < catSprites.Length / 2; i++)
                {
                    currentConfig.catTypes.Add(i);
                }
            }
        }

        leftShelves = currentConfig.leftShelves;
        rightShelves = currentConfig.rightShelves;

        InitializeShelves();
        GenerateLevel();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Shelf targetShelf = GetShelfAtPosition(worldPos);
            if (targetShelf != null)
            {
                SelectCats(targetShelf, worldPos);
            }
            else
            {
                Shelf moveTarget = shelves.Find(s => Vector2.Distance(worldPos, s.shelfTransform.position) < 0.5f);
                if (moveTarget != null && shelves.Any(s => s.cats.Any(c => c.isSelected)))
                {
                    MoveSelectedCats(moveTarget);
                }
            }
        }
    }

    private void InitializeShelves()
    {

        float screenWidth = Camera.main.orthographicSize * 2 * Camera.main.aspect;
        float shelfHeight = 1f;
        float shelfOffset = 0.5f;

        for (int i = 0; i < leftShelves; i++)
        {
            Shelf shelf = new Shelf { side = Shelf.ShelfSide.Left };
            Vector3 pos = CalculateShelfPosition(i, Shelf.ShelfSide.Left, screenWidth, shelfHeight, shelfOffset);
            GameObject shelfObj = Instantiate(shelfPrefab, pos, Quaternion.identity);
            shelfObj.transform.localScale = shelfPrefab.transform.localScale;
            shelf.shelfTransform = shelfObj.transform;
            shelves.Add(shelf);
        }
        for (int i = 0; i < rightShelves; i++)
        {
            Shelf shelf = new Shelf { side = Shelf.ShelfSide.Right };
            Vector3 pos = CalculateShelfPosition(i, Shelf.ShelfSide.Right, screenWidth, shelfHeight, shelfOffset);
            GameObject shelfObj = Instantiate(shelfPrefab, pos, Quaternion.identity);
            shelfObj.transform.localScale = shelfPrefab.transform.localScale;
            shelf.shelfTransform = shelfObj.transform;
            shelves.Add(shelf);
        }
        Debug.Log($"Initialized {shelves.Count} shelves");
    }
    private Vector3 CalculateShelfPosition(int index, Shelf.ShelfSide side, float screenWidth, float shelfHeight, float shelfOffset)
    {
        float x = side == Shelf.ShelfSide.Left ? -screenWidth / 2 + shelfOffset : screenWidth / 2 - shelfOffset;

        // Determine vertical offset so shelves alternate around the centre:
        // 0 -> 0, 1 -> +h, 2 -> -h, 3 -> +2h, 4 -> -2h, ...
        float y;
        if (index == 0) y = 0f;
        else
        {
            int step = (index + 1) / 2;
            float sign = (index % 2 == 1) ? 1f : -1f;
            y = sign * step * shelfHeight;
        }

        return new Vector3(x, y, 0);
    }


    private Shelf GetShelfAtPosition(Vector2 pos)
    {
        foreach (var shelf in shelves)
        {
            if (Vector2.Distance(pos, shelf.shelfTransform.position) < 0.5f)
                return shelf;
        }
        return null;
    }


    private void SelectCats(Shelf shelf, Vector2 clickPos)
    {
        int clickedIndex = -1;
        for (int i = 0; i < shelf.cats.Count; i++)
        {
            if (Vector2.Distance(clickPos, shelf.cats[i].transform.position) < 0.3f)
            {
                clickedIndex = i;
                break;
            }
        }

        if (clickedIndex < 0)
        {
            if (shelf.cats.Any(c => c.isSelected))
            {
                ClearSelection();
            }
            return;
        }

        float shelfCenterX = shelf.shelfTransform.position.x;
        bool rightSide = shelf.cats[clickedIndex].transform.position.x >= shelfCenterX;

        // find the index of the cat closest to the center on the clicked side
        int firstIndex = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < shelf.cats.Count; i++)
        {
            float dx = shelf.cats[i].transform.position.x - shelfCenterX;
            if (rightSide && dx >= 0 && Mathf.Abs(dx) < bestDist)
            {
                bestDist = Mathf.Abs(dx);
                firstIndex = i;
            }
            else if (!rightSide && dx <= 0 && Mathf.Abs(dx) < bestDist)
            {
                bestDist = Mathf.Abs(dx);
                firstIndex = i;
            }
        }

        if (firstIndex == -1) return;

        // click must be on the first cat or one of those further outward
        if ((rightSide && clickedIndex < firstIndex) || (!rightSide && clickedIndex > firstIndex))
        {
            return;
        }

        int catType = shelf.cats[firstIndex].type;
        ClearSelection();

        if (rightSide)
        {
            for (int i = firstIndex; i < shelf.cats.Count && i < firstIndex + 4; i++)
            {
                if (shelf.cats[i].type == catType && shelf.cats[i].transform.position.x >= shelfCenterX)
                {
                    shelf.cats[i].isSelected = true;
                }
                else break;
            }
        }
        else
        {
            for (int i = firstIndex; i >= 0 && i > firstIndex - 4; i--)
            {
                if (shelf.cats[i].type == catType && shelf.cats[i].transform.position.x <= shelfCenterX)
                {
                    shelf.cats[i].isSelected = true;
                }
                else break;
            }
        }

        UpdateVisualSelection();
    }
    private void ClearSelection()
    {
        foreach (var shelf in shelves)
        {
            foreach (var cat in shelf.cats)
            {
                cat.isSelected = false;
            }
        }
        UpdateVisualSelection();
    }

    private void UpdateVisualSelection()
    {
        foreach (var shelf in shelves)
        {
            foreach (var cat in shelf.cats)
            {
                SpriteRenderer sr = cat.spriteRenderer;
                if (sr != null)
                    sr.color = cat.isSelected ? Color.yellow : Color.white;
            }
        }
    }

    private void MoveSelectedCats(Shelf targetShelf)
    {
        Shelf sourceShelf = shelves.Find(s => s.cats.Exists(c => c.isSelected));
        if (sourceShelf == null) return;

        List<Cat> selectedCats = sourceShelf.cats.FindAll(c => c.isSelected);
        int selectedCount = selectedCats.Count;
        if (selectedCount == 0) return;

        // Check capacity of the target shelf
        if (targetShelf.cats.Count + selectedCount > 4)
        {
            audioVibrationManager?.Vibrate();
            StartCoroutine(FlashError(targetShelf));
            return;
        }

        // Ensure all cats on the target shelf are of the same type and match the selected cats
        if (targetShelf.cats.Count > 0)
        {
            int targetType = targetShelf.cats[0].type;
            bool sameType = targetShelf.cats.TrueForAll(c => c.type == targetType);
            if (!sameType || targetType != selectedCats[0].type)
            {
                audioVibrationManager?.Vibrate();
                StartCoroutine(FlashError(targetShelf));
                return;
            }
        }

        StartCoroutine(MoveCatsAnimation(sourceShelf, targetShelf, selectedCats));
        foreach (var cat in selectedCats)
        {
            sourceShelf.cats.Remove(cat);
            targetShelf.cats.Add(cat);
            cat.isSelected = false;
        }
        UpdateVisualSelection();
        CheckForCompletedShelf(targetShelf);
        CheckForCompletedShelf(sourceShelf);
        CheckLevelCompletion();
    }

    private IEnumerator MoveCatsAnimation(Shelf sourceShelf, Shelf targetShelf, List<Cat> cats)
    {
        float shelfHeight = sourceShelf.shelfTransform.GetComponent<SpriteRenderer>().bounds.size.y;
        float catHeight = catPrefab.GetComponent<SpriteRenderer>().bounds.size.y;
        float yOffset = shelfHeight / 2f + catHeight / 2f;
        Vector3 startPos = sourceShelf.shelfTransform.position + Vector3.up * yOffset;
        Vector3 endPos = targetShelf.shelfTransform.position + Vector3.up * yOffset;
        float duration = 0.5f;
        float elapsed = 0f;

        // Переключение на спрайт прыжка
        foreach (var cat in cats)
        {
            cat.spriteRenderer.sprite = catSprites[cat.type * 2 + 1]; // type_0_jump, type_1_jump, etc.
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float height = Mathf.Sin(t * Mathf.PI) * 1f; // Параболическая траектория прыжка
            Vector3 midPos = Vector3.Lerp(startPos, endPos, t) + Vector3.up * height;
            foreach (var cat in cats)
            {
                cat.transform.position = midPos;
            }
            yield return null;
        }

        // Возвращение к обычному спрайту
        for (int i = 0; i < cats.Count; i++)
        {
            cats[i].transform.position = targetShelf.shelfTransform.position + Vector3.up * yOffset + Vector3.right * (i - cats.Count / 2f) * 0.5f;
            cats[i].spriteRenderer.sprite = catSprites[cats[i].type * 2]; // type_0, type_1, etc.
        }
    }

    private IEnumerator FlashError(Shelf shelf)
    {
        SpriteRenderer sr = shelf.shelfTransform.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            sr.color = Color.white;
        }
    }

    private void CheckForCompletedShelf(Shelf shelf)
    {
        if (shelf.cats.Count == 4 && shelf.cats.All(c => c.type == shelf.cats[0].type))
        {
            StartCoroutine(CollectShelf(shelf));
        }
    }

    private IEnumerator CollectShelf(Shelf shelf)
    {
        float enlargeDuration = 0.1f;
        float shrinkDuration = 0.15f;
        float t = 0f;
        while (t < enlargeDuration)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 1.2f, t / enlargeDuration);
            foreach (var cat in shelf.cats)
            {
                cat.transform.localScale = Vector3.one * scale;
            }
            yield return null;
        }

        t = 0f;
        while (t < shrinkDuration)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(1.2f, 0f, t / shrinkDuration);
            foreach (var cat in shelf.cats)
            {
                cat.transform.localScale = Vector3.one * scale;
            }
            yield return null;
        }

        foreach (var cat in shelf.cats)
        {
            Destroy(cat.transform.gameObject);
        }
        shelf.cats.Clear();
        CheckLevelCompletion();
    }

    public void GenerateLevel()
    {
        foreach (var shelf in shelves)
        {
            shelf.cats.Clear();
        }

        List<int> availableTypes = currentConfig != null && currentConfig.catTypes.Count > 0
            ? currentConfig.catTypes
            : Enumerable.Range(0, catSprites.Length / 2).ToList();

        foreach (var shelf in shelves)
        {
            float shelfHeight = shelf.shelfTransform.GetComponent<SpriteRenderer>().bounds.size.y;
            float catHeight = catPrefab.GetComponent<SpriteRenderer>().bounds.size.y;
            float yOffset = shelfHeight / 2f + catHeight / 2f;
            int catCount = Random.Range(1, 5);

            List<int> types = new List<int>();
            for (int i = 0; i < catCount; i++)
            {
                types.Add(availableTypes[Random.Range(0, availableTypes.Count)]);
            }

            if (catCount == 4 && types.Distinct().Count() == 1 && availableTypes.Count > 1)
            {
                int changeIndex = Random.Range(0, 4);
                int newType = types[0];
                while (newType == types[0])
                {
                    newType = availableTypes[Random.Range(0, availableTypes.Count)];
                }
                types[changeIndex] = newType;
            }

            for (int i = 0; i < catCount; i++)
            {
                Vector3 pos = shelf.shelfTransform.position + Vector3.up * yOffset + Vector3.right * (i - catCount / 2f) * 0.5f;
                GameObject catObj = Instantiate(catPrefab, pos, Quaternion.identity);
                catObj.transform.localScale = catPrefab.transform.localScale;
                Cat cat = new Cat
                {
                    type = types[i],
                    transform = catObj.transform,
                    spriteRenderer = catObj.GetComponent<SpriteRenderer>()
                };
                cat.spriteRenderer.sprite = catSprites[cat.type * 2];
                shelf.cats.Add(cat);
            }
        }

        Debug.Log($"Generated level with {shelves.Sum(s => s.cats.Count)} cats");
    }

    private void CheckLevelCompletion()
    {
        bool isComplete = shelves.All(s => s.cats.Count == 0);
        if (isComplete)
        {
            ShowLevelCompletePanel(0);
        }
    }

    private void ShowLevelCompletePanel(int rawScore)
    {
        int current = PlayerPrefs.GetInt("CatSortLevel", 0);
        PlayerPrefs.SetInt("CatSortLevel", current + 1);
        PlayerPrefs.Save();

        Time.timeScale = 0f;
        levelCompletePanel.SetActive(true);

        nextLevelButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            levelCompletePanel.SetActive(false);
            ResetLevel();
        });

        exitButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            levelCompletePanel.SetActive(false);
            SceneManager.LoadScene("ModeSelectScene");
        });

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.ShowLevelCompletePanel(rawScore, 0, 0); // Передаем 0 для оптимальных и фактических ходов как заглушки
        }
    }

    public void Initialize()
    {
        Debug.Log("CatSortMode Initialize called");
        currentLevelIndex = PlayerPrefs.GetInt("CatSortLevel", 0);
        if (levelConfigs != null && levelConfigs.Length > currentLevelIndex)
        {
            currentConfig = levelConfigs[currentLevelIndex];
        }
        else
        {
            currentConfig = new LevelConfig { leftShelves = leftShelves, rightShelves = rightShelves };
            if (currentConfig.catTypes.Count == 0)
            {
                for (int i = 0; i < catSprites.Length / 2; i++)
                {
                    currentConfig.catTypes.Add(i);
                }
            }
        }

        leftShelves = currentConfig.leftShelves;
        rightShelves = currentConfig.rightShelves;

        InitializeShelves();
        GenerateLevel();
    }

    public void Resume()
    {
        Time.timeScale = 1f;
    }

    public void Pause()
    {
        Time.timeScale = 0f;
    }

    public void EndGame()
    {
        Time.timeScale = 1f;
        foreach (var shelf in shelves)
        {
            foreach (var cat in shelf.cats)
            {
                Destroy(cat.transform.gameObject);
            }
            Destroy(shelf.shelfTransform.gameObject);
        }
        shelves.Clear();
    }

    public void ResetLevel()
    {
        EndGame();
        currentLevelIndex = PlayerPrefs.GetInt("CatSortLevel", 0);
        if (levelConfigs != null && levelConfigs.Length > currentLevelIndex)
        {
            currentConfig = levelConfigs[currentLevelIndex];
        }
        leftShelves = currentConfig.leftShelves;
        rightShelves = currentConfig.rightShelves;
        InitializeShelves();
        GenerateLevel();
    }
}
