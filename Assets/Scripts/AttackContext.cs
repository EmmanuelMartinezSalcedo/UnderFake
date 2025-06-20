using UnityEngine;
using Alteruna;

public class AttackContext
{
    public Transform playerTransform;
    public Transform attackPoint;
    public Alteruna.Spawner spawner;

    public AttackContext(Transform player, Transform point, Alteruna.Spawner _spawner)
    {
        playerTransform = player;
        attackPoint = point;
        spawner = _spawner;
    }
}