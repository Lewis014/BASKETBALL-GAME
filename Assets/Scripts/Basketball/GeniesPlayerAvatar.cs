using UnityEngine;

/// <summary>
/// Gestiona el avatar visual del jugador.
///
/// El paquete Genies Avatar SDK fue removido, por lo que este script
/// solo oculta el mesh de la cápsula y deja activos los hijos visuales.
/// El movimiento y disparo siguen controlados por BasketballPlayer.cs.
/// </summary>
public class GeniesPlayerAvatar : MonoBehaviour
{
    [Header("Avatar")]
    [Tooltip("Offset local de posición del avatar dentro del jugador")]
    [SerializeField] private Vector3 avatarOffset = new Vector3(0f, -1f, 0f);

    [Tooltip("Nombre del hijo que representa el modelo visual del jugador (dejar vacío para ignorar)")]
    [SerializeField] private string nombreModeloVisual = "";

    private void Start()
    {
        // Ocultar MeshRenderer propio de la cápsula si existe
        var mr = GetComponent<MeshRenderer>();
        if (mr != null)
            mr.enabled = false;

        // Si hay un modelo visual asignado por nombre, posicionarlo correctamente
        if (!string.IsNullOrEmpty(nombreModeloVisual))
        {
            Transform modelo = transform.Find(nombreModeloVisual);
            if (modelo != null)
            {
                modelo.localPosition = avatarOffset;
                modelo.localRotation = Quaternion.identity;
                modelo.gameObject.SetActive(true);
                Debug.Log($"[GeniesPlayerAvatar] Modelo visual activado: {nombreModeloVisual}");
            }
            else
            {
                Debug.LogWarning($"[GeniesPlayerAvatar] No se encontró hijo '{nombreModeloVisual}'.", this);
            }
        }
    }
}
