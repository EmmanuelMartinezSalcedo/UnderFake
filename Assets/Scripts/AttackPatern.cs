using UnityEngine;
using System.Collections;

public abstract class AttackPattern : ScriptableObject
{
    public string patternType;
    public float duration = 3f;

    public abstract IEnumerator Execute(AttackContext context);
}
