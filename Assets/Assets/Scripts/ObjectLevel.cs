using UnityEngine;

public class ObjectLevel : MonoBehaviour
{
    public int level; // ������� �������

    private void Start()
    {
        if (level <= 0)
        {
            Debug.LogError("������� ���������� ������� ��� ����� �������!");
        }
    }
}