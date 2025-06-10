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
    [SerializeField] private Sprite[] catSprites; // Ìàññèâ ñïðàéòîâ: [type_0, type_0_jump, type_1, type_1_jump, ...]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button exitButton;
    private List<Shelf> shelves = new List<Shelf>();
    private AudioVibrationManager audioVibrationManager;

    [System.Serializable]
    public class Cat
    {
        public int type; // 0 to 11 äëÿ 12 òèïîâ êîòîâ
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
        float duration = 0.5f;
        float elapsed = 0f;

        Vector3[] startPositions = new Vector3[cats.Count];
        Vector3[] endPositions = new Vector3[cats.Count];

        for (int i = 0; i < cats.Count; i++)
        {
            startPositions[i] = cats[i].transform.position;
            int index = targetShelf.cats.IndexOf(cats[i]);
            float offset = (index - (targetShelf.cats.Count - 1) / 2f) * 0.5f;
            endPositions[i] = targetShelf.shelfTransform.position + Vector3.right * offset;
            cats[i].spriteRenderer.sprite = catSprites[cats[i].type * 2 + 1];
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float height = Mathf.Sin(t * Mathf.PI) * 1f;

            for (int i = 0; i < cats.Count; i++)
            {
                Vector3 midPos = Vector3.Lerp(startPositions[i], endPositions[i], t) + Vector3.up * height;
                cats[i].transform.position = midPos;
            }

            yield return null;
        }

        for (int i = 0; i < cats.Count; i++)
        {
            cats[i].transform.position = endPositions[i];
            cats[i].spriteRenderer.sprite = catSprites[cats[i].type * 2];
        }

        RepositionCats(sourceShelf);
        RepositionCats(targetShelf);
    }

    private void RepositionCats(Shelf shelf)
    {
        for (int i = 0; i < shelf.cats.Count; i++)
        {
            float offset = (i - (shelf.cats.Count - 1) / 2f) * 0.5f;
            shelf.cats[i].transform.position = shelf.shelfTransform.position + Vector3.right * offset;
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

        // Äëÿ ïåðâîãî óðîâíÿ: ãàðàíòèðóåì 4 êîòà
        int levelIndex = PlayerPrefs.GetInt("CatSortLevel", 0); // Ïîëó÷àåì òåêóùèé óðîâåíü (ïî óìîë÷àíèþ 0)
        if (levelIndex == 0) // Ïåðâûé óðîâåíü (ó÷åáíûé)
        {
            Shelf targetShelf = shelves[0]; // Èñïîëüçóåì ïåðâóþ ïîëêó
            for (int i = 0; i < 4; i++)
            {
                GameObject catObj = Instantiate(catPrefab, targetShelf.shelfTransform.position + Vector3.right * (i - 1.5f) * 0.5f, Quaternion.identity);
                Cat cat = new Cat
                {
                    type = 0, // Îäèí òèï êîòà äëÿ ïðîñòîòû
                    transform = catObj.transform,
                    spriteRenderer = catObj.GetComponent<SpriteRenderer>()
                };
                cat.spriteRenderer.sprite = catSprites[cat.type * 2]; // Óñòàíîâêà íà÷àëüíîãî ñïðàéòà
                targetShelf.cats.Add(cat);
            }
        }
        else // Äëÿ ïîñëåäóþùèõ óðîâíåé: ñëó÷àéíàÿ ãåíåðàöèÿ
        {
            foreach (var shelf in shelves)
            {
                int catCount = Random.Range(1, 5); // Îò 1 äî 4 êîòîâ íà ïîëêó
                for (int i = 0; i < catCount; i++)
                {
                    GameObject catObj = Instantiate(catPrefab, shelf.shelfTransform.position + Vector3.right * (i - catCount / 2) * 0.5f, Quaternion.identity);
                    Cat cat = new Cat
                    {
                        type = Random.Range(0, 12),
                        transform = catObj.transform,
                        spriteRenderer = catObj.GetComponent<SpriteRenderer>()
                    };
                    cat.spriteRenderer.sprite = catSprites[cat.type * 2]; // Óñòàíîâêà íà÷àëüíîãî ñïðàéòà
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
            ShowLevelCompletePanel(4 * shelves.Count); // Ïðîñòàÿ ëîãèêà ñ÷åòà (4 êîòà íà ïîëêó * êîëè÷åñòâî ïîëîê)
        }
    }

    private void ShowLevelCompletePanel(int rawScore)
    {
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