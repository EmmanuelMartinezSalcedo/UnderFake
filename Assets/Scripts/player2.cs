using UnityEngine;

public class player2 : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY = 1f;
        if (Input.GetKey(KeyCode.S)) moveY = -1f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;

        Vector3 moveDir = new Vector3(moveX, moveY, 0).normalized;

        if (transform.parent != null)
        {
            transform.parent.Translate(moveDir * moveSpeed * Time.deltaTime);
        }
    }
}
