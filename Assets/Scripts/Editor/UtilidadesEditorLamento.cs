using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Funciones compartidas por los constructores de Editor de Lamento:
/// crear tags y capas desde código y asegurar que existan carpetas.
/// Los cambios a TagManager se hacen con SerializedObject (API de Unity), nunca editando el .asset a mano.
/// </summary>
public static class UtilidadesEditorLamento
{
    public const string tagSuelo = "Ground";
    public const string tagZonaMuerte = "DeathZone";
    public const string tagOfrenda = "Ofrenda";
    public const string tagJugador = "Player";
    public const string capaSuelo = "Ground";

    private const string rutaTagManager = "ProjectSettings/TagManager.asset";
    private const int primeraCapaDeUsuario = 8;
    private const int totalCapas = 32;

    /// <summary>Crea las tags y la capa que usa el juego si todavía no existen.</summary>
    public static void AsegurarTagsYCapas()
    {
        AsegurarTag(tagSuelo);
        AsegurarTag(tagZonaMuerte);
        AsegurarTag(tagOfrenda);
        AsegurarCapa(capaSuelo);
    }

    public static void AsegurarTag(string nombre)
    {
        foreach (string existente in UnityEditorInternal.InternalEditorUtility.tags)
        {
            if (existente == nombre)
            {
                return;
            }
        }

        SerializedObject tagManager = CargarTagManager();
        SerializedProperty tags = tagManager.FindProperty("tags");
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = nombre;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[Lamento] Se creó la tag '{nombre}'.");
    }

    /// <summary>Devuelve el índice de la capa; la crea en el primer espacio libre si no existe.</summary>
    public static int AsegurarCapa(string nombre)
    {
        int indice = LayerMask.NameToLayer(nombre);
        if (indice >= 0)
        {
            return indice;
        }

        SerializedObject tagManager = CargarTagManager();
        SerializedProperty capas = tagManager.FindProperty("layers");

        for (int i = primeraCapaDeUsuario; i < totalCapas; i++)
        {
            SerializedProperty capa = capas.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(capa.stringValue))
            {
                capa.stringValue = nombre;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[Lamento] Se creó la capa '{nombre}' en la posición {i}.");
                return i;
            }
        }

        Debug.LogError($"[Lamento] No hay espacio libre para crear la capa '{nombre}'.");
        return 0;
    }

    public static LayerMask ObtenerMascaraSuelo()
    {
        return 1 << AsegurarCapa(capaSuelo);
    }

    /// <summary>Crea una carpeta de Assets (y sus padres) usando AssetDatabase.</summary>
    public static void AsegurarCarpeta(string ruta)
    {
        ruta = ruta.Replace('\\', '/').TrimEnd('/');
        if (AssetDatabase.IsValidFolder(ruta))
        {
            return;
        }

        string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        string nombre = Path.GetFileName(ruta);
        AsegurarCarpeta(padre);
        AssetDatabase.CreateFolder(padre, nombre);
    }

    /// <summary>Convierte un color HEX del GDD (por ejemplo "#0B1D3A") en Color.</summary>
    public static Color ColorHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }

        Debug.LogWarning($"[Lamento] Color HEX inválido: {hex}");
        return Color.magenta;
    }

    private static SerializedObject CargarTagManager()
    {
        Object[] activos = AssetDatabase.LoadAllAssetsAtPath(rutaTagManager);
        return new SerializedObject(activos[0]);
    }
}
