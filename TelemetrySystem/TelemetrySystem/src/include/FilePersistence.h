#pragma once

#include "IPersistence.h"

#include <fstream>


/**
 * @brief clase que se encarga de la persitencia en fichero en disco
 * 
 */
class FilePersistence : public IPersistence{

public:

	FilePersistence() = default;
	~FilePersistence() override = default;

	/**
	 * @brief abre el fichero indicado en modo binario
	 * @param fileName ruta relativa al directorio de trabajo del ejecutable en la que se encuentra el fichero que se quiere
	 * generar con los datos de telemetria
	 * @return true si la inicializacion es correcta, false en otro caso
	 */
	bool init(const std::string& fileName);

	bool close() noexcept override;

	bool persist(const DataChunk& data) noexcept override;

private:

	//fichero en el que estamos escribiendo los datos de telemetria
	std::fstream _outFile;

	// false: mantiene contenido previo (append). true: borra contenido previo (truncate).
	bool _truncateOnInit = true;
};