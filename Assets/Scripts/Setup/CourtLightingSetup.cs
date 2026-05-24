using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Configura la iluminación de la cancha de básquetbol.
/// Ejecutar desde: Tools > Court Setup > Apply Lighting
/// O bien: clic derecho sobre el componente → "Aplicar Iluminación"
/// </summary>
public class CourtLightingSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Court Setup/Apply Lighting")]
    private static void ApplyFromMenu()
    {
        CourtLightingSetup instance = FindFirstObjectByType<CourtLightingSetup>();
        if (instance == null)
        {
            GameObject go = new GameObject("CourtLightingSetup");
            instance = go.AddComponent<CourtLightingSetup>();
        }
        instance.AplicarIluminacion();
    }
#endif

    [ContextMenu("Aplicar Iluminación")]
    public void AplicarIluminacion()
    {
        ConfigurarAmbiente();
        ConfigurarLuzDireccional();
        CrearSpotlightsTecho();
        CrearLucesAro();

        Debug.Log("[CourtLightingSetup] Iluminación aplicada correctamente.");

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
#endif
    }

    // ────────────────────────────────────────────────────────────
    //  AMBIENTE
    // ────────────────────────────────────────────────────────────

    private void ConfigurarAmbiente()
    {
        // Luz ambiental color azul oscuro para interior de pabellón
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = HexColor("2A2A3A");
        RenderSettings.ambientIntensity = 0.5f;

        // Sin niebla (interior)
        RenderSettings.fog = false;
    }

    // ────────────────────────────────────────────────────────────
    //  LUZ DIRECCIONAL (sol de interior / relleno general)
    // ────────────────────────────────────────────────────────────

    private void ConfigurarLuzDireccional()
    {
        // Buscar luz direccional existente
        Light dirLight = null;
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional)
            {
                dirLight = l;
                break;
            }
        }

        if (dirLight == null)
        {
            GameObject go = new GameObject("Directional Light");
            dirLight = go.AddComponent<Light>();
            dirLight.type = LightType.Directional;
        }

        dirLight.color     = HexColor("FFF0D4");
        dirLight.intensity = 1.2f;
        dirLight.shadows   = LightShadows.Soft;
        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Sombras suaves de calidad media
        dirLight.shadowStrength        = 0.8f;
        dirLight.shadowBias            = 0.05f;
        dirLight.shadowNormalBias      = 0.4f;
        dirLight.shadowNearPlane       = 0.2f;
    }

    // ────────────────────────────────────────────────────────────
    //  SPOTLIGHTS DE TECHO  (4 focos principales)
    // ────────────────────────────────────────────────────────────

    private void CrearSpotlightsTecho()
    {
        // Limpiar grupo anterior si existe
        Transform viejo = transform.Find("SpotlightsTecho");
        if (viejo != null) DestroyImmediate(viejo.gameObject);

        GameObject grupo = new GameObject("SpotlightsTecho");
        grupo.transform.SetParent(transform);

        // Posiciones: 4 esquinas de la zona pintada + 4 traseras
        Vector3[] posiciones = new Vector3[]
        {
            new Vector3( 5f, 8f,  5f),
            new Vector3(-5f, 8f,  5f),
            new Vector3( 5f, 8f, -5f),
            new Vector3(-5f, 8f, -5f),
        };

        for (int i = 0; i < posiciones.Length; i++)
        {
            GameObject go = new GameObject($"Spotlight_{i + 1}");
            go.transform.SetParent(grupo.transform);
            go.transform.position = posiciones[i];

            // Apuntar al suelo (90° hacia abajo con leve inclinación hacia el centro)
            Vector3 dir = (Vector3.zero - posiciones[i]).normalized;
            go.transform.rotation = Quaternion.LookRotation(dir);

            Light spot = go.AddComponent<Light>();
            spot.type           = LightType.Spot;
            spot.color          = HexColor("FFE8C8");
            spot.intensity      = 30f;
            spot.range          = 15f;
            spot.spotAngle      = 80f;
            spot.innerSpotAngle = 60f;
            spot.shadows        = LightShadows.Soft;
            spot.shadowStrength = 0.6f;

            // Halo opcional (solo en Editor)
#if UNITY_EDITOR
            // No hay Halo en URP puro, dejamos así
#endif
        }
    }

    // ────────────────────────────────────────────────────────────
    //  LUCES CERCA DEL ARO  (point lights para iluminar la canasta)
    // ────────────────────────────────────────────────────────────

    private void CrearLucesAro()
    {
        // Limpiar grupo anterior si existe
        Transform viejo = transform.Find("LucesAro");
        if (viejo != null) DestroyImmediate(viejo.gameObject);

        GameObject grupo = new GameObject("LucesAro");
        grupo.transform.SetParent(transform);

        // Alturas y posiciones (aros en Z positivo y Z negativo aprox)
        Vector3[] posiciones = new Vector3[]
        {
            new Vector3(0f, 3.5f,  13f),   // aro lado positivo
            new Vector3(0f, 3.5f, -13f),   // aro lado negativo
        };

        for (int i = 0; i < posiciones.Length; i++)
        {
            GameObject go = new GameObject($"LuzAro_{i + 1}");
            go.transform.SetParent(grupo.transform);
            go.transform.position = posiciones[i];

            Light point = go.AddComponent<Light>();
            point.type      = LightType.Point;
            point.color     = Color.white;
            point.intensity = 15f;
            point.range     = 5f;
            point.shadows   = LightShadows.None; // point light shadows son caros
        }
    }

    // ────────────────────────────────────────────────────────────
    //  UTILIDADES
    // ────────────────────────────────────────────────────────────

    private static Color HexColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color c))
            return c;
        return Color.white;
    }
}
