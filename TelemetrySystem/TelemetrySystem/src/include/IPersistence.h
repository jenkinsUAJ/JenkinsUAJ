#pragma once

#include "DataChunk.h"

/**
 * @brief Interfaz para los metodos de persistencia
 * 
 * Cualquier metodo de persitencia (fichero, red...) debe heredar de esta interfaz e implementar los metodos indicados en la clase
 * para poder utilizarse correctamente
 * 
 */
class IPersistence {

public:

	IPersistence() = default;
	virtual ~IPersistence() = default;

	/**
	 * @brief Guarda el DataChunck en el soporte de persistencia
	 * @param data informacion que se quiere persitir
	 * @return true si se persiste correctamente, false en caso de error.
	 */
	virtual bool persist(const DataChunk& data) noexcept = 0;

	/**
	 * @brief Cierra el backend de persistencia.
	 * @return true si el cierre es correcto o ya estaba cerrado.
	 */
	virtual bool close() noexcept = 0;

};