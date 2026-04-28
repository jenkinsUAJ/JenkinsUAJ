
#include "TelemetrySystem.h"




#include <cstring>
#include <new>

#include "Tracker.h"
#include "EventData.h"
#include "TelemetryConfiguration.h"

/**
 * En este fichero se deben implementar todas las funciones declaradas en el API,
 * delegando en llamadas del resto de clases que manejan la implementacion real de las funciones
 */


TELEMETRY_API void* CreateTracker(int serializationFormat, int persistenceMethod, int eventQueuePolicy, const char* pathEventFile)
{
    Tracker* tracker = new (std::nothrow) Tracker();

    if (tracker == nullptr) {
        return nullptr;
    }

    if (!tracker->init(serializationFormat, persistenceMethod, eventQueuePolicy, pathEventFile)) {
        
        delete tracker;
        return nullptr;
    }

    //si todo ha salido correctamente, devolvemos el puntero al tracker
    return tracker;
}

TELEMETRY_API void CloseTracker(void* trackerHandle)
{
    if (trackerHandle == nullptr) {
        return;
    }

    Tracker* tracker = static_cast<Tracker*>(trackerHandle);

    //cerramos el tracker
    tracker->close();

    //borramos la instancia del tracker
    delete tracker;
}



TELEMETRY_API int TrackEvent(void* trackerHandle, EventData* eventData)
{
    if (trackerHandle == nullptr || eventData == nullptr) {

        // Si llega un evento valido y no se puede procesar, la libreria mantiene
        // ownership y debe liberar tanto el evento como sus atributos.
        if (eventData != nullptr) {
            DestroyEvent(eventData);
        }

        return -1;
    }

    Tracker* tracker = static_cast<Tracker*>(trackerHandle);
    return tracker->trackEvent(eventData);
}

TELEMETRY_API int Flush(void* trackerHandle)
{
    if (trackerHandle == nullptr) {
        return -1;
    }

    Tracker* tracker = static_cast<Tracker*>(trackerHandle);
    return tracker->flushQueue();
}

TELEMETRY_API int GetCurrentEventQueueSize(void* trackerHandle)
{
    if (trackerHandle == nullptr) {
        return -1;
    }

    Tracker* tracker = static_cast<Tracker*>(trackerHandle);
    return tracker->getCurrentEventQueueSize();
}

TELEMETRY_API EventData* CreateEvent(int32_t numAttributes)
{
    if (numAttributes < 0)
    {
        return nullptr;
    }

    EventData* e = new (std::nothrow) EventData;

    if (e == nullptr)
    {
        return nullptr;
    }

    e->eventTypeID = 0;
    e->timestamp = 0;

    e->attributeCount = numAttributes;
    e->attributes = nullptr;

    if (numAttributes > 0)
    {
        e->attributes = new (std::nothrow) EventAttributeData[(size_t)numAttributes];

        if (e->attributes == nullptr)
        {
            delete e;
            return nullptr;
        }
    }

   
    if (e->attributes != nullptr)
    {

        //se puede comentar si se asegura que se van a rellenar todos los campos
        //podriamos dejarlo solo en debug    

        //este memset asegura que la memoria reservada tiene valores "por defecto",
        //util para evitar comportamiento indefinido si no se rellenan todos los campos de los atributos
        memset(e->attributes, 0,
            sizeof(EventAttributeData) * (size_t)numAttributes);
    }

    //devolvemos el puntero al eventData
    return e;
}

TELEMETRY_API void DestroyEvent(EventData* eventData)
{
    if (eventData == nullptr)
    {
        return;
    }

    if (eventData->attributes != nullptr)
    {
        //liberamos todos sus attributos
        delete[] eventData->attributes;
        eventData->attributes = nullptr;
    }

    //liberamos el evento
    delete eventData;
}





