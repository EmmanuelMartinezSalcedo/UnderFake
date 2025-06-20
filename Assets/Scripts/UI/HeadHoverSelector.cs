using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HeadHoverSelector : MonoBehaviour
{
    public float requiredHoverTime = 2.0f;
    private float hoverTimer = 0f;

    [Header("Cursor")]
    public GameObject cursor;

    [Header("Botón")]
    public Button targetButton;

    [Header("Colores")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.red;
    public float colorLerpSpeed = 5f;

    private Color currentColor;
    private bool isHovering = false;

    void Start()
    {
        if (targetButton != null)
        {
            currentColor = normalColor;
            ApplyColorToButton(currentColor);
        }
    }

    void Update()
    {
        if (cursor == null || targetButton == null) return;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, cursor.transform.position);
        RectTransform rt = GetComponent<RectTransform>();

        bool inside = RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, null);

        if (inside)
        {
            hoverTimer += Time.deltaTime;
            isHovering = true;

            if (hoverTimer > requiredHoverTime)
            {
                onHoverSelect();
                hoverTimer = 0f;
            }
        }
        else
        {
            hoverTimer = 0f;
            isHovering = false;
        }

        // Transición de color suave
        Color targetColor = isHovering ? hoverColor : normalColor;
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorLerpSpeed);
        ApplyColorToButton(currentColor);
    }

    void ApplyColorToButton(Color color)
    {
        var cb = targetButton.colors;
        cb.normalColor = color;
        targetButton.colors = cb;
    }

    void onHoverSelect()
    {
        switch (gameObject.name)
        {
            case "Start":
                SceneManager.LoadScene("MultiplayerLobby");
                break;
            case "ExitButton":
                Application.Quit();
                break;
        }
    }
}
