using UnityEngine;

namespace Perpectivas
{
    public class ExitPortal : MonoBehaviour
    {
        [SerializeField] private string completionMessage = "Paradox Lab completado.";

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<ParadoxFirstPersonController>() != null)
                Debug.Log(completionMessage);
        }
    }
}
