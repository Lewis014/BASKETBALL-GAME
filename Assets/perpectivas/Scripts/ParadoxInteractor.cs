using UnityEngine;
using UnityEngine.InputSystem;

namespace Perpectivas
{
    public class ParadoxInteractor : MonoBehaviour
    {
        [SerializeField] private ParadoxFirstPersonController player;
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private float interactionDistance = 4f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private IParadoxInteractable _focusedInteractable;

        private void Awake()
        {
            if (player == null)
                player = GetComponent<ParadoxFirstPersonController>();

            if (interactionCamera == null && player != null)
                interactionCamera = player.PlayerCamera;
        }

        private void Update()
        {
            _focusedInteractable = FindFocusedInteractable();

            if (_focusedInteractable != null &&
                Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                _focusedInteractable.Interact(player);
            }
        }

        private IParadoxInteractable FindFocusedInteractable()
        {
            if (interactionCamera == null)
                return null;

            Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Collide))
                return null;

            MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IParadoxInteractable interactable && behaviour.isActiveAndEnabled)
                    return interactable;
            }

            return null;
        }

        private void OnGUI()
        {
            DrawCrosshair();

            if (_focusedInteractable == null)
                return;

            string text = "E - " + _focusedInteractable.Prompt;
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                normal = { textColor = Color.white }
            };

            Rect rect = new Rect(Screen.width * 0.5f - 180f, Screen.height - 88f, 360f, 32f);
            GUI.Label(rect, text, style);
        }

        private void DrawCrosshair()
        {
            const float size = 10f;
            Rect rect = new Rect(Screen.width * 0.5f - size * 0.5f, Screen.height * 0.5f - size * 0.5f, size, size);
            GUI.color = new Color(1f, 1f, 1f, 0.75f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
