using UnityEngine;

/// <summary>
/// Controla las animaciones de Kairo The Boxer según el movimiento del jugador.
/// Coloca este script en el mismo GameObject que el Animator de Kairo,
/// o en el Player (asignando el Animator en el Inspector).
///
/// Parámetros del Animator Controller que debes crear:
///   float Speed  — velocidad de movimiento (0 = idle, >0 = walk, >5 = run)
/// </summary>
[RequireComponent(typeof(Animator))]
public class KairoAnimator : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("CharacterController del Player padre")]
    [SerializeField] private CharacterController playerController;

    [Header("Umbrales de velocidad")]
    [SerializeField] private float walkThreshold = 0.1f;
    [SerializeField] private float runThreshold  = 4f;

    [Header("Suavizado")]
    [SerializeField] private float dampTime = 0.1f;

    private Animator _animator;
    private static readonly int SpeedParam = Animator.StringToHash("Speed");

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        // Auto-buscar el CharacterController en el padre si no está asignado
        if (playerController == null)
            playerController = GetComponentInParent<CharacterController>();
    }

    private void Update()
    {
        if (playerController == null) return;

        // Velocidad horizontal (ignoramos el eje Y de gravedad)
        Vector3 velocity = playerController.velocity;
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

        _animator.SetFloat(SpeedParam, horizontalSpeed, dampTime, Time.deltaTime);
    }
}
