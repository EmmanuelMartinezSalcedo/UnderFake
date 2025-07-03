using UnityEngine;
using System.Collections;
using Alteruna;
using TMPro;

public class MultiplayerPlayerController : Synchronizable
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

    private Alteruna.Avatar _avatar;
    private Collider2D _collider;

    private int _oldHealth;

    private bool isBlinking = false;
    private float blinkTimer = 0f;
    private int blinkStep = 0;
    private bool blinkVisible = true;

    private bool barrierDisabled = false;
    private float barrierTimer = 0f;
    private float barrierDuration = 0f;

    public override void DisassembleData(Reader reader, byte LOD)
    {
        health = reader.ReadInt();
        _oldHealth = health;
    }

    public override void AssembleData(Writer writer, byte LOD)
    {
        writer.Write(health);
    }

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

        if (health != _oldHealth)
        {
            _oldHealth = health;
            Commit();
        }

        if (isBlinking && spriteRenderer != null)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkDuration)
            {
                blinkTimer = 0f;
                blinkVisible = !blinkVisible;
                spriteRenderer.enabled = blinkVisible;
                blinkStep++;
                if (blinkStep >= blinkCount * 2)
                {
                    isBlinking = false;
                    spriteRenderer.enabled = true;
                }
            }
        }

        if (barrierDisabled)
        {
            barrierTimer += Time.deltaTime;
            if (barrierTimer >= barrierDuration)
            {
                barrierDisabled = false;
                if (_collider != null) _collider.enabled = true;
                if (spriteRenderer != null) spriteRenderer.enabled = true;
            }
        }

        base.SyncUpdate();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Destroy(collision.gameObject);
            if (!isInvincible)
            {
                health -= 10;
                RpcBlinkEffect();
                return;
            }
            RpcTempDisableBarrier(5f);
        }
    }

    [SynchronizableMethod]
    public void RpcBlinkEffect()
    {
        Debug.Log($"RpcBlinkEffect ejecutado en {gameObject.name}");
        isBlinking = true;
        blinkTimer = 0f;
        blinkStep = 0;
        blinkVisible = false;
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    [SynchronizableMethod]
    public void RpcTempDisableBarrier(float seconds)
    {
        Debug.Log($"RpcTempDisableBarrier ejecutado en {gameObject.name} por {seconds} segundos");
        barrierDisabled = true;
        barrierTimer = 0f;
        barrierDuration = seconds;
        if (_collider != null) _collider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
    }
}
