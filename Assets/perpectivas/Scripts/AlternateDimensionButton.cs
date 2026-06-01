using UnityEngine;

namespace Perpectivas
{
    public class AlternateDimensionButton : MonoBehaviour
    {
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private Color idleColor = new Color(1f, 0.2f, 0.85f);
        [SerializeField] private Color pressedColor = new Color(0.2f, 1f, 0.85f);

        public bool IsPressed { get; private set; }

        private void Awake()
        {
            if (buttonRenderer == null)
                buttonRenderer = GetComponentInChildren<Renderer>();

            ApplyColor(idleColor);
        }

        public void Press()
        {
            IsPressed = true;
            ApplyColor(pressedColor);
        }

        private void ApplyColor(Color color)
        {
            if (buttonRenderer == null)
                return;

            Material material = buttonRenderer.material;
            material.color = color;

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.8f);
            }
        }
    }
}
