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
    private CatSortLevelDefinition currentLevel;
        LoadCurrentLevel();
    private void LoadCurrentLevel()
    {
        int levelIndex = PlayerPrefs.GetInt("CatSortLevel", 0);
        TextAsset json = Resources.Load<TextAsset>($"CatSortLevels/level{levelIndex}");
        if (json != null)
        {
            currentLevel = JsonUtility.FromJson<CatSortLevelDefinition>(json.text);
        }
        else
        {
            Debug.LogWarning($"Level data for level{levelIndex} not found, using random level");
            currentLevel = null;
        }
    }

        if (currentLevel != null && currentLevel.shelves != null)
            for (int i = 0; i < currentLevel.shelves.Count; i++)
            {
                var data = currentLevel.shelves[i];
                Shelf shelf = new Shelf { side = data.side == "Left" ? Shelf.ShelfSide.Left : Shelf.ShelfSide.Right };
                float x = shelf.side == Shelf.ShelfSide.Left ? -screenWidth / 2 + shelfOffset : screenWidth / 2 - shelfOffset;
                shelf.shelfTransform = Instantiate(shelfPrefab, new Vector3(x, i * shelfHeight, 0), Quaternion.identity).transform;
                shelves.Add(shelf);
            }
        else
            int leftShelves = 2;
            int rightShelves = 1;
            for (int i = 0; i < leftShelves; i++)
            {
                Shelf shelf = new Shelf { side = Shelf.ShelfSide.Left };
                shelf.shelfTransform = Instantiate(shelfPrefab, new Vector3(-screenWidth / 2 + shelfOffset, i * shelfHeight, 0), Quaternion.identity).transform;
                shelves.Add(shelf);
            }
            for (int i = 0; i < rightShelves; i++)
            {
                Shelf shelf = new Shelf { side = Shelf.ShelfSide.Right };
                shelf.shelfTransform = Instantiate(shelfPrefab, new Vector3(screenWidth / 2 - shelfOffset, i * shelfHeight, 0), Quaternion.identity).transform;
                shelves.Add(shelf);
            }
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
        int leftShelves = 2; // 2 ïîëêè ñëåâà
        int rightShelves = 1; // 1 ïîëêà ñïðàâà

        float screenWidth = Camera.main.orthographicSize * 2 * Camera.main.aspect;
        float shelfHeight = 1f;
        float shelfOffset = 0.5f;

        for (int i = 0; i < leftShelves; i++)
        {
            Shelf shelf = new Shelf { side = Shelf.ShelfSide.Left };
            shelf.shelfTransform = Instantiate(shelfPrefab, new Vector3(-screenWidth / 2 + shelfOffset, i * shelfHeight, 0), Quaternion.identity).transform;
            shelves.Add(shelf);
        }
        for (int i = 0; i < rightShelves; i++)
        {
            Shelf shelf = new Shelf { side = Shelf.ShelfSide.Right };
            shelf.shelfTransform = Instantiate(shelfPrefab, new Vector3(screenWidth / 2 - shelfOffset, i * shelfHeight, 0), Quaternion.identity).transform;
            shelves.Add(shelf);
        }
        Debug.Log($"Initialized {shelves.Count} shelves");
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
        int startIndex = -1;
        for (int i = 0; i < shelf.cats.Count; i++)
        {
            if (Vector2.Distance(clickPos, shelf.cats[i].transform.position) < 0.3f)
            {
                startIndex = i;
                break;
            }
        }

        if (startIndex >= 0)
        {
            int catType = shelf.cats[startIndex].type;
            ClearSelection();
            for (int i = startIndex; i < shelf.cats.Count && i < startIndex + 4; i++)
            {
                if (shelf.cats[i].type == catType)
                {
                    shelf.cats[i].isSelected = true;
                }
                else break;
            }
            UpdateVisualSelection();
        }
        else if (shelf.cats.Any(c => c.isSelected))
        {
            ClearSelection();
        }
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
        if (selectedCats.Count == 0) return;

        int targetType = targetShelf.cats.Count > 0 ? targetShelf.cats[0].type : -1;
        if (targetShelf.IsFull || (targetType != -1 && targetType != selectedCats[0].type))
        {
            audioVibrationManager?.Vibrate();
            StartCoroutine(FlashError(targetShelf));
            return;
        }

        StartCoroutine(MoveCatsAnimation(sourceShelf, targetShelf, selectedCats));
        foreach (var cat in selectedCats)
        {
            sourceShelf.cats.Remove(cat);
            targetShelf.cats.Add(cat);
            cat.isSelected = false;
        }
        UpdateVisualSelection();
        CheckLevelCompletion();
    }

    private IEnumerator MoveCatsAnimation(Shelf sourceShelf, Shelf targetShelf, List<Cat> cats)
    {
        Vector3 startPos = sourceShelf.shelfTransform.position;
        Vector3 endPos = targetShelf.shelfTransform.position;
        float duration = 0.5f;
        float elapsed = 0f;

        // Ïåðåêëþ÷åíèå íà ñïðàéò ïðûæêà
        foreach (var cat in cats)
        {
            cat.spriteRenderer.sprite = catSprites[cat.type * 2 + 1]; // type_0_jump, type_1_jump, etc.
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float height = Mathf.Sin(t * Mathf.PI) * 1f; // Ïàðàáîëè÷åñêàÿ òðàåêòîðèÿ ïðûæêà
            Vector3 midPos = Vector3.Lerp(startPos, endPos, t) + Vector3.up * height;
            foreach (var cat in cats)
            {
                cat.transform.position = midPos;
            }
            yield return null;
        }

        // Âîçâðàùåíèå ê îáû÷íîìó ñïðàéòó
        for (int i = 0; i < cats.Count; i++)
        {
            cats[i].transform.position = targetShelf.shelfTransform.position + Vector3.right * (i - cats.Count / 2) * 0.5f;
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

    public void GenerateLevel()
    {
        foreach (var shelf in shelves)
        {
            shelf.cats.Clear();
        }

        if (currentLevel != null && currentLevel.shelves != null)
        {
            for (int i = 0; i < currentLevel.shelves.Count && i < shelves.Count; i++)
            {
                var data = currentLevel.shelves[i];
                Shelf targetShelf = shelves[i];
                for (int j = 0; j < data.cats.Count; j++)
                {
                    GameObject catObj = Instantiate(catPrefab, targetShelf.shelfTransform.position + Vector3.right * (j - data.cats.Count / 2f) * 0.5f, Quaternion.identity);
                    Cat cat = new Cat
                    {
                        type = data.cats[j],
                        transform = catObj.transform,
                        spriteRenderer = catObj.GetComponent<SpriteRenderer>()
                    };
                    cat.spriteRenderer.sprite = catSprites[cat.type * 2];
                    targetShelf.cats.Add(cat);
                }
            }
        }
        else
        {
            foreach (var shelf in shelves)
            {
                int catCount = Random.Range(1, 5);
                for (int i = 0; i < catCount; i++)
                {
                    GameObject catObj = Instantiate(catPrefab, shelf.shelfTransform.position + Vector3.right * (i - catCount / 2) * 0.5f, Quaternion.identity);
                    Cat cat = new Cat
                    {
                        type = Random.Range(0, 12),
                        transform = catObj.transform,
                        spriteRenderer = catObj.GetComponent<SpriteRenderer>()
                    };
                    cat.spriteRenderer.sprite = catSprites[cat.type * 2];
                    shelf.cats.Add(cat);
                }
            }
        }
        Debug.Log($"Generated level with {shelves.Sum(s => s.cats.Count)} cats");
    }

    private void CheckLevelCompletion()
    {
        bool isComplete = true;
        foreach (var shelf in shelves)
        {
            if (shelf.cats.Count != 4 || shelf.cats.Select(c => c.type).Distinct().Count() != 1)
            {
                isComplete = false;
                break;
            }
        }

        if (isComplete)
        {
            ResetLevel();
            GenerateLevel();
        LoadCurrentLevel();
        LoadCurrentLevel();
}
        Time.timeScale = 0f;
        levelCompletePanel.SetActive(true);

        nextLevelButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            levelCompletePanel.SetActive(false);
            ResetLevel();
            GenerateLevel();
            int newLevelIndex = PlayerPrefs.GetInt("CatSortLevel", 0) + 1;
            PlayerPrefs.SetInt("CatSortLevel", newLevelIndex);
            PlayerPrefs.Save();
        });

        exitButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            levelCompletePanel.SetActive(false);
            SceneManager.LoadScene("ModeSelectScene");
        });

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.ShowLevelCompletePanel(rawScore, 0, 0); // Ïåðåäàåì 0 äëÿ îïòèìàëüíûõ è ôàêòè÷åñêèõ õîäîâ êàê çàãëóøêè
        }
    }

    public void Initialize()
    {
        Debug.Log("CatSortMode Initialize called");
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
        InitializeShelves();
        GenerateLevel();
    }
}