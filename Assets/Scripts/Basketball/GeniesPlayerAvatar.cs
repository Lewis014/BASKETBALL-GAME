using System;
using Cysharp.Threading.Tasks;
using Genies.Sdk;
using UnityEngine;

/// <summary>
/// Carga el avatar Genies del usuario como modelo visual del jugador.
///
/// Si el usuario NO está logueado en Genies, se carga un avatar por defecto
/// automáticamente.
///
/// El movimiento sigue siendo controlado por BasketballPlayer.cs —
/// este script solo gestiona el aspecto visual.
/// </summary>
public class GeniesPlayerAvatar : MonoBehaviour
{
    [Header("Avatar")]
    [Tooltip("AnimatorController para aplicar al avatar Genies (puede ser KairoController o uno propio)")]
    [SerializeField] private RuntimeAnimatorController animatorController;

    [Tooltip("Offset local de posición del avatar dentro del jugador")]
    [SerializeField] private Vector3 avatarOffset = new Vector3(0f, -1f, 0f);

    [Header("Estado (solo lectura)")]
    [SerializeField] private bool _avatarLoaded;

    private ManagedAvatar _managedAvatar;

    // ── Ciclo de vida ────────────────────────────────────────────────

    private async void Start()
    {
        // 1. Ocultar cápsula visual del CharacterController
        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        // 2. Desactivar el avatar anterior (Kairo) si existe como hijo
        DisablePreviousAvatar();

        // 3. Cargar avatar Genies
        try
        {
            await LoadAvatarAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GeniesPlayerAvatar] Error cargando avatar: {ex.Message}\n{ex.StackTrace}", this);
        }
    }

    private void OnDestroy()
    {
        _managedAvatar?.Dispose();
    }

    // ── Carga del avatar ─────────────────────────────────────────────

    private async UniTask LoadAvatarAsync()
    {
        Debug.Log("[GeniesPlayerAvatar] Iniciando carga de avatar Genies...");

        // LoadAvatarOptions.User: carga el avatar del usuario logueado.
        // Si no hay sesión activa, el SDK carga automáticamente un avatar por defecto.
        _managedAvatar = await AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.User
        {
            AvatarName  = "Basketball Player",
            Parent      = transform,
            PlayerAnimationController = animatorController,
        });

        if (_managedAvatar == null)
        {
            Debug.LogError("[GeniesPlayerAvatar] LoadAvatarAsync devolvió null.", this);
            return;
        }

        if (_managedAvatar.Root == null)
        {
            Debug.LogError("[GeniesPlayerAvatar] El avatar cargado no tiene Root.", this);
            return;
        }

        // Ajustar posición local del avatar dentro del jugador
        _managedAvatar.Root.transform.localPosition = avatarOffset;
        _managedAvatar.Root.transform.localRotation = Quaternion.identity;

        _avatarLoaded = true;
        Debug.Log("[GeniesPlayerAvatar] Avatar Genies cargado correctamente.");
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Desactiva cualquier avatar previo (ej. Kairo) que sea hijo de este GameObject.
    /// </summary>
    private void DisablePreviousAvatar()
    {
        foreach (Transform child in transform)
        {
            // Desactivar hijos que no sean el ShotPoint ni la trayectoria
            string n = child.gameObject.name;
            if (n != "ShotPoint" && n != "TrajectoryLine")
            {
                child.gameObject.SetActive(false);
                Debug.Log($"[GeniesPlayerAvatar] Avatar previo desactivado: {n}");
            }
        }
    }
}
