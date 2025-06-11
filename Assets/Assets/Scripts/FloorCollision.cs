using UnityEngine;
using System.Collections.Generic;

public class FloorCollision : MonoBehaviour
{
    private GameModeManager gameModeManager;
    private float floorY;
    private readonly Dictionary<int, float> timeAtOrAboveFloor = new Dictionary<int, float>();
    private readonly HashSet<GameObject> trackedObjects = new HashSet<GameObject>();
    private const float timeThreshold = 3f;

    void Start()
    {
        gameModeManager = FindObjectOfType<GameModeManager>();
        floorY = transform.position.y;
    }

    void Update()
    {
        // Iterate over a copy to allow modifications during the loop
        foreach (GameObject obj in trackedObjects.ToArray())
        {
            if (obj == null)
            {
                trackedObjects.Remove(obj);
                continue;
            }

            int objectId = obj.GetInstanceID();
            Collider2D collider = obj.GetComponent<Collider2D>();
            if (collider == null)
            {
                continue;
            }

            // collider.bounds.min.y already gives the world position of the bottom
            // of the collider.
            float objectBottomY = collider.bounds.min.y;

            if (objectBottomY >= floorY)
            {
                if (!timeAtOrAboveFloor.ContainsKey(objectId))
                {
                    timeAtOrAboveFloor[objectId] = Time.time;
                }
                else
                {
                    float timeElapsed = Time.time - timeAtOrAboveFloor[objectId];
                    if (timeElapsed >= timeThreshold)
                    {
                        if (gameModeManager != null)
                        {
                            gameModeManager.GameOver("FloorCollision");
                        }
                        timeAtOrAboveFloor.Clear();
                        trackedObjects.Clear();
                        return;
                    }
                }
            }
            else if (timeAtOrAboveFloor.ContainsKey(objectId))
            {
                timeAtOrAboveFloor.Remove(objectId);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("GameObject"))
        {
            trackedObjects.Add(collision.gameObject);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("GameObject"))
        {
            trackedObjects.Remove(collision.gameObject);
            timeAtOrAboveFloor.Remove(collision.gameObject.GetInstanceID());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("GameObject"))
        {
            trackedObjects.Add(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("GameObject"))
        {
            trackedObjects.Remove(other.gameObject);
            timeAtOrAboveFloor.Remove(other.gameObject.GetInstanceID());
        }
    }
}
