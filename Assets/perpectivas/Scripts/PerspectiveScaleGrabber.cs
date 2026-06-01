using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Perpectivas
{
    public class PerspectiveScaleGrabber : MonoBehaviour
    {
        [SerializeField] private Camera grabCamera;
        [SerializeField] private float pickupDistance = 5f;
        [SerializeField] private float projectionDistance = 35f;
        [SerializeField] private float placementOffset = 0.05f;
        [SerializeField] private float positionLerp = 18f;
        [SerializeField] private LayerMask rayMask = ~0;

        private PerspectiveScalable _heldObject;
        private Rigidbody _heldRigidbody;
        private Vector3 _originalScale;
        private float _initialDistance;

        private void Awake()
        {
            if (grabCamera == null)
                grabCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                ToggleHeldObject();

            if (_heldObject != null)
                UpdateHeldObject();
        }

        private void ToggleHeldObject()
        {
            if (_heldObject != null)
            {
                DropObject();
                return;
            }

            TryGrabObject();
        }

        private void TryGrabObject()
        {
            if (grabCamera == null)
                return;

            Ray ray = grabCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (!Physics.Raycast(ray, out RaycastHit hit, pickupDistance, rayMask, QueryTriggerInteraction.Ignore))
                return;

            PerspectiveScalable scalable = hit.collider.GetComponentInParent<PerspectiveScalable>();
            if (scalable == null)
                return;

            _heldObject = scalable;
            _heldRigidbody = _heldObject.GetComponent<Rigidbody>();
            _originalScale = _heldObject.transform.localScale;
            _initialDistance = Mathf.Max(0.15f, hit.distance);

            if (_heldRigidbody != null)
            {
                _heldRigidbody.useGravity = false;
                _heldRigidbody.isKinematic = true;
                _heldRigidbody.linearVelocity = Vector3.zero;
                _heldRigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void UpdateHeldObject()
        {
            Ray ray = grabCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float currentDistance = projectionDistance;
            Vector3 hitPoint = ray.GetPoint(projectionDistance);

            if (TryFindProjectionSurface(ray, out RaycastHit surfaceHit))
            {
                currentDistance = Mathf.Max(0.15f, surfaceHit.distance);
                hitPoint = surfaceHit.point;
            }

            float scaleRatio = currentDistance / _initialDistance;
            scaleRatio = Mathf.Clamp(
                scaleRatio,
                _heldObject.MinimumScaleMultiplier,
                _heldObject.MaximumScaleMultiplier);

            Transform heldTransform = _heldObject.transform;
            heldTransform.localScale = _originalScale * scaleRatio;

            float surfaceOffset = CalculateProjectedRadius(heldTransform, ray.direction) + placementOffset;
            Vector3 targetPosition = hitPoint - ray.direction * surfaceOffset;
            heldTransform.position = Vector3.Lerp(
                heldTransform.position,
                targetPosition,
                1f - Mathf.Exp(-positionLerp * Time.deltaTime));
        }

        private bool TryFindProjectionSurface(Ray ray, out RaycastHit surfaceHit)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, projectionDistance, rayMask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (_heldObject != null && hit.collider.transform.IsChildOf(_heldObject.transform))
                    continue;

                surfaceHit = hit;
                return true;
            }

            surfaceHit = default;
            return false;
        }

        private float CalculateProjectedRadius(Transform target, Vector3 direction)
        {
            Bounds bounds = new Bounds(target.position, Vector3.one * 0.2f);
            Renderer renderer = target.GetComponentInChildren<Renderer>();
            Collider collider = target.GetComponentInChildren<Collider>();

            if (renderer != null)
                bounds = renderer.bounds;
            else if (collider != null)
                bounds = collider.bounds;

            Vector3 extents = bounds.extents;
            Vector3 absDirection = new Vector3(
                Mathf.Abs(direction.x),
                Mathf.Abs(direction.y),
                Mathf.Abs(direction.z));

            return Vector3.Dot(extents, absDirection);
        }

        private void DropObject()
        {
            if (_heldRigidbody != null)
            {
                _heldRigidbody.isKinematic = false;
                _heldRigidbody.useGravity = true;
            }

            _heldObject = null;
            _heldRigidbody = null;
        }
    }
}
