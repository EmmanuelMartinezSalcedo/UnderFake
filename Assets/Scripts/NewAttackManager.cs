using Alteruna;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PatternWithDuration
{
    public AttackPattern pattern;
    public float duration;
    public Transform spawnpoint;
}

public class NewAttackManager : MonoBehaviour
{
    [Header("Attack Patterns With Duration")]
    public List<PatternWithDuration> patterns;

    private Spawner _spawner;

    public Transform playerTransform;
    private void Awake()
    {
        Debug.Log("AYUDA1");
        _spawner = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<Spawner>();
        
    }

    private IEnumerator Start()
    {
        Debug.Log("AYUDA - esperando al Player");

        while (GameObject.FindGameObjectWithTag("Player") == null)
        {
            yield return null;
        }

        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        Debug.Log("Player encontrado: " + playerTransform.name);

        StartCoroutine(ExecutePatternsLoop());
    }

    private IEnumerator ExecutePatternsLoop()
    {
        while (true)
        {
            foreach (PatternWithDuration pattern in patterns)
            {
                Debug.Log("Starting pattern: " + pattern.pattern.patternType);
                Transform chosenPoint = playerTransform;

                if (pattern.pattern.patternType == "Circle" && pattern.spawnpoint)
                {
                    chosenPoint = pattern.spawnpoint;
                }

                AttackContext context = new AttackContext(playerTransform, chosenPoint, _spawner);
                yield return StartCoroutine(pattern.pattern.Execute(context));
                yield return new WaitForSeconds(pattern.duration);
            }
        }
    }
}