using UnityEngine;

public class sus_control : MonoBehaviour
{

    public float velocidad = 6f;
    public float fuerzaSalto = 6f;
    public int saltosMaximos = 2;

    private Rigidbody rb;
    private Vector3 entrada;          // dirección que pide el jugador
    private bool quiereSaltar;
    private int saltosRestantes;
    private Vector3 puntoInicio;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;     // que no ruede
        puntoInicio = transform.position;
        saltosRestantes = saltosMaximos;
    }

    // Update is called once per frame
    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D o flechas
        float v = Input.GetAxisRaw("Vertical");   // W/S o flechas
        entrada = new Vector3(h, 0f, v).normalized;
        
        if (Input.GetKeyDown(KeyCode.Space) && saltosRestantes > 0)
            quiereSaltar = true;
    }

    void FixedUpdate()
    {
        Vector3 mov = entrada * velocidad;
        rb.linearVelocity = new Vector3(mov.x, rb.linearVelocity.y, mov.z);

        if (quiereSaltar)
        {
            // Reiniciar la caída para que el doble salto siempre se sienta igual
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.VelocityChange);
            saltosRestantes--;
            quiereSaltar = false;
        }
    }

    // Recuperar saltos solo al tocar suelo por ARRIBA (no paredes)
    void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.CompareTag("Ground")) return;
        foreach (ContactPoint c in col.contacts)
        {
            if (c.normal.y > 0.5f)
            {
                saltosRestantes = saltosMaximos;
                return;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathZone")) Reaparecer();
        else if (other.CompareTag("Finish")) Debug.Log("¡Meta alcanzada!");
    }

    void Reaparecer()
    {
        rb.linearVelocity = Vector3.zero;
        transform.position = puntoInicio;
        saltosRestantes = saltosMaximos;
    }
}
