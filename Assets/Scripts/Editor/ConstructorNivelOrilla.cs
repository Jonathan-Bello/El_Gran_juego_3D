using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Genera Assets/Scenes/Zona_Orilla.unity desde cero, siguiendo docs/niveles/Zona_Orilla.md.
/// Se puede ejecutar varias veces: cada vez crea una escena nueva y la sobrescribe, sin duplicar objetos.
///
/// Convención (GDD 8.3, v0.3): la cámara mira hacia +Z.
///   Fondo  → Z +5 a +20  (decorativo, sin collider)
///   Juego  → Z = 0       (collider, capa y tag Ground)
///   Frente → Z −2 a −6   (carrizos, sin collider)
/// </summary>
public static class ConstructorNivelOrilla
{
    private const string rutaEscena = "Assets/Scenes/Zona_Orilla.unity";
    private const string carpetaModelos = "Assets/Models/Orilla/";
    private const string carpetaTexturas = "Assets/Textures/Orilla/";
    private const string carpetaMateriales = "Assets/Materials/Orilla";
    private const int semillaDecoracion = 7;

    // ── Paleta del GDD 9.2 ──
    private const string abismo = "#050B1A";
    private const string nocheDeRio = "#0B1D3A";
    private const string aguaProfunda = "#123A63";
    private const string azulNeblina = "#2C5F8A";
    private const string lunaFria = "#6FA3C7";
    private const string velo = "#B8D8EB";
    private const string aliento = "#E6F3FA";
    private const string veladora = "#E8A84A";

    // ── Métricas del nivel (docs/niveles/Zona_Orilla.md) ──
    private const float alturaMuelle = 1.8f;
    private const float grosorMuelle = 0.25f;
    private const float alturaLibreTronco = 0.9f;
    private const float alturaAgua = -0.9f;
    private const float profundidadSueloFrente = -2.5f;
    private const float profundidadSueloFondo = 4f;
    private const float grosorSuelo = 3f;
    private const float densidadNiebla = 0.015f;

    private static int capaSuelo;
    private static Dictionary<string, Material> materiales;
    private static System.Random azar;

    [MenuItem("Lamento/Construir nivel La Orilla", priority = 20)]
    public static void Construir()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        UtilidadesEditorLamento.AsegurarTagsYCapas();
        capaSuelo = UtilidadesEditorLamento.AsegurarCapa(UtilidadesEditorLamento.capaSuelo);
        azar = new System.Random(semillaDecoracion);

        GameObject prefabItzel = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorPrefabItzel.rutaPrefab);
        if (prefabItzel == null)
        {
            Debug.Log("[Lamento] No existe Itzel.prefab; lo construyo primero.");
            ConstructorPrefabItzel.Construir();
            prefabItzel = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorPrefabItzel.rutaPrefab);
        }

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        materiales = CrearMateriales();

        Transform juego = new GameObject("Capa de juego (Z = 0)").transform;
        Transform fondo = new GameObject("Capa de fondo (Z +5 a +20)").transform;
        Transform frente = new GameObject("Capa frontal (Z -2 a -6)").transform;
        Transform guia = new GameObject("Guía ámbar").transform;
        Transform peligros = new GameObject("Zonas de muerte").transform;

        ConstruirSuelo(juego);
        ConstruirCanoa(juego);
        ConstruirMuelle(juego);
        ConstruirTronco(juego);
        ConstruirLimites(juego);
        ConstruirZonasDeMuerte(peligros);
        ConstruirOfrenda(juego, new Vector3(106f, 0f, 0.6f));
        ConstruirLucesDeRuta(guia);
        ConstruirFondo(fondo);
        ConstruirFrente(frente);
        ConfigurarIluminacion();

        GameObject itzel = InstanciarItzel(prefabItzel);
        ConfigurarCamara(itzel != null ? itzel.transform : null);

        UtilidadesEditorLamento.AsegurarCarpeta("Assets/Scenes");
        EditorSceneManager.SaveScene(escena, rutaEscena);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Lamento] Nivel La Orilla construido y guardado en {rutaEscena}.");
    }

    // ───────────────────────────── Capa de juego ─────────────────────────────

    private static void ConstruirSuelo(Transform padre)
    {
        // Tramos de suelo: [inicio, fin] en X. Los huecos (44–47 y 58–80) dejan ver el agua.
        CrearSuelo(padre, "Suelo 1-2 Despertar y Ribera", -6f, 44f);
        CrearSuelo(padre, "Suelo 3 Tras el hueco", 47f, 58f);
        CrearSuelo(padre, "Suelo 5 Tronco y ofrenda", 80f, 116f);

        // Rocas-escalón para bajar del muelle (X 80–83).
        CrearCaja(padre, "Escalón de roca alto", new Vector3(80.6f, 0.6f, 0f), new Vector3(1.2f, 1.2f, 2.2f), materiales["Roca"], true);
        CrearCaja(padre, "Escalón de roca bajo", new Vector3(81.8f, 0.3f, 0f), new Vector3(1.2f, 0.6f, 2.2f), materiales["Roca"], true);
        InstanciarModelo("rock_largeC", padre, new Vector3(80.8f, 0f, 1.4f), 1.6f, 20f, false);
        InstanciarModelo("rock_smallA", padre, new Vector3(82.2f, 0f, -1.2f), 0.5f, 0f, false);
    }

    private static void CrearSuelo(Transform padre, string nombre, float inicioX, float finX)
    {
        float largo = finX - inicioX;
        float profundidad = profundidadSueloFondo - profundidadSueloFrente;
        Vector3 centro = new Vector3(inicioX + largo * 0.5f, -grosorSuelo * 0.5f, profundidadSueloFrente + profundidad * 0.5f);
        CrearCaja(padre, nombre, centro, new Vector3(largo, grosorSuelo, profundidad), materiales["Lodo"], true);
    }

    private static void ConstruirCanoa(Transform padre)
    {
        // Obstáculo pedido por la clase: canoa varada de 1 m de alto en La Ribera (X 31–34).
        GameObject canoa = new GameObject("Canoa varada (obstáculo)");
        canoa.transform.SetParent(padre, false);
        canoa.transform.position = new Vector3(32.5f, 0f, 0f);
        canoa.layer = capaSuelo;
        canoa.tag = UtilidadesEditorLamento.tagSuelo;

        GameObject visual = InstanciarModeloPorLargo("canoe", canoa.transform, canoa.transform.position, 3f, false);
        BoxCollider colision = canoa.AddComponent<BoxCollider>();

        if (visual != null)
        {
            AjustarAltura(visual, 1f, 0f);
            Centrar(visual, canoa.transform.position);

            // El collider copia el tamaño real del modelo (la raíz no está girada ni escalada).
            Bounds limites = CalcularLimites(visual);
            colision.center = limites.center - canoa.transform.position;
            colision.size = limites.size;
        }
        else
        {
            colision.center = new Vector3(0f, 0.5f, 0f);
            colision.size = new Vector3(3f, 1f, 1.4f);
        }
    }

    private static void ConstruirMuelle(Transform padre)
    {
        Transform muelle = new GameObject("Muelle roto").transform;
        muelle.SetParent(padre, false);

        // Dos tramos de tablones a 1.8 m; falta un tablón entre X 68 y 70.5.
        CrearTramoMuelle(muelle, "Muelle tramo A", 58f, 68f);
        CrearTramoMuelle(muelle, "Muelle tramo B", 70.5f, 80f);

        // Postes decorativos (sin collider) que bajan hasta el agua.
        float[] postesX = { 58.3f, 61.5f, 64.8f, 67.7f, 70.8f, 74f, 77f, 79.7f };
        float alturaPoste = alturaMuelle - alturaAgua + 0.6f;
        foreach (float x in postesX)
        {
            foreach (float z in new[] { -0.85f, 0.85f })
            {
                Vector3 centro = new Vector3(x, alturaAgua - 0.6f + alturaPoste * 0.5f, z);
                CrearCaja(muelle, "Poste", centro, new Vector3(0.22f, alturaPoste, 0.22f), materiales["MaderaOscura"], false);
            }
        }

        // Tablón roto colgando hacia el agua: cuenta la historia del muelle sin palabras.
        GameObject roto = CrearCaja(muelle, "Tablón roto", new Vector3(68.9f, 1.1f, 0.3f), new Vector3(1.6f, 0.12f, 1.4f), materiales["Madera"], false);
        roto.transform.rotation = Quaternion.Euler(0f, 0f, -38f);
    }

    private static void CrearTramoMuelle(Transform padre, string nombre, float inicioX, float finX)
    {
        float largo = finX - inicioX;
        Vector3 centro = new Vector3(inicioX + largo * 0.5f, alturaMuelle - grosorMuelle * 0.5f, 0f);
        CrearCaja(padre, nombre, centro, new Vector3(largo, grosorMuelle, 2f), materiales["Madera"], true);

        // Barandal de tablones al fondo para que se lea como muelle.
        InstanciarModeloPorLargo("fence_planks", padre, new Vector3(inicioX + largo * 0.5f, alturaMuelle, 1.05f), Mathf.Min(largo, 4f), false);
    }

    private static void ConstruirTronco(Transform padre)
    {
        // Tronco caído (X 88–93): su parte baja queda a 0.9 m; Itzel de pie mide 1.2 m.
        const float grosorTronco = 0.7f;
        GameObject tronco = CrearCaja(
            padre,
            "Tronco bajo (agacharse)",
            new Vector3(90.5f, alturaLibreTronco + grosorTronco * 0.5f, 0f),
            new Vector3(6f, grosorTronco, 1.6f),
            materiales["Madera"],
            true);
        tronco.GetComponent<Renderer>().enabled = false;

        // El visual va aparte (no como hijo de la caja escalada, que lo deformaría).
        GameObject visual = InstanciarModeloPorLargo("log_large", padre, tronco.transform.position, 6.4f, false);
        if (visual != null)
        {
            visual.name = "Tronco bajo (visual)";
            AjustarAltura(visual, grosorTronco + 0.1f, alturaLibreTronco - 0.05f);
            Centrar(visual, tronco.transform.position);
        }

        // Rocas que "sostienen" el tronco, detrás del plano de juego para no estorbar.
        InstanciarModelo("rock_tallA", padre, new Vector3(87.6f, 0f, 1.5f), alturaLibreTronco + 0.3f, 0f, false);
        InstanciarModelo("rock_tallC", padre, new Vector3(93.4f, 0f, 1.5f), alturaLibreTronco + 0.3f, 90f, false);
    }

    private static void ConstruirLimites(Transform padre)
    {
        // Paredes invisibles en los extremos (capa Default para no contar como suelo).
        foreach (float x in new[] { -4f, 112f })
        {
            GameObject limite = new GameObject("Límite invisible");
            limite.transform.SetParent(padre, false);
            limite.transform.position = new Vector3(x, 10f, 0f);
            limite.AddComponent<BoxCollider>().size = new Vector3(1f, 30f, 6f);
        }
    }

    private static void ConstruirZonasDeMuerte(Transform padre)
    {
        CrearZonaDeMuerte(padre, "Zona de muerte - Hueco", 44f, 47f);
        CrearZonaDeMuerte(padre, "Zona de muerte - Bajo el muelle", 58f, 80f);
        CrearZonaDeMuerte(padre, "Zona de muerte - Red de seguridad", -10f, 120f, -6f);
    }

    private static void CrearZonaDeMuerte(Transform padre, string nombre, float inicioX, float finX, float alturaCentro = -1.6f)
    {
        GameObject zona = new GameObject(nombre);
        zona.transform.SetParent(padre, false);
        zona.transform.position = new Vector3((inicioX + finX) * 0.5f, alturaCentro, 0f);
        zona.tag = UtilidadesEditorLamento.tagZonaMuerte;

        BoxCollider colision = zona.AddComponent<BoxCollider>();
        colision.isTrigger = true;
        colision.size = new Vector3(finX - inicioX, 1.2f, 6f);
    }

    private static void ConstruirOfrenda(Transform padre, Vector3 posicion)
    {
        GameObject ofrenda = new GameObject("Ofrenda (placeholder)");
        ofrenda.transform.SetParent(padre, false);
        ofrenda.transform.position = posicion;
        ofrenda.tag = UtilidadesEditorLamento.tagOfrenda;

        BoxCollider zona = ofrenda.AddComponent<BoxCollider>();
        zona.isTrigger = true;
        zona.center = new Vector3(0f, 1f, -0.6f);
        zona.size = new Vector3(2f, 2f, 2f);

        CrearCaja(ofrenda.transform, "Altar", posicion + new Vector3(0f, 0.3f, 0f), new Vector3(1.2f, 0.6f, 0.7f), materiales["Madera"], false);
        CrearCaja(ofrenda.transform, "Mantel", posicion + new Vector3(0f, 0.62f, 0f), new Vector3(1.25f, 0.04f, 0.75f), materiales["Velo"], false);

        float[] velasX = { -0.4f, 0f, 0.4f };
        float[] velasAltura = { 0.25f, 0.35f, 0.22f };
        for (int i = 0; i < velasX.Length; i++)
        {
            GameObject vela = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vela.name = "Veladora";
            vela.transform.SetParent(ofrenda.transform, false);
            vela.transform.position = posicion + new Vector3(velasX[i], 0.64f + velasAltura[i] * 0.5f, 0f);
            vela.transform.localScale = new Vector3(0.12f, velasAltura[i] * 0.5f, 0.12f);
            Object.DestroyImmediate(vela.GetComponent<Collider>());
            vela.GetComponent<Renderer>().sharedMaterial = materiales["Veladora"];
        }

        CrearLuzPuntual(ofrenda.transform, "Luz de la ofrenda", posicion + new Vector3(0f, 1.4f, -0.4f), 3f, 7f);
    }

    private static void ConstruirLucesDeRuta(Transform padre)
    {
        // Ámbar tenue a lo largo de la ruta principal (GDD 8.2: señalización por luz).
        Vector3[] puntos =
        {
            new Vector3(4f, 0f, 1.2f), new Vector3(14f, 0f, 1.2f), new Vector3(24f, 0f, 1.2f),
            new Vector3(36f, 0f, 1.2f), new Vector3(42.5f, 0f, 1.2f), new Vector3(56.5f, 0f, 1.2f),
            new Vector3(63f, alturaMuelle, 0.7f), new Vector3(75f, alturaMuelle, 0.7f),
            new Vector3(86f, 0f, 1.2f), new Vector3(98f, 0f, 1.2f),
        };

        foreach (Vector3 punto in puntos)
        {
            GameObject vela = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vela.name = "Veladora de ruta";
            vela.transform.SetParent(padre, false);
            vela.transform.position = punto + new Vector3(0f, 0.12f, 0f);
            vela.transform.localScale = new Vector3(0.1f, 0.12f, 0.1f);
            Object.DestroyImmediate(vela.GetComponent<Collider>());
            vela.GetComponent<Renderer>().sharedMaterial = materiales["Veladora"];

            CrearLuzPuntual(padre, "Luz de ruta", punto + new Vector3(0f, 0.6f, -0.3f), 1.2f, 4f);
        }
    }

    // ───────────────────────────── Capa de fondo ─────────────────────────────

    private static void ConstruirFondo(Transform padre)
    {
        // Río: un plano de agua que pasa detrás y por debajo de todo el nivel.
        CrearCaja(padre, "Río", new Vector3(55f, alturaAgua - 0.05f, 14f), new Vector3(150f, 0.1f, 36f), materiales["Agua"], false);

        // Orilla lejana y colinas.
        CrearCaja(padre, "Orilla lejana", new Vector3(55f, -0.6f, 26f), new Vector3(150f, 1.2f, 6f), materiales["Silueta"], false);
        for (int i = 0; i < 7; i++)
        {
            GameObject colina = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            colina.name = "Colina";
            colina.transform.SetParent(padre, false);
            colina.transform.position = new Vector3(-10f + i * 22f + Rango(-5f, 5f), -2f, 34f + Rango(0f, 8f));
            colina.transform.localScale = new Vector3(Rango(28f, 40f), Rango(8f, 14f), 12f);
            Object.DestroyImmediate(colina.GetComponent<Collider>());
            colina.GetComponent<Renderer>().sharedMaterial = materiales["Noche"];
        }

        // Árboles secos (siluetas) en la orilla de atrás y en nuestra orilla.
        string[] arboles = { "tree_thin_dark", "tree_tall_dark", "tree_pineTallA", "tree_default_dark", "stump_oldTall" };
        for (float x = -6f; x < 116f; x += Rango(7f, 12f))
        {
            string arbol = arboles[azar.Next(arboles.Length)];
            InstanciarModelo(arbol, padre, new Vector3(x, -0.3f, Rango(23f, 28f)), Rango(5f, 9f), Rango(0f, 360f), true);
        }

        for (float x = -2f; x < 112f; x += Rango(14f, 20f))
        {
            if (x > 40f && x < 85f)
            {
                continue; // Sin árboles detrás del hueco y del muelle: ahí se ve el río.
            }

            InstanciarModelo("tree_thin_dark", padre, new Vector3(x, 0f, Rango(3.2f, 3.8f)), Rango(3.5f, 5f), Rango(0f, 360f), true);
        }

        // Rocas y troncos en nuestra orilla, detrás del plano de juego.
        string[] rocas = { "rock_largeA", "rock_largeE", "rock_smallC", "rock_smallFlatA", "stone_tallB", "stump_old", "log" };
        for (float x = 0f; x < 110f; x += Rango(4f, 8f))
        {
            if ((x > 43f && x < 48f) || (x > 57f && x < 81f))
            {
                continue;
            }

            InstanciarModelo(rocas[azar.Next(rocas.Length)], padre, new Vector3(x, 0f, Rango(2f, 3.6f)), Rango(0.4f, 1.2f), Rango(0f, 360f), false);
        }

        // Narrativa ambiental: una segunda canoa en el agua y lirios.
        GameObject canoaLejana = InstanciarModeloPorLargo("canoe", padre, new Vector3(64f, alturaAgua, 11f), 3.4f, false);
        if (canoaLejana != null)
        {
            canoaLejana.transform.Rotate(0f, 25f, 6f, Space.World);
        }

        for (int i = 0; i < 14; i++)
        {
            string lirio = i % 2 == 0 ? "lily_large" : "lily_small";
            InstanciarModeloPorLargo(lirio, padre, new Vector3(Rango(40f, 90f), alturaAgua + 0.01f, Rango(5f, 16f)), Rango(0.6f, 1.2f), false);
        }

        // Luna fría.
        GameObject luna = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        luna.name = "Luna";
        luna.transform.SetParent(padre, false);
        luna.transform.position = new Vector3(70f, 24f, 45f);
        luna.transform.localScale = Vector3.one * 5f;
        Object.DestroyImmediate(luna.GetComponent<Collider>());
        luna.GetComponent<Renderer>().sharedMaterial = materiales["Luna"];
        luna.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    // ───────────────────────────── Capa frontal ─────────────────────────────

    private static void ConstruirFrente(Transform padre)
    {
        // Matas de carrizos: marcan los futuros escondites y tapan un poco la vista.
        float[] matasX = { 4f, 26f, 56f, 96f };
        string[] carrizos = { "crops_bambooStageA", "crops_bambooStageB", "grass_large", "plant_flatTall", "crops_wheatStageB" };

        foreach (float centroX in matasX)
        {
            Transform mata = new GameObject($"Carrizos (escondite X≈{centroX:0})").transform;
            mata.SetParent(padre, false);
            for (int i = 0; i < 9; i++)
            {
                Vector3 posicion = new Vector3(centroX + Rango(-1.8f, 1.8f), 0f, Rango(-2.2f, -3.4f));
                InstanciarModelo(carrizos[azar.Next(carrizos.Length)], mata, posicion, Rango(1.4f, 2.4f), Rango(0f, 360f), false);
            }
        }

        // Pasto disperso más cerca de la cámara.
        for (float x = -4f; x < 112f; x += Rango(3f, 6f))
        {
            InstanciarModelo("grass_leafsLarge", padre, new Vector3(x, 0f, Rango(-4f, -5.8f)), Rango(0.5f, 1f), Rango(0f, 360f), false);
        }
    }

    // ───────────────────────────── Iluminación y cámara ─────────────────────────────

    private static void ConfigurarIluminacion()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = UtilidadesEditorLamento.ColorHex(aguaProfunda);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = UtilidadesEditorLamento.ColorHex(nocheDeRio);
        RenderSettings.fogDensity = densidadNiebla;

        GameObject objetoLuna = new GameObject("Luz de luna");
        Light luz = objetoLuna.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.color = UtilidadesEditorLamento.ColorHex(lunaFria);
        luz.intensity = 0.7f;
        luz.shadows = LightShadows.Soft;
        // Viene desde el fondo (detrás y arriba): contraluz que recorta la silueta de Itzel.
        objetoLuna.transform.rotation = Quaternion.Euler(38f, 200f, 0f);
        RenderSettings.sun = luz;
    }

    private static GameObject InstanciarItzel(GameObject prefabItzel)
    {
        if (prefabItzel == null)
        {
            Debug.LogError("[Lamento] No se pudo crear Itzel.prefab; la escena queda sin jugador.");
            return null;
        }

        GameObject itzel = (GameObject)PrefabUtility.InstantiatePrefab(prefabItzel);
        itzel.transform.position = new Vector3(1.5f, 0.65f, 0f);
        return itzel;
    }

    private static void ConfigurarCamara(Transform objetivo)
    {
        GameObject objetoCamara = new GameObject("Main Camera");
        objetoCamara.tag = "MainCamera";
        Camera camara = objetoCamara.AddComponent<Camera>();
        camara.fieldOfView = 32f;
        camara.nearClipPlane = 0.3f;
        camara.farClipPlane = 250f;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = UtilidadesEditorLamento.ColorHex(abismo);
        objetoCamara.AddComponent<AudioListener>();

        CamaraPruebasItzel seguimiento = objetoCamara.AddComponent<CamaraPruebasItzel>();
        SerializedObject datos = new SerializedObject(seguimiento);
        datos.FindProperty("objetivo").objectReferenceValue = objetivo;
        datos.ApplyModifiedPropertiesWithoutUndo();

        Vector3 desplazamiento = datos.FindProperty("desplazamiento").vector3Value;
        float alturaMira = datos.FindProperty("alturaMira").floatValue;
        Vector3 posicionObjetivo = objetivo != null ? objetivo.position : Vector3.zero;
        objetoCamara.transform.position = posicionObjetivo + desplazamiento;
        objetoCamara.transform.LookAt(posicionObjetivo + Vector3.up * alturaMira);
    }

    // ───────────────────────────── Materiales ─────────────────────────────

    private static Dictionary<string, Material> CrearMateriales()
    {
        UtilidadesEditorLamento.AsegurarCarpeta(carpetaMateriales);

        Material lodo = CrearMaterialLit("Orilla_Lodo", lunaFria, 0.25f);
        lodo.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(carpetaTexturas + "Ground106_Color.png"));
        lodo.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(carpetaTexturas + "Ground106_NormalGL.png"));
        lodo.EnableKeyword("_NORMALMAP");
        lodo.mainTextureScale = new Vector2(12f, 2f);
        AsegurarImportacionNormal(carpetaTexturas + "Ground106_NormalGL.png");

        Material veladoraMaterial = CrearMaterialLit("Orilla_Veladora", veladora, 0.5f);
        veladoraMaterial.EnableKeyword("_EMISSION");
        veladoraMaterial.SetColor("_EmissionColor", UtilidadesEditorLamento.ColorHex(veladora) * 2.5f);
        veladoraMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        Material luna = CrearMaterial("Orilla_Luna", "Universal Render Pipeline/Unlit");
        luna.SetColor("_BaseColor", UtilidadesEditorLamento.ColorHex(aliento));

        return new Dictionary<string, Material>
        {
            { "Lodo", lodo },
            { "Madera", CrearMaterialLit("Orilla_Madera", azulNeblina, 0.15f) },
            { "MaderaOscura", CrearMaterialLit("Orilla_MaderaOscura", aguaProfunda, 0.1f) },
            { "Roca", CrearMaterialLit("Orilla_Roca", aguaProfunda, 0.2f) },
            { "Vegetacion", CrearMaterialLit("Orilla_Vegetacion", nocheDeRio, 0.1f) },
            { "VegetacionClara", CrearMaterialLit("Orilla_VegetacionClara", aguaProfunda, 0.1f) },
            { "Silueta", CrearMaterialLit("Orilla_Silueta", abismo, 0f) },
            { "Noche", CrearMaterialLit("Orilla_Noche", nocheDeRio, 0f) },
            { "Agua", CrearMaterialLit("Orilla_Agua", aguaProfunda, 0.92f) },
            { "Velo", CrearMaterialLit("Orilla_Velo", velo, 0.2f) },
            { "Veladora", veladoraMaterial },
            { "Luna", luna },
        };
    }

    private static Material CrearMaterialLit(string nombre, string colorHex, float suavidad)
    {
        Material material = CrearMaterial(nombre, "Universal Render Pipeline/Lit");
        material.SetColor("_BaseColor", UtilidadesEditorLamento.ColorHex(colorHex));
        material.SetFloat("_Smoothness", suavidad);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CrearMaterial(string nombre, string nombreShader)
    {
        string ruta = $"{carpetaMateriales}/{nombre}.mat";
        Shader shader = Shader.Find(nombreShader);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, ruta);
        }
        else
        {
            material.shader = shader;
        }

        return material;
    }

    private static void AsegurarImportacionNormal(string ruta)
    {
        TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador != null && importador.textureType != TextureImporterType.NormalMap)
        {
            importador.textureType = TextureImporterType.NormalMap;
            importador.SaveAndReimport();
        }
    }

    /// <summary>Recolorea un modelo de Kenney según el nombre de sus materiales (wood, stone, grass…).</summary>
    private static void AplicarPaleta(GameObject modelo, bool comoSilueta)
    {
        foreach (Renderer renderer in modelo.GetComponentsInChildren<Renderer>(true))
        {
            Material[] actuales = renderer.sharedMaterials;
            for (int i = 0; i < actuales.Length; i++)
            {
                string nombre = actuales[i] != null ? actuales[i].name.ToLowerInvariant() : string.Empty;
                actuales[i] = comoSilueta ? materiales["Silueta"] : materiales[ClasificarMaterial(nombre)];
            }

            renderer.sharedMaterials = actuales;
        }
    }

    private static string ClasificarMaterial(string nombre)
    {
        if (nombre.Contains("wood") || nombre.Contains("bark"))
        {
            return nombre.Contains("dark") ? "MaderaOscura" : "Madera";
        }

        if (nombre.Contains("leaf") || nombre.Contains("grass") || nombre.Contains("plant"))
        {
            return nombre.Contains("dark") ? "Vegetacion" : "VegetacionClara";
        }

        return "Roca";
    }

    // ───────────────────────────── Ayudantes ─────────────────────────────

    private static GameObject CrearCaja(Transform padre, string nombre, Vector3 centro, Vector3 tamano, Material material, bool jugable)
    {
        GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caja.name = nombre;
        caja.transform.SetParent(padre, false);
        caja.transform.position = centro;
        caja.transform.localScale = tamano;
        caja.GetComponent<Renderer>().sharedMaterial = material;

        if (jugable)
        {
            caja.layer = capaSuelo;
            caja.tag = UtilidadesEditorLamento.tagSuelo;
            caja.isStatic = true;
        }
        else
        {
            Object.DestroyImmediate(caja.GetComponent<Collider>());
        }

        return caja;
    }

    private static void CrearLuzPuntual(Transform padre, string nombre, Vector3 posicion, float intensidad, float alcance)
    {
        GameObject objetoLuz = new GameObject(nombre);
        objetoLuz.transform.SetParent(padre, false);
        objetoLuz.transform.position = posicion;
        Light luz = objetoLuz.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = UtilidadesEditorLamento.ColorHex(veladora);
        luz.intensity = intensidad;
        luz.range = alcance;
        luz.shadows = LightShadows.None;
    }

    /// <summary>Instancia un FBX de Kenney, lo escala a la altura pedida y apoya su base en posicion.y.</summary>
    private static GameObject InstanciarModelo(string nombre, Transform padre, Vector3 posicion, float altura, float giroY, bool comoSilueta)
    {
        GameObject modelo = CrearInstancia(nombre, padre);
        if (modelo == null)
        {
            return null;
        }

        modelo.transform.rotation = Quaternion.Euler(0f, giroY, 0f);
        AplicarPaleta(modelo, comoSilueta);
        AjustarAltura(modelo, altura, posicion.y);
        Centrar(modelo, posicion);
        return modelo;
    }

    /// <summary>Instancia un FBX, lo gira para que su lado largo quede en X y lo escala a ese largo.</summary>
    private static GameObject InstanciarModeloPorLargo(string nombre, Transform padre, Vector3 posicion, float largo, bool comoSilueta)
    {
        GameObject modelo = CrearInstancia(nombre, padre);
        if (modelo == null)
        {
            return null;
        }

        AplicarPaleta(modelo, comoSilueta);
        Bounds limites = CalcularLimites(modelo);
        if (limites.size.z > limites.size.x)
        {
            modelo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            limites = CalcularLimites(modelo);
        }

        if (limites.size.x > 0.0001f)
        {
            modelo.transform.localScale *= largo / limites.size.x;
        }

        Bounds escalados = CalcularLimites(modelo);
        modelo.transform.position += new Vector3(0f, posicion.y - escalados.min.y, 0f);
        Centrar(modelo, posicion);
        return modelo;
    }

    private static GameObject CrearInstancia(string nombre, Transform padre)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{carpetaModelos}{nombre}.fbx");
        if (asset == null)
        {
            Debug.LogWarning($"[Lamento] No se encontró el modelo {nombre}.fbx en {carpetaModelos}.");
            return null;
        }

        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        modelo.transform.SetParent(padre, false);
        return modelo;
    }

    private static void AjustarAltura(GameObject modelo, float altura, float baseY)
    {
        Bounds limites = CalcularLimites(modelo);
        if (limites.size.y > 0.0001f)
        {
            modelo.transform.localScale *= altura / limites.size.y;
        }

        Bounds escalados = CalcularLimites(modelo);
        modelo.transform.position += new Vector3(0f, baseY - escalados.min.y, 0f);
    }

    private static void Centrar(GameObject modelo, Vector3 posicion)
    {
        Bounds limites = CalcularLimites(modelo);
        modelo.transform.position += new Vector3(posicion.x - limites.center.x, 0f, posicion.z - limites.center.z);
    }

    private static Bounds CalcularLimites(GameObject modelo)
    {
        Renderer[] renderers = modelo.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(modelo.transform.position, Vector3.zero);
        }

        Bounds limites = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            limites.Encapsulate(renderer.bounds);
        }

        return limites;
    }

    private static float Rango(float minimo, float maximo)
    {
        return minimo + (float)azar.NextDouble() * (maximo - minimo);
    }
}
