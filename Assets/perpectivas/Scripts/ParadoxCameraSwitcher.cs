using UnityEngine;
using UnityEngine.InputSystem;

namespace Perpectivas
{
    public class ParadoxCameraSwitcher : MonoBehaviour
    {
        [SerializeField] private Camera firstPersonCamera;
        [SerializeField] private Camera isometricCamera;

        private bool _isIsometric;

        private void Awake()
        {
            SetIsometric(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
                SetIsometric(!_isIsometric);
        }

        private void SetIsometric(bool enabled)
        {
            _isIsometric = enabled;
            SetCameraState(firstPersonCamera, !enabled);
            SetCameraState(isometricCamera, enabled);
        }

        private void SetCameraState(Camera targetCamera, bool enabled)
        {
            if (targetCamera == null)
                return;

            targetCamera.enabled = enabled;

            AudioListener listener = targetCamera.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = enabled;
        }
    }
}
