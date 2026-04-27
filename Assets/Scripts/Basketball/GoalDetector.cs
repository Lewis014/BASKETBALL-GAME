using UnityEngine;

/// <summary>
/// Detecta canastas válidas.
/// Coloca este script en el objeto padre del aro.
/// Los hijos TriggerSuperior y TriggerInferior tienen GoalTrigger.
/// </summary>
public class GoalDetector : MonoBehaviour
{
    [SerializeField] private AudioSource goalSound;
    [SerializeField] private int pointsPerBasket = 2;

    private bool _passedTop;

    /// <summary>Llamado por GoalTrigger cuando una pelota entra en un trigger.</summary>
    public void OnBallTrigger(bool isTop)
    {
        if (isTop)
        {
            _passedTop = true;
        }
        else if (_passedTop)
        {
            _passedTop = false;
            GameManager.Instance?.AddScore(pointsPerBasket);
            goalSound?.Play();
            Debug.Log("¡CANASTA!");
        }
        else
        {
            // Pasó por abajo primero: no es válido (rebote)
            _passedTop = false;
        }
    }
}
