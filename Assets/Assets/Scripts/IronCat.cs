using UnityEngine;

public class IronCat : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Iron Cat не участвует в слиянии
        return;
    }
}