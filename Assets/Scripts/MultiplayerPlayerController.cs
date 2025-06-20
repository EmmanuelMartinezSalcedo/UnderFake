using UnityEngine;
using System.Collections;
using Alteruna;

public class MultiplayerPlayerController : CommunicationBridge
{
    public bool isInvincible;
    
    [Header("Movimiento")]
    public float speed = 1f;

    [Header("Tamaño y salud")]
    public float size = 1f;
    public int health = 100;

    [Header("Feedback")]
    public float blinkDuration = 0.1f;
    public int blinkCount = 5;

    [Header("Background")]
    public SpriteRenderer background;

    private Vector2 targetPosition;
    private SpriteRenderer spriteRenderer;
    private bool isBlinking = false;

    private Alteruna.Avatar _avatar;

    private IEnumerator Start()
    {
        yield return null;

        _avatar = GetComponentInParent<Alteruna.Avatar>();

        int attempts = 0;
        while (_avatar == null && attempts < 3)
        {
            attempts++;
            yield return null;
            _avatar = GetComponentInParent<Alteruna.Avatar>();
        }

        if (_avatar == null)
        {
            Debug.LogError($"No se encontró Alteruna.Avatar en los padres de {name}. Jerarquía actual:", gameObject);
            LogFullHierarchy();
            yield break;
        }

        if (!_avatar.IsMe) yield break;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) Debug.LogWarning("No SpriteRenderer found on Player!");
        if (background == null) Debug.LogWarning("No Background found for Player!");
    }

    private void LogFullHierarchy()
    {
        Transform current = transform;
        string hierarchyPath = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            hierarchyPath = $"{current.name}/{hierarchyPath}";
        }
        Debug.Log($"Jerarquía completa: {hierarchyPath}");
    }

    public void SetTargetPosition(Vector2 worldPosition)
    {
        targetPosition = worldPosition;
    }

    void Update()
    {
        if (!_avatar.IsMe)
        {
            return;
        }
        Vector2 currentPos = transform.position;
        Vector2 newPos = Vector2.Lerp(currentPos, targetPosition, speed * Time.deltaTime);
        transform.position = newPos;

        if (Vector2.Distance(newPos, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Destroy(collision.gameObject);
            if (!isInvincible)
            {
                health -= 10;
                StartCoroutine(BlinkEffect());
            }
            
        }
    }

    private IEnumerator BlinkEffect()
    {
        if (isBlinking || spriteRenderer == null) yield break;

        isBlinking = true;
        for (int i = 0; i < blinkCount; i++)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(blinkDuration);
            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(blinkDuration);
        }
        isBlinking = false;
    }

    public override void Possessed(bool isPossessor, User user)
    {
        enabled = isPossessor;
    }
}
