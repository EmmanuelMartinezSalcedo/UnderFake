using UnityEngine;

public class player1 : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float moveX = 0f;
        float moveY = 0f;

        // Usar teclas 1 (abajo), 2 (izquierda), 3 (derecha), 4 (arriba)
        if (Input.GetKey(KeyCode.Alpha1)) moveY = -1f; // abajo
        if (Input.GetKey(KeyCode.Alpha2)) moveX = -1f; // izquierda
        if (Input.GetKey(KeyCode.Alpha3)) moveX = 1f;  // derecha
        if (Input.GetKey(KeyCode.Alpha4)) moveY = 1f;  // arriba

        Vector3 moveDir = new Vector3(moveX, moveY, 0f).normalized;

        if (transform.parent != null)
        {
            transform.parent.Translate(moveDir * moveSpeed * Time.deltaTime);
        }
    }
}
