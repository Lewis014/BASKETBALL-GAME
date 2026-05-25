using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gestor de modos de camara.
/// Cambiar con V: Tercera Persona -> Primera Persona -> Lateral -> Tercera Persona.
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("Camaras")]
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private FirstPersonCamera firstPersonCamera;
    [SerializeField] private SideCamera sideCamera;

    private const int CameraCount = 3;

    private int _currentCameraIndex;
    private MonoBehaviour[] _cameraControllers;
    private Camera[] _unityCameras;

    private void Awake()
    {
        ResolveReferences();

        _cameraControllers = new MonoBehaviour[]
        {
            thirdPersonCamera,
            firstPersonCamera,
            sideCamera
        };

        _unityCameras = new Camera[]
        {
            thirdPersonCamera != null ? thirdPersonCamera.GetComponent<Camera>() : null,
            firstPersonCamera != null ? firstPersonCamera.GetComponent<Camera>() : null,
            sideCamera != null ? sideCamera.GetComponent<Camera>() : null
        };

        SetActiveCamera(0);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.vKey.wasPressedThisFrame)
            return;

        _currentCameraIndex = (_currentCameraIndex + 1) % CameraCount;
        SetActiveCamera(_currentCameraIndex);
    }

    private void ResolveReferences()
    {
        if (thirdPersonCamera == null)
            thirdPersonCamera = GetComponent<ThirdPersonCamera>();

        if (firstPersonCamera == null)
            firstPersonCamera = GetComponent<FirstPersonCamera>();

        if (sideCamera == null)
            sideCamera = GetComponent<SideCamera>();
    }

    private void SetActiveCamera(int index)
    {
        Camera activeUnityCamera = _unityCameras[index];

        for (int i = 0; i < _cameraControllers.Length; i++)
        {
            if (_cameraControllers[i] != null)
                _cameraControllers[i].enabled = (i == index);

            if (_unityCameras[i] != null && activeUnityCamera != null)
                _unityCameras[i].enabled = (_unityCameras[i] == activeUnityCamera);
        }
    }
}
