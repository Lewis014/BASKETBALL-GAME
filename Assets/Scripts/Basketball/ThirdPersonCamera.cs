using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cámara orbital en tercera persona con anti-colisión y modo de apuntado.
///
/// Configuración en escena:
///   1. Crear un GameObject vacío "CameraRig" en la raíz de la escena.
///   2. Hacer la Main Camera hija de CameraRig.
///   3. Adjuntar este script Y CameraCollisionHandler a la Main Camera.
///   4. Asignar las referencias en el Inspector.
///
/// El CameraRig NO debe ser hijo del jugador — la cámara sigue al jugador
/// a través de código, no por jerarquía.
/// </summary>
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CameraCollisionHandler))]
public class ThirdPersonCamera : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ════════════════════════════════════════════════════════════════════

    [Header("─── Referencias obligatorias ──────────────────────────")]
    [Tooltip("Transform del jugador que seguirá la cámara")]
    [SerializeField] private Transform player;

    [Tooltip("Script BasketballPlayer para leer IsAiming y CameraYaw")]
    [SerializeField] private BasketballPlayer basketballPlayer;

    [Header("─── Posición normal ──────────────────────────────────────")]
    [Tooltip("Distancia de la cámara al jugador en modo libre (metros)")]
    [SerializeField] private float normalDistance = 5f;

    [Tooltip("Altura del pivote sobre los pies del jugador (metros).\n" +
             "2 = altura aproximada de cabeza para un personaje de 2 m.")]
    [SerializeField] private float pivotHeight = 2f;

    [Header("─── Modo apuntado (over-the-shoulder) ────────────────────")]
    [Tooltip("Distancia de la cámara al apuntar (más cerca que normal)")]
    [SerializeField] private float aimDistance = 3f;

    [Tooltip("Desplazamiento lateral y vertical al apuntar.\n" +
             "X positivo = hombro derecho | Y positivo = más arriba")]
    [SerializeField] private Vector3 aimShoulderOffset = new Vector3(0.6f, 0.15f, 0f);

    [Tooltip("Velocidad de transición entre modo normal y apuntado")]
    [SerializeField] private float aimTransitionSpeed = 8f;

    [Header("─── Ángulos verticales ────────────────────────────────────")]
    [Tooltip("Ángulo mínimo de pitch — límite mirando hacia abajo (°)")]
    [SerializeField] private float minPitch = -20f;

    [Tooltip("Ángulo máximo de pitch — límite mirando hacia arriba (°)")]
    [SerializeField] private float maxPitch = 60f;

    [Header("─── Sensibilidad del mouse ──────────────────────────────")]
    [Tooltip("Multiplicador de sensibilidad horizontal y vertical del mouse")]
    [SerializeField] private float mouseSensitivity = 0.18f;

    [Tooltip("Invertir el eje Y del mouse (false = movimiento natural)")]
    [SerializeField] private bool invertY = false;

    [Header("─── Suavizado ───────────────────────────────────────────")]
    [Tooltip("Tiempo de suavizado de posición con SmoothDamp.\n" +
             "Valores bajos = más reactivo | altos = más flotante")]
    [SerializeField] private float positionSmoothTime = 0.08f;

    [Tooltip("Velocidad de interpolación de la rotación (Slerp)")]
    [SerializeField] private float rotationSmoothSpeed = 12f;

    [Header("─── Seguimiento del jugador ─────────────────────────────")]
    [Tooltip("Velocidad con la que el pivote sigue al jugador (SmoothDamp).\n" +
             "Ajustar para más o menos 'lag' al caminar.")]
    [SerializeField] private float followSmoothTime = 0.05f;

    // ════════════════════════════════════════════════════════════════════
    //  ESTADO INTERNO
    // ════════════════════════════════════════════════════════════════════

    private CameraCollisionHandler _collision;

    private float _yaw;              // rotación horizontal acumulada
    private float _pitch;            // rotación vertical actual (clamped)
    private float _currentDistance;  // distancia interpolada

    private Vector3 _pivotSmoothed;          // posición del pivote suavizada
    private Vector3 _pivotVelocity;          // velocidad para SmoothDamp del pivote
    private Vector3 _positionVelocity;       // velocidad para SmoothDamp de la cámara
    private Vector3 _currentShoulderOffset;  // offset de hombro interpolado

    // ════════════════════════════════════════════════════════════════════
    //  CICLO DE VIDA
    // ════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _collision = GetComponent<CameraCollisionHandler>();

        // Inicializar rotación con la orientación actual de la cámara para evitar
        // el salto de rotación en el primer frame
        Vector3 angles = transform.eulerAngles;
        _yaw   = angles.y;
        _pitch = angles.x > 180f ? angles.x - 360f : angles.x; // normalizar

        _currentDistance = normalDistance;

        // Inicializar pivote en la posición del jugador para evitar salto inicial
        if (player != null)
            _pivotSmoothed = player.position + Vector3.up * pivotHeight;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        LeerInputMouse();
        ActualizarDistancia();
        ActualizarPivote();
        AplicarTransformCamara();
    }

    // ════════════════════════════════════════════════════════════════════
    //  INPUT
    // ════════════════════════════════════════════════════════════════════

    private void LeerInputMouse()
    {
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        // Escalar por sensibilidad y Time.deltaTime
        // (delta del mouse ya viene en píxeles/frame — dividir por una base para normalizar)
        float scaledX = delta.x * mouseSensitivity;
        float scaledY = delta.y * mouseSensitivity * (invertY ? 1f : -1f);

        _yaw   += scaledX;
        _pitch += scaledY;
        _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    // ════════════════════════════════════════════════════════════════════
    //  DISTANCIA (normal ↔ apuntado)
    // ════════════════════════════════════════════════════════════════════

    private void ActualizarDistancia()
    {
        bool   isAiming      = basketballPlayer != null && basketballPlayer.IsAiming;
        float  targetDistance = isAiming ? aimDistance : normalDistance;

        _currentDistance = Mathf.Lerp(
            _currentDistance,
            targetDistance,
            Time.deltaTime * aimTransitionSpeed
        );

        // Interpolación del offset de hombro
        Vector3 targetShoulder = isAiming ? aimShoulderOffset : Vector3.zero;
        _currentShoulderOffset = Vector3.Lerp(
            _currentShoulderOffset,
            targetShoulder,
            Time.deltaTime * aimTransitionSpeed
        );
    }

    // ════════════════════════════════════════════════════════════════════
    //  PIVOTE (punto alrededor del cual orbita la cámara)
    // ════════════════════════════════════════════════════════════════════

    private void ActualizarPivote()
    {
        // El pivote sigue al jugador con suavizado para evitar sacudidas bruscas
        Vector3 targetPivot = player.position + Vector3.up * pivotHeight;

        _pivotSmoothed = Vector3.SmoothDamp(
            _pivotSmoothed,
            targetPivot,
            ref _pivotVelocity,
            followSmoothTime
        );
    }

    // ════════════════════════════════════════════════════════════════════
    //  POSICIÓN Y ROTACIÓN FINAL DE LA CÁMARA
    // ════════════════════════════════════════════════════════════════════

    private void AplicarTransformCamara()
    {
        // Rotación orbital: pitch vertical + yaw horizontal
        Quaternion orbitalRotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // Offset de hombro: desplazar el pivote lateralmente en espacio de cámara
        Vector3 pivotConHombro = _pivotSmoothed
            + Quaternion.Euler(0f, _yaw, 0f) * _currentShoulderOffset;

        // Posición deseada: pivote + rotación * atrás
        Vector3 posicionDeseada = pivotConHombro
            + orbitalRotation * new Vector3(0f, 0f, -_currentDistance);

        // Posición segura: pasar por el anti-colisión
        Vector3 posicionSegura = _collision != null
            ? _collision.GetSafePosition(pivotConHombro, posicionDeseada)
            : posicionDeseada;

        // Aplicar posición con SmoothDamp (suaviza pequeñas vibraciones)
        transform.position = Vector3.SmoothDamp(
            transform.position,
            posicionSegura,
            ref _positionVelocity,
            positionSmoothTime
        );

        // La cámara siempre mira hacia el pivote (no hacia el jugador directamente,
        // sino hacia el punto alrededor del cual orbita)
        Vector3 dirAlPivote = pivotConHombro - transform.position;
        if (dirAlPivote.sqrMagnitude > 0.001f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(dirAlPivote);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotacionObjetivo,
                Time.deltaTime * rotationSmoothSpeed
            );
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Yaw actual de la cámara en grados.
    /// BasketballPlayer lo usa para calcular el movimiento relativo a la cámara.
    /// </summary>
    public float CameraYaw => _yaw;

    /// <summary>
    /// Forward horizontal de la cámara (sin componente Y).
    /// Útil para orientar el movimiento del jugador relativo a la cámara.
    /// </summary>
    public Vector3 CameraForwardFlat
    {
        get
        {
            Vector3 f = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
            return f.normalized;
        }
    }

    /// <summary>
    /// Right horizontal de la cámara (sin componente Y).
    /// </summary>
    public Vector3 CameraRightFlat
    {
        get
        {
            Vector3 r = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
            return r.normalized;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  GIZMOS DE DEPURACIÓN
    // ════════════════════════════════════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Vector3 pivot = player.position + Vector3.up * pivotHeight;

        // Línea pivote → cámara
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pivot, transform.position);

        // Esfera en el pivote
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pivot, 0.1f);
    }
}
