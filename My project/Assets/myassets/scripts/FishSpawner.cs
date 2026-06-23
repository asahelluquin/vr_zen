using UnityEngine;
using FishAlive;

public class FishSpawner : MonoBehaviour
{
    [Header("Prefab del pez")]
    public GameObject fishPrefab;

    [Header("Configuracion")]
    public int fishCount = 30;
    public float areaX = 2.5f;
    public float areaZ = 1.2f;
    public float waterDepth = -0.2f;
    public float depthRange = 0.15f;

    [Header("Referencias")]
    public GameObject targetParent;

    void Start()
    {
        SpawnFish();
    }

    void SpawnFish()
    {
        if (!fishPrefab)
        {
            Debug.LogError("Asigna el prefab del pez en FishSpawner");
            return;
        }

        if (!targetParent)
        {
            targetParent = new GameObject("Fish_Targets");
        }

        for (int i = 0; i < fishCount; i++)
        {
            Vector3 targetPos = GetRandomPosition();
            Vector3 fishPos = targetPos + Vector3.up * 0.1f;

            GameObject target = new GameObject($"Target_Pez_{i + 1}");
            target.transform.position = targetPos;
            target.transform.parent = targetParent.transform;

            GameObject fish = Instantiate(fishPrefab, fishPos,
                Quaternion.Euler(0, Random.Range(0, 360), 0));
            fish.name = $"Pez_{i + 1}";

            FishMotion fishMotion = fish.GetComponent<FishMotion>();
            if (fishMotion != null)
            {
                fishMotion.target = target;
            }
        }
    }

    Vector3 GetRandomPosition()
    {
        Vector3 localPos = new Vector3(
            Random.Range(-areaX, areaX),
            Random.Range(waterDepth - depthRange, waterDepth + depthRange),
            Random.Range(-areaZ, areaZ)
        );

        return transform.position + transform.rotation * localPos;
    }

    void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(
            new Vector3(transform.position.x, transform.position.y + waterDepth, transform.position.z),
            transform.rotation,
            Vector3.one
        );

        Gizmos.color = new Color(0, 1, 1, 0.2f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(areaX * 2, depthRange * 2, areaZ * 2));

        Gizmos.color = new Color(0, 1, 1, 0.8f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaX * 2, depthRange * 2, areaZ * 2));
    }
}