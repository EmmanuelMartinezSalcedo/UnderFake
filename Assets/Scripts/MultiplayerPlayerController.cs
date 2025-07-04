using UnityEngine;
using System.Collections;
using Alteruna;
using TMPro;
using UnityEngine.SceneManagement;

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

    // NUEVAS VARIABLES
    private float playTime = 0f;
    private bool isCountingTime = false;
    private bool hasDied = false;

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

        _collider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Si no es el jugador principal (corazón), desactivar vida
        if (!_avatar.IsMe)
        {
            if (healthText != null)
            {
                healthText.gameObject.SetActive(false);
            }

            health = 0;
        }
        else
        {
            isCountingTime = true;
        }
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
        if (isBlinking)
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
                _collider.enabled = true;
                spriteRenderer.enabled = true;
            }
        }

        if (!_avatar.IsMe)
        {
            return;
        }

        // Aumentar tiempo jugado
        if (isCountingTime && !hasDied)
        {
            playTime += Time.deltaTime;
        }

        Vector2 currentPos = transform.position;
        Vector2 newPos = Vector2.Lerp(currentPos, targetPosition, speed * Time.deltaTime);
        transform.position = newPos;

        if (Vector2.Distance(newPos, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
        }

        // Mostrar vida si no ha muerto
        if (healthText != null && !hasDied)
        {
            healthText.text = "HP: " + health.ToString();
        }

        // Si la vida llegó a 0 y aún no se ha procesado la muerte
        if (health <= 0 && !hasDied)
        {
            hasDied = true;
            isCountingTime = false;

            if (healthText != null)
            {
                healthText.text = "Tiempo jugado: " + playTime.ToString("F2") + " segundos";
            }

            StartCoroutine(WaitAndLoadMenu(5f));
        }

        if (health != _oldHealth)
        {
            _oldHealth = health;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_avatar.IsMe)
            return;

        if (collision.CompareTag("Bullet"))
        {
            Destroy(collision.gameObject);
            if (!isInvincible)
            {
                health -= 5;
                BlinkEffect();
                return;
            }
            TempDisableBarrier(5f);
        }
    }

    public void BlinkEffect()
    {
        Debug.LogWarning("Blink activado");
        isBlinking = true;
        blinkTimer = 0f;
        blinkStep = 0;
        blinkVisible = false;
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    public void TempDisableBarrier(float seconds)
    {
        Debug.LogWarning("Desactivando barrera");
        barrierDisabled = true;
        barrierTimer = 0f;
        barrierDuration = seconds;
        if (_collider != null) _collider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
    }

    private IEnumerator WaitAndLoadMenu(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        SceneManager.LoadScene("MainMenu");
    }
}
