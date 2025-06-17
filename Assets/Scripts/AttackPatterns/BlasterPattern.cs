using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "BulletHell/Patterns/Blasters")]
public class BlasterPattern : AttackPattern
{
    public GameObject blasterPrefab;

    public int rows = 2;
    public int columns = 3;
    public float spacing = 2f;

    public override IEnumerator Execute(AttackContext context)
    {
        if (blasterPrefab == null || context.attackPoint == null)
        {
            Debug.LogError("Faltan referencias en RectangleBlasterPattern");
            yield break;
        }

        Vector3 origin = context.attackPoint.position;

        // Centrado del patrón respecto al punto de origen
        float offsetX = (columns - 1) * spacing / 2f;
        float offsetY = (rows - 1) * spacing / 2f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 spawnPos = origin + new Vector3(
                    col * spacing - offsetX,
                    row * spacing - offsetY,
                    0f
                );

                Quaternion rotation = context.attackPoint.rotation;
                GameObject.Instantiate(blasterPrefab, spawnPos, rotation);
            }
        }

        yield return new WaitForSeconds(duration);
    }
}
