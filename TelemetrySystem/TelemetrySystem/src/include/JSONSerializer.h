#pragma once

#include "ISerializer.h"

/**
 * @brief Serializador al formato JSON
 */
class JSONSerializer : public ISerializer {
public:

	JSONSerializer() = default;
	~JSONSerializer() override = default;

	/**
	 * @brief Serializa una porcion de stream JSON y lo escribe en outChunk.
	 *
	 * Protocolo de eventOrderType para generar un array JSON valido en streaming:
	 * - FIRST: genera "[eventData"
	 * - MIDDLE: genera ",eventData"
	 * - LAST: genera "eventData]" si eventData no es null, o solo "]" si eventData es null.
	 *
	 * Este contrato permite cerrar un array JSON con LAST y eventData null cuando ya no
	 * quedan elementos que serializar.
	 *
	 * @param eventData evento a serializar; puede ser null solo cuando eventOrderType es LAST.
	 * @param eventOrderType posicion del frame dentro de la secuencia de serializacion.
	 * @param outChunk buffer de salida. La implementacion anade el fragmento serializado al final.
	 * @return true si la serializacion ha tenido exito; false si hay error de entrada o runtime.
	 */
	bool serialize(const EventData* eventData, ISerializer::EventOrderType eventOrderType, DataChunk& outChunk) noexcept override;

	bool close() noexcept override { return true; }

private:
};