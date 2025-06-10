using UnityEngine;

public class ObjectLevel : MonoBehaviour
{
    public int level; // Уровень объекта

    private void Start()
    {
        if (level <= 0)
        {
            Debug.LogError("Укажите корректный уровень для этого объекта!");
        }
    }
}