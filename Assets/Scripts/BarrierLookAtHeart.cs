using UnityEngine;

public class BarrierLookAtHeart : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform del coraz�n; d�jalo vac�o y se buscar� por Tag 'Heart'")]
    public Transform heart;

    [Header("Configuraci�n")]
    [Tooltip("�Qu� lado debe mirar al Heart? true = lado derecho (transform.right) / false = izquierdo")]
    public bool useRightSide = true;

    void Awake()
    {
        if (heart == null)
        {
            GameObject h = GameObject.FindGameObjectWithTag("Heart");
            if (h != null)
                heart = h.transform;
            else
                Debug.LogWarning($"[{name}] No se encontr� objeto con tag 'Heart'.");
        }
    }

    void LateUpdate()
    {
        if (heart == null) return;

        Vector2 dir = (Vector2)(heart.position - transform.position);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (!useRightSide)
            angle += 180f;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}