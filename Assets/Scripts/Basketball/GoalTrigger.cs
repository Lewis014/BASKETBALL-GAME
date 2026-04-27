using UnityEngine;

/// <summary>
/// Coloca este script en TriggerSuperior e TriggerInferior.
/// Notifica al GoalDetector padre cuando la pelota lo atraviesa.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoalTrigger : MonoBehaviour
{
    [Tooltip("Referencia al GoalDetector del objeto padre del aro")]
    [SerializeField] private GoalDetector detector;

    [Tooltip("Marcar true en TriggerSuperior, false en TriggerInferior")]
    [SerializeField] private bool isTopTrigger;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        // Auto-buscar en el padre si no se asignó
        if (detector == null)
            detector = GetComponentInParent<GoalDetector>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
            detector?.OnBallTrigger(isTopTrigger);
    }
}
