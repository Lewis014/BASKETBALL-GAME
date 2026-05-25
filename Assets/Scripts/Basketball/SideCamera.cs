using UnityEngine;

/// <summary>
/// Camara lateral fija tipo transmision NBA.
/// Mantiene un lado de la cancha y sigue al jugador sobre el eje largo.
/// </summary>
[RequireComponent(typeof(Camera))]
public class SideCamera : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;

    [Header("Vista lateral")]
    [Tooltip("X fija de la camara. Usa negativo para verla desde el otro lateral.")]
    [SerializeField] private float sidelineX = 13f;

    [Tooltip("Altura fija de la camara.")]
    [SerializeField] private float cameraHeight = 6f;

    [Tooltip("Offset sobre el eje largo de la cancha.")]
    [SerializeField] private float zOffset = -1f;

    [Tooltip("Altura del punto que mira la camara.")]
    [SerializeField] private float lookHeight = 1.4f;

    [Tooltip("Cuanto acompana la mirada al jugador en el ancho de la cancha.")]
    [SerializeField, Range(0f, 1f)] private float lookAtPlayerWidth = 0.75f;

    [Header("Suavizado")]
    [Tooltip("Velocidad de seguimiento lateral.")]
    [SerializeField] private float followSmoothTime = 0.12f;

    [Tooltip("Velocidad de giro hacia el jugador.")]
    [SerializeField] private float rotationSmoothSpeed = 10f;

    private Vector3 _positionSmoothed;
    private Vector3 _positionVelocity;

    private void Awake()
    {
        if (player != null)
            _positionSmoothed = GetTargetPosition();
    }

    private void OnEnable()
    {
        if (player == null) return;

        _positionSmoothed = GetTargetPosition();
        _positionVelocity = Vector3.zero;
        transform.position = _positionSmoothed;
        UpdateRotation(true);
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPosition = GetTargetPosition();
        _positionSmoothed = Vector3.SmoothDamp(
            _positionSmoothed,
            targetPosition,
            ref _positionVelocity,
            followSmoothTime
        );

        transform.position = _positionSmoothed;
        UpdateRotation(false);
    }

    private Vector3 GetTargetPosition()
    {
        return new Vector3(sidelineX, cameraHeight, player.position.z + zOffset);
    }

    private void UpdateRotation(bool snap)
    {
        Vector3 lookTarget = new Vector3(
            player.position.x * lookAtPlayerWidth,
            player.position.y + lookHeight,
            player.position.z
        );

        Vector3 direction = lookTarget - transform.position;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = snap
            ? targetRotation
            : Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
    }
}
