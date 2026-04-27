using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float verticalLookClamp = 80f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference crouchAction;
    [SerializeField] private InputActionReference attackAction;
    [SerializeField] private InputActionReference interactAction;

    private CharacterController _controller;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private Vector3 _velocity;
    private float _verticalRotation;
    private bool _isSprinting;
    private bool _isCrouching;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        moveAction.action.performed += OnMove;
        moveAction.action.canceled += OnMove;
        moveAction.action.Enable();

        lookAction.action.performed += OnLook;
        lookAction.action.canceled += OnLook;
        lookAction.action.Enable();

        jumpAction.action.performed += OnJump;
        jumpAction.action.Enable();

        sprintAction.action.performed += ctx => _isSprinting = true;
        sprintAction.action.canceled += ctx => _isSprinting = false;
        sprintAction.action.Enable();

        crouchAction.action.performed += ctx => _isCrouching = !_isCrouching;
        crouchAction.action.Enable();

        attackAction.action.performed += OnAttack;
        attackAction.action.Enable();

        interactAction.action.performed += OnInteract;
        interactAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.performed -= OnMove;
        moveAction.action.canceled -= OnMove;

        lookAction.action.performed -= OnLook;
        lookAction.action.canceled -= OnLook;

        jumpAction.action.performed -= OnJump;

        attackAction.action.performed -= OnAttack;
        interactAction.action.performed -= OnInteract;

        moveAction.action.Disable();
        lookAction.action.Disable();
        jumpAction.action.Disable();
        sprintAction.action.Disable();
        crouchAction.action.Disable();
        attackAction.action.Disable();
        interactAction.action.Disable();
    }

    private void Update()
    {
        ApplyGravity();
        Move();
        Look();
    }

    private void Move()
    {
        float speed = moveSpeed * (_isSprinting ? sprintMultiplier : 1f);
        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        _controller.Move(move * speed * Time.deltaTime);
        _controller.Move(_velocity * Time.deltaTime);
    }

    private void Look()
    {
        if (cameraTransform == null) return;

        transform.Rotate(Vector3.up * _lookInput.x * lookSensitivity);

        _verticalRotation -= _lookInput.y * lookSensitivity;
        _verticalRotation = Mathf.Clamp(_verticalRotation, -verticalLookClamp, verticalLookClamp);
        cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;
    }

    private void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
    private void OnLook(InputAction.CallbackContext ctx) => _lookInput = ctx.ReadValue<Vector2>();

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (_controller.isGrounded)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private void OnAttack(InputAction.CallbackContext ctx) => Debug.Log("Attack!");
    private void OnInteract(InputAction.CallbackContext ctx) => Debug.Log("Interact!");
}
