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
    [SerializeField] private float shotAngle = 50f;

    [Header("Trayectoria")]
    [SerializeField] private TrajectoryRenderer trajectoryRenderer;

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

    private void BeginAim()
    {
        _isAiming = true;
        // Rota al jugador hacia el aro si hay referencia
        if (hoopTarget != null)
        {
            Vector3 dir = hoopTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    private void EndAim()
    {
        _isAiming = false;
        trajectoryRenderer?.Hide();
    }

    private void Shoot()
    {
        if (ballPrefab == null || shotPoint == null) return;

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
    /// </summary>
    private Vector3 CalculateShotVelocity()
    {
        if (hoopTarget == null || shotPoint == null)
            return (transform.forward + Vector3.up * 0.7f).normalized * 10f;

        Vector3 start  = shotPoint.position;
        Vector3 target = hoopTarget.position;

        Vector3 horizontal = target - start;
        horizontal.y = 0f;
        float dx = horizontal.magnitude;
        float dy = target.y - start.y;

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
}
