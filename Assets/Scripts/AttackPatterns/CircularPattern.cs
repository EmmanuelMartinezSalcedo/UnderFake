using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "BulletHell/Patterns/CircularPattern")]
public class CircularPattern : AttackPattern
{
    public GameObject bulletPrefab;
    public int bulletCount = 12;
    public float bulletSpeed = 3f;
    public float timeBetweenBursts = 0.2f;     // Tiempo entre cada r�faga
    public float rotationPerBurst = 10f;       // Cu�nto rota el patr�n por r�faga

    private void OnEnable()
    {
        patternType = "Circle";
    }

    public override IEnumerator Execute(AttackContext context)
    {
        Vector3 spawnPos = context.attackPoint != null ? context.attackPoint.position : Vector3.zero;
        float elapsed = 0f;
        float angleOffset = 0f;

        while (elapsed < duration)
        {
            float angleStep = 360f / bulletCount;

            for (int i = 0; i < bulletCount; i++)
            {
                float angle = angleOffset + i * angleStep;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                GameObject bullet = context.spawner.Spawn(0, spawnPos, dir, new Vector3(0.5f, 0.5f, 0.5f));
                bullet.GetComponent<Rigidbody2D>().linearVelocity = dir * bulletSpeed;
            }

            // Esperar entre r�fagas
            yield return new WaitForSeconds(timeBetweenBursts);
            elapsed += timeBetweenBursts;

            // Rotar el �ngulo para la pr�xima r�faga
            angleOffset += rotationPerBurst;
        }
    }
}
