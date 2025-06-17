using UnityEngine;

public class InstantiatePlayer : MonoBehaviour
{
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private GameObject handsPrefab;

    private static int playerCount = 0;

    void Start()
    {
        GameObject prefabToSpawn;

        if (playerCount == 0)
        {
            prefabToSpawn = heartPrefab;
        }
        else if (playerCount == 1)
        {
            prefabToSpawn = handsPrefab;
        }
        else
        {
            Debug.LogWarning("Más de 2 jugadores no están soportados.");
            return;
        }

        Instantiate(prefabToSpawn, transform.position, transform.rotation);
        playerCount++;
    }
}
