#pragma once

//para usar uint32_t, uint64_t...
#include <cstdint>

/**
 * En este fichero se declaran los structs de los atributos y de los eventos
 * 
 * Es muy importante que el layout de memoria sea el mismo que el que se utilice desde C# u otros lenguajes
 * 
 * No modificar este fichero sin sincronizarlo con el resto de lenguajes
 * 
 */


/**
 * @brief Enum para los tipos de atributos soportados
 * 
 */
enum class AttributeType : uint32_t
{
    Bool,
    Int32,
    Int64,
    Float,
    Double,
    FixedStr //solo soportamos 8 bytes, (7 caracteres + '\0')
};

//pragam pack para garantizar el alineamiento (debe ser el mismo que en el resto de lenguajes)
#pragma pack(push, 8)

/**
 * @brief Usamos un union para el valor de los atributos
 * El tamaño de este union es de 8 bytes, por tanto todos los valores ocuparan este espacio aunque necesitasen menos
 * 
 */
union AttributeValue
{
    uint8_t b;
    int32_t i32;
    int64_t i64;
    float f;
    double d;
    // 8 bytes: 7 chars + '\0'.
    char FixedStr[8];
};

/**
 * @brief Struct que representa la informacion completa de un atributo (nombre, tipo y valor)
 * 
 */
struct EventAttributeData
{
    int32_t attributeNameID;
    AttributeType attributeTypeID;
    AttributeValue value;
};

/**
 * @brief Struct que representa la informacion completa de un evento para trackear (tipo de evento, timestamp, atributos y numero de atributos)
 * 
 * Consideramos eventTypeID y timestamp valores comunes a todos los eventos y por eso los colocamos dentro de este struct
 * 
 */
struct EventData
{
    int32_t eventTypeID;
    int64_t timestamp;

    EventAttributeData* attributes;
    int32_t attributeCount;
};

//fin del pragma pack
#pragma pack(pop)

