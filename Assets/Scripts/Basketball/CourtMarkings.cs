using UnityEngine;

/// <summary>
/// Dibuja en tiempo de ejecución las marcaciones reales de una cancha de básquetbol:
///   • Arco de 3 puntos
///   • Línea de tiro libre
///   • Zona de pintura (key/paint)
///   • Línea central
///
/// Agrega este componente al GameObject "Court" o a uno vacío llamado "CourtMarkings".
/// Requiere una referencia al Transform del Hoop.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class CourtMarkings : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform hoopTransform;   // GameObject Hoop
    [SerializeField] private float courtHalfLength = 15f; // mitad del largo de la cancha (z total = 30)
    [SerializeField] private float courtHalfWidth  = 10f; // mitad del ancho  (x total = 20)

    [Header("Medidas FIBA (metros)")]
    [SerializeField] private float threePtRadius   = 6.75f;
    [SerializeField] private float freeThrowDist   = 4.57f;
    [SerializeField] private float paintWidth      = 4.90f;  // ancho de la zona de pintura
    [SerializeField] private float paintLength     = 5.80f;  // largo de la zona de pintura

    [Header("Apariencia")]
    [SerializeField] private float lineWidth  = 0.05f;
    [SerializeField] private Color lineColor  = Color.white;
    [SerializeField] private Material lineMaterial;

    // Cada "sección" de línea se dibuja con su propio LineRenderer hijo
    private void Start()
    {
        // Desactiva el LineRenderer principal (solo lo usamos para tener el componente)
        GetComponent<LineRenderer>().enabled = false;

        if (hoopTransform == null)
        {
            Debug.LogWarning("[CourtMarkings] Asigna el Transform del Hoop en el Inspector.");
            return;
        }

        Vector3 hoopFloor = new Vector3(hoopTransform.position.x, 0.01f, hoopTransform.position.z);

        DrawThreePtArc(hoopFloor);
        DrawFreeThrowLine(hoopFloor);
        DrawPaint(hoopFloor);
        DrawCenterLine();
    }

    // ── Arco de 3 puntos ─────────────────────────────────────────────

    private void DrawThreePtArc(Vector3 hoopFloor)
    {
        // Semicírculo del lado del jugador (z < hoopFloor.z)
        // Ángulo 180°→360° alrededor del aro, luego recortar al ancho de la cancha
        int segments = 60;
        var pts = new System.Collections.Generic.List<Vector3>();

        // Línea recta izquierda (del límite de la cancha hasta donde empieza el arco)
        float straightX = Mathf.Min(threePtRadius, courtHalfWidth);
        float straightZ_start = hoopFloor.z - Mathf.Sqrt(
            Mathf.Max(0f, threePtRadius * threePtRadius - straightX * straightX));

        pts.Add(new Vector3(-courtHalfWidth, 0.01f, straightZ_start));

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(180f, 360f, t) * Mathf.Deg2Rad;
            float x = hoopFloor.x + threePtRadius * Mathf.Cos(angle);
            float z = hoopFloor.z + threePtRadius * Mathf.Sin(angle);

            // Recortar al ancho de la cancha
            x = Mathf.Clamp(x, -courtHalfWidth, courtHalfWidth);
            pts.Add(new Vector3(x, 0.01f, z));
        }

        pts.Add(new Vector3(courtHalfWidth, 0.01f, straightZ_start));

        CreateLine("ThreePtArc", pts.ToArray(), new Color(1f, 0.9f, 0.1f)); // amarillo
    }

    // ── Línea de tiro libre ───────────────────────────────────────────

    private void DrawFreeThrowLine(Vector3 hoopFloor)
    {
        float z = hoopFloor.z - freeThrowDist;
        float halfPaint = paintWidth / 2f;

        Vector3[] pts = {
            new Vector3(-halfPaint, 0.01f, z),
            new Vector3( halfPaint, 0.01f, z)
        };

        CreateLine("FreeThrowLine", pts, Color.white);

        // Semicírculo detrás de la línea de tiro libre
        int segs = 30;
        var arc = new Vector3[segs + 1];
        for (int i = 0; i <= segs; i++)
        {
            float t = i / (float)segs;
            float angle = Mathf.Lerp(180f, 360f, t) * Mathf.Deg2Rad;
            arc[i] = new Vector3(
                hoopFloor.x + halfPaint * Mathf.Cos(angle),
                0.01f,
                z + halfPaint * Mathf.Sin(angle)
            );
        }
        CreateLine("FreeThrowArc", arc, new Color(0.7f, 0.7f, 0.7f));
    }

    // ── Zona de pintura (paint / key) ────────────────────────────────

    private void DrawPaint(Vector3 hoopFloor)
    {
        float halfW = paintWidth  / 2f;
        float nearZ = hoopFloor.z;               // borde cerca del aro
        float farZ  = hoopFloor.z - paintLength; // borde lejano (hacia jugador)

        Vector3[] rect = {
            new Vector3(-halfW, 0.01f, nearZ),
            new Vector3(-halfW, 0.01f, farZ),
            new Vector3( halfW, 0.01f, farZ),
            new Vector3( halfW, 0.01f, nearZ),
            new Vector3(-halfW, 0.01f, nearZ)  // cerrar el rectángulo
        };

        CreateLine("Paint", rect, new Color(0.6f, 0.85f, 1f)); // azul claro
    }

    // ── Línea central ────────────────────────────────────────────────

    private void DrawCenterLine()
    {
        Vector3[] pts = {
            new Vector3(-courtHalfWidth, 0.01f, 0f),
            new Vector3( courtHalfWidth, 0.01f, 0f)
        };
        CreateLine("CenterLine", pts, new Color(1f, 1f, 1f, 0.4f));

        // Círculo central
        int segs = 40;
        float r  = 1.8f;
        var circle = new Vector3[segs + 1];
        for (int i = 0; i <= segs; i++)
        {
            float a = i / (float)segs * 2f * Mathf.PI;
            circle[i] = new Vector3(r * Mathf.Cos(a), 0.01f, r * Mathf.Sin(a));
        }
        CreateLine("CenterCircle", circle, new Color(1f, 1f, 1f, 0.4f));
    }

    // ── Helper ───────────────────────────────────────────────────────

    private void CreateLine(string goName, Vector3[] points, Color color)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = points.Length;
        lr.SetPositions(points);
        lr.startWidth = lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        lr.loop = false;

        if (lineMaterial != null)
            lr.material = lineMaterial;
        else
        {
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        lr.startColor = lr.endColor = color;
    }
}
