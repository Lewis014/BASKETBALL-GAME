using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
// Post-processing overrides are available in URP via UnityEngine.Rendering.Universal
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// Configura el post-procesado URP de la cancha de básquetbol.
/// Ejecutar desde: Tools > Court Setup > Apply Post Processing
/// O bien: clic derecho sobre el componente → "Aplicar Post Processing"
///
/// Requiere:
///   - Universal Render Pipeline instalado
///   - "Post Processing" activado en la URP Asset
///   - Main Camera con "Post Processing" activado en su componente Camera
/// </summary>
public class CourtPostProcessing : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Court Setup/Apply Post Processing")]
    private static void ApplyFromMenu()
    {
        CourtPostProcessing instance = FindFirstObjectByType<CourtPostProcessing>();
        if (instance == null)
        {
            GameObject go = new GameObject("CourtPostProcessing");
            instance = go.AddComponent<CourtPostProcessing>();
        }
        instance.AplicarPostProcessing();
    }
#endif

    [ContextMenu("Aplicar Post Processing")]
    public void AplicarPostProcessing()
    {
        HabilitarPostProcessingEnCamara();
        CrearVolumenGlobal();

        Debug.Log("[CourtPostProcessing] Post-procesado aplicado correctamente.");

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
#endif
    }

    // ────────────────────────────────────────────────────────────
    //  CÁMARA: activar post processing
    // ────────────────────────────────────────────────────────────

    private void HabilitarPostProcessingEnCamara()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[CourtPostProcessing] No se encontró Main Camera.");
            return;
        }

#if UNITY_EDITOR
        // Activar "Post Processing" en el componente UniversalAdditionalCameraData
        var cameraData = cam.GetUniversalAdditionalCameraData();
        if (cameraData != null)
            cameraData.renderPostProcessing = true;
        else
            Debug.LogWarning("[CourtPostProcessing] No se encontró UniversalAdditionalCameraData en la cámara.");
#endif
    }

    // ────────────────────────────────────────────────────────────
    //  VOLUMEN GLOBAL
    // ────────────────────────────────────────────────────────────

    private void CrearVolumenGlobal()
    {
        // Limpiar volumen anterior si existe
        Transform viejo = null;
        foreach (Transform child in transform)
        {
            if (child.name == "GlobalPostProcessVolume")
            {
                viejo = child;
                break;
            }
        }
        if (viejo != null) DestroyImmediate(viejo.gameObject);

        // También buscar en escena por si fue creado antes sin padre
        Volume volumenExistente = null;
        foreach (Volume v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (v.name == "GlobalPostProcessVolume")
            {
                volumenExistente = v;
                break;
            }
        }
        if (volumenExistente != null)
            DestroyImmediate(volumenExistente.gameObject);

        // Crear nuevo GameObject con Volume
        GameObject go = new GameObject("GlobalPostProcessVolume");
        go.transform.SetParent(transform);

        Volume volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;

        // Crear el perfil en memoria (sin asset en disco para simplificar)
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "CourtPostProcessProfile";

#if UNITY_EDITOR
        ConfigurarEfectos(profile);
#endif

        volume.profile = profile;

#if UNITY_EDITOR
        // Guardar el asset del perfil para que persista
        string carpeta = "Assets/Settings";
        if (!System.IO.Directory.Exists(carpeta))
            System.IO.Directory.CreateDirectory(carpeta);

        string ruta = carpeta + "/CourtPostProcessProfile.asset";
        AssetDatabase.CreateAsset(profile, ruta);
        AssetDatabase.SaveAssets();
        volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
        volume.profile       = null; // usar sharedProfile

        Debug.Log($"[CourtPostProcessing] Perfil guardado en {ruta}");
#endif
    }

    // ────────────────────────────────────────────────────────────
    //  CONFIGURAR EFECTOS  (solo en Editor, necesitan URP types)
    // ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void ConfigurarEfectos(VolumeProfile profile)
    {
        // ── Tonemapping ───────────────────────────────────────────
        if (profile.TryGet<Tonemapping>(out var tonemapping) == false)
            tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.overrideState = true;
        tonemapping.mode.value         = TonemappingMode.ACES;

        // ── Bloom ────────────────────────────────────────────────
        if (profile.TryGet<Bloom>(out var bloom) == false)
            bloom = profile.Add<Bloom>(true);
        bloom.threshold.overrideState  = true;
        bloom.threshold.value          = 1.2f;
        bloom.intensity.overrideState  = true;
        bloom.intensity.value          = 0.3f;
        bloom.scatter.overrideState    = true;
        bloom.scatter.value            = 0.6f;

        // ── Vignette ─────────────────────────────────────────────
        if (profile.TryGet<Vignette>(out var vignette) == false)
            vignette = profile.Add<Vignette>(true);
        vignette.intensity.overrideState  = true;
        vignette.intensity.value          = 0.25f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value         = 0.4f;
        vignette.rounded.overrideState    = true;
        vignette.rounded.value            = true;

        // ── Color Adjustments ─────────────────────────────────────
        if (profile.TryGet<ColorAdjustments>(out var colorAdj) == false)
            colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.postExposure.overrideState  = true;
        colorAdj.postExposure.value          = 0.1f;
        colorAdj.contrast.overrideState      = true;
        colorAdj.contrast.value              = 10f;
        colorAdj.saturation.overrideState    = true;
        colorAdj.saturation.value            = 5f;

        // ── Film Grain ────────────────────────────────────────────
        if (profile.TryGet<FilmGrain>(out var grain) == false)
            grain = profile.Add<FilmGrain>(true);
        grain.intensity.overrideState = true;
        grain.intensity.value         = 0.1f;
        grain.type.overrideState      = true;
        grain.type.value              = FilmGrainLookup.Thin1;
    }
#endif
}
