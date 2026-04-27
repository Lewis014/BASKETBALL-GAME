using UnityEngine;

/// <summary>
/// Singleton que gestiona puntuación, power-ups y feedback visual.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private int _score;
    private int _nextPointMultiplier = 1;

    // ── Popup de puntos ─────────────────────────────────────────────
    private string _popupText   = "";
    private float  _popupTimer  = 0f;
    private const float PopupDuration = 2f;

    // ── Estilos GUI ─────────────────────────────────────────────────
    private GUIStyle _scoreStyle;
    private GUIStyle _popupStyle;
    private GUIStyle _multiplierStyle;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (_popupTimer > 0f)
            _popupTimer -= Time.deltaTime;
    }

    private void OnGUI()
    {
        InitStyles();

        // ── Marcador principal ───────────────────────────────────────
        DrawShadowLabel(new Rect(20, 20, 300, 60), $"🏀 Puntos: {_score}", _scoreStyle);

        // ── Indicador power-up ───────────────────────────────────────
        if (_nextPointMultiplier > 1)
            DrawShadowLabel(new Rect(20, 70, 300, 40), "⚡ x2 ACTIVO!", _multiplierStyle);

        // ── Popup de canasta ─────────────────────────────────────────
        if (_popupTimer > 0f)
        {
            float alpha = Mathf.Clamp01(_popupTimer / PopupDuration);
            Color c = _popupStyle.normal.textColor;
            c.a = alpha;
            _popupStyle.normal.textColor = c;

            float yOffset = (1f - alpha) * 60f; // sube mientras desaparece
            DrawShadowLabel(new Rect(0, 180 - yOffset, Screen.width, 80),
                            _popupText, _popupStyle);
        }
    }

    // ── API pública ──────────────────────────────────────────────────

    public void AddScore(int basePoints)
    {
        int gained = basePoints * _nextPointMultiplier;
        _score += gained;

        // Mostrar popup
        string bonus = _nextPointMultiplier > 1 ? " x2 ⚡" : "";
        ShowPopup($"+{gained}{bonus}");

        _nextPointMultiplier = 1;
        Debug.Log($"[GameManager] +{gained} pts | Total: {_score}");
    }

    public void ActivateDoublePoints()
    {
        _nextPointMultiplier = 2;
        Debug.Log("[GameManager] ¡Puntos dobles activados!");
    }

    public int GetScore() => _score;

    // ── Internos ──────────────────────────────────────────────────────

    private void ShowPopup(string text)
    {
        _popupText  = text;
        _popupTimer = PopupDuration;
    }

    private void InitStyles()
    {
        if (_scoreStyle != null) return;

        _scoreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal    = { textColor = Color.white }
        };

        _popupStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 56,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(1f, 0.85f, 0f, 1f) }
        };

        _multiplierStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal    = { textColor = new Color(1f, 0.75f, 0f) }
        };
    }

    private void DrawShadowLabel(Rect rect, string text, GUIStyle style)
    {
        Color original = style.normal.textColor;
        style.normal.textColor = new Color(0, 0, 0, original.a * 0.6f);
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);
        style.normal.textColor = original;
        GUI.Label(rect, text, style);
    }
}
