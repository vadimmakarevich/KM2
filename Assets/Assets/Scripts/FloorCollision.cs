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

            float objectBottomY = obj.transform.position.y + collider.bounds.min.y;

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