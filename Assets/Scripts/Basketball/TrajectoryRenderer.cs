using UnityEngine;

/// <summary>
/// Dibuja la parábola de trayectoria usando un LineRenderer.
/// Ecuación: p(t) = p0 + v0*t + 0.5*g*t²
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class TrajectoryRenderer : MonoBehaviour
{
    [Tooltip("Número de puntos en la línea")]
    [SerializeField] private int resolution = 35;

    [Tooltip("Paso de tiempo entre puntos")]
    [SerializeField] private float timeStep = 0.08f;

    [Tooltip("Detener la línea al llegar al suelo (Y <= 0)")]
    [SerializeField] private float groundLevel = 0f;

    private LineRenderer _line;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.startWidth = 0.06f;
        _line.endWidth = 0.02f;
        _line.useWorldSpace = true;
        _line.enabled = false;

        // Material simple
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.startColor = new Color(1f, 1f, 0f, 0.8f);
        _line.endColor   = new Color(1f, 0.5f, 0f, 0.2f);
    }

    /// <summary>Muestra la trayectoria a partir de origen y velocidad inicial.</summary>
    public void Show(Vector3 origin, Vector3 velocity)
    {
        _line.enabled = true;
        Vector3 gravity = Physics.gravity;

        var points = new System.Collections.Generic.List<Vector3>(resolution);

        for (int i = 0; i < resolution; i++)
        {
            float t = i * timeStep;
            Vector3 pos = origin + velocity * t + 0.5f * gravity * t * t;

            if (pos.y < groundLevel && i > 0)
            {
                // Interpolamos hasta el suelo exactamente
                Vector3 prev = points[points.Count - 1];
                float frac = (prev.y - groundLevel) / (prev.y - pos.y);
                points.Add(Vector3.Lerp(prev, pos, frac));
                break;
            }

            points.Add(pos);
        }

        _line.positionCount = points.Count;
        _line.SetPositions(points.ToArray());
    }

    public void Hide()
    {
        _line.enabled = false;
        _line.positionCount = 0;
    }
}
