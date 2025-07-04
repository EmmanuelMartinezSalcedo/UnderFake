using System.Collections;
using Alteruna;
using UnityEngine;

public class ArrowSpawner : MonoBehaviour
{
    [Header("Configuración")]
    public int arrowCount = 3;
    public GameObject arrowPrefab;
    public GameObject alertPrefab;
    public float spawnInterval = 30f;
    public Vector2 spawnAreaMin = new Vector2(-8, -4);
    public Vector2 spawnAreaMax = new Vector2(8, 4);
    private Spawner _spawner;
    private Transform playerTransform;

    private void Awake()
    {
        Debug.Log("AYUDA1");
        _spawner = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<Spawner>();
    }
    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(spawnInterval);

        while (true)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Heart")?.transform;
            if (playerTransform == null)
            {
                Debug.LogWarning("ArrowSpawner: No se encontró jugador con tag 'Heart'.");
                yield return new WaitForSeconds(spawnInterval);
                continue;
            }

            // 1. Generar posiciones y alertas
            Vector2[] spawnPositions = new Vector2[arrowCount];
            AlertBlink[] alertBlinks = new AlertBlink[arrowCount];
            bool[] blinkDone = new bool[arrowCount];

            for (int i = 0; i < arrowCount; i++)
            {
                spawnPositions[i] = new Vector2(
                    Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                    Random.Range(spawnAreaMin.y, spawnAreaMax.y)
                );
                Debug.LogWarning($"ArrowSpawner: Flecha {i + 1} se va a spawnear en {spawnPositions[i]}");

                GameObject alert = _spawner.Spawn(2, spawnPositions[i], Quaternion.identity, new Vector3(1f, 1f, 1f));
                alertBlinks[i] = alert.GetComponent<AlertBlink>();
                int idx = i; // Captura de índice para el closure
                alertBlinks[i].OnBlinkComplete.AddListener(() => blinkDone[idx] = true);
            }

            // 2. Esperar a que terminen todos los blinks
            yield return new WaitUntil(() => {
                for (int i = 0; i < arrowCount; i++)
                    if (!blinkDone[i]) return false;
                return true;
            });

            // 3. Spawnear todas las flechas
            for (int i = 0; i < arrowCount; i++)
            {
                GameObject arrowObj = _spawner.Spawn(1, spawnPositions[i], Quaternion.identity, new Vector3(1f, 1f, 1f));
                ArrowEnemy arrow = arrowObj.GetComponent<ArrowEnemy>();
                arrow.Initialize(playerTransform);
                arrow.Shoot();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

}
