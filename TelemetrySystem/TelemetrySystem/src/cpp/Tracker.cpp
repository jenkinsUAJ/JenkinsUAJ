#include "Tracker.h"
#include "TelemetrySystem.h"

#include "JSONSerializer.h"
#include "FilePersistence.h"

#include <new>



Tracker::~Tracker()
{
	close();
}

bool Tracker::init(int serializationFormat, int persistenceMethod, int eventQueuePolicy, const char* pathEventFile)
{
	if (!initSerializer(serializationFormat)) {
		return false;
	}

	if (!initPersistence(persistenceMethod, pathEventFile)) {
		closeSerializer();

		return false;
	}

	if (!initQueuePolicy(eventQueuePolicy)) {
		closePersistence();
		closeSerializer();
		
		return false;
	}

	return true;
}

void Tracker::close()
{
	flushQueue();


	if (!_streamClosed && _persistence != nullptr) {
		if (_hasPersistedAnyEvent) {
			DataChunk closingChunk;

			//hacemos una ultima persistencia del caracter de LAST
			if (!_serializer->serialize(nullptr, ISerializer::LAST, closingChunk) || !_persistence->persist(closingChunk)) {
				// No propagamos error desde close para no impactar al juego.
			}
		}
		else {
			static const DataChunk emptyArrayChunk = { '[', ']' };
			if (!_persistence->persist(emptyArrayChunk)) {
				// No propagamos error desde close para no impactar al juego.
			}
		}

		_streamClosed = true;
	}

	clearQueue();
	_eventQueue.clear();

	closeSerializer();
	closePersistence();

	_hasPersistedAnyEvent = false;
	_streamClosed = false;
}

int Tracker::trackEvent(EventData* eventData)
{

	if (eventData == nullptr || _eventQueue.empty()) {

		if (eventData != nullptr) {
			DestroyEvent(eventData);
		}

		//devolvemos que no se ha podido trackear el evento
		return -1;
	}

	//calculamos el indice del elemento que vamos a insertar
	const size_t insertIndex = (_queueHead + _queueSize) % _queueCapacity;

	//si la cola esta llena
	if (_queueSize == _queueCapacity) {
		
		//destruimos lo que habia antes en head
		DestroyEvent(_eventQueue[_queueHead]);

		//actualizamos head
		_eventQueue[_queueHead] = eventData;

		//aumentamos head
		_queueHead = (_queueHead + 1) % _queueCapacity;
		
		//indicamos que hemos sobreescrito un elemento
		return 1;
	}

	//si no estaba llena insertamos directamente
	_eventQueue[insertIndex] = eventData;
	_queueSize++;
	
	//indicamos que el elemento se ha insertado directamente
	return 0;
}

int Tracker::flushQueue()
{
	if (_serializer == nullptr || _persistence == nullptr || _streamClosed) {
		return -1;
	}

	//si no tenemos ningun evento al que hacer flush
	if (_queueSize == 0) {
		return 0;
	}

	DataChunk queueChunk;
	size_t serializedCount = 0;
	bool droppedAnyEvent = false;

	//recorrido por todos los eventos de la cola
	for (size_t i = 0; i < _queueSize; ++i)
	{
		const size_t index = (_queueHead + i) % _queueCapacity;
		EventData* eventData = _eventQueue[index];
		
		if (eventData == nullptr) {
			droppedAnyEvent = true;
			continue;
		}

		//ver si es el primer evento o no
		const ISerializer::EventOrderType eventOrderType = (!_hasPersistedAnyEvent && serializedCount == 0)
			? ISerializer::FIRST
			: ISerializer::MIDDLE;

		//serializamos el evento y agregamos su informacion al final del chunk
		const bool serializedOk = _serializer->serialize(eventData, eventOrderType, queueChunk);

		//si no se ha serializado correctamente, lo destruimos
		if (!serializedOk) {
			DestroyEvent(eventData);
			_eventQueue[index] = nullptr;
			droppedAnyEvent = true;
			continue;
		}

		//si se ha serializado correctamente, destruir y aumentar el contador

		DestroyEvent(eventData);
		_eventQueue[index] = nullptr;

		serializedCount++;
	}

	//reseteamos head y size (limpieza de la cola)
	_queueHead = 0;
	_queueSize = 0;


	if (serializedCount == 0) {
		return droppedAnyEvent ? -1 : 0;
	}

	//intentamos persistir el chunk completo (una unica llamada)

	bool persistenceOK = _persistence->persist(queueChunk);

	if (!persistenceOK) {
		return -1;
	}

	//si hemos persistido correctamente lo indicamos
	_hasPersistedAnyEvent = true;

	//devolvemos si hemos tenido que eliminar algun evento o no
	return droppedAnyEvent ? -2 : 0;
}

int Tracker::getCurrentEventQueueSize() const
{
	return static_cast<int>(_queueSize);
}

bool Tracker::initSerializer(int serializationFormat)
{
	switch (serializationFormat)
	{
	case SoportedSerializationFormats::JSON:
		_serializer = new (std::nothrow) JSONSerializer();
		return _serializer != nullptr;
	default:
		return false;
	}
}

bool Tracker::initPersistence(int persistenceMethod, const char* pathEventFile)
{
	if (pathEventFile == nullptr) {
		return false;
	}

	switch (persistenceMethod)
	{
	case SoportedPersistenceMethods::FILE_PERSITENCE:
	{
		FilePersistence* filePersistence = new (std::nothrow) FilePersistence();
		if (filePersistence == nullptr) {
			return false;
		}

		if (!filePersistence->init(pathEventFile)) {
			delete filePersistence;
			return false;
		}

		_persistence = filePersistence;
		return true;
	}
	default:
		return false;
	}
}

bool Tracker::initQueuePolicy(int eventQueuePolicy)
{
	switch (eventQueuePolicy)
	{
	case SoportedEventQueuePolicy::CIRCULAR_ARRAY:

		//try catch por si falla la reserva de capacidad del vector
		try {
			_eventQueue.assign(_queueCapacity, nullptr);
		}
		catch (...) {
			return false;
		}
		
		_queueHead = 0;
		_queueSize = 0;
		return true;
	default:
		return false;
	}
}

void Tracker::closeSerializer() noexcept
{
	if (_serializer == nullptr) {
		return;
	}

	_serializer->close();
	delete _serializer;
	_serializer = nullptr;
}

void Tracker::closePersistence() noexcept
{
	if (_persistence == nullptr) {
		return;
	}

	_persistence->close();
	delete _persistence;
	_persistence = nullptr;
}

void Tracker::clearQueue()
{
	if (_eventQueue.empty() || _queueSize == 0) {
		return;
	}

	//bucle liberando todos los eventos que quedasen en la cola
	for (size_t i = 0; i < _queueSize; ++i)
	{
		const size_t index = (_queueHead + i) % _queueCapacity;
		DestroyEvent(_eventQueue[index]);
		_eventQueue[index] = nullptr;
	}

	//reseteamos head y size
	_queueHead = 0;
	_queueSize = 0;
}