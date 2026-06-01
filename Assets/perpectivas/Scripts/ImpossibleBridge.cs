using UnityEngine;

namespace Perpectivas
{
    public class ImpossibleBridge : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform requiredViewPoint;
        [SerializeField] private Transform alignmentTarget;
        [SerializeField] private float maxViewPointDistance = 1.5f;
        [SerializeField] private float angleThreshold = 5f;
        [SerializeField] private Collider bridgeCollider;
        [SerializeField] private Renderer bridgeRenderer;

        private bool _isActive;

        private void Awake()
        {
            if (bridgeCollider == null)
                bridgeCollider = GetComponent<Collider>();

            if (bridgeRenderer == null)
                bridgeRenderer = GetComponentInChildren<Renderer>();

            SetBridgeActive(false);
        }

        private void Update()
        {
            if (playerCamera == null || alignmentTarget == null)
                return;

            bool closeEnough = requiredViewPoint == null ||
                               Vector3.Distance(playerCamera.transform.position, requiredViewPoint.position) <= maxViewPointDistance;

            Vector3 targetDirection = (alignmentTarget.position - playerCamera.transform.position).normalized;
            bool aligned = Vector3.Angle(playerCamera.transform.forward, targetDirection) < angleThreshold;

            SetBridgeActive(closeEnough && aligned);
        }

        private void SetBridgeActive(bool active)
        {
            if (_isActive == active)
                return;

            _isActive = active;

            if (bridgeCollider != null)
                bridgeCollider.enabled = active;

            if (bridgeRenderer != null)
                bridgeRenderer.enabled = active;
        }
    }
}
