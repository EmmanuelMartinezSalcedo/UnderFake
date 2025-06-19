using UnityEngine;
using Alteruna;
using System.Collections;
using System.Collections.Generic;

public class AttackSpawner : MonoBehaviour
{
    private Alteruna.Avatar _avatar;
    private Spawner _spawner;

    [SerializeField] private int indexToSpawn = 0;
    [SerializeField] private float delayBetweenShots = 1f;
    

    [Header("Attack Patterns")]
    public List<AttackPattern> patterns;

    [Header("Circle Pattern origin")]
    public List<Transform> circleAttackPoints;

    public Transform playerTransform;

    private bool canShoot = true;

    private void Awake()
    {
        _avatar = GetComponent<Alteruna.Avatar>();
        _spawner = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<Spawner>();
    }
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


                AttackContext context = new AttackContext(playerTransform, chosenPoint, _spawner);
                yield return StartCoroutine(pattern.Execute(context));
            }
        }
    }
}
