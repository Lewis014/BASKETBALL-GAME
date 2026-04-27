using UnityEngine;

/// <summary>
/// Singleton que gestiona puntuación y power-ups.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private int _score;
    private int _nextPointMultiplier = 1;

    // Estilo reutilizable para OnGUI
    private GUIStyle _guiStyle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnGUI()
    {
        if (_guiStyle == null)
        {
            _guiStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        // Sombra
        GUI.color = Color.black;
        GUI.Label(new Rect(22, 22, 300, 60), $"Puntos: {_score}", _guiStyle);

        // Texto principal
        GUI.color = Color.white;
        GUI.Label(new Rect(20, 20, 300, 60), $"Puntos: {_score}", _guiStyle);

        if (_nextPointMultiplier > 1)
        {
            GUI.color = new Color(1f, 0.8f, 0f);
            GUI.Label(new Rect(20, 60, 300, 40), "x2 ACTIVO!", _guiStyle);
        }
    }

    /// <summary>Suma puntos aplicando el multiplicador actual.</summary>
    public void AddScore(int basePoints)
    {
        int gained = basePoints * _nextPointMultiplier;
        _score += gained;
        _nextPointMultiplier = 1;
        Debug.Log($"[GameManager] +{gained} puntos | Total: {_score}");
    }

    /// <summary>Activa puntos dobles para el siguiente lanzamiento.</summary>
    public void ActivateDoublePoints()
    {
        _nextPointMultiplier = 2;
        Debug.Log("[GameManager] ¡Puntos dobles activados!");
    }

    public int GetScore() => _score;
}
