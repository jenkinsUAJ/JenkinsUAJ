using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class LoadingBarController : MonoBehaviour
{
    [Header("Configuración Visual")]
    public Image barraCircular;
    public float tiempoRequerido = 0.5f; // Segundos necesarios para completar

    [Tooltip("Acción a ejecutar")]
    public UnityEvent alCompletar;

    private bool estaPresionado = false;
    private float temporizador = 0f;

    void Update()
    {
        if (estaPresionado)
        {
            // Aumentar el tiempo mientras se presiona
            temporizador += Time.deltaTime;
            barraCircular.fillAmount = temporizador / tiempoRequerido;

            // Comprobar si se ha llenado la tarta
            if (temporizador >= tiempoRequerido)
            {
                estaPresionado = false;
                barraCircular.fillAmount = 0f;
                alCompletar.Invoke();
                
                temporizador = 0f;
            }
        }
        else
        {
            // Si el jugador suelta el botón a la mitad, la barra baja
            if (temporizador > 0)
            {
                temporizador -= Time.deltaTime * 2f; // Se vacía el doble de rápido
                barraCircular.fillAmount = temporizador / tiempoRequerido;
            }
        }
    }

    public void SetPressed(bool presionado)
    {
        estaPresionado = presionado;
    }
}