using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cámara de primera persona con control de mouse.
/// Mira desde la cabeza del jugador.
/// </summary>
[RequireComponent(typeof(Camera))]
public class FirstPersonCamera : MonoBehaviour
{
    [Header("─── Referencias ──────────────────────────────────────")]
    [SerializeField] private Transform player;
    [SerializeField] private BasketballPlayer basketballPlayer;

    [Header("─── Posición ───────────────────────────────────────")]
    [Tooltip("Altura desde los pies del jugador (típicamente cabeza)")]
    [SerializeField] private float eyeHeight = 1.8f;

    [Header("─── Sensibilidad del mouse ────────────────────────")]
    [SerializeField] private float mouseSensitivity = 0.18f;
    [SerializeField] private bool invertY = false;

    [Header("─── Límites verticales ────────────────────────────")]
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 60f;

    private float _yaw;
    private float _pitch;

    private void Awake()
    {
        SyncRotationFromTransform();
    }

    private void OnEnable()
    {
        SyncRotationFromTransform();
    }

    private void SyncRotationFromTransform()
    {
        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = angles.x > 180f ? angles.x - 360f : angles.x;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        LeerInputMouse();
        ActualizarPosicion();
        ActualizarRotacion();
    }

    private void LeerInputMouse()
    {
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        float scaledX = delta.x * mouseSensitivity;
        float scaledY = delta.y * mouseSensitivity * (invertY ? 1f : -1f);

        _yaw += scaledX;
        _pitch += scaledY;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    private void ActualizarPosicion()
    {
        Vector3 eyePosition = player.position + Vector3.up * eyeHeight;
        transform.position = eyePosition;
    }

    private void ActualizarRotacion()
    {
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}
