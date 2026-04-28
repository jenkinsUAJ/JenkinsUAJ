#pragma once

#include <cstddef>
#include <vector>

#include "TelemetryExports.h"
#include "TelemetryConfiguration.h"

#include "EventData.h"

#include "ISerializer.h"
#include "IPersistence.h"



/**
 * @brief Esta clase se encarga de gestionar el tracking de eventos 
 * 
 * Lleva la gestion de la cola de eventos y se encarga de serializar los eventos con el serializer
 * y pasarselos al persister para que los persista.
 * 
 * Tambien se encarga de la creacion y el cierre de los recursos
 * 
 */
class Tracker {
private:

	//capacidad default de la cola
	static constexpr size_t DEFAULT_CIRCULAR_QUEUE_CAPACITY = 1024;

public:

	Tracker() = default;
	virtual ~Tracker(); //virtual por si queremos heredar de esta clase en un futuro


	bool init(
		int serializationFormat,
		int persistenceMethod,
		int eventQueuePolicy,
		const char* pathEventFile
	);


	/**
	 * @brief cierra los recurros del tracker
	 * 
	 * @note antes de cerrar llama a flushQueue para persistir los eventos pendientes
	 * 
	 */
	void close();

	int trackEvent(EventData* eventData);

	/**
	 * @brief Vacia la cola y persiste su contenido.
	 * 
	 * @note si la persistencia falla, se pierden los eventos que habia en la cola
	 * 
	 * @return 0 si todo se persiste correctamente.
	 * @return -1 si no se ha podido persistir correctamente
	 * @return -2 si se ha podido persistir correctamente pero fallo la serializacion de algun evento
	 */
	int flushQueue();

	/**
	 * @return devuelve el numero de elementos de la cola actualmente sin persistir
	 */
	int getCurrentEventQueueSize() const;

private:

	//metodos de inicializacion
	bool initSerializer(int serializationFormat);
	bool initPersistence(int persistenceMethod, const char* pathEventFile);
	bool initQueuePolicy(int eventQueuePolicy);

	//metodos de cierre
	void closeSerializer() noexcept;
	void closePersistence() noexcept;
	void clearQueue();


	ISerializer* _serializer = nullptr;
	IPersistence* _persistence = nullptr;

	
	//variables de gestion de la cola

	std::vector<EventData*> _eventQueue;
	
	uint64_t _queueCapacity = DEFAULT_CIRCULAR_QUEUE_CAPACITY;
	uint64_t _queueHead = 0;
	uint64_t _queueSize = 0;


	//gestion de flujo general
	
	bool _hasPersistedAnyEvent = false;
	bool _streamClosed = false;

};

