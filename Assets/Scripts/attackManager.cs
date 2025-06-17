using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BulletHellManager : MonoBehaviour
{
    [Header("Attack Patterns")]
    public List<AttackPattern> patterns;

    [Header("References")]
    public Transform playerTransform;
    public List<Transform> circleAttackPoints;

    [Header("Blaster Settings")]
    public List<Transform> blasterAttackPoints;

    [Header("Timing")]
    public float patternDelay = 2f;

    private void Start()
    {
        StartCoroutine(ExecutePatternsLoop());
    }

    private IEnumerator ExecutePatternsLoop()
    {
        while (true)
        {
            foreach (AttackPattern pattern in patterns)
            {
                Debug.Log("Starting pattern: " + pattern.patternType);

                Transform chosenPoint = playerTransform;

                if (pattern.patternType == "Circle" && circleAttackPoints.Count > 0)
                {
                    chosenPoint = circleAttackPoints[Random.Range(0, circleAttackPoints.Count)];
                }
                else if (pattern.patternType == "Arrow")
                {
                    chosenPoint = playerTransform;
                }
                else if (pattern.patternType == "Blaster")
                {
                    if (blasterAttackPoints.Count > 0)
                    {
                        chosenPoint = blasterAttackPoints[Random.Range(0, blasterAttackPoints.Count)];
                    }
                    else
                    {
                        Debug.LogWarning("No blaster attack points assigned!");
                        chosenPoint = transform;
                    }
                }

                AttackContext context = new AttackContext(playerTransform, chosenPoint);
                yield return StartCoroutine(pattern.Execute(context));

                yield return new WaitForSeconds(patternDelay);
            }
        }
    }
}
