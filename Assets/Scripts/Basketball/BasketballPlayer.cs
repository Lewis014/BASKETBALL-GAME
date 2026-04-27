using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador del jugador para el juego de básquet.
///
/// Asigna en el Inspector:
///   moveAction  → InputSystem_Actions > Player > Move
///   aimAction   → InputSystem_Actions > Player > Sprint  (mantener para apuntar)
///   shootAction → InputSystem_Actions > Player > Attack  (disparar)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class BasketballPlayer : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Disparo")]
    [SerializeField] private Transform shotPoint;
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform hoopTarget;
    [SerializeField] private float shotAngle = 58f;

    [Header("Asistencia de tiro")]
    [Tooltip("Corrige lateralmente el tiro hacia el centro del aro (0 = desactivado, 1 = máximo)")]
    [SerializeField, Range(0f, 1f)] private float aimAssist = 0.6f;

    [Header("Trayectoria")]
    [SerializeField] private TrajectoryRenderer trajectoryRenderer;

    [Header("Zonas de puntuación")]
    [SerializeField] private ShotZoneDetector zoneDetector;
    [SerializeField] private GoalDetector goalDetector;

    [Header("Input Actions")]
    [Tooltip("Player/Move")]
    [SerializeField] private InputActionReference moveAction;
    [Tooltip("Player/Sprint — mantener para ver trayectoria")]
    [SerializeField] private InputActionReference aimAction;
    [Tooltip("Player/Attack — lanzar pelota")]
    [SerializeField] private InputActionReference shootAction;

    private CharacterController _controller;
    private Vector2 _moveInput;
    private Vector3 _velocity;
    private bool _isAiming;
    private ShotZone _currentZone = ShotZone.TwoPoint;

    // Estilos HUD zona
    private GUIStyle _zoneStyle;

    private void Awake() => _controller = GetComponent<CharacterController>();

    private void OnEnable()
    {
        moveAction.action.performed  += ctx => _moveInput = ctx.ReadValue<Vector2>();
        moveAction.action.canceled   += ctx => _moveInput = Vector2.zero;
        moveAction.action.Enable();

        aimAction.action.performed   += ctx => BeginAim();
        aimAction.action.canceled    += ctx => EndAim();
        aimAction.action.Enable();

        shootAction.action.performed += ctx => Shoot();
        shootAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        aimAction.action.Disable();
        shootAction.action.Disable();
    }

    private void Update()
    {
        HandleGravity();
        Move();

        // Actualizar zona actual
        if (zoneDetector != null)
            _currentZone = zoneDetector.GetZone(transform.position);

        if (_isAiming && trajectoryRenderer != null && shotPoint != null)
            trajectoryRenderer.Show(shotPoint.position, CalculateShotVelocity());
    }

    // ──────────────────────────────────────────────
    //  Movimiento
    // ──────────────────────────────────────────────

    private void HandleGravity()
    {
        if (_controller.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
        _velocity.y += Physics.gravity.y * Time.deltaTime;
    }

    private void Move()
    {
        Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y);

        // Rota el jugador hacia la dirección de movimiento
        if (move.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(move);

        move.y = _velocity.y;
        _controller.Move(move * moveSpeed * Time.deltaTime);
    }

    // ──────────────────────────────────────────────
    //  Apuntado y disparo
    // ──────────────────────────────────────────────

    private void FaceHoop()
    {
        if (hoopTarget == null) return;
        Vector3 dir = hoopTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private void BeginAim()
    {
        _isAiming = true;
        FaceHoop(); // Auto-rotar hacia el aro al apuntar
    }

    private void EndAim()
    {
        _isAiming = false;
        trajectoryRenderer?.Hide();
    }

    private void Shoot()
    {
        if (ballPrefab == null || shotPoint == null) return;

        // Siempre apuntar al aro al disparar (aunque no se haya apuntado antes)
        FaceHoop();

        // Registrar zona ANTES de mover (la posición actual es la del tiro)
        int shotPoints = zoneDetector != null ? zoneDetector.GetPoints(transform.position) : 2;
        if (goalDetector != null)
            goalDetector.SetPendingPoints(shotPoints);

        EndAim();

        GameObject ball = Instantiate(ballPrefab, shotPoint.position, Quaternion.identity);
        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(CalculateShotVelocity(), ForceMode.VelocityChange);

        Destroy(ball, 6f); // limpieza automática
    }

    /// <summary>
    /// Calcula la velocidad inicial necesaria para alcanzar el aro
    /// usando movimiento proyectil con ángulo fijo.
    ///   v = sqrt( g * dx² / (2·cos²a·(dx·tana − dy)) )
    ///
    /// Incluye asistencia de apuntado: corrige lateralmente hacia el centro del aro.
    /// </summary>
    private Vector3 CalculateShotVelocity()
    {
        if (hoopTarget == null || shotPoint == null)
            return (transform.forward + Vector3.up * 0.7f).normalized * 10f;

        Vector3 start  = shotPoint.position;

        // Aim assist: interpolar posición de origen hacia la línea directa al aro
        Vector3 assistedStart = start;
        if (aimAssist > 0f && hoopTarget != null)
        {
            // Proyectar el shotPoint sobre la línea jugador→aro para reducir offset lateral
            Vector3 toHoop = hoopTarget.position - transform.position;
            toHoop.y = 0f;
            Vector3 toHoopNorm = toHoop.normalized;
            Vector3 offset = start - transform.position;
            offset.y = 0f;
            float along  = Vector3.Dot(offset, toHoopNorm);
            Vector3 corrected = transform.position + toHoopNorm * along;
            corrected.y = start.y;
            assistedStart = Vector3.Lerp(start, corrected, aimAssist);
        }

        Vector3 target = hoopTarget.position;

        Vector3 horizontal = target - assistedStart;
        horizontal.y = 0f;
        float dx = horizontal.magnitude;
        float dy = target.y - assistedStart.y;

        float g    = Mathf.Abs(Physics.gravity.y);
        float a    = shotAngle * Mathf.Deg2Rad;
        float cosA = Mathf.Cos(a);
        float tanA = Mathf.Tan(a);
        float denom = 2f * cosA * cosA * (dx * tanA - dy);

        if (denom <= 0f || dx < 0.01f)
            return (horizontal.normalized + Vector3.up).normalized * 10f;

        float speed = Mathf.Sqrt(g * dx * dx / denom);

        if (float.IsNaN(speed) || float.IsInfinity(speed))
            return (horizontal.normalized + Vector3.up).normalized * 10f;

        Vector3 dir = horizontal.normalized;
        return dir * speed * cosA + Vector3.up * speed * Mathf.Sin(a);
    }

    // ── HUD de zona ─────────────────────────────────────────────────

    private void OnGUI()
    {
        if (zoneDetector == null) return;

        InitZoneStyle();

        bool isThree = _currentZone == ShotZone.ThreePoint;
        string label    = isThree ? "ZONA 3 PUNTOS" : "ZONA 2 PUNTOS";
        Color  bgColor  = isThree ? new Color(0.1f, 0.6f, 1f, 0.75f)
                                  : new Color(0.1f, 0.8f, 0.2f, 0.75f);

        // Fondo redondeado (simulado con un rect coloreado)
        float w = 240f, h = 36f;
        float x = Screen.width / 2f - w / 2f;
        float y = Screen.height - 70f;

        GUI.color = bgColor;
        GUI.DrawTexture(new Rect(x - 6, y - 4, w + 12, h + 8), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(x, y, w, h), label, _zoneStyle);

        // Distancia al aro
        float dist = zoneDetector.HorizontalDistToHoop(transform.position);
        var distStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(1f, 1f, 1f, 0.8f) }
        };
        GUI.Label(new Rect(x, y + h, w, 20f),
                  $"Dist. al aro: {dist:F1} m  |  Línea 3PT: {zoneDetector.ThreePtRadius} m",
                  distStyle);
    }

    private void InitZoneStyle()
    {
        if (_zoneStyle != null) return;
        _zoneStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white }
        };
    }
}
