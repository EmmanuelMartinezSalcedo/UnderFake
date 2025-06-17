using UnityEngine;

public class AttackContext
{
    public Transform playerTransform;
    public Transform attackPoint;

    public AttackContext(Transform player, Transform point)
    {
        playerTransform = player;
        attackPoint = point;
    }
}
