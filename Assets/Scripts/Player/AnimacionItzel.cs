using UnityEngine;

/// <summary>
/// Traduce el estado de ItzelController a parámetros del Animator.
/// Va en el mismo GameObject que el Animator (el modelo visual, hijo de Itzel).
/// No mueve a Itzel: solo informa a la máquina de estados qué está pasando.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimacionItzel : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Controlador de movimiento de Itzel. Si se deja vacío se busca en los padres.")]
    [SerializeField] private ItzelController controlador;
    [Tooltip("Animator del modelo. Si se deja vacío se usa el de este GameObject.")]
    [SerializeField] private Animator animador;

    [Header("Suavizado")]
    [Tooltip("Segundos que tarda el parámetro Velocidad en alcanzar el valor real; evita saltos bruscos en el Blend Tree.")]
    [SerializeField] private float tiempoSuavizadoVelocidad = 0.08f;

    private static readonly int parametroVelocidad = Animator.StringToHash("Velocidad");
    private static readonly int parametroEnSuelo = Animator.StringToHash("EnSuelo");
    private static readonly int parametroAgachada = Animator.StringToHash("Agachada");
    private static readonly int parametroVelocidadVertical = Animator.StringToHash("VelocidadVertical");
    private static readonly int parametroSaltar = Animator.StringToHash("Saltar");

    private Rigidbody cuerpoRigido;

    private void Awake()
    {
        if (animador == null)
        {
            animador = GetComponent<Animator>();
        }

        if (controlador == null)
        {
            controlador = GetComponentInParent<ItzelController>();
        }

        if (controlador == null)
        {
            Debug.LogError("AnimacionItzel necesita un ItzelController en este objeto o en uno de sus padres.", this);
            enabled = false;
            return;
        }

        cuerpoRigido = controlador.GetComponent<Rigidbody>();

        // El Rigidbody mueve a Itzel; la animación no debe desplazarla.
        animador.applyRootMotion = false;
    }

    private void OnEnable()
    {
        if (controlador == null)
        {
            return;
        }

        controlador.AlSaltar += ManejarSalto;
        controlador.AlAterrizar += ManejarAterrizaje;
    }

    private void OnDisable()
    {
        if (controlador == null)
        {
            return;
        }

        controlador.AlSaltar -= ManejarSalto;
        controlador.AlAterrizar -= ManejarAterrizaje;
    }

    private void Update()
    {
        animador.SetFloat(parametroVelocidad, controlador.VelocidadActual, tiempoSuavizadoVelocidad, Time.deltaTime);
        animador.SetBool(parametroEnSuelo, controlador.EnSuelo);
        animador.SetBool(parametroAgachada, controlador.EstaAgachada);
        animador.SetFloat(parametroVelocidadVertical, cuerpoRigido != null ? cuerpoRigido.linearVelocity.y : 0f);
    }

    private void ManejarSalto()
    {
        animador.SetTrigger(parametroSaltar);
    }

    private void ManejarAterrizaje(float velocidadImpacto)
    {
        // Si el trigger quedó pendiente (por ejemplo, un salto cancelado), lo limpiamos al tocar suelo.
        animador.ResetTrigger(parametroSaltar);
    }
}
