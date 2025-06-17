using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnArrowsController : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject arrowPrefab;
    public GameObject alertPrefab;

    [Header("Arrow Settings")]
    public int arrowCount = 3;
    public float arrowSpeed = 5f;
    public int alertBlinkCount = 3;
    public float alertBlinkDuration = 0.2f;

    [Header("References")]
    public Transform playerTransform;
    public SpriteRenderer backgroundSprite;

    private float spawnDistance = 1f;
    private float alertOffset = 0.2f;

    private List<ArrowEnemy> arrows = new List<ArrowEnemy>();
    private List<AlertBlink> alerts = new List<AlertBlink>();

    void Start()
    {
        StartCoroutine(InitializeAfterDelay(2f));
    }

    IEnumerator InitializeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (backgroundSprite == null)
        {
            Debug.LogError("El SpriteRenderer del fondo no está asignado.");
            yield break;
        }

        Vector2 baseSize = backgroundSprite.sprite.bounds.size;
        Vector3 scale = backgroundSprite.transform.localScale;
        Vector2 realSize = new Vector2(baseSize.x * scale.x, baseSize.y * scale.y);

        spawnDistance = Mathf.Max(realSize.x, realSize.y) * 0.5f;
        alertOffset = Mathf.Min(realSize.x, realSize.y) * 0.05f;

        SpawnArrowsAndAlerts();
    }

    void SpawnArrowsAndAlerts()
    {
        for (int i = 0; i < arrowCount; i++)
        {
            int spawnEdge;
            Vector3 spawnPos = GetRandomSpawnPositionOutsideBackground(out spawnEdge);

            GameObject arrowObj = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
            ArrowEnemy arrowEnemy = arrowObj.GetComponent<ArrowEnemy>();
            arrowEnemy.speed = arrowSpeed;
            arrowEnemy.Initialize(playerTransform);
            arrows.Add(arrowEnemy);

            Vector3 alertPos = GetAlertPositionForEdge(spawnPos, spawnEdge);
            GameObject alertObj = Instantiate(alertPrefab, alertPos, Quaternion.identity);

            AlertBlink alertBlink = alertObj.GetComponent<AlertBlink>();
            alertBlink.blinkCount = alertBlinkCount;
            alertBlink.blinkDuration = alertBlinkDuration;
            alertBlink.OnBlinkComplete.AddListener(() => OnAlertComplete(arrowEnemy));
            alerts.Add(alertBlink);
        }
    }

    Vector3 GetRandomSpawnPositionOutsideBackground(out int edge)
    {
        Vector3 bgCenter = backgroundSprite.transform.position;

        Vector2 baseSize = backgroundSprite.sprite.bounds.size;
        Vector3 scale = backgroundSprite.transform.localScale;
        Vector2 size = new Vector2(baseSize.x * scale.x, baseSize.y * scale.y);

        float left = bgCenter.x - size.x / 2f;
        float right = bgCenter.x + size.x / 2f;
        float top = bgCenter.y + size.y / 2f;
        float bottom = bgCenter.y - size.y / 2f;

        edge = Random.Range(0, 4);
        Vector3 pos = Vector3.zero;

        switch (edge)
        {
            case 0:
                pos = new Vector3(Random.Range(left, right), top + spawnDistance, 0);
                break;
            case 1:
                pos = new Vector3(right + spawnDistance, Random.Range(bottom, top), 0);
                break;
            case 2:
                pos = new Vector3(Random.Range(left, right), bottom - spawnDistance, 0);
                break;
            case 3:
                pos = new Vector3(left - spawnDistance, Random.Range(bottom, top), 0);
                break;
        }

        return pos;
    }

    Vector3 GetAlertPositionForEdge(Vector3 spawnPos, int spawnEdge)
    {
        Vector3 bgCenter = backgroundSprite.transform.position;

        Vector2 baseSize = backgroundSprite.sprite.bounds.size;
        Vector3 scale = backgroundSprite.transform.localScale;
        Vector2 size = new Vector2(baseSize.x * scale.x, baseSize.y * scale.y);

        float left = bgCenter.x - size.x / 2f;
        float right = bgCenter.x + size.x / 2f;
        float top = bgCenter.y + size.y / 2f;
        float bottom = bgCenter.y - size.y / 2f;

        Vector3 alertPos = Vector3.zero;

        switch (spawnEdge)
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

    void OnAlertComplete(ArrowEnemy arrow)
    {
        arrow.Shoot();
    }
}
