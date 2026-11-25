using UnityEngine;

public class CamaraPruebasItzel : MonoBehaviour
{
    [SerializeField] private Transform objetivo;
    [SerializeField] private Vector3 desplazamiento = new Vector3(0f, 4.5f, -18f);
    [SerializeField] private float alturaMira = 0.75f;
    [SerializeField] private float suavizado = 8f;

    public void Configurar(Transform nuevoObjetivo)
    {
        objetivo = nuevoObjetivo;
        ActualizarCamara(true);
    }

    private void LateUpdate()
    {
        ActualizarCamara(false);
    }

    private void ActualizarCamara(bool instantaneo)
    {
        if (objetivo == null)
        {
            return;
        }

        Vector3 puntoMira = objetivo.position + Vector3.up * alturaMira;
        Vector3 posicionObjetivo = objetivo.position + desplazamiento;
        float factor = instantaneo ? 1f : 1f - Mathf.Exp(-suavizado * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, factor);
        transform.rotation = Quaternion.LookRotation(puntoMira - transform.position, Vector3.up);
    }
}
