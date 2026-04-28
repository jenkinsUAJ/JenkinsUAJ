/// <summary>
/// Define el contrato para cualquier objeto que pueda ser activado o desactivado.
/// </summary>
public interface IActivable
{
    /// <summary>
    /// Cambia el estado de activación del objeto.
    /// </summary>
    /// <param name="state">
    /// Estado recibido:
    /// <c>true</c> → activa el objeto (ej. abrir una puerta).  
    /// <c>false</c> → desactiva el objeto (ej. cerrar una puerta).
    /// </param>
    void Activar(bool state);


    /// <summary>
    /// Cambia el estado de activacion del objeto al opuesto del estado actual
    /// </summary>
    void SwicthActivableState();
}
