using UnityEngine;

/// <summary>
/// Zona de power-up en el suelo.
/// Si el jugador está dentro al lanzar, el siguiente tiro vale el doble.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PowerUpZone : MonoBehaviour
{
    [SerializeField] private Color zoneColor = new Color(1f, 0.8f, 0f, 0.6f);

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetFloat("_Surface", 1);          // Transparent
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_SrcBlend", 5);
            mat.SetFloat("_DstBlend", 10);
            mat.SetFloat("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.color = zoneColor;
            rend.material = mat;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance?.ActivateDoublePoints();
            Debug.Log("[PowerUpZone] ¡Puntos dobles activados!");
        }
    }
}
