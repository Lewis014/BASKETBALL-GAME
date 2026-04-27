using UnityEngine;

/// <summary>
/// Determina si el jugador está en zona de 2 o 3 puntos
/// según las reglas FIBA (radio de 6.75 m desde el aro).
/// </summary>
public class ShotZoneDetector : MonoBehaviour
{
    [Header("Referencia al aro")]
    [Tooltip("Transform del GameObject Hoop (o su base a nivel del suelo)")]
    [SerializeField] private Transform hoopTransform;

    [Header("Medidas FIBA")]
    [Tooltip("Radio de la línea de 3 puntos en metros (FIBA: 6.75, NBA: 7.24)")]
    [SerializeField] private float threePtRadius = 6.75f;

    [Tooltip("Distancia de la línea de tiro libre al aro en metros (FIBA: 4.57)")]
    [SerializeField] private float freeThrowDistance = 4.57f;

    // ── API pública ────────────────────────────────────────────────────

    /// <summary>Devuelve cuántos puntos vale un tiro desde worldPos.</summary>
    public int GetPoints(Vector3 worldPos)
    {
        return GetZone(worldPos) == ShotZone.ThreePoint ? 3 : 2;
    }

    public ShotZone GetZone(Vector3 worldPos)
    {
        float dist = HorizontalDistToHoop(worldPos);
        return dist >= threePtRadius ? ShotZone.ThreePoint : ShotZone.TwoPoint;
    }

    /// <summary>True si el jugador está en la línea de tiro libre (±0.3 m de tolerancia).</summary>
    public bool IsAtFreeThrowLine(Vector3 worldPos, float tolerance = 0.3f)
    {
        float dist = HorizontalDistToHoop(worldPos);
        return Mathf.Abs(dist - freeThrowDistance) <= tolerance;
    }

    public float HorizontalDistToHoop(Vector3 worldPos)
    {
        if (hoopTransform == null) return 0f;
        Vector3 hoopFloor   = new Vector3(hoopTransform.position.x, 0f, hoopTransform.position.z);
        Vector3 playerFloor = new Vector3(worldPos.x, 0f, worldPos.z);
        return Vector3.Distance(playerFloor, hoopFloor);
    }

    public float ThreePtRadius   => threePtRadius;
    public float FreeThrowDistance => freeThrowDistance;
    public Transform HoopTransform => hoopTransform;
}

public enum ShotZone { TwoPoint, ThreePoint }
