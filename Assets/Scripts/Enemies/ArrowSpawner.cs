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

            for (int i = 0; i < arrowCount; i++)
            {
                Vector2 spawnPos = new Vector2(
                    Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                    Random.Range(spawnAreaMin.y, spawnAreaMax.y)
                );

                // Instancia la alerta y espera a que termine el blink
                GameObject alert = _spawner.Spawn(2, spawnPos, Quaternion.identity, new Vector3(1f, 1f, 1f));
                AlertBlink alertBlink = alert.GetComponent<AlertBlink>();
                bool blinkDone = false;
                alertBlink.OnBlinkComplete.AddListener(() => blinkDone = true);

                yield return new WaitUntil(() => blinkDone);

                // Instancia y dispara la flecha
                GameObject arrowObj = _spawner.Spawn(1, spawnPos, Quaternion.identity, new Vector3(1f, 1f, 1f));
                ArrowEnemy arrow = arrowObj.GetComponent<ArrowEnemy>();
                arrow.Initialize(playerTransform);
                arrow.Shoot();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
