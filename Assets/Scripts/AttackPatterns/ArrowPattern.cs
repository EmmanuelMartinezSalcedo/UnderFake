using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "ArrowRainAttack", menuName = "BulletHell/Patterns/ArrowRain")]
public class ArrowRainAttack : AttackPattern
{
    [Header("Prefabs")]
    public GameObject arrowPrefab;
    public GameObject alertPrefab;

    [Header("Arrow Settings")]
    public int arrowCount = 3;
    public float arrowSpeed = 5f;
    public int alertBlinkCount = 3;
    public float alertBlinkDuration = 0.2f;

    private float spawnDistance = 1f;
    private float alertOffset = 0.5f;

    private void OnEnable()
    {
        patternType = "Arrow";
    }

    public override IEnumerator Execute(AttackContext context)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Main Camera not found.");
            yield break;
        }

        Vector3 camCenter = cam.transform.position;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        float left = camCenter.x - camWidth / 2f;
        float right = camCenter.x + camWidth / 2f;
        float top = camCenter.y + camHeight / 2f;
        float bottom = camCenter.y - camHeight / 2f;

        spawnDistance = Mathf.Max(camWidth, camHeight) * 0.5f;
        alertOffset = Mathf.Min(camWidth, camHeight) * 0.05f;

        for (int i = 0; i < arrowCount; i++)
        {
            int edge;
            Vector3 spawnPos = GetRandomSpawnPosition(out edge, left, right, top, bottom);
            Vector3 alertPos = GetAlertPositionForEdge(spawnPos, edge, left, right, top, bottom);

            GameObject alertObj = Instantiate(alertPrefab, alertPos, Quaternion.identity);
            AlertBlink alertBlink = alertObj.GetComponent<AlertBlink>();
            alertBlink.blinkCount = alertBlinkCount;
            alertBlink.blinkDuration = alertBlinkDuration;

            Vector3 capturedSpawnPos = spawnPos;

            alertBlink.OnBlinkComplete.AddListener(() =>
            {
                GameObject arrowObj = Instantiate(arrowPrefab, capturedSpawnPos, Quaternion.identity);
                ArrowEnemy arrow = arrowObj.GetComponent<ArrowEnemy>();
                arrow.speed = arrowSpeed;
                arrow.Initialize(context.playerTransform);

                Vector3 direction = (context.playerTransform.position - capturedSpawnPos);
                Debug.DrawLine(capturedSpawnPos, capturedSpawnPos + direction.normalized * 3f, Color.red, 2f);

                arrow.Shoot();
            });
        }

        yield return new WaitForSeconds(duration);
    }

    private Vector3 GetRandomSpawnPosition(out int edge, float left, float right, float top, float bottom)
    {
        edge = Random.Range(0, 4);
        switch (edge)
        {
            case 0: return new Vector3(Random.Range(left, right), top + spawnDistance, 0);      // Top
            case 1: return new Vector3(right + spawnDistance, Random.Range(bottom, top), 0);    // Right
            case 2: return new Vector3(Random.Range(left, right), bottom - spawnDistance, 0);   // Bottom
            case 3: return new Vector3(left - spawnDistance, Random.Range(bottom, top), 0);     // Left
            default: return Vector3.zero;
        }
    }

    private Vector3 GetAlertPositionForEdge(Vector3 spawnPos, int edge, float left, float right, float top, float bottom)
    {
        Vector3 alertPos = Vector3.zero;

        switch (edge)
        {
            case 0:
                alertPos.x = Mathf.Clamp(spawnPos.x, left + alertOffset, right - alertOffset);
                alertPos.y = top - alertOffset;
                break;
            case 1:
                alertPos.x = right - alertOffset;
                alertPos.y = Mathf.Clamp(spawnPos.y, bottom + alertOffset, top - alertOffset);
                break;
            case 2:
                alertPos.x = Mathf.Clamp(spawnPos.x, left + alertOffset, right - alertOffset);
                alertPos.y = bottom + alertOffset;
                break;
            case 3:
                alertPos.x = left + alertOffset;
                alertPos.y = Mathf.Clamp(spawnPos.y, bottom + alertOffset, top - alertOffset);
                break;
        }

        alertPos.z = 0;
        return alertPos;
    }
}
