using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PreviewManager : MonoBehaviour
{
    public Image[] previewImages;

    private GameObject[] playerPrefabs;
    private Queue<int> prefabQueue;

    void Start()
    {
        if (previewImages.Length != 3)
        {
            return;
        }
        foreach (Image img in previewImages)
        {
            if (img == null)
            {
                return;
            }
        }
    }

    void OnEnable()
    {
        GameModeManager gameModeManager = FindObjectOfType<GameModeManager>();
        if (gameModeManager != null && gameModeManager.CurrentMode == GameModeManager.GameMode.MergeMode)
        {
            gameObject.SetActive(false);
            return;
        }

        if (prefabQueue != null)
        {
            UpdatePreviews(prefabQueue);
        }
    }

    public void ResetPreviews()
    {
        foreach (Image img in previewImages)
        {
            if (img != null)
            {
                img.gameObject.SetActive(true);
                img.sprite = null;
                img.color = new Color(1, 1, 1, 0);
            }
        }
    }

    public void InitializePreviews(PointerController pointer, Queue<int> prefabQueue)
    {
        if (pointer == null || pointer.playerPrefabs.Length == 0)
        {
            return;
        }

        this.prefabQueue = prefabQueue;
        playerPrefabs = pointer.playerPrefabs;

        foreach (Image img in previewImages)
        {
            if (img != null && !img.gameObject.activeSelf)
            {
                img.gameObject.SetActive(true);
            }
        }

        UpdatePreviews(prefabQueue);
    }

    public void OnPrefabSpawned(Queue<int> prefabQueue)
    {
        GameModeManager gameModeManager = FindObjectOfType<GameModeManager>();
        if (gameModeManager != null && gameModeManager.CurrentMode == GameModeManager.GameMode.MergeMode)
        {
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            this.prefabQueue = prefabQueue;
            return;
        }

        this.prefabQueue = prefabQueue;
        UpdatePreviews(prefabQueue);
    }

    void UpdatePreviews(Queue<int> prefabQueue)
    {
        int[] queueArray = prefabQueue.ToArray();
        if (queueArray.Length >= 4)
        {
            UpdateImage(0, queueArray[1]);
            UpdateImage(1, queueArray[2]);
            UpdateImage(2, queueArray[3]);
        }
        else
        {
            PointerController pointer = FindObjectOfType<PointerController>();
            if (pointer != null)
            {
                pointer.FillQueue();
                queueArray = prefabQueue.ToArray();
                if (queueArray.Length >= 4)
                {
                    UpdateImage(0, queueArray[1]);
                    UpdateImage(1, queueArray[2]);
                    UpdateImage(2, queueArray[3]);
                }
            }
        }
    }

    void UpdateImage(int index, int prefabIndex)
    {
        if (index >= 0 && index < previewImages.Length && prefabIndex >= 0 && prefabIndex < playerPrefabs.Length)
        {
            if (previewImages[index] != null)
            {
                if (!previewImages[index].gameObject.activeSelf)
                {
                    previewImages[index].gameObject.SetActive(true);
                }
                SpriteRenderer sr = playerPrefabs[prefabIndex].GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    previewImages[index].sprite = sr.sprite;
                    previewImages[index].color = new Color(1, 1, 1, 1);
                    previewImages[index].preserveAspect = true;
                }
            }
        }
    }

    public void HidePreviews()
    {
        foreach (Image img in previewImages)
        {
            if (img != null)
            {
                img.gameObject.SetActive(false);
            }
        }
    }

    public void ShowPreviews()
    {
        GameModeManager gameModeManager = FindObjectOfType<GameModeManager>();
        if (gameModeManager != null && gameModeManager.CurrentMode == GameModeManager.GameMode.MergeMode)
        {
            return;
        }

        foreach (Image img in previewImages)
        {
            if (img != null)
            {
                img.gameObject.SetActive(true);
            }
        }
        if (prefabQueue != null)
        {
            UpdatePreviews(prefabQueue);
        }
    }
}