using System.Collections;
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

    private Transform playerTransform;

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

            Vector2 spawnPos = new Vector2(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y)
            );

            // Instancia la alerta y espera a que termine el blink
            GameObject alert = Instantiate(alertPrefab, spawnPos, Quaternion.identity);
            AlertBlink alertBlink = alert.GetComponent<AlertBlink>();
            bool blinkDone = false;
            alertBlink.OnBlinkComplete.AddListener(() => blinkDone = true);

            // Espera a que termine el blink
            yield return new WaitUntil(() => blinkDone);

            // Instancia y dispara las flechas
            for (int i = 0; i < arrowCount; i++)
            {
                GameObject arrowObj = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
                ArrowEnemy arrow = arrowObj.GetComponent<ArrowEnemy>();
                arrow.Initialize(playerTransform);
                arrow.Shoot();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}