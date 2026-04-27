using UnityEngine;

/// <summary>
/// Detecta canastas válidas con cooldown anti-doble conteo.
/// La pelota debe pasar primero por TriggerSuperior y luego por TriggerInferior.
/// </summary>
public class GoalDetector : MonoBehaviour
{
    [SerializeField] private AudioSource goalSound;

    [Tooltip("Segundos de espera antes de poder contar otra canasta")]
    [SerializeField] private float scoreCooldown = 1.5f;

    [Tooltip("Segundos que se recuerda que la pelota pasó por el trigger superior")]
    [SerializeField] private float passedTopWindow = 3f;

    private float _passedTopTime = -999f; // momento en que la pelota tocó el trigger superior
    private float _lastScoreTime = -999f;
    private int   _pendingPoints = 2;     // puntos del tiro en vuelo (2 o 3)

    /// <summary>Llamado por BasketballPlayer al disparar para registrar la zona del tiro.</summary>
    public void SetPendingPoints(int pts) => _pendingPoints = pts;

    private bool PassedTopRecently => (Time.time - _passedTopTime) < passedTopWindow;

    // ── Llamado por GoalTrigger ──────────────────────────────────────
    public void OnBallTrigger(bool isTop)
    {
        if (isTop)
        {
            _passedTopTime = Time.time; // marcar el momento exacto
        }
        else if (PassedTopRecently)
        {
            _passedTopTime = -999f; // resetear ventana

            // Cooldown: ignorar si se acaba de anotar
            if (Time.time - _lastScoreTime < scoreCooldown) return;

            _lastScoreTime = Time.time;
            int pts = _pendingPoints;
            _pendingPoints = 2; // reset para el siguiente tiro
            GameManager.Instance?.AddScore(pts);
            goalSound?.Play();
            Debug.Log($"[GoalDetector] ¡CANASTA! +{pts} pts ({(pts == 3 ? "TRIPLE" : "doble")})");
        }
    }

    /// <summary>
    /// Llamado por GoalTrigger cuando la pelota sale del trigger superior sin bajar.
    /// Con la ventana de tiempo ya no necesitamos resetear aquí —
    /// el timer se encarga de expirar solo.
    /// </summary>
    public void OnBallExitTop()
    {
        // No reseteamos inmediatamente: damos la ventana passedTopWindow para
        // que un rebote leve en el aro todavía cuente como canasta.
    }
}
