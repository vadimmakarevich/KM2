using UnityEngine;
using System.Collections.Generic;

public class FloorCollision : MonoBehaviour
{
    private GameModeManager gameModeManager;
    private float floorY;
    private Dictionary<int, float> timeAtOrAboveFloor = new Dictionary<int, float>();
    private const float timeThreshold = 3f;

    void Start()
    {
        gameModeManager = FindObjectOfType<GameModeManager>();
        floorY = transform.position.y;
    }

    void Update()
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag("GameObject");

        foreach (GameObject obj in objects)
        {
            int objectId = obj.GetInstanceID();
            Collider2D collider = obj.GetComponent<Collider2D>();
            if (collider == null)
            {
                continue;
            }

            // collider.bounds.min.y already gives the world position of the bottom
            // of the collider, so adding obj.transform.position.y results in an
            // incorrect value. Use bounds.min.y directly to check against the
            // floor position.
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
                        return;
                    }
                }
            }
            else
            {
                if (timeAtOrAboveFloor.ContainsKey(objectId))
                {
                    timeAtOrAboveFloor.Remove(objectId);
                }
            }
        }
    }
}
