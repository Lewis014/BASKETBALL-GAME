using UnityEngine;
using UnityEngine.InputSystem;

namespace Perpectivas
{
    [RequireComponent(typeof(CharacterController))]
    public class ParadoxFirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.7f;
        [SerializeField] private float jumpHeight = 1.35f;
        [SerializeField] private float groundCheckDistance = 0.18f;

        [Header("Look")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float verticalLookClamp = 80f;

        [Header("Gravity Rotation")]
        [SerializeField] private float gravityRotationSpeed = 8f;

        private CharacterController _controller;
        private Vector3 _gravityVelocity;
        private Quaternion _targetBodyRotation;
        private float _pitch;
        private bool _isRotatingToGravity;

        public Camera PlayerCamera => playerCamera;
        public Vector3 CurrentGravity => Physics.gravity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera != null)
                playerCamera.fieldOfView = 75f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            ToggleCursorLock();
            HandleLook();
            HandleMovement();
            RotateBodyToGravity();
        }

        public void SetGravity(Vector3 newGravity)
        {
            if (newGravity.sqrMagnitude < 0.01f)
                return;

            Physics.gravity = newGravity;
            Vector3 newUp = -newGravity.normalized;
            _targetBodyRotation = Quaternion.FromToRotation(transform.up, newUp) * transform.rotation;
            _gravityVelocity = Vector3.Project(_gravityVelocity, newGravity.normalized);
            _isRotatingToGravity = true;
        }

        private void ToggleCursorLock()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            bool shouldLock = Cursor.lockState != CursorLockMode.Locked;
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }

        private void HandleLook()
        {
            if (Mouse.current == null || playerCamera == null || Cursor.lockState != CursorLockMode.Locked)
                return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            transform.Rotate(transform.up, mouseDelta.x * mouseSensitivity, Space.World);

            _pitch = Mathf.Clamp(_pitch - mouseDelta.y * mouseSensitivity, -verticalLookClamp, verticalLookClamp);
            playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            if (Keyboard.current == null || playerCamera == null)
                return;

            Vector2 moveInput = ReadMoveInput();
            Vector3 up = transform.up;
            Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, up).normalized;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.ProjectOnPlane(transform.forward, up).normalized;

            Vector3 right = Vector3.ProjectOnPlane(playerCamera.transform.right, up).normalized;
            Vector3 wishMove = forward * moveInput.y + right * moveInput.x;

            if (wishMove.sqrMagnitude > 1f)
                wishMove.Normalize();

            float speed = Keyboard.current.leftShiftKey.isPressed ? moveSpeed * sprintMultiplier : moveSpeed;
            bool grounded = IsGrounded(up);

            if (grounded && Vector3.Dot(_gravityVelocity, Physics.gravity) > 0f)
                _gravityVelocity = Physics.gravity.normalized * 0.8f;

            if (grounded && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                float jumpSpeed = Mathf.Sqrt(jumpHeight * 2f * Physics.gravity.magnitude);
                _gravityVelocity = up * jumpSpeed;
            }

            _gravityVelocity += Physics.gravity * Time.deltaTime;
            _controller.Move((wishMove * speed + _gravityVelocity) * Time.deltaTime);
        }

        private Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;

            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;

            return input;
        }

        private bool IsGrounded(Vector3 up)
        {
            Vector3 center = transform.TransformPoint(_controller.center);
            float radius = Mathf.Max(0.05f, _controller.radius * 0.92f);
            float castDistance = Mathf.Max(groundCheckDistance, _controller.height * 0.5f - radius + groundCheckDistance);

            return Physics.SphereCast(
                center,
                radius,
                -up,
                out _,
                castDistance,
                ~0,
                QueryTriggerInteraction.Ignore);
        }

        private void RotateBodyToGravity()
        {
            if (!_isRotatingToGravity)
                return;

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                _targetBodyRotation,
                gravityRotationSpeed * Time.deltaTime);

            if (Quaternion.Angle(transform.rotation, _targetBodyRotation) < 0.35f)
            {
                transform.rotation = _targetBodyRotation;
                _isRotatingToGravity = false;
            }
        }
    }
}
