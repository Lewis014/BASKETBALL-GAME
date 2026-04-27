using UnityEngine;

/// <summary>
/// Coloca este script en TriggerSuperior (isTopTrigger = true)
/// y en TriggerInferior (isTopTrigger = false).
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoalTrigger : MonoBehaviour
{
    [SerializeField] private GoalDetector detector;
    [SerializeField] private bool isTopTrigger;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (detector == null)
            detector = GetComponentInParent<GoalDetector>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
            detector?.OnBallTrigger(isTopTrigger);
    }

    private void OnTriggerExit(Collider other)
    {
        // Si la pelota sale del trigger superior hacia arriba (rebote), reseteamos
        if (other.CompareTag("Ball") && isTopTrigger)
            detector?.OnBallExitTop();
    }
}
