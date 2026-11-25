using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class ItzelController : MonoBehaviour
{
    [Header("Entrada")]
    [Tooltip("Asset Input System que contiene el mapa Player.")]
    [SerializeField] private InputActionAsset accionesEntrada;

    [Header("Movimiento")]
    [Tooltip("Velocidad máxima al caminar, en metros por segundo.")]
    [SerializeField] private float velocidadCaminar = 3.5f;
    [Tooltip("Velocidad máxima al correr, en metros por segundo.")]
    [SerializeField] private float velocidadCorrer = 6.5f;
    [Tooltip("Multiplicador de velocidad mientras Itzel está agachada.")]
    [SerializeField] private float multiplicadorVelocidadAgachada = 0.55f;
    [Tooltip("Cambio de velocidad horizontal por segundo mientras está en el suelo.")]
    [SerializeField] private float aceleracionSuelo = 30f;
    [Tooltip("Cambio de velocidad horizontal por segundo mientras está en el aire.")]
    [SerializeField] private float aceleracionAire = 16f;

    [Header("Salto")]
    [Tooltip("Impulso vertical aplicado al iniciar un salto.")]
    [SerializeField] private float fuerzaSalto = 7f;
    [Tooltip("Cantidad total de saltos disponibles tras aterrizar.")]
    [SerializeField] private int saltosMaximos = 1;
    [Tooltip("Segundos durante los que aún se permite saltar tras abandonar un borde.")]
    [SerializeField] private float tiempoCoyote = 0.1f;
    [Tooltip("Segundos durante los que se recuerda una pulsación de salto antes de aterrizar.")]
    [SerializeField] private float tiempoBufferSalto = 0.12f;
    [Tooltip("Fracción de la velocidad vertical que queda al soltar el botón durante el ascenso.")]
    [Range(0f, 1f)]
    [SerializeField] private float multiplicadorSaltoCorto = 0.5f;
    [Tooltip("Multiplicador adicional de gravedad mientras Itzel cae.")]
    [SerializeField] private float multiplicadorGravedadCaida = 2.5f;
    [Tooltip("Velocidad vertical máxima hacia abajo, en metros por segundo.")]
    [SerializeField] private float velocidadMaximaCaida = 20f;

    [Header("Comprobación de suelo")]
    [Tooltip("Desplazamiento local desde el pivote del jugador hasta los pies.")]
    [SerializeField] private Vector3 desplazamientoComprobacionSuelo = new Vector3(0f, -0.95f, 0f);
    [Tooltip("Radio de la esfera usada para detectar el suelo.")]
    [SerializeField] private float radioComprobacionSuelo = 0.2f;
    [Tooltip("Capas consideradas suelo para movimiento, salto y techo.")]
    [SerializeField] private LayerMask mascaraSuelo;

    [Header("Agacharse")]
    [Tooltip("Multiplicador de la altura normal de la cápsula al agacharse.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float multiplicadorAlturaAgachada = 0.55f;

    [Header("Visual")]
    [Tooltip("Hijo visual que gira al cambiar la dirección horizontal. No asignes el Rigidbody.")]
    [SerializeField] private Transform modeloVisual;
    [Tooltip("Actívalo si el hijo visual apunta hacia +X antes de ejecutar el juego.")]
    [SerializeField] private bool modeloMiraDerechaPorDefecto = true;

    public event Action AlSaltar;
    public event Action<float> AlAterrizar;

    public bool EnSuelo { get; private set; }
    public bool EstaCorriendo { get; private set; }
    public bool EstaAgachada { get; private set; }
    public float VelocidadActual => cuerpoRigido == null ? 0f : Mathf.Abs(cuerpoRigido.linearVelocity.x);

    private Rigidbody cuerpoRigido;
    private CapsuleCollider capsula;
    private InputAction accionMover;
    private InputAction accionSaltar;
    private InputAction accionCorrer;
    private InputAction accionAgacharse;
    private Vector2 entradaMovimiento;
    private bool quiereCorrer;
    private bool quiereAgacharse;
    private bool cortarSalto;
    private bool sueloAnterior;
    private bool estadoSueloInicializado;
    private int saltosRestantes;
    private float temporizadorCoyote;
    private float temporizadorBufferSalto;
    private float ultimaVelocidadVertical;
    private float alturaCapsulaNormal;
    private Vector3 centroCapsulaNormal;
    private Quaternion rotacionVisualInicial;
    private Vector3 puntoInicio;

    private void Awake()
    {
        cuerpoRigido = GetComponent<Rigidbody>();
        capsula = GetComponent<CapsuleCollider>();

        cuerpoRigido.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        cuerpoRigido.interpolation = RigidbodyInterpolation.Interpolate;
        cuerpoRigido.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Vector3 posicionInicial = cuerpoRigido.position;
        posicionInicial.z = 0f;
        cuerpoRigido.position = posicionInicial;
        puntoInicio = posicionInicial;
        saltosRestantes = Mathf.Max(1, saltosMaximos);

        alturaCapsulaNormal = capsula.height;
        centroCapsulaNormal = capsula.center;

        if (modeloVisual != null)
        {
            rotacionVisualInicial = modeloVisual.localRotation;
        }

        BuscarAccionesEntrada();
    }

    private void OnEnable()
    {
        HabilitarAccion(accionMover);
        HabilitarAccion(accionSaltar);
        HabilitarAccion(accionCorrer);
        HabilitarAccion(accionAgacharse);
    }

    private void OnDisable()
    {
        DeshabilitarAccion(accionMover);
        DeshabilitarAccion(accionSaltar);
        DeshabilitarAccion(accionCorrer);
        DeshabilitarAccion(accionAgacharse);
    }

    private void Update()
    {
        ActualizarEntrada();
        ActualizarSueloYTemporizadores();
        ActualizarAgachado();
        ActualizarOrientacionVisual();
    }

    private void FixedUpdate()
    {
        AplicarMovimientoHorizontal();
        IntentarSaltar();
        AplicarCorteDeSalto();
        AplicarGravedadDeCaida();
        LimitarVelocidadDeCaida();
        ultimaVelocidadVertical = cuerpoRigido.linearVelocity.y;
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (otro.GetComponent<ZonaMuertePruebas>() != null || TieneTagZonaMuerte(otro))
        {
            Reaparecer(puntoInicio);
        }
    }

    public void Reaparecer(Vector3 posicion)
    {
        posicion.z = 0f;
        cuerpoRigido.linearVelocity = Vector3.zero;
        cuerpoRigido.position = posicion;
        temporizadorCoyote = 0f;
        temporizadorBufferSalto = 0f;
        saltosRestantes = Mathf.Max(1, saltosMaximos);
    }

    public void EstablecerPuntoInicio(Vector3 posicion)
    {
        puntoInicio = posicion;
        puntoInicio.z = 0f;
    }

    public void ConfigurarMascaraSuelo(LayerMask nuevaMascara)
    {
        mascaraSuelo = nuevaMascara;
    }

    private void BuscarAccionesEntrada()
    {
        if (accionesEntrada == null)
        {
            accionesEntrada = CrearAccionesDePrueba();
            Debug.LogWarning("ItzelController usa controles de prueba porque no tiene un InputActionAsset asignado.", this);
        }

        accionMover = accionesEntrada.FindAction("Player/Move", false);
        accionSaltar = accionesEntrada.FindAction("Player/Jump", false);
        accionCorrer = accionesEntrada.FindAction("Player/Sprint", false);
        accionAgacharse = accionesEntrada.FindAction("Player/Crouch", false);

        if (accionMover == null || accionSaltar == null || accionCorrer == null || accionAgacharse == null)
        {
            Debug.LogError("Faltan una o más acciones requeridas en el mapa Player: Move, Jump, Sprint y Crouch.", this);
        }
    }

    private void ActualizarEntrada()
    {
        entradaMovimiento = accionMover != null ? accionMover.ReadValue<Vector2>() : Vector2.zero;
        quiereCorrer = accionCorrer != null && accionCorrer.IsPressed();
        quiereAgacharse = accionAgacharse != null && accionAgacharse.IsPressed();

        if (accionSaltar == null)
        {
            return;
        }

        if (accionSaltar.WasPressedThisFrame())
        {
            temporizadorBufferSalto = tiempoBufferSalto;
        }

        if (accionSaltar.WasReleasedThisFrame() && cuerpoRigido.linearVelocity.y > 0f)
        {
            cortarSalto = true;
        }
    }

    private void ActualizarSueloYTemporizadores()
    {
        bool sueloDetectado = Physics.CheckSphere(
            ObtenerPosicionComprobacionSuelo(),
            radioComprobacionSuelo,
            mascaraSuelo,
            QueryTriggerInteraction.Ignore);
        EnSuelo = sueloDetectado && cuerpoRigido.linearVelocity.y <= 0f;

        temporizadorBufferSalto = Mathf.Max(0f, temporizadorBufferSalto - Time.deltaTime);

        if (!estadoSueloInicializado)
        {
            estadoSueloInicializado = true;
            sueloAnterior = EnSuelo;
        }

        if (EnSuelo)
        {
            temporizadorCoyote = tiempoCoyote;

            if (!sueloAnterior)
            {
                saltosRestantes = Mathf.Max(1, saltosMaximos);
                AlAterrizar?.Invoke(Mathf.Abs(ultimaVelocidadVertical));
            }
        }
        else
        {
            temporizadorCoyote = Mathf.Max(0f, temporizadorCoyote - Time.deltaTime);

            if (sueloAnterior && temporizadorCoyote <= 0f)
            {
                ConsumirSaltoDeSueloAlCaer();
            }

            if (!sueloAnterior && temporizadorCoyote <= 0f)
            {
                ConsumirSaltoDeSueloAlCaer();
            }
        }

        sueloAnterior = EnSuelo;
    }

    private void AplicarMovimientoHorizontal()
    {
        float direccionHorizontal = Mathf.Clamp(entradaMovimiento.x, -1f, 1f);
        float velocidadObjetivo = direccionHorizontal * ObtenerVelocidadMaxima();
        float aceleracion = EnSuelo ? aceleracionSuelo : aceleracionAire;
        float velocidadX = Mathf.MoveTowards(
            cuerpoRigido.linearVelocity.x,
            velocidadObjetivo,
            aceleracion * Time.fixedDeltaTime);

        cuerpoRigido.linearVelocity = new Vector3(velocidadX, cuerpoRigido.linearVelocity.y, 0f);
    }

    private void IntentarSaltar()
    {
        if (temporizadorBufferSalto <= 0f)
        {
            return;
        }

        bool puedeUsarSaltoDeSuelo = EnSuelo || temporizadorCoyote > 0f;
        bool puedeUsarSaltoAereo = !puedeUsarSaltoDeSuelo && saltosRestantes > 0;
        if (!puedeUsarSaltoDeSuelo && !puedeUsarSaltoAereo)
        {
            return;
        }

        cuerpoRigido.linearVelocity = new Vector3(cuerpoRigido.linearVelocity.x, 0f, 0f);
        cuerpoRigido.AddForce(Vector3.up * fuerzaSalto, ForceMode.VelocityChange);
        saltosRestantes = Mathf.Max(0, saltosRestantes - 1);
        temporizadorBufferSalto = 0f;
        temporizadorCoyote = 0f;
        EnSuelo = false;
        AlSaltar?.Invoke();
    }

    private void AplicarCorteDeSalto()
    {
        if (!cortarSalto)
        {
            return;
        }

        if (cuerpoRigido.linearVelocity.y > 0f)
        {
            cuerpoRigido.linearVelocity = new Vector3(
                cuerpoRigido.linearVelocity.x,
                cuerpoRigido.linearVelocity.y * multiplicadorSaltoCorto,
                0f);
        }

        cortarSalto = false;
    }

    private void AplicarGravedadDeCaida()
    {
        if (cuerpoRigido.linearVelocity.y < 0f)
        {
            Vector3 gravedadExtra = Physics.gravity * (multiplicadorGravedadCaida - 1f);
            cuerpoRigido.AddForce(gravedadExtra, ForceMode.Acceleration);
        }
    }

    private void LimitarVelocidadDeCaida()
    {
        if (cuerpoRigido.linearVelocity.y < -velocidadMaximaCaida)
        {
            cuerpoRigido.linearVelocity = new Vector3(
                cuerpoRigido.linearVelocity.x,
                -velocidadMaximaCaida,
                0f);
        }
    }

    private void ActualizarAgachado()
    {
        if (quiereAgacharse)
        {
            EstablecerAgachada(true);
        }
        else if (EstaAgachada && PuedeLevantarse())
        {
            EstablecerAgachada(false);
        }

        EstaCorriendo = quiereCorrer && !EstaAgachada && Mathf.Abs(entradaMovimiento.x) > 0f;
    }

    private void EstablecerAgachada(bool agachada)
    {
        EstaAgachada = agachada;
        capsula.height = agachada
            ? Mathf.Max(capsula.radius * 2f, alturaCapsulaNormal * multiplicadorAlturaAgachada)
            : alturaCapsulaNormal;
        capsula.center = agachada
            ? centroCapsulaNormal - Vector3.up * (alturaCapsulaNormal - capsula.height) * 0.5f
            : centroCapsulaNormal;
    }

    private bool PuedeLevantarse()
    {
        ObtenerExtremosCapsulaDePie(out Vector3 extremoInferior, out Vector3 extremoSuperior);
        return !Physics.CheckCapsule(
            extremoInferior,
            extremoSuperior,
            capsula.radius,
            mascaraSuelo,
            QueryTriggerInteraction.Ignore);
    }

    private void ObtenerExtremosCapsulaDePie(out Vector3 extremoInferior, out Vector3 extremoSuperior)
    {
        Vector3 centroMundo = transform.TransformPoint(centroCapsulaNormal);
        float semialturaRecta = Mathf.Max(0f, alturaCapsulaNormal * 0.5f - capsula.radius);
        Vector3 desplazamientoVertical = transform.up * semialturaRecta;
        extremoInferior = centroMundo - desplazamientoVertical;
        extremoSuperior = centroMundo + desplazamientoVertical;
    }

    private float ObtenerVelocidadMaxima()
    {
        if (EstaAgachada)
        {
            return velocidadCaminar * multiplicadorVelocidadAgachada;
        }

        return quiereCorrer ? velocidadCorrer : velocidadCaminar;
    }

    private void ConsumirSaltoDeSueloAlCaer()
    {
        saltosRestantes = Mathf.Min(saltosRestantes, Mathf.Max(0, saltosMaximos - 1));
    }

    private void ActualizarOrientacionVisual()
    {
        if (modeloVisual == null || Mathf.Approximately(entradaMovimiento.x, 0f))
        {
            return;
        }

        bool miraDerecha = entradaMovimiento.x > 0f;
        if (!modeloMiraDerechaPorDefecto)
        {
            miraDerecha = !miraDerecha;
        }

        modeloVisual.localRotation = rotacionVisualInicial * Quaternion.Euler(0f, miraDerecha ? 0f : 180f, 0f);
    }

    private Vector3 ObtenerPosicionComprobacionSuelo()
    {
        return transform.TransformPoint(desplazamientoComprobacionSuelo);
    }

    private void HabilitarAccion(InputAction accion)
    {
        if (accion != null)
        {
            accion.Enable();
        }
    }

    private InputActionAsset CrearAccionesDePrueba()
    {
        InputActionAsset assetTemporal = ScriptableObject.CreateInstance<InputActionAsset>();
        InputActionMap mapaJugador = new InputActionMap("Player");
        InputAction mover = mapaJugador.AddAction("Move", InputActionType.Value);
        mover.AddCompositeBinding("2DVector")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s");
        mover.AddCompositeBinding("2DVector")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow");
        mover.AddBinding("<Gamepad>/leftStick");

        InputAction saltar = mapaJugador.AddAction("Jump", InputActionType.Button);
        saltar.AddBinding("<Keyboard>/space");
        saltar.AddBinding("<Gamepad>/buttonSouth");

        InputAction correr = mapaJugador.AddAction("Sprint", InputActionType.Button);
        correr.AddBinding("<Keyboard>/leftShift");
        correr.AddBinding("<Gamepad>/rightTrigger");

        InputAction agacharse = mapaJugador.AddAction("Crouch", InputActionType.Button);
        agacharse.AddBinding("<Keyboard>/c");
        agacharse.AddBinding("<Gamepad>/buttonEast");

        assetTemporal.AddActionMap(mapaJugador);
        return assetTemporal;
    }

    private void DeshabilitarAccion(InputAction accion)
    {
        if (accion != null)
        {
            accion.Disable();
        }
    }

    private bool TieneTagZonaMuerte(Collider otro)
    {
        try
        {
            return otro.CompareTag("DeathZone");
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(ObtenerPosicionComprobacionSuelo(), radioComprobacionSuelo);

        if (capsula == null)
        {
            capsula = GetComponent<CapsuleCollider>();
        }

        if (capsula == null)
        {
            return;
        }

        float alturaAgachada = Mathf.Max(capsula.radius * 2f, capsula.height * multiplicadorAlturaAgachada);
        Vector3 centroAgachado = capsula.center - Vector3.up * (capsula.height - alturaAgachada) * 0.5f;
        Gizmos.color = Color.cyan;
        DibujarCapsulaGizmo(transform.TransformPoint(centroAgachado), capsula.radius, alturaAgachada);
        Gizmos.DrawWireCube(
            transform.TransformPoint(centroAgachado),
            new Vector3(capsula.radius * 2f, alturaAgachada, capsula.radius * 2f));
    }

    private void DibujarCapsulaGizmo(Vector3 centro, float radio, float altura)
    {
        float semialturaRecta = Mathf.Max(0f, altura * 0.5f - radio);
        Vector3 desplazamientoVertical = transform.up * semialturaRecta;
        Gizmos.DrawWireSphere(centro - desplazamientoVertical, radio);
        Gizmos.DrawWireSphere(centro + desplazamientoVertical, radio);
    }
}
