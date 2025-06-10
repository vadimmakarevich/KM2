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
    [SerializeField] private int leftShelves = 2;
    [SerializeField] private int rightShelves = 2;
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

        // Äëÿ ïåðâîãî óðîâíÿ: ãàðàíòèðóåì 4 êîòà
        int levelIndex = PlayerPrefs.GetInt("CatSortLevel", 0); // Ïîëó÷àåì òåêóùèé óðîâåíü (ïî óìîë÷àíèþ 0)
        if (levelIndex == 0) // Ïåðâûé óðîâåíü (ó÷åáíûé)
        {
            Shelf targetShelf = shelves[0]; // Èñïîëüçóåì ïåðâóþ ïîëêó
            for (int i = 0; i < 4; i++)
            {
                GameObject catObj = Instantiate(catPrefab, targetShelf.shelfTransform.position + Vector3.right * (i - 1.5f) * 0.5f, Quaternion.identity);
                catObj.transform.localScale = catPrefab.transform.localScale;
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
                    catObj.transform.localScale = catPrefab.transform.localScale;
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
