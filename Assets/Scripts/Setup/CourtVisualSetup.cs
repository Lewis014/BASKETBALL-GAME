using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Mejora visual completa de la cancha de básquetbol.
/// Crea materiales procedurales y props sin necesidad de assets externos.
///
/// Uso:  Tools > Court Setup > Apply Visuals
///       O adjuntar a cualquier GameObject y usar el botón en el Inspector.
/// </summary>
public class CourtVisualSetup : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ════════════════════════════════════════════════════════════════════

    [Header("─── Colores de la cancha ──────────────────────────────")]
    [SerializeField] private Color colorPisoClaro  = Hex("C4873B");
    [SerializeField] private Color colorPisoOscuro = Hex("A0672B");
    [SerializeField] private Color colorPared      = Hex("3A3A3A");
    [SerializeField] private Color colorEquipo     = Hex("1E3A5F");
    [SerializeField] private Color colorMetal      = new Color(0.78f, 0.78f, 0.82f, 1f);
    [SerializeField] private Color colorGradasA    = Hex("1E3A5F"); // sección local (azul)
    [SerializeField] private Color colorGradasB    = Hex("8B1A1A"); // sección visita (rojo)
    [SerializeField] private Color colorBanca      = Hex("5C3A1E");

    [Header("─── Props ────────────────────────────────────────────")]
    [Tooltip("Número de filas de gradas")]
    [SerializeField] private int filasGradas = 7;
    [Tooltip("Altura de cada escalón (metros)")]
    [SerializeField] private float alturaFilaGradas = 0.52f;
    [Tooltip("Profundidad de cada fila (asiento + espacio, metros)")]
    [SerializeField] private float profundidadFila  = 0.72f;
    [Tooltip("Separación entre asientos individuales (metros)")]
    [SerializeField] private float anchoAsiento = 0.55f;

    // ════════════════════════════════════════════════════════════════════
    //  PUNTO DE ENTRADA
    // ════════════════════════════════════════════════════════════════════

    [Header("─── Paredes ──────────────────────────────────────────")]
    [Tooltip("Altura de las barreras bajas que bordean el campo (metros)")]
    [SerializeField] private float alturaBarreraCancha = 0.4f;
    [Tooltip("Altura de las paredes exteriores del estadio, detrás de las gradas")]
    [SerializeField] private float alturaParedEstadio = 8f;

    [ContextMenu("Aplicar Visuales de Cancha")]
    public void AplicarVisuales()
    {
        AplicarMateriales();
        AjustarParedes();       // ← convierte muros en barreras + crea paredes de estadio
        CrearGradas();
        CrearBancas();
        CrearMarcador();
        CrearLogoCentral();
        CrearParticulasPolvo();
        Debug.Log("[CourtVisualSetup] ✓ Visuales aplicados correctamente.");
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Court Setup/Apply Visuals")]
    private static void MenuAplicarVisuales()
    {
        var setup = FindFirstObjectByType<CourtVisualSetup>();
        if (setup == null)
        {
            var go = new GameObject("_CourtVisualSetup");
            setup = go.AddComponent<CourtVisualSetup>();
        }
        setup.AplicarVisuales();
        EditorUtility.SetDirty(setup.gameObject);
    }
#endif

    // ════════════════════════════════════════════════════════════════════
    //  MATERIALES
    // ════════════════════════════════════════════════════════════════════

    private void AplicarMateriales()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[CourtVisualSetup] No se encontró el shader URP/Lit. ¿Está configurado URP?");
            return;
        }

        // ── Piso ─────────────────────────────────────────────────────────
        var piso = EncontrarPiso();
        if (piso != null)
        {
            var mr = piso.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(urpLit) { name = "Parquet" };
                mat.SetColor("_BaseColor", colorPisoClaro);
                mat.SetTexture("_BaseMap", CrearTexturaParquet());
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Smoothness", 0.7f);
                // Escalado UV: 1 repetición cada 1m aprox (piso 20x30m)
                mat.SetTextureScale("_BaseMap", new Vector2(12f, 18f));
                mr.sharedMaterial = mat;
            }
        }

        // ── Paredes ───────────────────────────────────────────────────────
        var matPared = new Material(urpLit) { name = "Pared Concreto" };
        matPared.SetColor("_BaseColor", colorPared);
        matPared.SetTexture("_BaseMap", CrearTexturaPared());
        matPared.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
        matPared.SetFloat("_Metallic", 0f);
        matPared.SetFloat("_Smoothness", 0.3f);

        foreach (var nombre in new[] { "ParedNorte", "ParedSur", "ParedEste", "ParedOeste" })
        {
            var go = GameObject.Find(nombre);
            var mr = go?.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = matPared;
        }

        // ── Metal (poste, brazo, aro) ─────────────────────────────────────
        var matMetal = new Material(urpLit) { name = "Metal Plateado" };
        matMetal.SetColor("_BaseColor", colorMetal);
        matMetal.SetFloat("_Metallic", 0.8f);
        matMetal.SetFloat("_Smoothness", 0.6f);

        foreach (var nombre in new[] { "Poste", "Brazo", "Rim" })
        {
            var go = GameObject.Find(nombre);
            var mr = go?.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = matMetal;
        }

        // ── Tablero (vidrio acrílico) ─────────────────────────────────────
        var goTablero = GameObject.Find("BackBoard");
        if (goTablero != null)
        {
            var mr = goTablero.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = CrearMaterialVidrio(urpLit);
        }

        // ── Red/Net ───────────────────────────────────────────────────────
        var goRed = GameObject.Find("Red");
        if (goRed == null) goRed = GameObject.Find("Net");
        if (goRed != null)
        {
            var mr = goRed.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = CrearMaterialTransparente(urpLit, new Color(1f, 1f, 1f, 0.3f));
                mat.name = "Red Net";
                mr.sharedMaterial = mat;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  TEXTURAS PROCEDURALES
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Genera textura de parquet con franjas alternadas de madera.</summary>
    private Texture2D CrearTexturaParquet()
    {
        const int W = 512, H = 512;
        const int ANCHO_PLANCHA = 64;   // 8 planchas horizontales
        const int OFFSET_UNION  = 128;  // desplazamiento vertical entre grupos

        var tex = new Texture2D(W, H, TextureFormat.RGB24, true);
        var pixels = new Color[W * H];

        Color linea = colorPisoOscuro * 0.55f;
        linea.a = 1f;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                int plancha    = x / ANCHO_PLANCHA;
                int offsetY    = (plancha % 2 == 0) ? 0 : OFFSET_UNION / 2;
                int pixelLocal = (y + offsetY) % OFFSET_UNION;

                bool esLinea = (x % ANCHO_PLANCHA <= 1) || (pixelLocal <= 1);
                if (esLinea)
                    pixels[y * W + x] = linea;
                else
                    pixels[y * W + x] = (plancha % 2 == 0) ? colorPisoClaro : colorPisoOscuro;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.name = "Parquet_Procedural";
        return tex;
    }

    /// <summary>
    /// Genera textura de pared con franja horizontal de color equipo.
    /// La franja ocupa entre el 35% y 55% de la altura.
    /// </summary>
    private Texture2D CrearTexturaPared()
    {
        const int W = 4, H = 128;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        var pixels = new Color[W * H];

        for (int y = 0; y < H; y++)
        {
            float pct = y / (float)H;
            Color c = (pct >= 0.35f && pct <= 0.55f) ? colorEquipo : colorPared;
            for (int x = 0; x < W; x++)
                pixels[y * W + x] = c;
        }

        tex.SetPixels(pixels);
        tex.Apply(false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;
        tex.name = "Pared_Procedural";
        return tex;
    }

    /// <summary>Genera textura de círculo para el logo central del piso.</summary>
    private Texture2D CrearTexturaLogo()
    {
        const int W = 256, H = 256;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, true);
        var pixels = new Color[W * H];

        Vector2 centro = new Vector2(W * 0.5f, H * 0.5f);
        float radioExt = W * 0.46f;
        float radioInt = W * 0.38f;
        float radioFondo = W * 0.35f;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), centro);

                Color c;
                if (dist > radioExt)
                    c = Color.clear;                          // fuera del círculo
                else if (dist > radioInt)
                    c = Color.white;                          // borde blanco
                else if (dist > radioFondo)
                    c = colorEquipo;                          // anillo equipo
                else
                    c = new Color(colorEquipo.r * 0.7f,       // interior más oscuro
                                  colorEquipo.g * 0.7f,
                                  colorEquipo.b * 0.7f, 1f);

                pixels[y * W + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.name = "Logo_Procedural";
        return tex;
    }

    // ════════════════════════════════════════════════════════════════════
    //  CREACIÓN DE MATERIALES
    // ════════════════════════════════════════════════════════════════════

    private Material CrearMaterialVidrio(Shader shader)
    {
        var mat = new Material(shader) { name = "Vidrio Acrílico" };
        ConfigurarTransparente(mat, new Color(1f, 1f, 1f, 0.85f));
        mat.SetFloat("_Metallic", 0.1f);
        mat.SetFloat("_Smoothness", 0.92f);
        return mat;
    }

    private Material CrearMaterialTransparente(Shader shader, Color color)
    {
        var mat = new Material(shader);
        ConfigurarTransparente(mat, color);
        return mat;
    }

    /// <summary>Configura un material URP/Lit para ser transparente (Alpha Blend).</summary>
    private static void ConfigurarTransparente(Material mat, Color color)
    {
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Surface", 1f);           // Transparent
        mat.SetFloat("_Blend",   0f);           // Alpha
        mat.SetFloat("_SrcBlend", 5f);          // SrcAlpha
        mat.SetFloat("_DstBlend", 10f);         // OneMinusSrcAlpha
        mat.SetFloat("_ZWrite",   0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    // ════════════════════════════════════════════════════════════════════
    //  PROPS PROCEDURALES
    // ════════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════════
    //  PAREDES Y BARRERAS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Transforma las paredes existentes en barreras bajas del campo
    /// y crea muros altos de estadio DETRÁS de las gradas, para que
    /// la cámara nunca quede bloqueada.
    ///
    /// Layout de sección transversal (vista desde arriba de x):
    ///
    ///   [CANCHA] | barrera(0.4m) | gradas | muro estadio(8m)
    ///       ←court→  ←barrier→   ←bleach→  ←wall→
    /// </summary>
    private void AjustarParedes()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");

        // Material para barreras delimitadoras del campo
        var matBarrera = new Material(lit) { name = "Barrera Campo" };
        matBarrera.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.9f));
        matBarrera.SetFloat("_Metallic",   0.5f);
        matBarrera.SetFloat("_Smoothness", 0.7f);

        // Material para paredes exteriores del estadio
        var matEstadio = new Material(lit) { name = "Pared Estadio" };
        matEstadio.SetColor("_BaseColor", colorPared);
        matEstadio.SetTexture("_BaseMap", CrearTexturaPared());
        matEstadio.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
        matEstadio.SetFloat("_Smoothness", 0.2f);
        // Franja de color del equipo en las paredes del estadio
        matEstadio.EnableKeyword("_EMISSION");
        matEstadio.SetColor("_EmissionColor", colorEquipo * 0.15f);

        // ── Paredes existentes → barreras bajas ───────────────────────
        string[] nombresMuros = { "ParedNorte", "ParedSur", "ParedEste", "ParedOeste" };
        foreach (string nombre in nombresMuros)
        {
            var go = GameObject.Find(nombre);
            if (go == null) continue;

            // Conservar escala X/Z (ancho del muro) pero aplanar a alturaBarreraCancha
            Vector3 s = go.transform.localScale;
            go.transform.localScale = new Vector3(s.x, alturaBarreraCancha, s.z);

            // Re-centrar verticalmente (el pivot suele estar en el centro del cubo)
            Vector3 p = go.transform.position;
            go.transform.position = new Vector3(p.x, alturaBarreraCancha * 0.5f, p.z);

            // Aplicar material de barrera metálica
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = matBarrera;
        }

        // ── Paredes exteriores del estadio (detrás de las gradas) ─────
        const string PAREDES_NOMBRE = "ParedesEstadio";
        DestruirSiExiste(PAREDES_NOMBRE);
        var paredesParent = new GameObject(PAREDES_NOMBRE);

        float gradaProf    = filasGradas * profundidadFila;    // profundidad total de las gradas
        float margen       = 0.8f;                              // espacio entre gradas y muro

        // Muro Este y Oeste (a lo largo de Z)
        float xMuro       = 11.5f + gradaProf + margen;
        float largoZmuro  = 26f + (gradaProf + margen) * 2f + 2f;  // cubre también esquinas
        float yMuroCentro = alturaParedEstadio * 0.5f;

        CrearMuroEstadio(paredesParent, matEstadio,
            pos:   new Vector3( xMuro, yMuroCentro, 0f),
            escala: new Vector3(0.3f, alturaParedEstadio, largoZmuro),
            nombre: "MuroEste");

        CrearMuroEstadio(paredesParent, matEstadio,
            pos:   new Vector3(-xMuro, yMuroCentro, 0f),
            escala: new Vector3(0.3f, alturaParedEstadio, largoZmuro),
            nombre: "MuroOeste");

        // Muro Norte y Sur (a lo largo de X)
        float zMuro       = 15f + gradaProf + margen;
        float largoXmuro  = largoZmuro;

        CrearMuroEstadio(paredesParent, matEstadio,
            pos:   new Vector3(0f, yMuroCentro,  zMuro),
            escala: new Vector3(largoXmuro, alturaParedEstadio, 0.3f),
            nombre: "MuroNorte");

        CrearMuroEstadio(paredesParent, matEstadio,
            pos:   new Vector3(0f, yMuroCentro, -zMuro),
            escala: new Vector3(largoXmuro, alturaParedEstadio, 0.3f),
            nombre: "MuroSur");

        // ── Techo del estadio (opcional, da sensación de recinto cerrado) ─
        CrearTechoEstadio(paredesParent, lit, xMuro, zMuro);

        Debug.Log("[CourtVisualSetup] Paredes ajustadas. Barreras de campo + muros de estadio creados.");
    }

    private static void CrearMuroEstadio(GameObject padre, Material mat,
        Vector3 pos, Vector3 escala, string nombre)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre.transform);
        go.transform.position   = pos;
        go.transform.localScale = escala;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // Asignar capa CameraObstacle (layer 8) si existe, para que
        // CameraCollisionHandler haga el anti-colisión correctamente
        int capaObstaculo = LayerMask.NameToLayer("CameraObstacle");
        if (capaObstaculo >= 0)
            go.layer = capaObstaculo;
    }

    /// <summary>
    /// Crea un techo plano oscuro sobre las gradas para cerrar visualmente el estadio.
    /// Tiene agujero en el centro (quad con clip alpha) → simulado con 4 piezas de techo.
    /// </summary>
    private void CrearTechoEstadio(GameObject padre, Shader lit, float xMuro, float zMuro)
    {
        var matTecho = new Material(lit) { name = "Techo Estadio" };
        matTecho.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.12f));
        matTecho.SetFloat("_Smoothness", 0.1f);

        float yTecho  = alturaParedEstadio;
        float grosor  = 0.3f;

        // Área abierta sobre la cancha (apertura de techo): x = ±11, z = ±14
        float aberturaX = 11f;
        float aberturaZ = 14.5f;

        // Panel Norte del techo
        float anchoN = xMuro - aberturaX;
        CrearMuroEstadio(padre, matTecho,
            pos:   new Vector3((aberturaX + xMuro) * 0.5f, yTecho, 0f),
            escala: new Vector3(anchoN, grosor, zMuro * 2f),
            nombre: "TechoEste");

        // Panel Oeste del techo
        CrearMuroEstadio(padre, matTecho,
            pos:   new Vector3(-(aberturaX + xMuro) * 0.5f, yTecho, 0f),
            escala: new Vector3(anchoN, grosor, zMuro * 2f),
            nombre: "TechoOeste");

        // Panel Norte del techo (sobre las gradas traseras)
        float anchoZ = zMuro - aberturaZ;
        CrearMuroEstadio(padre, matTecho,
            pos:   new Vector3(0f, yTecho, (aberturaZ + zMuro) * 0.5f),
            escala: new Vector3(aberturaX * 2f, grosor, anchoZ),
            nombre: "TechoNorte");

        // Panel Sur del techo
        CrearMuroEstadio(padre, matTecho,
            pos:   new Vector3(0f, yTecho, -(aberturaZ + zMuro) * 0.5f),
            escala: new Vector3(aberturaX * 2f, grosor, anchoZ),
            nombre: "TechoSur");
    }

    /// <summary>
    /// Genera gradas escalonadas realistas a ambos lados largos de la cancha
    /// y detrás de cada aro. Cada fila tiene:
    ///   - Riser  (panel vertical que forma el escalón)
    ///   - Tread  (superficie horizontal del escalón)
    ///   - Asientos individuales coloreados por sección
    ///   - Estructura de soporte visible
    /// </summary>
    private void CrearGradas()
    {
        const string PADRE_NOMBRE = "Gradas";
        DestruirSiExiste(PADRE_NOMBRE);

        var padre  = new GameObject(PADRE_NOMBRE);
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");

        // ── Crear un material por color de sección ────────────────────
        Material MatAsiento(Color c, float smooth = 0.35f)
        {
            var m = new Material(lit);
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0.05f);
            return m;
        }

        // Colores
        Color cRiser    = new Color(0.18f, 0.18f, 0.22f); // gris oscuro estructural
        Color cEstruc   = new Color(0.12f, 0.12f, 0.15f); // gris muy oscuro base
        Color cSecA     = Hex("1E3A5F"); // sección azul (local)
        Color cSecB     = Hex("8B1A1A"); // sección roja (visita)
        Color cSecC     = Hex("2E5E2E"); // sección verde (neutral)
        Color cSecVip   = Hex("4A3000"); // sección VIP (marrón oscuro)

        Material matRiser  = MatAsiento(cRiser,  0.2f);
        Material matEstruc = MatAsiento(cEstruc, 0.1f);
        Material matSecA   = MatAsiento(cSecA,   0.4f);
        Material matSecB   = MatAsiento(cSecB,   0.4f);
        Material matSecC   = MatAsiento(cSecC,   0.4f);
        Material matSecVip = MatAsiento(cSecVip, 0.5f);

        // ── Parámetros generales ──────────────────────────────────────
        float riserGrosor  = 0.12f;           // grosor del panel vertical
        float treadGrosor  = 0.10f;           // grosor del asiento horizontal
        float seatAltura   = 0.28f;           // altura del respaldo del asiento
        float seatGrosor   = 0.06f;           // grosor del respaldo

        // ── Posiciones de cada bloque de gradas ──────────────────────
        //
        //  ESTE  (x+): long side, sección local
        //  OESTE (x-): long side, sección visita
        //  NORTE (z+): detrás del aro norte, zona neutral/VIP
        //  SUR   (z-): detrás del aro sur, zona neutral

        CrearBloqueGradas(padre, lit,
            origen:        new Vector3(11.5f, 0f, 0f),
            dirProf:       Vector3.right,
            largoU:        Vector3.forward,
            largoTotal:    26f,
            matRiser:      matRiser,
            matEstruc:     matEstruc,
            matAsiento:    matSecA,
            riserGrosor:   riserGrosor,
            treadGrosor:   treadGrosor,
            seatAltura:    seatAltura,
            seatGrosor:    seatGrosor,
            nombre:        "Gradas_Este",
            voltear:       false);

        CrearBloqueGradas(padre, lit,
            origen:        new Vector3(-11.5f, 0f, 0f),
            dirProf:       Vector3.left,
            largoU:        Vector3.forward,
            largoTotal:    26f,
            matRiser:      matRiser,
            matEstruc:     matEstruc,
            matAsiento:    matSecB,
            riserGrosor:   riserGrosor,
            treadGrosor:   treadGrosor,
            seatAltura:    seatAltura,
            seatGrosor:    seatGrosor,
            nombre:        "Gradas_Oeste",
            voltear:       true);

        CrearBloqueGradas(padre, lit,
            origen:        new Vector3(0f, 0f, 15f),
            dirProf:       Vector3.forward,
            largoU:        Vector3.right,
            largoTotal:    16f,
            matRiser:      matRiser,
            matEstruc:     matEstruc,
            matAsiento:    matSecC,
            riserGrosor:   riserGrosor,
            treadGrosor:   treadGrosor,
            seatAltura:    seatAltura,
            seatGrosor:    seatGrosor,
            nombre:        "Gradas_Norte",
            voltear:       false);

        CrearBloqueGradas(padre, lit,
            origen:        new Vector3(0f, 0f, -15f),
            dirProf:       Vector3.back,
            largoU:        Vector3.right,
            largoTotal:    16f,
            matRiser:      matRiser,
            matEstruc:     matEstruc,
            matAsiento:    matSecC,
            riserGrosor:   riserGrosor,
            treadGrosor:   treadGrosor,
            seatAltura:    seatAltura,
            seatGrosor:    seatGrosor,
            nombre:        "Gradas_Sur",
            voltear:       true);

        Debug.Log($"[CourtVisualSetup] Gradas creadas ({filasGradas} filas x 4 lados).");
    }

    /// <summary>
    /// Construye un bloque de gradas con dirección y orientación arbitrarias.
    /// </summary>
    private void CrearBloqueGradas(
        GameObject padre,
        Shader     lit,
        Vector3    origen,
        Vector3    dirProf,        // dirección que se aleja de la cancha
        Vector3    largoU,         // dirección a lo largo de la tribuna
        float      largoTotal,
        Material   matRiser,
        Material   matEstruc,
        Material   matAsiento,
        float      riserGrosor,
        float      treadGrosor,
        float      seatAltura,
        float      seatGrosor,
        string     nombre,
        bool       voltear)
    {
        var bloque = new GameObject(nombre);
        bloque.transform.SetParent(padre.transform);

        // ── Base de concreto (talud de soporte) ───────────────────────
        {
            float baseAlto = filasGradas * alturaFilaGradas * 0.5f;
            float baseProf = filasGradas * profundidadFila;
            Vector3 centro = origen
                + dirProf  * (baseProf * 0.5f + 0.05f)
                + Vector3.up * (baseAlto * 0.5f);

            Vector3 escala = EscalaPorDireccion(dirProf, largoU, baseProf + 0.1f, baseAlto, largoTotal);

            var baseConc = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseConc.name = "BaseConcreto";
            baseConc.transform.SetParent(bloque.transform);
            baseConc.transform.position   = centro;
            baseConc.transform.localScale = escala;
            baseConc.GetComponent<MeshRenderer>().sharedMaterial = matEstruc;
        }

        // ── Filas de gradas ───────────────────────────────────────────
        for (int fila = 0; fila < filasGradas; fila++)
        {
            float profOffset = fila * profundidadFila;
            float altoBase   = fila * alturaFilaGradas;

            // Centro del riser (panel vertical del escalón)
            Vector3 posRiser = origen
                + dirProf  * (profOffset + riserGrosor * 0.5f)
                + Vector3.up * (altoBase + alturaFilaGradas * 0.5f);

            Vector3 escRiser = EscalaPorDireccion(dirProf, largoU,
                riserGrosor, alturaFilaGradas, largoTotal);

            // Centro del tread (superficie horizontal)
            Vector3 posTread = origen
                + dirProf  * (profOffset + riserGrosor + (profundidadFila - riserGrosor) * 0.5f)
                + Vector3.up * (altoBase + alturaFilaGradas - treadGrosor * 0.5f);

            Vector3 escTread = EscalaPorDireccion(dirProf, largoU,
                profundidadFila - riserGrosor, treadGrosor, largoTotal);

            // Riser
            var riser = GameObject.CreatePrimitive(PrimitiveType.Cube);
            riser.name = $"Riser_F{fila}";
            riser.transform.SetParent(bloque.transform);
            riser.transform.position   = posRiser;
            riser.transform.localScale = escRiser;
            riser.GetComponent<MeshRenderer>().sharedMaterial = matRiser;
            OrientarPorDireccion(riser.transform, dirProf, largoU);

            // Tread (base del asiento)
            var tread = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tread.name = $"Tread_F{fila}";
            tread.transform.SetParent(bloque.transform);
            tread.transform.position   = posTread;
            tread.transform.localScale = escTread;
            tread.GetComponent<MeshRenderer>().sharedMaterial = matEstruc;
            OrientarPorDireccion(tread.transform, dirProf, largoU);

            // ── Asientos individuales ──────────────────────────────────
            int numAsientos = Mathf.FloorToInt(largoTotal / anchoAsiento);
            float offsetInicio = -(largoTotal * 0.5f) + anchoAsiento * 0.5f;
            float asientoProf  = profundidadFila - riserGrosor - 0.06f;

            for (int s = 0; s < numAsientos; s++)
            {
                float largoPos = offsetInicio + s * anchoAsiento;

                // Asiento (parte horizontal donde se sienta)
                Vector3 posAsiento = origen
                    + dirProf  * (profOffset + riserGrosor + asientoProf * 0.5f)
                    + Vector3.up * (altoBase + alturaFilaGradas + treadGrosor * 0.5f)
                    + largoU   * largoPos;

                Vector3 escAsiento = EscalaPorDireccion(dirProf, largoU,
                    asientoProf, treadGrosor + 0.02f, anchoAsiento - 0.04f);

                // Variar color: sección VIP en las filas más altas y centrales
                bool esVip = (fila >= filasGradas - 2) && (s > numAsientos / 4) && (s < numAsientos * 3 / 4);
                Material matSilla = esVip ? new Material(lit) : matAsiento;
                if (esVip)
                {
                    matSilla.SetColor("_BaseColor", new Color(0.55f, 0.38f, 0.05f));
                    matSilla.SetFloat("_Smoothness", 0.6f);
                }

                var silla = GameObject.CreatePrimitive(PrimitiveType.Cube);
                silla.name = $"Silla_F{fila}_S{s}";
                silla.transform.SetParent(bloque.transform);
                silla.transform.position   = posAsiento;
                silla.transform.localScale = escAsiento;
                silla.GetComponent<MeshRenderer>().sharedMaterial = matSilla;
                OrientarPorDireccion(silla.transform, dirProf, largoU);

                // Respaldo del asiento (parte vertical)
                Vector3 posRespaldo = origen
                    + dirProf  * (profOffset + riserGrosor + 0.06f)
                    + Vector3.up * (altoBase + alturaFilaGradas + treadGrosor + seatAltura * 0.5f)
                    + largoU   * largoPos;

                Vector3 escRespaldo = EscalaPorDireccion(dirProf, largoU,
                    seatGrosor, seatAltura, anchoAsiento - 0.06f);

                var respaldo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                respaldo.name = $"Respaldo_F{fila}_S{s}";
                respaldo.transform.SetParent(bloque.transform);
                respaldo.transform.position   = posRespaldo;
                respaldo.transform.localScale = escRespaldo;
                respaldo.GetComponent<MeshRenderer>().sharedMaterial = matSilla;
                OrientarPorDireccion(respaldo.transform, dirProf, largoU);
            }

            // ── Pasamanos frontal de cada fila ─────────────────────────
            if (fila > 0)
            {
                Vector3 posBar = origen
                    + dirProf  * (profOffset + riserGrosor * 0.5f)
                    + Vector3.up * (altoBase + alturaFilaGradas + treadGrosor + 0.35f);

                Vector3 escBar = EscalaPorDireccion(dirProf, largoU, 0.04f, 0.04f, largoTotal);

                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = $"Pasamanos_F{fila}";
                bar.transform.SetParent(bloque.transform);
                bar.transform.position   = posBar;
                bar.transform.localScale = escBar;
                bar.GetComponent<MeshRenderer>().sharedMaterial = matRiser;
                OrientarPorDireccion(bar.transform, dirProf, largoU);
            }
        }

        // ── Baranda de seguridad frontal ──────────────────────────────
        {
            Vector3 posFrontal = origen
                + dirProf * 0.04f
                + Vector3.up * (alturaFilaGradas + 0.5f);

            Vector3 escFrontal = EscalaPorDireccion(dirProf, largoU, 0.05f, 0.5f, largoTotal);

            var baranda = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baranda.name = "BarandaFrontal";
            baranda.transform.SetParent(bloque.transform);
            baranda.transform.position   = posFrontal;
            baranda.transform.localScale = escFrontal;
            baranda.GetComponent<MeshRenderer>().sharedMaterial = matRiser;
            OrientarPorDireccion(baranda.transform, dirProf, largoU);
        }
    }

    /// <summary>
    /// Calcula la escala de un cubo según la dirección del bloque de gradas.
    ///
    ///   Este/Oeste  (largoU = forward) → prof en X, largo en Z → (prof, alto, largo)
    ///   Norte/Sur   (largoU = right)   → largo en X, prof en Z → (largo, alto, prof)
    /// </summary>
    private static Vector3 EscalaPorDireccion(
        Vector3 dirProf, Vector3 largoU,
        float prof, float alto, float largo)
    {
        if (largoU == Vector3.right || largoU == Vector3.left)
            // Norte/Sur: largo corre en X, profundidad en Z
            return new Vector3(largo, alto, prof);
        else
            // Este/Oeste: profundidad corre en X, largo en Z
            return new Vector3(prof, alto, largo);
    }

    /// <summary>No-op: la orientación se maneja por escala en EscalaPorDireccion.</summary>
    private static void OrientarPorDireccion(Transform t, Vector3 dirProf, Vector3 largoU) { }

    /// <summary>
    /// Genera 2 bancas a cada lado de la cancha (norte y sur),
    /// cada una con asiento + 4 patas.
    /// </summary>
    private void CrearBancas()
    {
        const string PADRE_NOMBRE = "Bancas";
        DestruirSiExiste(PADRE_NOMBRE);

        var padre   = new GameObject(PADRE_NOMBRE);
        Shader lit  = Shader.Find("Universal Render Pipeline/Lit");
        var matBanca = new Material(lit) { name = "MaderaBanca" };
        matBanca.SetColor("_BaseColor", colorBanca);
        matBanca.SetFloat("_Metallic",   0f);
        matBanca.SetFloat("_Smoothness", 0.25f);

        // Posiciones: 2 bancas lado oeste (x=-11), z = 5 y -5
        float[] posZ  = { 5f, -5f };
        float   xPos  = -11.5f;

        foreach (float z in posZ)
        {
            var bancaParent = new GameObject($"Banca_z{z}");
            bancaParent.transform.SetParent(padre.transform);

            // ── Asiento ────────────────────────────────────────────────
            var asiento = GameObject.CreatePrimitive(PrimitiveType.Cube);
            asiento.name = "Asiento";
            asiento.transform.SetParent(bancaParent.transform);
            asiento.transform.position   = new Vector3(xPos, 0.3f, z);
            asiento.transform.localScale = new Vector3(0.4f, 0.08f, 3f);
            asiento.GetComponent<MeshRenderer>().sharedMaterial = matBanca;

            // ── 4 Patas ────────────────────────────────────────────────
            float[] pataZ = { z - 1.2f, z + 1.2f };
            float[] pataX = { xPos - 0.12f, xPos + 0.12f };

            foreach (float pz in pataZ)
            {
                foreach (float px in pataX)
                {
                    var pata = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pata.name = "Pata";
                    pata.transform.SetParent(bancaParent.transform);
                    pata.transform.position   = new Vector3(px, 0.15f, pz);
                    pata.transform.localScale = new Vector3(0.06f, 0.3f, 0.06f);
                    pata.GetComponent<MeshRenderer>().sharedMaterial = matBanca;
                }
            }
        }

        Debug.Log("[CourtVisualSetup] Bancas creadas.");
    }

    /// <summary>
    /// Crea un marcador electrónico en la pared norte (z=15),
    /// con borde emisivo rojo y Canvas WorldSpace para el texto.
    /// </summary>
    private void CrearMarcador()
    {
        const string NOMBRE = "Scoreboard";
        DestruirSiExiste(NOMBRE);

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");

        // ── Marco del marcador ────────────────────────────────────────
        var marcador = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marcador.name = NOMBRE;
        marcador.transform.position   = new Vector3(0f, 4.5f, 14.8f);
        marcador.transform.localScale = new Vector3(3.4f, 1.9f, 0.08f);

        var matMarco = new Material(lit) { name = "Marco Marcador" };
        matMarco.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.05f));
        matMarco.SetFloat("_Smoothness", 0.1f);
        // Borde emisivo rojo
        matMarco.EnableKeyword("_EMISSION");
        matMarco.SetColor("_EmissionColor", new Color(0.8f, 0.05f, 0.05f) * 2f);
        marcador.GetComponent<MeshRenderer>().sharedMaterial = matMarco;

        // ── Pantalla interior (Quad) ──────────────────────────────────
        var pantalla = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pantalla.name = "Pantalla";
        pantalla.transform.SetParent(marcador.transform, false);
        pantalla.transform.localPosition = new Vector3(0f, 0f, -0.7f);
        pantalla.transform.localScale    = new Vector3(0.85f, 0.78f, 1f);

        var matPantalla = new Material(lit) { name = "Pantalla LCD" };
        matPantalla.SetColor("_BaseColor", new Color(0.03f, 0.03f, 0.05f));
        matPantalla.EnableKeyword("_EMISSION");
        matPantalla.SetColor("_EmissionColor", new Color(0f, 0.3f, 0.6f) * 0.5f);
        matPantalla.SetFloat("_Smoothness", 0.8f);
        pantalla.GetComponent<MeshRenderer>().sharedMaterial = matPantalla;

        // ── Canvas WorldSpace ─────────────────────────────────────────
        var canvasGO = new GameObject("Canvas_Marcador");
        canvasGO.transform.SetParent(marcador.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0f, -0.75f);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale    = new Vector3(0.003f, 0.003f, 0.003f);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        if (Camera.main != null) canvas.worldCamera = Camera.main;

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900f, 500f);

        // Texto del marcador
        var textGO = new GameObject("Texto_Score");
        textGO.transform.SetParent(canvasGO.transform, false);
        var texto = textGO.AddComponent<UnityEngine.UI.Text>();
        texto.text       = "LOCAL   00 : 00   VISITA";
        texto.color      = Color.white;
        texto.fontSize   = 72;
        texto.alignment  = TextAnchor.MiddleCenter;
        texto.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var textoRT = textGO.GetComponent<RectTransform>();
        textoRT.anchorMin = Vector2.zero;
        textoRT.anchorMax = Vector2.one;
        textoRT.offsetMin = textoRT.offsetMax = Vector2.zero;

        Debug.Log("[CourtVisualSetup] Marcador creado.");
    }

    /// <summary>
    /// Crea un Quad plano en el centro de la cancha con textura
    /// de logo circular procedural.
    /// </summary>
    private void CrearLogoCentral()
    {
        const string NOMBRE = "LogoCentral";
        DestruirSiExiste(NOMBRE);

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");

        var logo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        logo.name = NOMBRE;
        logo.transform.position   = new Vector3(0f, 0.02f, 0f);
        logo.transform.rotation   = Quaternion.Euler(90f, 0f, 0f);
        logo.transform.localScale = new Vector3(4f, 4f, 1f);

        var matLogo = new Material(lit) { name = "Logo Central" };
        ConfigurarTransparente(matLogo, Color.white);
        matLogo.SetTexture("_BaseMap", CrearTexturaLogo());
        matLogo.SetFloat("_Smoothness", 0.1f);
        logo.GetComponent<MeshRenderer>().sharedMaterial = matLogo;

        Debug.Log("[CourtVisualSetup] Logo central creado.");
    }

    /// <summary>
    /// Crea un sistema de partículas con polvo flotante ambiental.
    /// Partículas pequeñas, lentas y semi-transparentes para atmosfera indoor.
    /// </summary>
    private void CrearParticulasPolvo()
    {
        const string NOMBRE = "PolvoAmbiental";
        DestruirSiExiste(NOMBRE);

        var go = new GameObject(NOMBRE);
        go.transform.position = new Vector3(0f, 3f, 0f);

        var ps = go.AddComponent<ParticleSystem>();

        // ── Módulo principal ──────────────────────────────────────────
        var main = ps.main;
        main.loop         = true;
        main.startLifetime = 8f;
        main.startSpeed    = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor    = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.05f),
            new Color(0.9f, 0.95f, 1f, 0.15f)
        );
        main.maxParticles  = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // ── Emisión ───────────────────────────────────────────────────
        var emission = ps.emission;
        emission.rateOverTime = 5f;

        // ── Forma: volumen de la cancha ───────────────────────────────
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(18f, 5f, 28f);

        // ── Ruido para movimiento orgánico ────────────────────────────
        var noise = ps.noise;
        noise.enabled   = true;
        noise.strength  = 0.05f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.05f;

        // ── Renderer ──────────────────────────────────────────────────
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpUnlit != null)
        {
            var matPolvo = new Material(urpUnlit) { name = "Polvo Ambiental" };
            matPolvo.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.1f));
            renderer.sharedMaterial = matPolvo;
        }

        Debug.Log("[CourtVisualSetup] Partículas de polvo ambiental creadas.");
    }

    // ════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Busca el GameObject del piso por varios nombres posibles.</summary>
    private static GameObject EncontrarPiso()
    {
        // Buscar "Plane" dentro de "Court"
        var court = GameObject.Find("Court");
        if (court != null)
        {
            // Si Court tiene MeshRenderer directamente
            if (court.GetComponent<MeshRenderer>() != null) return court;
            // Si el piso es un hijo
            foreach (Transform child in court.transform)
                if (child.GetComponent<MeshRenderer>() != null) return child.gameObject;
        }
        return GameObject.Find("Plane") ?? GameObject.Find("Floor");
    }

    private static void DestruirSiExiste(string nombre)
    {
        var existing = GameObject.Find(nombre);
        if (existing != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existing);
#else
            Destroy(existing);
#endif
        }
    }

    /// <summary>Convierte un string hexadecimal a Color (sin #).</summary>
    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
