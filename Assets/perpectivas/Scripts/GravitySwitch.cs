using UnityEngine;

namespace Perpectivas
{
    public class GravitySwitch : MonoBehaviour, IParadoxInteractable
    {
        [SerializeField] private Vector3[] gravityModes =
        {
            new Vector3(0f, -9.81f, 0f),
            new Vector3(9.81f, 0f, 0f),
            new Vector3(-9.81f, 0f, 0f)
        };

        [SerializeField] private string prompt = "cambiar gravedad";
        [SerializeField] private Transform animatedLever;

        private int _gravityIndex;

        public string Prompt => prompt;

        public void Interact(ParadoxFirstPersonController player)
        {
            if (player == null || gravityModes == null || gravityModes.Length == 0)
                return;

            _gravityIndex = (_gravityIndex + 1) % gravityModes.Length;
            player.SetGravity(gravityModes[_gravityIndex]);

            if (animatedLever != null)
                animatedLever.localRotation = Quaternion.Euler(0f, 0f, _gravityIndex * 45f);
        }
    }
}
