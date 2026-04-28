#pragma once


/**
 * @brief macro para exportar funciones al compilar como dll
 * 
 */
#ifdef TELEMETRY_EXPORTS
#define TELEMETRY_API _declspec(dllexport)
#else
#define TELEMETRY_API _declspec(dllimport)
#endif // TELEMETRY_EXPORTS


