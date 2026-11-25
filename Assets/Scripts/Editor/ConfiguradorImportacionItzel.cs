using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Configura automáticamente la importación de los modelos, animaciones y texturas de Itzel
/// que viven en Assets/Models/Itzel/. Se ejecuta cada vez que Unity importa uno de esos archivos.
/// </summary>
public class ConfiguradorImportacionItzel : AssetPostprocessor
{
    private const string carpetaItzel = "Assets/Models/Itzel/";
    private const string carpetaAnimaciones = "Assets/Models/Itzel/Animaciones/";
    private const string carpetaTexturas = "Assets/Models/Itzel/Texturas/";
    private const string archivoCuerpo = "Superhero_Female_FullBody.fbx";
    private const string sufijoBucle = "_Loop";
    private const int tamanoMaximoTextura = 2048;

    // ───────────────────────────── Modelos ─────────────────────────────

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(carpetaItzel))
        {
            return;
        }

        ModelImporter importador = (ModelImporter)assetImporter;
        string nombreArchivo = Path.GetFileName(assetPath);
        bool esAnimacion = assetPath.StartsWith(carpetaAnimaciones);
        bool esCuerpo = nombreArchivo == archivoCuerpo;

        // Escala 1: los FBX de Quaternius ya vienen en metros. La altura final de Itzel
        // (unos 1.2 m) la ajusta ConstructorPrefabItzel escalando el hijo visual.
        importador.globalScale = 1f;
        importador.useFileScale = true;

        // Conservamos los nombres de material (MI_...) para reemplazarlos por materiales URP propios.
        importador.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importador.materialLocation = ModelImporterMaterialLocation.InPrefab;

        if (esCuerpo || esAnimacion)
        {
            // Rig Humanoid: Unity traduce los huesos a un esqueleto estándar (cabeza, brazos, piernas)
            // y por eso cualquier animación Humanoid se puede reutilizar en Itzel (retargeting).
            importador.animationType = ModelImporterAnimationType.Human;
            importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }
        else
        {
            // Ropa y cabello: solo aportan mallas. Sus huesos se reasignan al esqueleto del cuerpo.
            importador.animationType = ModelImporterAnimationType.Generic;
        }

        importador.importAnimation = esAnimacion;
    }

    // ───────────────────────────── Animaciones ─────────────────────────────

    private void OnPreprocessAnimation()
    {
        if (!assetPath.StartsWith(carpetaAnimaciones))
        {
            return;
        }

        ModelImporter importador = (ModelImporter)assetImporter;
        ModelImporterClipAnimation[] clips = importador.defaultClipAnimations;

        foreach (ModelImporterClipAnimation clip in clips)
        {
            bool esBucle = clip.name.EndsWith(sufijoBucle) || clip.takeName.EndsWith(sufijoBucle);

            // idle, caminar, correr, agachada y caída se repiten; el resto se reproduce una vez.
            clip.loopTime = esBucle;
            clip.loopPose = esBucle;

            // El Rigidbody mueve a Itzel; la animación se queda en su sitio ("in place").
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionY = true;
            clip.keepOriginalPositionXZ = true;
        }

        importador.clipAnimations = clips;
    }

    // ───────────────────────────── Texturas ─────────────────────────────

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(carpetaTexturas))
        {
            return;
        }

        TextureImporter importador = (TextureImporter)assetImporter;
        string nombreArchivo = Path.GetFileNameWithoutExtension(assetPath);

        importador.maxTextureSize = tamanoMaximoTextura;
        importador.mipmapEnabled = true;

        if (nombreArchivo.EndsWith("_Normal"))
        {
            importador.textureType = TextureImporterType.NormalMap;
            return;
        }

        importador.textureType = TextureImporterType.Default;
        importador.sRGBTexture = true;
        importador.alphaIsTransparency = nombreArchivo.Contains("Hair");
    }

    // ───────────────────────────── Menú ─────────────────────────────

    [MenuItem("Lamento/Reimportar modelos de Itzel", priority = 0)]
    private static void ReimportarModelosItzel()
    {
        string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { carpetaItzel.TrimEnd('/') });
        int total = 0;

        foreach (string guid in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(ruta))
            {
                continue;
            }

            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            total++;
        }

        Debug.Log($"[Lamento] Reimportados {total} archivos de Itzel con rig Humanoid y animaciones en bucle.");
    }
}
