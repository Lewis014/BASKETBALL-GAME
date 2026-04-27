using UnityEditor;
using UnityEngine;

/// <summary>
/// Configura automáticamente el Player con el modelo Kairo The Boxer.
/// Menú: Tools > Basketball > Setup Player with Kairo
/// </summary>
public static class PlayerSetup
{
    private const string KairoPrefabGuid      = "00ec558ebb92eb546a524d1b7ef34ada";
    private const string KairoControllerGuid = "ae7171096016a024bb7560425491fdb5";

    [MenuItem("Tools/Basketball/Setup Player with Kairo")]
    public static void SetupPlayer()
    {
        // 1. Buscar el Player en la escena
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[PlayerSetup] No se encontró el objeto 'Player' en la escena. " +
                           "Abre BasketballScene primero.");
            return;
        }

        // 2. Evitar duplicados
        Transform existing = player.transform.Find("Kairo The Boxer");
        if (existing != null)
        {
            Debug.LogWarning("[PlayerSetup] Kairo ya está configurado en el Player.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // 3. Cargar el prefab de Kairo
        string path = AssetDatabase.GUIDToAssetPath(KairoPrefabGuid);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[PlayerSetup] No se encontró el prefab de Kairo. " +
                           "Verifica que esté importado.");
            return;
        }

        GameObject kairoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (kairoPrefab == null)
        {
            Debug.LogError($"[PlayerSetup] No se pudo cargar el prefab en: {path}");
            return;
        }

        // 4. Instanciar Kairo como hijo del Player
        GameObject kairo = (GameObject)PrefabUtility.InstantiatePrefab(kairoPrefab, player.transform);
        kairo.transform.localPosition = new Vector3(0f, -1f, 0f); // centrar respecto al CharacterController
        kairo.transform.localRotation = Quaternion.identity;
        kairo.transform.localScale    = Vector3.one;

        // 5. Ocultar la malla de la cápsula (mantener CharacterController)
        MeshRenderer capsuleRenderer = player.GetComponent<MeshRenderer>();
        if (capsuleRenderer != null)
        {
            capsuleRenderer.enabled = false;
            Debug.Log("[PlayerSetup] MeshRenderer de la cápsula desactivado.");
        }

        // 6. Asignar KairoController al Animator de Kairo
        Animator animator = kairo.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            string controllerPath = AssetDatabase.GUIDToAssetPath(KairoControllerGuid);
            if (!string.IsNullOrEmpty(controllerPath))
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    Debug.Log("[PlayerSetup] KairoController asignado al Animator.");
                }
            }
        }
        else
        {
            Debug.LogWarning("[PlayerSetup] No se encontró Animator en Kairo.");
        }

        // 7. Registrar cambios para que Unity pueda guardar la escena
        Undo.RegisterCreatedObjectUndo(kairo, "Setup Player with Kairo");
        EditorUtility.SetDirty(player);

        // 7. Seleccionar Kairo en el editor
        Selection.activeGameObject = kairo;

        Debug.Log("[PlayerSetup] ✅ Kairo The Boxer integrado al Player correctamente.");
        Debug.Log($"[PlayerSetup] Prefab cargado desde: {path}");
    }

    [MenuItem("Tools/Basketball/Setup Player with Kairo", validate = true)]
    public static bool SetupPlayerValidate()
    {
        // Solo habilitar si hay una escena abierta
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().isLoaded;
    }
}
