using UnityEngine;

public class SpawnDelay : MonoBehaviour
{
    public float delayTime = 0.2f; // �������� ����� ��������� �������
    private PointerController pointerController;

    void Start()
    {
        // ������� ������ �� PointerController
        pointerController = FindObjectOfType<PointerController>();

        // ���� ������, ��������� �����
        if (pointerController != null)
        {
            pointerController.isSpawning = true;

            // ����� �������� ����� ���������� ����
            Destroy(this, delayTime);
        }
    }

    void OnDestroy()
    {
        // ���������� ���� ��� �������� ����������
        if (pointerController != null)
        {
            pointerController.isSpawning = false;
        }
    }
}