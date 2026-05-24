using UnityEngine;

/// <summary>
/// Detecta colisiones entre la cámara y la geometría del escenario usando SphereCast.
/// La cámara NUNCA atraviesa geometría ni pierde de vista al personaje.
///
/// Uso: adjuntar al mismo GameObject que ThirdPersonCamera (la cámara principal).
/// Asignar el LayerMask "CameraObstacle" en el Inspector.
/// </summary>
public class CameraCollisionHandler : MonoBehaviour
{
    [Header("─── Detección SphereCast ────────────────────────────")]
    [Tooltip("Radio de la esfera para detectar obstáculos (0.3 recomendado)")]
    [SerializeField] private float sphereRadius = 0.3f;

    [Tooltip("Offset desde el punto de impacto hacia el normal de la superficie.\n" +
             "Evita que la cámara quede pegada a la pared.")]
    [SerializeField] private float surfaceOffset = 0.12f;

    [Tooltip("Distancia mínima garantizada entre el pivote y la cámara (evita zoom extremo)")]
    [SerializeField] private float minimumDistance = 0.8f;

    [Tooltip("Layers que bloquean la cámara.\n" +
             "Asignar: Paredes, Suelo, Poste, Tablero → Layer 'CameraObstacle'.\n" +
             "El jugador NO debe estar en este layer.")]
    [SerializeField] private LayerMask obstacleLayerMask;

    [Header("─── Suavizado de transición ───────────────────────────")]
    [Tooltip("Velocidad con la que la cámara se ACERCA al detectar un obstáculo.\n" +
             "Debe ser alta para evitar clipping instantáneo.")]
    [SerializeField] private float approachSpeed = 20f;

    [Tooltip("Velocidad con la que la cámara se ALEJA al despejar el obstáculo.\n" +
             "Más lenta que el acercamiento para una transición natural.")]
    [SerializeField] private float recoverSpeed = 6f;

    // ── Estado interno ──────────────────────────────────────────────────
    private float _smoothedDistance; // distancia actual suavizada
    private bool  _initialized;

    // ── API pública ─────────────────────────────────────────────────────

    /// <summary>
    /// Calcula y devuelve la posición segura de la cámara.
    ///
    /// Lanza un SphereCast desde <paramref name="pivot"/> (cabeza del jugador)
    /// hacia <paramref name="desiredPos"/>. Si hay obstáculo, recorta la distancia.
    /// La transición es suave en ambas direcciones.
    /// </summary>
    /// <param name="pivot">Punto de origen del SphereCast (altura de la cabeza del jugador).</param>
    /// <param name="desiredPos">Posición ideal de la cámara sin colisiones.</param>
    /// <returns>Posición final segura de la cámara.</returns>
    public Vector3 GetSafePosition(Vector3 pivot, Vector3 desiredPos)
    {
        Vector3 direction      = desiredPos - pivot;
        float   desiredDist    = direction.magnitude;
        float   targetDistance = desiredDist;

        // Inicializar en la primera llamada para evitar salto desde cero
        if (!_initialized)
        {
            _smoothedDistance = desiredDist;
            _initialized      = true;
        }

        // ── SphereCast ───────────────────────────────────────────────────
        bool collision = Physics.SphereCast(
            origin:     pivot,
            radius:     sphereRadius,
            direction:  direction.normalized,
            hitInfo:    out RaycastHit hit,
            maxDistance: desiredDist,
            layerMask:  obstacleLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        if (collision)
        {
            // Recortar la distancia al punto de impacto menos el offset de superficie
            targetDistance = Mathf.Max(hit.distance - surfaceOffset, minimumDistance);
        }

        // ── Suavizado asimétrico ─────────────────────────────────────────
        // Acercarse rápido (hay obstáculo) — alejarse lento (obstáculo despejado)
        float blendSpeed = targetDistance < _smoothedDistance ? approachSpeed : recoverSpeed;
        _smoothedDistance = Mathf.Lerp(_smoothedDistance, targetDistance, Time.deltaTime * blendSpeed);

        // ── Posición final ───────────────────────────────────────────────
        return pivot + direction.normalized * _smoothedDistance;
    }

    // ── Gizmos de depuración ─────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Visualizar el radio del SphereCast en la posición de la cámara
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, sphereRadius);
    }
}
