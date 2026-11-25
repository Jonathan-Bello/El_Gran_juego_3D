using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Genera Assets/Prefabs/Itzel.prefab:
///
///   Itzel (Rigidbody + CapsuleCollider + ItzelController)   ← física, en Z = 0
///   └── Visual (escala y gira; es el "modeloVisual")
///       └── Modelo (Animator + AnimacionItzel)              ← cuerpo Humanoid
///           ├── Superhero_Female, Eyes, Eyebrows             ← mallas del cuerpo
///           ├── Female_Peasant_*                              ← ropa reasignada al esqueleto
///           └── Hair_Long                                     ← cabello reasignado al esqueleto
/// </summary>
public static class ConstructorPrefabItzel
{
    public const string rutaPrefab = "Assets/Prefabs/Itzel.prefab";

    private const string carpetaModelos = "Assets/Models/Itzel/";
    private const string carpetaTexturas = "Assets/Models/Itzel/Texturas/";
    private const string carpetaMateriales = "Assets/Materials/Itzel";
    private const string rutaCuerpo = carpetaModelos + "Superhero_Female_FullBody.fbx";
    private const string rutaRopa = carpetaModelos + "Female_Peasant.fbx";
    private const string rutaAccionesEntrada = "Assets/InputSystem_Actions.inputactions";

    // Cambia a "Hair_Buns.fbx" para usar chongos.
    private const string archivoCabello = "Hair_Long.fbx";

    // Medidas de Itzel (GDD 9.3: silueta pequeña).
    private const float alturaItzel = 1.2f;
    private const float radioCapsula = 0.22f;
    private const float margenDeteccionSuelo = 0.05f;
    private const float alturaModeloPorDefecto = 1.75f;

    // El modelo de Quaternius mira hacia +Z; lo giramos para que mire a la derecha (+X).
    private const float giroHaciaDerecha = 90f;

    [MenuItem("Lamento/Construir prefab de Itzel", priority = 11)]
    public static void Construir()
    {
        GameObject modeloCuerpo = AssetDatabase.LoadAssetAtPath<GameObject>(rutaCuerpo);
        GameObject modeloRopa = AssetDatabase.LoadAssetAtPath<GameObject>(rutaRopa);
        GameObject modeloCabello = AssetDatabase.LoadAssetAtPath<GameObject>(carpetaModelos + archivoCabello);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorAnimatorItzel.rutaController);

        if (modeloCuerpo == null || modeloRopa == null || modeloCabello == null)
        {
            Debug.LogError("[Lamento] No se encontraron los FBX de Itzel en Assets/Models/Itzel/.");
            return;
        }

        if (controller == null)
        {
            Debug.Log("[Lamento] No existe el Animator de Itzel; lo construyo primero.");
            ConstructorAnimatorItzel.Construir();
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorAnimatorItzel.rutaController);
        }

        UtilidadesEditorLamento.AsegurarTagsYCapas();
        UtilidadesEditorLamento.AsegurarCarpeta("Assets/Prefabs");
        Dictionary<string, Material> materiales = CrearMateriales();

        // ── Raíz con física ──
        GameObject raiz = new GameObject("Itzel");
        raiz.tag = UtilidadesEditorLamento.tagJugador;

        Rigidbody cuerpoRigido = raiz.AddComponent<Rigidbody>();
        cuerpoRigido.useGravity = true;
        cuerpoRigido.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        cuerpoRigido.interpolation = RigidbodyInterpolation.Interpolate;
        cuerpoRigido.collisionDetectionMode = CollisionDetectionMode.Continuous;

        CapsuleCollider capsula = raiz.AddComponent<CapsuleCollider>();
        capsula.height = alturaItzel;
        capsula.radius = radioCapsula;
        capsula.center = Vector3.zero;

        ItzelController control = raiz.AddComponent<ItzelController>();

        // ── Hijo visual ──
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform, false);
        visual.transform.localPosition = new Vector3(0f, -alturaItzel * 0.5f, 0f);
        visual.transform.localRotation = Quaternion.Euler(0f, giroHaciaDerecha, 0f);

        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(modeloCuerpo);
        PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        modelo.name = "Modelo";
        modelo.transform.SetParent(visual.transform, false);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;

        // Ropa y cabello: se copian sus mallas y se conectan a los huesos del cuerpo.
        AdjuntarPiezaAlEsqueleto(modeloRopa, modelo.transform);
        AdjuntarPiezaAlEsqueleto(modeloCabello, modelo.transform);
        AsignarMateriales(modelo, materiales);

        // Escala: medimos el modelo y lo reducimos a la altura de Itzel.
        float alturaModelo = MedirAltura(modelo);
        float escala = alturaItzel / alturaModelo;
        visual.transform.localScale = Vector3.one * escala;

        Animator animador = modelo.GetComponent<Animator>();
        if (animador == null)
        {
            animador = modelo.AddComponent<Animator>();
        }

        animador.runtimeAnimatorController = controller;
        animador.applyRootMotion = false;
        animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        modelo.AddComponent<AnimacionItzel>();

        ConfigurarControlador(control, visual.transform);

        PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab, out bool guardado);
        Object.DestroyImmediate(raiz);

        if (guardado)
        {
            Debug.Log($"[Lamento] Prefab de Itzel guardado en {rutaPrefab}. Altura del modelo original: {alturaModelo:0.00} m → escala {escala:0.000}.");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
        }
        else
        {
            Debug.LogError("[Lamento] No se pudo guardar el prefab de Itzel.");
        }
    }

    private static void ConfigurarControlador(ItzelController control, Transform visual)
    {
        SerializedObject datos = new SerializedObject(control);
        datos.FindProperty("accionesEntrada").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<InputActionAsset>(rutaAccionesEntrada);
        datos.FindProperty("mascaraSuelo").intValue = UtilidadesEditorLamento.ObtenerMascaraSuelo();
        datos.FindProperty("modeloVisual").objectReferenceValue = visual;
        datos.FindProperty("modeloMiraDerechaPorDefecto").boolValue = true;
        datos.FindProperty("desplazamientoComprobacionSuelo").vector3Value =
            new Vector3(0f, -alturaItzel * 0.5f + radioCapsula - margenDeteccionSuelo, 0f);
        datos.FindProperty("radioComprobacionSuelo").floatValue = radioCapsula * 0.9f;
        datos.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Técnica de "personaje modular": instancia la pieza, copia cada SkinnedMeshRenderer
    /// dentro del modelo base y cambia sus huesos por los del esqueleto base con el mismo nombre.
    /// Así la ropa y el cabello siguen las animaciones del cuerpo.
    /// </summary>
    private static void AdjuntarPiezaAlEsqueleto(GameObject modeloPieza, Transform modeloBase)
    {
        Dictionary<string, Transform> huesosBase = new Dictionary<string, Transform>();
        foreach (Transform hueso in modeloBase.GetComponentsInChildren<Transform>(true))
        {
            if (!huesosBase.ContainsKey(hueso.name))
            {
                huesosBase.Add(hueso.name, hueso);
            }
        }

        GameObject pieza = Object.Instantiate(modeloPieza);

        foreach (SkinnedMeshRenderer origen in pieza.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            GameObject copia = new GameObject(origen.name);
            copia.transform.SetParent(modeloBase, false);

            SkinnedMeshRenderer destino = copia.AddComponent<SkinnedMeshRenderer>();
            destino.sharedMesh = origen.sharedMesh;
            destino.sharedMaterials = origen.sharedMaterials;
            destino.localBounds = origen.localBounds;
            destino.updateWhenOffscreen = false;

            Transform[] huesos = new Transform[origen.bones.Length];
            for (int i = 0; i < origen.bones.Length; i++)
            {
                Transform huesoOrigen = origen.bones[i];
                if (huesoOrigen != null && huesosBase.TryGetValue(huesoOrigen.name, out Transform huesoBase))
                {
                    huesos[i] = huesoBase;
                }
                else
                {
                    Debug.LogWarning($"[Lamento] El hueso '{huesoOrigen?.name}' de {origen.name} no existe en el cuerpo base.");
                }
            }

            destino.bones = huesos;
            if (origen.rootBone != null && huesosBase.TryGetValue(origen.rootBone.name, out Transform raizBase))
            {
                destino.rootBone = raizBase;
            }
        }

        Object.DestroyImmediate(pieza);
    }

    private static float MedirAltura(GameObject modelo)
    {
        Renderer[] renderers = modelo.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return alturaModeloPorDefecto;
        }

        Bounds limites = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            limites.Encapsulate(renderer.bounds);
        }

        float altura = limites.size.y / Mathf.Max(0.0001f, modelo.transform.lossyScale.y);
        if (altura < 0.5f || altura > 3f)
        {
            Debug.LogWarning($"[Lamento] Altura medida extraña ({altura:0.00} m); uso {alturaModeloPorDefecto} m.");
            return alturaModeloPorDefecto;
        }

        return altura;
    }

    // ───────────────────────────── Materiales URP ─────────────────────────────

    private static Dictionary<string, Material> CrearMateriales()
    {
        UtilidadesEditorLamento.AsegurarCarpeta(carpetaMateriales);

        return new Dictionary<string, Material>
        {
            { "Piel", CrearMaterial("Itzel_Piel", "T_Superhero_Female_Dark_BaseColor", "T_Superhero_Female_Normal", 0.35f, false) },
            { "Ojos", CrearMaterial("Itzel_Ojos", "T_Eye_Brown", null, 0.8f, false) },
            { "Cabello", CrearMaterial("Itzel_Cabello", "T_Hair_2_BaseColor", "T_Hair_2_Normal", 0.3f, true) },
            { "Ropa", CrearMaterial("Itzel_Ropa", "T_Peasant_Itzel_BaseColor", "T_Peasant_Normal", 0.2f, false) },
        };
    }

    private static Material CrearMaterial(string nombre, string texturaBase, string texturaNormal, float suavidad, bool recorteAlfa)
    {
        string ruta = $"{carpetaMateriales}/{nombre}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        Shader shaderLit = Shader.Find("Universal Render Pipeline/Lit");

        if (material == null)
        {
            material = new Material(shaderLit);
            AssetDatabase.CreateAsset(material, ruta);
        }
        else
        {
            material.shader = shaderLit;
        }

        material.SetTexture("_BaseMap", CargarTextura(texturaBase));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", suavidad);

        if (!string.IsNullOrEmpty(texturaNormal))
        {
            material.SetTexture("_BumpMap", CargarTextura(texturaNormal));
            material.EnableKeyword("_NORMALMAP");
        }

        material.SetFloat("_AlphaClip", recorteAlfa ? 1f : 0f);
        material.SetFloat("_Cutoff", 0.5f);
        if (recorteAlfa)
        {
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        else
        {
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = -1;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D CargarTextura(string nombre)
    {
        Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>($"{carpetaTexturas}{nombre}.png");
        if (textura == null)
        {
            Debug.LogWarning($"[Lamento] No se encontró la textura {nombre}.png.");
        }

        return textura;
    }

    /// <summary>Cambia los materiales importados (MI_...) por los materiales URP de Itzel según su nombre.</summary>
    private static void AsignarMateriales(GameObject modelo, Dictionary<string, Material> materiales)
    {
        foreach (Renderer renderer in modelo.GetComponentsInChildren<Renderer>(true))
        {
            Material[] actuales = renderer.sharedMaterials;
            for (int i = 0; i < actuales.Length; i++)
            {
                string nombre = actuales[i] != null ? actuales[i].name : renderer.name;
                actuales[i] = materiales[ClasificarMaterial(nombre, renderer.name)];
            }

            renderer.sharedMaterials = actuales;
        }
    }

    private static string ClasificarMaterial(string nombreMaterial, string nombreMalla)
    {
        string texto = (nombreMaterial + " " + nombreMalla).ToLowerInvariant();
        if (texto.Contains("eye") && !texto.Contains("eyebrow"))
        {
            return "Ojos";
        }

        if (texto.Contains("hair") || texto.Contains("eyebrow"))
        {
            return "Cabello";
        }

        if (texto.Contains("peasant"))
        {
            return "Ropa";
        }

        return "Piel";
    }
}
