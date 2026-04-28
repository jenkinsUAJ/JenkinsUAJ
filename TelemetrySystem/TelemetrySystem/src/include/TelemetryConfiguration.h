#pragma once

/**
 * @brief Formatos de serializacion soportados por el tracker.
 */
enum SoportedSerializationFormats
{
    JSON = 0
};

/**
 * @brief Metodos de persistencia soportados por el tracker.
 */
enum SoportedPersistenceMethods
{
    FILE_PERSITENCE = 0
};

/**
 * @brief Politicas de cola de eventos soportadas por el tracker.
 */
enum SoportedEventQueuePolicy
{
    CIRCULAR_ARRAY = 0
};
