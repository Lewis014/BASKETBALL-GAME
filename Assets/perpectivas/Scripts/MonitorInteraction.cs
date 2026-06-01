using UnityEngine;

namespace Perpectivas
{
    [RequireComponent(typeof(Collider))]
    public class MonitorInteraction : MonoBehaviour, IParadoxInteractable
    {
        [SerializeField] private AlternateDimensionManager dimensionManager;
        [SerializeField] private string prompt = "activar monitor dimensional";

        public string Prompt => prompt;

        public void Interact(ParadoxFirstPersonController player)
        {
            if (dimensionManager == null || player == null || player.PlayerCamera == null)
                return;

            Ray ray = player.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (!Physics.Raycast(ray, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Collide))
                return;

            if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
                return;

            dimensionManager.TryActivateFromMonitor(hit.textureCoord);
        }
    }
}
