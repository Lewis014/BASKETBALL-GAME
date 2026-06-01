using UnityEngine;

namespace Perpectivas
{
    public class AlternateDimensionManager : MonoBehaviour
    {
        [SerializeField] private Camera alternateCamera;
        [SerializeField] private DoorController worldADoor;
        [SerializeField] private LayerMask alternateButtonMask = ~0;
        [SerializeField] private float alternateRayDistance = 80f;

        public bool TryActivateFromMonitor(Vector2 monitorUv)
        {
            if (alternateCamera == null)
                return false;

            Ray alternateRay = alternateCamera.ViewportPointToRay(new Vector3(monitorUv.x, monitorUv.y, 0f));

            if (!Physics.Raycast(alternateRay, out RaycastHit hit, alternateRayDistance, alternateButtonMask, QueryTriggerInteraction.Collide))
                return false;

            AlternateDimensionButton button = hit.collider.GetComponentInParent<AlternateDimensionButton>();
            if (button == null)
                return false;

            button.Press();

            if (worldADoor != null)
                worldADoor.Open();

            return true;
        }
    }
}
