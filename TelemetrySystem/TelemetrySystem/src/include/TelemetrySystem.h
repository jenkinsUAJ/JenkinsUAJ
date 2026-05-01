#pragma once

#include <stdint.h>

#include "TelemetryExports.h"


/**
 * En este fichero se deben colocar todos las funciones que exporte la DLL, es lo que seria el API de nuestra libreria
 * 
 */


//forward declaration
struct EventData;


extern "C" {

	/**
	 * @brief crea e inicializa una instancia de un tracker con los parametros especificados
	 * 
	 * @param serializationFormat formato de serializacion utilizado
	 * @param persistenceMethod formato de persistencia utilizado
	 * @param eventQueuePolicy politca de cola de eventos utilizada
	 * @param pathEventFile ruta al fichero en el que se va a persistir (utilizado solo si la persitencia es FILE_PERSITENCE)
	 * 
	 * @return un puntero al tracker creado si se ha podido crear e inicializar correctamente, nullptr en otro caso
	 */
	TELEMETRY_API void* CreateTracker(
		int serializationFormat,
		int persistenceMethod,
		int eventQueuePolicy,
		const char* pathEventFile //solo se usa si la persitencia es FILE_PERSITENCE
	);

	/**
	 * @brief cierra la instancia del tracker, liberando sus recursos asociados y liberando la memoria del objeto
	 * 
	 * @param trackerHandle puntero al tracker que se va a liberar
	 */
	TELEMETRY_API void CloseTracker(void* trackerHandle);


	/**
	 * @brief Encola un evento en el tracker.
	 *
	 * Devuelve el resultado de la operacion. Si es correcta el trackker toma ownership de eventData y lo liberara en Flush/CloseTracker.
	 * Si no se ha podido trackear el evento, se eliminara internamente
	 * 
	 * En ningun caso se deberia volver a acceder desde fuera a eventData despues de esta llamada
	 * 
	 * @param trackerHandle puntero al tracker
	 * @param eventData puntero al objeto eventData que se quiere trackear
	 *
	 * @return 0 track correcto.
	 * @return 1 track correcto, pero se sobrescribe (descarta) el evento mas antiguo.
	 * @return -1 fallo. Libera la memoria de el objeto eventData
	 */
	TELEMETRY_API int TrackEvent(void* trackerHandle, EventData* eventData);


	/**
	 * @brief Persiste todos los eventos pendientes.
	 * 
	 * @param trackerHandle puntero al tracker
	 * 
	 * @note si la persistencia falla, se pierden los eventos que habia en la cola
	 * @note si la serializacion de algun evento falla, dichos eventos se pierden
	 *
	 * 
	 * @return 0 si todo se persiste correctamente.
	 * @return -1 si NO se ha podido persistir correctamente
	 * @return -2 si SI se ha podido persistir correctamente, pero fallo la serializacion de algun evento (dichos eventos se pierden)
	 */
	TELEMETRY_API int Flush(void* trackerHandle);


	/**
	 * @param trackerHandle puntero al tracker
	 * 
	 * @return el size de los eventos de la cola que todavia NO han sido persistido. Si trackerHandle es nullptr se devolvera -1
	 */
	TELEMETRY_API int GetCurrentEventQueueSize(void* trackerHandle);


	/**
	 * @brief crea una instancia de un objeto de EventData con el numero de atributos especificado
	 * 
	 * @param numAttributes numero de atributos que tendra el objeto EventData. Debe ser >= 0 o de lo contrario se devolvera nullptr
	 * 
	 * @return un puntero al objeto EventData creado, nullptr si algo ha fallado en la creacion o los parametros son incorrectos
	 */
	TELEMETRY_API EventData* CreateTelemetryEvent(int32_t numAttributes);

	/**
	 * @brief libera la memoria asociada a un objeto EventData
	 * 
	 * @note esta funcion NO deberia llamarse para ningun evento que se haya trackeado con los 
	 * metodos del API, ya que en el momento en el que se llama a esos metodos el responsable de la memoria es el propio tracker
	 * 
	 * @param eventData puntero al objeto EventData que se va a liberar
	 */
	TELEMETRY_API void DestroyEvent(EventData* eventData);



}
