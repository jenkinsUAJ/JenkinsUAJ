#pragma once

#include <string>
#include "EventData.h"
#include "DataChunk.h"

/**
 * @brief Interfaz para los formatos de serializacion
 * 
 * Cualquier formato de serializacion debe implementar los metodos indicados en esta clase
 */
class ISerializer {

public:

	// Indica la cronologia (primer evento, ultimo evento, evento en medio) de los eventos.
	enum EventOrderType { FIRST, MIDDLE, LAST };
	
	ISerializer() = default;
	virtual ~ISerializer() = default;

	/**
	 * @brief serializa un eventData y rellena en outChunk el contenido del eventData serializado
	 * 
	 * outChunk se rellena en modo append es decir, se agrega al final el nuevo contenido serializado, pensado para poder hacer multiples llamadas con el mismo outChunk
	 * 
	 * devuelve si se ha podido serializar el eventData o no
	 * 
	 * Si ocurre cualquier error, outChunk permanece igual que antes de llamar a este metodo
	 * 
	 * @param eventData evento que se quiere serializar
	 * @param eventOrderType orden cronologico del evento (necesario en algunos formatos de serializacion)
	 * @param outChunk estructura en la que se agrega la informacion del nuevo evento serializado
	 * @return true si se ha podido serializar el eventData, false si ha ocurrido algun error
	 */
	virtual bool serialize(const EventData* eventData, EventOrderType eventOrderType, DataChunk& outChunk) noexcept = 0;

	/**
	 * @brief cierra el serializer y libera sus recursos asociados
	 * 
	 * @return true si el cierre ha sido correcto, false en otro caso
	 */
	virtual bool close() noexcept = 0;
};