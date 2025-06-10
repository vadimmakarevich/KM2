using UnityEngine;
using System.Collections;

public class MergeManager : MonoBehaviour
{
    public GameObject[] prefabs;

    private ObjectLevel thisLevel;
    private PointerController pointerController;
    private AudioVibrationManager audioVibrationManager;
    private ScoreManager scoreManager;
    private bool isMerging = false;
    private bool hasCollided = false;

    void Start()
    {
        thisLevel = GetComponent<ObjectLevel>();
        if (thisLevel == null)
        {
            Destroy(gameObject);
        }

        pointerController = FindObjectOfType<PointerController>();
        audioVibrationManager = AudioVibrationManager.Instance;
        scoreManager = FindObjectOfType<ScoreManager>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isMerging) return;

        if (!hasCollided && audioVibrationManager != null)
        {
            int prefabIndex = thisLevel != null ? thisLevel.level - 1 : 0;
            AudioClip collisionSound = audioVibrationManager.GetPrefabCollisionSound(prefabIndex);
            if (collisionSound != null)
            {
                audioVibrationManager.PlaySFX(collisionSound);
                hasCollided = true;
            }
        }

        if (gameObject.GetComponent<ElectricCat>() != null || collision.gameObject.GetComponent<ElectricCat>() != null)
        {
            return;
        }

        ObjectLevel otherLevel = collision.gameObject.GetComponent<ObjectLevel>();

        if (thisLevel == null || otherLevel == null)
        {
            return;
        }

        if (thisLevel.level == 13 || otherLevel.level == 13)
            return;

        if (thisLevel.level == otherLevel.level && thisLevel.level < 11)
        {
            isMerging = true;

            int currentLevel = thisLevel.level;
            int nextLevel = currentLevel + 1;

            if (nextLevel <= prefabs.Length && prefabs[nextLevel - 1] != null)
            {
                if (this.GetInstanceID() < collision.gameObject.GetInstanceID())
                {
                    GameObject newObj = Instantiate(prefabs[nextLevel - 1], transform.position, Quaternion.identity);

                    SpriteRenderer sr = newObj.GetComponent<SpriteRenderer>();
                    if (sr != null && pointerController != null)
                    {
                        sr.sortingOrder = pointerController.GetNextSortingOrder();
                    }

                    if (pointerController != null && pointerController.canBlink[nextLevel - 1])
                    {
                        int blinkSpriteIndex = pointerController.GetBlinkSpriteIndex(nextLevel - 1);
                        if (blinkSpriteIndex >= 0 && blinkSpriteIndex < pointerController.closedEyesSprites.Length)
                        {
                            BlinkController blinkController = newObj.AddComponent<BlinkController>();
                            blinkController.openEyesSprite = prefabs[nextLevel - 1].GetComponent<SpriteRenderer>().sprite;
                            blinkController.closedEyesSprite = pointerController.closedEyesSprites[blinkSpriteIndex];
                            blinkController.minBlinkInterval = 5f;
                            blinkController.maxBlinkInterval = 20f;
                            blinkController.blinkDuration = 0.2f;
                        }
                    }

                    if (audioVibrationManager != null)
                    {
                        if (audioVibrationManager.mergeSound != null)
                        {
                            audioVibrationManager.PlaySFX(audioVibrationManager.mergeSound);
                        }
                        audioVibrationManager.Vibrate();
                    }

                    if (scoreManager != null)
                    {
                        scoreManager.AddScore(currentLevel * 2);
                    }
                }
            }

            Destroy(gameObject);
            Destroy(collision.gameObject);
            isMerging = false;
        }
    }

    public void ResetCollisionState()
    {
        hasCollided = false;
        isMerging = false;
    }
}