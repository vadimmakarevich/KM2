using UnityEngine;
using System.Collections.Generic;

public class ShelfCompletionChecker : MonoBehaviour
{
    public bool AreAllShelvesEmpty(List<CatSortMode.Shelf> shelves)
    {
        foreach (var shelf in shelves)
        {
            if (shelf.cats.Count > 0)
            {
                return false;
            }
        }
        return true;
    }

    public void CheckAllShelvesEmpty(List<CatSortMode.Shelf> shelves)
    {
        if (!AreAllShelvesEmpty(shelves))
        {
            return;
        }

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetGameActive(false);
            GameModeManager.Instance.ShowLevelCompletePanel(shelves.Count * 4, 0, 0);
        }
    }
}
