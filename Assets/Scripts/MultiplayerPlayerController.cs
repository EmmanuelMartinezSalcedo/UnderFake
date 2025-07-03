using UnityEngine;
using System.Collections;
using Alteruna;
using TMPro;

public class MultiplayerPlayerController : CommunicationBridge
{
    public bool isInvincible;
    
    [Header("Movimiento")]
    public float speed = 1f;

    [Header("Tamaño y salud")]
    public float size = 1f;
    public int health = 100;
    public TextMeshProUGUI healthText;

    [Header("Feedback")]
    public float blinkDuration = 0.1f;
    public int blinkCount = 5;

    [Header("Background")]
    public SpriteRenderer background;

    private Vector2 targetPosition;
    private SpriteRenderer spriteRenderer;
    private bool isBlinking = false;

    private Alteruna.Avatar _avatar;
    private Collider2D _collider;

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

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
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

        if (healthText != null)
            healthText.text = "HP: " + health.ToString();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_avatar.IsMe) return;
        if (collision.CompareTag("Bullet"))
        {
            Destroy(collision.gameObject);
            if (!isInvincible)
            {
                health -= 10;
                StartCoroutine(BlinkEffect());
                return;
            }
            StartCoroutine(TempDisableBarrier(5f));
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

    bool _disabledTemp = false;
    IEnumerator TempDisableBarrier(float seconds)
    {
        if (_disabledTemp) yield break;
        _disabledTemp = true;

        if (_collider != null) _collider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (background != null) background.enabled = false;

        yield return new WaitForSeconds(seconds);

        if (_collider != null) _collider.enabled = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (background != null) background.enabled = true;

        _disabledTemp = false;
    }
}
