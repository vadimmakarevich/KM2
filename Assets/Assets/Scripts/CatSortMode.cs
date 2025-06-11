using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CatSortMode : MonoBehaviour
{
    [SerializeField] private GameObject leftShelfPrefab;
    [SerializeField] private GameObject rightShelfPrefab;
    [SerializeField] private GameObject catPrefab;
    [SerializeField] private Sprite[] catSprites; // Ìàññèâ ñïðàéòîâ: [type_0, type_0_jump, type_1, type_1_jump, ...]
    [SerializeField] private ShelfCompletionChecker shelfCompletionChecker;
    [SerializeField] private int leftShelves = 2;
    [SerializeField] private int rightShelves = 2;
    [SerializeField] private float shelfOffset = 0.5f;
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
            Shelf clickedShelf = GetShelfAtPosition(worldPos);

            if (clickedShelf != null)
            {
                Shelf selectedShelf = shelves.Find(s => s.cats.Any(c => c.isSelected));
                if (selectedShelf != null && selectedShelf != clickedShelf)
                {
                    MoveSelectedCats(clickedShelf);
                }
                else
                {
                    SelectCats(clickedShelf, worldPos);
                }
            }
            else
            {
                ClearSelection();
            }
        }
    }

    private void InitializeShelves()
    {

        float screenWidth = Camera.main.orthographicSize * 2 * Camera.main.aspect;
        float shelfHeight = 1f;

        for (int i = 0; i < leftShelves; i++)
        {
            Shelf shelf = new Shelf { side = Shelf.ShelfSide.Left };
            Vector3 pos = CalculateShelfPosition(i, Shelf.ShelfSide.Left, screenWidth, shelfHeight, shelfOffset);
            GameObject shelfObj = Instantiate(leftShelfPrefab, pos, Quaternion.identity);
            shelfObj.transform.localScale = leftShelfPrefab.transform.localScale;
            shelf.shelfTransform = shelfObj.transform;
            shelves.Add(shelf);
        }
        for (int i = 0; i < rightShelves; i++)
        {
            Shelf shelf = new Shelf { side = Shelf.ShelfSide.Right };
            Vector3 pos = CalculateShelfPosition(i, Shelf.ShelfSide.Right, screenWidth, shelfHeight, shelfOffset);
            GameObject shelfObj = Instantiate(rightShelfPrefab, pos, Quaternion.identity);
            shelfObj.transform.localScale = rightShelfPrefab.transform.localScale;
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
            Vector2 center = shelf.cats[i].transform.position +
                             Vector3.up * shelf.cats[i].spriteRenderer.bounds.size.y / 2f;

            if (Vector2.Distance(clickPos, center) < 0.3f)
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
        TryAssembleShelf(targetShelf);
        shelfCompletionChecker?.CheckAllShelvesEmpty(shelves);
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

    private void TryAssembleShelf(Shelf shelf)
    {
        if (shelf.cats.Count == 4 && shelf.cats.All(c => c.type == shelf.cats[0].type))
        {
            StartCoroutine(AssembleShelf(shelf));
        }
    }


    private IEnumerator AssembleShelf(Shelf shelf)
    {
        float upDuration = 0.2f;
        float downDuration = 0.2f;

        float elapsed = 0f;
        while (elapsed < upDuration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 1.2f, elapsed / upDuration);
            foreach (var cat in shelf.cats)
            {
                if (cat.transform != null) cat.transform.localScale = Vector3.one * scale;
            }
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < downDuration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1.2f, 0f, elapsed / downDuration);
            foreach (var cat in shelf.cats)
            {
                if (cat.transform != null) cat.transform.localScale = Vector3.one * scale;
            }
            yield return null;
        }

        foreach (var cat in shelf.cats)
        {
            if (cat.transform != null) Destroy(cat.transform.gameObject);
        }
        shelf.cats.Clear();

        shelfCompletionChecker?.CheckAllShelvesEmpty(shelves);
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
            float shelfHeight = targetShelf.shelfTransform.GetComponent<SpriteRenderer>().bounds.size.y;
            float catHeight = catPrefab.GetComponent<SpriteRenderer>().bounds.size.y;
            float yOffset = shelfHeight / 2f + catHeight / 2f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 pos = targetShelf.shelfTransform.position + Vector3.up * yOffset + Vector3.right * (i - 1.5f) * 0.5f;
                GameObject catObj = Instantiate(catPrefab, pos, Quaternion.identity);
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
                float shelfHeight = shelf.shelfTransform.GetComponent<SpriteRenderer>().bounds.size.y;
                float catHeight = catPrefab.GetComponent<SpriteRenderer>().bounds.size.y;
                float yOffset = shelfHeight / 2f + catHeight / 2f;
                int catCount = Random.Range(1, 5); // Îò 1 äî 4 êîòîâ íà ïîëêó
                for (int i = 0; i < catCount; i++)
                {
                    Vector3 pos = shelf.shelfTransform.position + Vector3.up * yOffset + Vector3.right * (i - catCount / 2f) * 0.5f;
                    GameObject catObj = Instantiate(catPrefab, pos, Quaternion.identity);
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
                if (catCount == 4 && shelf.cats.All(c => c.type == shelf.cats[0].type))
                {
                    int newType = (shelf.cats[0].type + 1) % 12;
                    shelf.cats[0].type = newType;
                    shelf.cats[0].spriteRenderer.sprite = catSprites[newType * 2];
                }
            }
        }
        Debug.Log($"Generated level with {shelves.Sum(s => s.cats.Count)} cats");
    }


    public void Initialize()
    {
        Debug.Log("CatSortMode Initialize called");
        EndGame();
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
