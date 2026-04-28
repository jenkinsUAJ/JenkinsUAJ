using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Telemetry
{
public class EventManager : MonoBehaviour
{

    public const int SerializationJson = 0;
    public const int PersistenceFile = 0;
    public const int QueueCircularArray = 0;

    private int sessionID = -1;
    private int userID = -1;
    private IntPtr _trackerHandle = IntPtr.Zero;

    private EventManager _instance = null;

    [SerializeField]
    private bool _persistPeriodically = false;

    [SerializeField]
    private int _persistSeconds = 60;

    private double _elapsedTime = 0;

    public EventManager Instance
    {
        get { return _instance; }
    }

    enum AtributesNameId
    {
        eventType = 0,
        levelID = 1,
        sessionID = 2,
        userID = 3,
        timeStamp = 4,
        positionX = 5,
        positionY = 6,
        shadowID = 7,
        buttonID = 8,
        leverID = 9
    }

    private void SubmitEvent(IntPtr eventPtr)
    {
        if (eventPtr == IntPtr.Zero)
        {
            return;
        }

        // El ownership pasa siempre a C++ al invocar TrackEvent,
        // incluso si internamente falla la insercion.
        TelemetryNative.TrackEvent(_trackerHandle, eventPtr);
    }

    /// <summary>
    /// Metodo de envio de evento de muerte. Si alguno de los parametros que se envien presenta valores anormales o no validos no
    /// se hara el envio del metodo.
    ///
    /// La liberacion de la memoria creada por este metodo sera responsabilidad del plugin de C++
    /// </summary>
    /// <param name="levelID">Id del nivel actual que esta jugando el player</param>
    /// <param name="positionX">Posicion en X donde se encuentra el player al morir</param>
    /// <param name="positionY">Posicion en Y donde se encuentra el player al morir</param>
    public void sendDeathEvent(int levelID, double positionX, double positionY)
    {

        //Establecimiento del numero de atributos
        const int attributeCount = 5;

        //Creacion del evento
        IntPtr eventPtr;

        try
        {
            eventPtr = TelemetryNative.CreateEvent(attributeCount);
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no disponible o funci�n no encontrada");
            return;
        }
        catch (System.DllNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no encontrada");
            return;
        }

        if (eventPtr == IntPtr.Zero)
        {
            return;
        }

        //obtencion del puntero de los atributos
        IntPtr attributesBase = TelemetryUtils.GetEventAttributesPtr(eventPtr);

        if (attributesBase == IntPtr.Zero)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Comprobacion de valores de los atributos
        if(levelID < 0 || sessionID == -1 || userID == -1)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Escritura de los atributos
        TelemetryUtils.WriteAttributeInt32(attributesBase, 0, (int)AtributesNameId.levelID, levelID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 1, (int)AtributesNameId.sessionID, sessionID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 2, (int)AtributesNameId.userID, userID);
        TelemetryUtils.WriteAttributeDouble(attributesBase, 3, (int)AtributesNameId.positionX, positionX);
        TelemetryUtils.WriteAttributeDouble(attributesBase, 4, (int)AtributesNameId.positionY, positionY);

        //Escritura de directivas externas a atributos
        TelemetryUtils.WriteEventHeader(eventPtr, 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); 

        //TODO Envio del evento
        SubmitEvent(eventPtr);
    }

    /// <summary>
    /// Metodo de envio de evento de fin de iteracion. Si alguno de los parametros que se envien presenta valores anormales o no validos no
    /// se hara el envio del metodo.
    ///
    /// La liberacion de la memoria creada por este metodo sera responsabilidad del plugin de C++
    /// </summary>
    /// <param name="levelID">Id del nivel actual que esta jugando el player</param>
    /// <param name="positionX">Posicion en X donde se encuentra el player al morir</param>
    /// <param name="positionY">Posicion en Y donde se encuentra el player al morir</param>
    /// <param name="shadowID">Identificador de la cronorama seleccionada al ocurrir este evento</param>
    public void sendEndIterationEvent(int levelID, double positionX, double positionY, int shadowID)
    {
        //Establecimiento del numero de atributos
        const int attributeCount = 6;

        //Creacion del evento
        IntPtr eventPtr;

        try
        {
            eventPtr = TelemetryNative.CreateEvent(attributeCount);
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no disponible o funci�n no encontrada");
            return;
        }
        catch (System.DllNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no encontrada");
            return;
        }

        if (eventPtr == IntPtr.Zero)
        {
            return;
        }

        //obtencion del puntero de los atributos
        IntPtr attributesBase = TelemetryUtils.GetEventAttributesPtr(eventPtr);

        if (attributesBase == IntPtr.Zero)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Comprobacion de valores de los atributos
        if (levelID < 0 || sessionID == -1 || userID == -1 || shadowID < 0)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Escritura de los atributos
        TelemetryUtils.WriteAttributeInt32(attributesBase, 0, (int)AtributesNameId.levelID, levelID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 1, (int)AtributesNameId.sessionID, sessionID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 2, (int)AtributesNameId.userID, userID);
        TelemetryUtils.WriteAttributeDouble(attributesBase, 3, (int)AtributesNameId.positionX, positionX);
        TelemetryUtils.WriteAttributeDouble(attributesBase, 4, (int)AtributesNameId.positionY, positionY);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 5, (int)AtributesNameId.shadowID, shadowID);

        //Escritura de directivas externas a atributos
        TelemetryUtils.WriteEventHeader(eventPtr, 1, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        //TODO Envio del evento
        SubmitEvent(eventPtr);
    }

    /// <summary>
    /// Metodo de envio de evento de fallo de determinismo. Si alguno de los parametros que se envien presenta valores anormales o no validos no
    /// se hara el envio del metodo.
    ///
    /// La liberacion de la memoria creada por este metodo sera responsabilidad del plugin de C++
    /// </summary>
    /// <param name="levelID">Id del nivel actual que esta jugando el player</param>
    /// <param name="positionX">Posicion en X donde se encuentra el player al morir</param>
    /// <param name="positionY">Posicion en Y donde se encuentra el player al morir</param>
    /// <param name="shadowID">Identificador de la cronorama seleccionada al ocurrir este evento</param>
    public void sendDetFailureEvent(int levelID, double positionX, double positionY, int shadowID)
    {
        //Establecimiento del numero de atributos
        const int attributeCount = 6;

        //Creacion del evento
        IntPtr eventPtr;

        try
        {
            eventPtr = TelemetryNative.CreateEvent(attributeCount);
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no disponible o funci�n no encontrada");
            return;
        }
        catch (System.DllNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no encontrada");
            return;
        }

        if (eventPtr == IntPtr.Zero)
        {
            return;
        }

        //obtencion del puntero de los atributos
        IntPtr attributesBase = TelemetryUtils.GetEventAttributesPtr(eventPtr);

        if (attributesBase == IntPtr.Zero)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Comprobacion de valores de los atributos
        if (levelID < 0 || sessionID == -1 || userID == -1 || shadowID < 0)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Escritura de los atributos
        TelemetryUtils.WriteAttributeInt32(attributesBase, 0, (int)AtributesNameId.levelID, levelID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 1, (int)AtributesNameId.sessionID, sessionID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 2, (int)AtributesNameId.userID, userID);
        TelemetryUtils.WriteAttributeDouble(attributesBase, 3, (int)AtributesNameId.positionX, positionX);
        TelemetryUtils.WriteAttributeDouble(attributesBase, 4, (int)AtributesNameId.positionY, positionY);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 5, (int)AtributesNameId.shadowID, shadowID);

        //Escritura de directivas externas a atributos
        TelemetryUtils.WriteEventHeader(eventPtr, 2, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        //TODO Envio del evento
        SubmitEvent(eventPtr);
    }

    /// <summary>
    /// Metodo de envio de evento de abandono de juego. Si alguno de los parametros que se envien presenta valores anormales o no validos no
    /// se hara el envio del metodo.
    ///
    /// La liberacion de la memoria creada por este metodo sera responsabilidad del plugin de C++
    /// </summary>
    /// <param name="levelID">Id del ultimo nivel al que jugo el player antes de abandonar el juego</param>
    public void sendLeftGameEvent(int levelID)
    {
        //Establecimiento del numero de atributos
        const int attributeCount = 3;

        //Creacion del evento
        IntPtr eventPtr;

        try
        {
            eventPtr = TelemetryNative.CreateEvent(attributeCount);
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no disponible o funci�n no encontrada");
            return;
        }
        catch (System.DllNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no encontrada");
            return;
        }

        if (eventPtr == IntPtr.Zero)
        {
            return;
        }

        //obtencion del puntero de los atributos
        IntPtr attributesBase = TelemetryUtils.GetEventAttributesPtr(eventPtr);

        if (attributesBase == IntPtr.Zero)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Comprobacion de valores de los atributos
        if (levelID < 0 || sessionID == -1 || userID == -1)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Escritura de los atributos
        TelemetryUtils.WriteAttributeInt32(attributesBase, 0, (int)AtributesNameId.levelID, levelID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 1, (int)AtributesNameId.sessionID, sessionID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 2, (int)AtributesNameId.userID, userID);

        //Escritura de directivas externas a atributos
        TelemetryUtils.WriteEventHeader(eventPtr, 3, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        //TODO Envio del evento
        SubmitEvent(eventPtr);
    }

    /// <summary>
    /// Metodo de envio de evento de abandono de nivel. Si alguno de los parametros que se envien presenta valores anormales o no validos no
    /// se hara el envio del metodo.
    ///
    /// La liberacion de la memoria creada por este metodo sera responsabilidad del plugin de C++
    /// </summary>
    /// <param name="levelID">Id del nivel actual que esta jugando el player</param>
    public void sendLeftLevelEvent(int levelID)
    {
        //Establecimiento del numero de atributos
        const int attributeCount = 3;

        //Creacion del evento
        IntPtr eventPtr;

        try
        {
            eventPtr = TelemetryNative.CreateEvent(attributeCount);
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no disponible o funci�n no encontrada");
            return;
        }
        catch (System.DllNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no encontrada");
            return;
        }

        if (eventPtr == IntPtr.Zero)
        {
            return;
        }

        //obtencion del puntero de los atributos
        IntPtr attributesBase = TelemetryUtils.GetEventAttributesPtr(eventPtr);

        if (attributesBase == IntPtr.Zero)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Comprobacion de valores de los atributos
        if (levelID < 0 || sessionID == -1 || userID == -1)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Escritura de los atributos
        TelemetryUtils.WriteAttributeInt32(attributesBase, 0, (int)AtributesNameId.levelID, levelID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 1, (int)AtributesNameId.sessionID, sessionID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 2, (int)AtributesNameId.userID, userID);

        //Escritura de directivas externas a atributos
        TelemetryUtils.WriteEventHeader(eventPtr, 4, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        //TODO Envio del evento
        SubmitEvent(eventPtr);

    }

    /// <summary>
    /// Metodo de envio de evento de seleccion de sombra. Si alguno de los parametros que se envien presenta valores anormales o no validos no
    /// se hara el envio del metodo.
    ///
    /// La liberacion de la memoria creada por este metodo sera responsabilidad del plugin de C++
    /// </summary>
    /// <param name="levelID">Id del nivel actual que esta jugando el player</param>
    /// <param name="shadowID">Identificador de la cronorama seleccionada al ocurrir este evento</param>
    public void sendShadowSelectEvent(int levelID, int shadowID)
    {
        //Establecimiento del numero de atributos
        const int attributeCount = 4;

        //Creacion del evento
        IntPtr eventPtr;

        try
        {
            eventPtr = TelemetryNative.CreateEvent(attributeCount);
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no disponible o funci�n no encontrada");
            return;
        }
        catch (System.DllNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no encontrada");
            return;
        }

        if (eventPtr == IntPtr.Zero)
        {
            return;
        }

        //obtencion del puntero de los atributos
        IntPtr attributesBase = TelemetryUtils.GetEventAttributesPtr(eventPtr);

        if (attributesBase == IntPtr.Zero)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Comprobacion de valores de los atributos
        if (levelID < 0 || sessionID == -1 || userID == -1 || shadowID < 0)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Escritura de los atributos
        TelemetryUtils.WriteAttributeInt32(attributesBase, 0, (int)AtributesNameId.levelID, levelID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 1, (int)AtributesNameId.sessionID, sessionID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 2, (int)AtributesNameId.userID, userID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 3, (int)AtributesNameId.shadowID, shadowID);

        //Escritura de directivas externas a atributos
        TelemetryUtils.WriteEventHeader(eventPtr, 5, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        //TODO Envio del evento
        SubmitEvent(eventPtr);
    }

    /// <summary>
    /// Metodo de envio de evento de boton presionado. Si alguno de los parametros que se envien presenta valores anormales o no validos no
    /// se hara el envio del metodo.
    ///
    /// La liberacion de la memoria creada por este metodo sera responsabilidad del plugin de C++
    /// </summary>
    /// <param name="levelID">Id del nivel actual que esta jugando el player</param>
    /// <param name="buttonID">Identificador del boton que se ha presionado. El identificador se realiza en el orden en el que se pulsan los
    /// botones en una iteracion comun del nivel</param>
    public void sendButtonPressEvent(int levelID, int buttonID)
    {
        //Establecimiento del numero de atributos
        const int attributeCount = 4;

        //Creacion del evento
        IntPtr eventPtr;

        try
        {
            eventPtr = TelemetryNative.CreateEvent(attributeCount);
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no disponible o funci�n no encontrada");
            return;
        }
        catch (System.DllNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no encontrada");
            return;
        }

        if (eventPtr == IntPtr.Zero)
        {
            return;
        }

        //obtencion del puntero de los atributos
        IntPtr attributesBase = TelemetryUtils.GetEventAttributesPtr(eventPtr);

        if (attributesBase == IntPtr.Zero)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Comprobacion de valores de los atributos
        if (levelID < 0 || sessionID == -1 || userID == -1 || buttonID < 0)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Escritura de los atributos
        TelemetryUtils.WriteAttributeInt32(attributesBase, 0, (int)AtributesNameId.levelID, levelID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 1, (int)AtributesNameId.sessionID, sessionID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 2, (int)AtributesNameId.userID, userID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 3, (int)AtributesNameId.buttonID, buttonID);

        //Escritura de directivas externas a atributos
        TelemetryUtils.WriteEventHeader(eventPtr, 6, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        //TODO Envio del evento
        SubmitEvent(eventPtr);
    }

    /// <summary>
    /// Metodo de envio de evento de palanca accionada. Si alguno de los parametros que se envien presenta valores anormales o no validos no
    /// se hara el envio del metodo.
    ///
    /// La liberacion de la memoria creada por este metodo sera responsabilidad del plugin de C++
    /// </summary>
    /// <param name="levelID">Id del nivel actual que esta jugando el player</param>
    /// <param name="leverID">Identificador de la palanca que se ha accionado. El identificador se realiza en el orden en el que se pulsan los
    /// botones en una iteracion comun del nivel</param>
    public void sendLeverActionEvent(int levelID, int leverID)
    {
        //Establecimiento del numero de atributos
        const int attributeCount = 4;

        //Creacion del evento
        IntPtr eventPtr;

        try
        {
            eventPtr = TelemetryNative.CreateEvent(attributeCount);
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no disponible o funci�n no encontrada");
            return;
        }
        catch (System.DllNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no encontrada");
            return;
        }

        if (eventPtr == IntPtr.Zero)
        {
            return;
        }

        //obtencion del puntero de los atributos
        IntPtr attributesBase = TelemetryUtils.GetEventAttributesPtr(eventPtr);

        if (attributesBase == IntPtr.Zero)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Comprobacion de valores de los atributos
        if (levelID < 0 || sessionID == -1 || userID == -1 || leverID < 0)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Escritura de los atributos
        TelemetryUtils.WriteAttributeInt32(attributesBase, 0, (int)AtributesNameId.levelID, levelID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 1, (int)AtributesNameId.sessionID, sessionID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 2, (int)AtributesNameId.userID, userID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 3, (int)AtributesNameId.leverID, leverID);

        //Escritura de directivas externas a atributos
        TelemetryUtils.WriteEventHeader(eventPtr, 7, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        //TODO Envio del evento
        SubmitEvent(eventPtr);
    }

    /// <summary>
    /// Metodo de envio de evento de inicio de nivel. Si alguno de los parametros que se envien presenta valores anormales o no validos no
    /// se hara el envio del metodo
    ///
    /// La liberacion de la memoria creada por este metodo sera responsabilidad del plugin de C++
    /// </summary>
    /// <param name="levelID">Id del nivel actual que esta jugando el player</param>
    public void sendLevelStartEvent(int levelID)
    {
        //Establecimiento del numero de atributos
        const int attributeCount = 3;

        //Creacion del evento
        IntPtr eventPtr;

        try
        {
            eventPtr = TelemetryNative.CreateEvent(attributeCount);
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no disponible o funci�n no encontrada");
            return;
        }
        catch (System.DllNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no encontrada");
            return;
        }

        if (eventPtr == IntPtr.Zero)
        {
            return;
        }

        //obtencion del puntero de los atributos
        IntPtr attributesBase = TelemetryUtils.GetEventAttributesPtr(eventPtr);

        if (attributesBase == IntPtr.Zero)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Comprobacion de valores de los atributos
        if (levelID < 0 || sessionID == -1 || userID == -1)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Escritura de los atributos
        TelemetryUtils.WriteAttributeInt32(attributesBase, 0, (int)AtributesNameId.levelID, levelID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 1, (int)AtributesNameId.sessionID, sessionID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 2, (int)AtributesNameId.userID, userID);

        //Escritura de directivas externas a atributos
        TelemetryUtils.WriteEventHeader(eventPtr, 8, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        //TODO Envio del evento
        SubmitEvent(eventPtr);
    }

    /// <summary>
    /// Metodo de envio de evento de finalizacion de nivel. Si alguno de los parametros que se envien presenta valores anormales o no validos no
    /// se hara el envio del metodo
    ///
    /// La liberacion de la memoria creada por este metodo sera responsabilidad del plugin de C++
    /// </summary>
    /// <param name="levelID">Id del nivel actual que esta jugando el player</param>
    public void sendLevelEndEvent(int levelID)
    {
        //Establecimiento del numero de atributos
        const int attributeCount = 3;

        //Creacion del evento
        IntPtr eventPtr;

        try
        {
            eventPtr = TelemetryNative.CreateEvent(attributeCount);
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no disponible o funci�n no encontrada");
            return;
        }
        catch (System.DllNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no encontrada");
            return;
        }

        if (eventPtr == IntPtr.Zero)
        {
            return;
        }

        //obtencion del puntero de los atributos
        IntPtr attributesBase = TelemetryUtils.GetEventAttributesPtr(eventPtr);

        if (attributesBase == IntPtr.Zero)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Comprobacion de valores de los atributos
        if (levelID < 0 || sessionID == -1 || userID == -1)
        {
            TelemetryNative.DestroyEvent(eventPtr);
            return;
        }

        //Escritura de los atributos
        TelemetryUtils.WriteAttributeInt32(attributesBase, 0, (int)AtributesNameId.levelID, levelID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 1, (int)AtributesNameId.sessionID, sessionID);
        TelemetryUtils.WriteAttributeInt32(attributesBase, 2, (int)AtributesNameId.userID, userID);

        //Escritura de directivas externas a atributos
        TelemetryUtils.WriteEventHeader(eventPtr, 9, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        //TODO Envio del evento
        SubmitEvent(eventPtr);
    }

    void Start()
    {

        int numSession = 0;

        if (PlayerPrefs.HasKey("SESSION_NUMBER"))
        {
            numSession = PlayerPrefs.GetInt("SESSION_NUMBER") + 1;
            PlayerPrefs.SetInt("SESSION_NUMBER", numSession);
        }
        else
        {
            PlayerPrefs.SetInt("SESSION_NUMBER", 0);
        }

        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "telemetry_events_" + numSession + "_" + userID + ".json");

        try
        {
            _trackerHandle = TelemetryNative.CreateTracker(
                        SerializationJson,
                        PersistenceFile,
                        QueueCircularArray,
                        filePath
                    );

            if (_trackerHandle == IntPtr.Zero)
            {
                Debug.Log("No se pudo abrir el archivo");
            }
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no disponible o funci�n no encontrada");
        }
        catch (System.DllNotFoundException)
        {
            Debug.LogWarning("Telemetry DLL no encontrada");
        }
        
    }

    void Update()
    {
        tryPersistPeriodically();
    }

    private void Awake()
    {
        if (_instance == null) {

            _instance = this;

            userID = TelemetryUtils.GetUserID();
            sessionID = userID + System.Guid.NewGuid().GetHashCode();
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (_trackerHandle == IntPtr.Zero)
        {
            return;
        }

        TelemetryNative.Flush(_trackerHandle);
        TelemetryNative.CloseTracker(_trackerHandle);
        _trackerHandle = IntPtr.Zero;
    }


    void tryPersistPeriodically()
    {
        //solo persistimos si esta marcado el booleano
        if (!_persistPeriodically)
        {
            return;
        }

        //cuenta de tiempo para persistir
        if (_elapsedTime > _persistSeconds)
        {
            _elapsedTime = 0;

            if (_trackerHandle != IntPtr.Zero)
            {
                TelemetryNative.Flush(_trackerHandle);
            }
        }
        else
        {
            _elapsedTime += Time.deltaTime;
        }

    }

    private void OnApplicationQuit()
    {

        TelemetryDispatch.SendLeftGame();
    }





}



}