using UnityEngine;
using UnityEngine.SceneManagement;

public class NivelPruebasItzel : MonoBehaviour
{
    [Header("Nivel de pruebas")]
    [Tooltip("Nombre del objeto raíz creado durante el modo Play.")]
    [SerializeField] private string nombreRaiz = "Nivel de pruebas - Itzel";
    [Tooltip("Posición inicial de Itzel en el recorrido tutorial.")]
    [SerializeField] private Vector3 posicionInicio = new Vector3(-9f, 0f, 0f);
    [Tooltip("Nombre de la capa que se usará como suelo si existe en el proyecto.")]
    [SerializeField] private string nombreCapaSuelo = "Ground";

    private const float GrosorPlataforma = 1f;
    private Transform raizNivel;
    private int capaSuelo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearNivelAlIniciar()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene")
        {
            return;
        }

        if (FindFirstObjectByType<NivelPruebasItzel>() == null)
        {
            GameObject objetoNivel = new GameObject("Iniciador nivel de pruebas");
            objetoNivel.AddComponent<NivelPruebasItzel>();
        }
    }

    private void Awake()
    {
        CrearNivel();
    }

    private void CrearNivel()
    {
        if (GameObject.Find(nombreRaiz) != null)
        {
            return;
        }

        capaSuelo = LayerMask.NameToLayer(nombreCapaSuelo);
        if (capaSuelo < 0)
        {
            capaSuelo = 0;
            Debug.LogWarning("No existe la capa Ground. El nivel de pruebas usará Default hasta que la crees.");
        }

        raizNivel = new GameObject(nombreRaiz).transform;
        CrearJugadora();
        CrearEscenario();
        ConfigurarCamara();
        ConfigurarLuz();
    }

    private void CrearJugadora()
    {
        GameObject jugador = GameObject.Find("Player");
        if (jugador == null)
        {
            jugador = new GameObject("Player");
            jugador.AddComponent<Rigidbody>();
            jugador.AddComponent<CapsuleCollider>();
        }

        sus_control controlAnterior = jugador.GetComponent<sus_control>();
        if (controlAnterior != null)
        {
            controlAnterior.enabled = false;
        }

        ItzelController control = jugador.GetComponent<ItzelController>();
        if (control == null)
        {
            control = jugador.AddComponent<ItzelController>();
        }

        jugador.transform.position = posicionInicio;
        jugador.transform.rotation = Quaternion.identity;
        control.EstablecerPuntoInicio(posicionInicio);
        control.ConfigurarMascaraSuelo(1 << capaSuelo);

        if (jugador.transform.Find("VisualItzelPruebas") == null)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "VisualItzelPruebas";
            visual.transform.SetParent(jugador.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            Destroy(visual.GetComponent<Collider>());
            AsignarColor(visual, new Color(0.95f, 0.62f, 0.32f));
        }
    }

    private void CrearEscenario()
    {
        CrearPlataforma("01 - Caminar y correr", new Vector3(-7f, -1.5f, 0f), new Vector3(8f, GrosorPlataforma, 3f), new Color(0.17f, 0.24f, 0.31f));
        CrearTexto("A / D: caminar     Shift: correr", new Vector3(-8f, 1.5f, 0.1f));

        CrearPlataforma("02 - Salto y coyote", new Vector3(0.8f, -1.5f, 0f), new Vector3(5f, GrosorPlataforma, 3f), new Color(0.22f, 0.31f, 0.4f));
        CrearTexto("Salta el hueco: prueba coyote y buffer", new Vector3(0.5f, 1.5f, 0.1f));

        CrearPlataforma("03 - Plataforma elevada", new Vector3(5.1f, 0f, 0f), new Vector3(2.5f, GrosorPlataforma, 3f), new Color(0.29f, 0.4f, 0.5f));
        CrearTexto("Mantén Espacio para salto alto", new Vector3(5.1f, 2.2f, 0.1f));

        CrearPlataforma("04 - Tunel agachado", new Vector3(10f, -1.5f, 0f), new Vector3(5f, GrosorPlataforma, 3f), new Color(0.17f, 0.28f, 0.3f));
        CrearBloque("Techo bajo - mantener C", new Vector3(10f, 0.55f, 0f), new Vector3(4f, 0.5f, 3f), new Color(0.34f, 0.22f, 0.25f));
        CrearTexto("Mantén C para cruzar", new Vector3(10f, 1.7f, 0.1f));

        CrearPlataforma("05 - Plataforma final", new Vector3(15f, -1.5f, 0f), new Vector3(3f, GrosorPlataforma, 3f), new Color(0.32f, 0.37f, 0.23f));
        CrearTexto("Aléjate del borde: zona de reaparición", new Vector3(15f, 1.5f, 0.1f));

        CrearZonaMuerte();
        CrearBloque("Límite izquierdo", new Vector3(-12f, 1f, 0f), new Vector3(0.5f, 4f, 3f), new Color(0.1f, 0.1f, 0.12f));
        CrearBloque("Límite derecho", new Vector3(18f, 1f, 0f), new Vector3(0.5f, 4f, 3f), new Color(0.1f, 0.1f, 0.12f));
    }

    private void CrearZonaMuerte()
    {
        GameObject zonaMuerte = new GameObject("Zona de muerte - pruebas");
        zonaMuerte.transform.SetParent(raizNivel);
        zonaMuerte.transform.position = new Vector3(3f, -5f, 0f);
        BoxCollider colisionador = zonaMuerte.AddComponent<BoxCollider>();
        colisionador.isTrigger = true;
        colisionador.size = new Vector3(40f, 1f, 10f);
        zonaMuerte.AddComponent<ZonaMuertePruebas>();
    }

    private void ConfigurarCamara()
    {
        Camera camara = Camera.main;
        if (camara == null)
        {
            GameObject objetoCamara = new GameObject("Main Camera");
            objetoCamara.tag = "MainCamera";
            camara = objetoCamara.AddComponent<Camera>();
        }

        camara.fieldOfView = 33f;
        camara.nearClipPlane = 0.1f;
        camara.farClipPlane = 100f;
        camara.backgroundColor = new Color(0.035f, 0.055f, 0.08f);
        CamaraPruebasItzel seguimiento = camara.GetComponent<CamaraPruebasItzel>();
        if (seguimiento == null)
        {
            seguimiento = camara.gameObject.AddComponent<CamaraPruebasItzel>();
        }

        seguimiento.Configurar(GameObject.Find("Player").transform);
    }

    private void ConfigurarLuz()
    {
        Light luz = FindFirstObjectByType<Light>();
        if (luz == null)
        {
            GameObject objetoLuz = new GameObject("Luz direccional de pruebas");
            luz = objetoLuz.AddComponent<Light>();
            luz.type = LightType.Directional;
        }

        luz.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        luz.intensity = 1.2f;
        luz.color = new Color(0.62f, 0.73f, 1f);
    }

    private void CrearPlataforma(string nombre, Vector3 posicion, Vector3 escala, Color color)
    {
        CrearBloque(nombre, posicion, escala, color);
    }

    private void CrearBloque(string nombre, Vector3 posicion, Vector3 escala, Color color)
    {
        GameObject bloque = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bloque.name = nombre;
        bloque.transform.SetParent(raizNivel);
        bloque.transform.position = posicion;
        bloque.transform.localScale = escala;
        bloque.layer = capaSuelo;
        AsignarColor(bloque, color);
    }

    private void CrearTexto(string contenido, Vector3 posicion)
    {
        GameObject objetoTexto = new GameObject("Indicador - " + contenido);
        objetoTexto.transform.SetParent(raizNivel);
        objetoTexto.transform.position = posicion;
        objetoTexto.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        TextMesh texto = objetoTexto.AddComponent<TextMesh>();
        texto.text = contenido;
        texto.anchor = TextAnchor.MiddleCenter;
        texto.alignment = TextAlignment.Center;
        texto.fontSize = 42;
        texto.characterSize = 0.08f;
        texto.color = new Color(0.9f, 0.94f, 1f);
    }

    private void AsignarColor(GameObject objeto, Color color)
    {
        Renderer renderizador = objeto.GetComponent<Renderer>();
        if (renderizador == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        renderizador.material = material;
    }
}
