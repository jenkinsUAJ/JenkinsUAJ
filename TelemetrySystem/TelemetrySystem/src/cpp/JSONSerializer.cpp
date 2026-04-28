#include "JSONSerializer.h"
#include "json.hpp"

#include <cstdint>

#include <string>



bool JSONSerializer::serialize(const EventData* ev, ISerializer::EventOrderType eventOrderType, DataChunk& outChunk) noexcept
{
    try {

        std::string jsonString;
        
        if (eventOrderType == ISerializer::FIRST) {
            jsonString += "[";
        }
        else if (eventOrderType == ISerializer::MIDDLE) {
            jsonString += ",";
        }

        if (ev == nullptr) {

            if (eventOrderType != ISerializer::LAST) {
                return false;
            }

            jsonString += "]";
            outChunk.insert(outChunk.end(), jsonString.begin(), jsonString.end());
            return true;
        }

        nlohmann::json json = nlohmann::json::object();
        json["eventType"] = ev->eventTypeID;
        json["timestamp"] = ev->timestamp;

        // Guard clause 
        if (ev->attributes == nullptr || ev->attributeCount <= 0) {
            jsonString += json.dump(0);
            if (eventOrderType == ISerializer::LAST) {
                jsonString += "]";
            }

            outChunk.insert(outChunk.end(), jsonString.begin(), jsonString.end());
            return true;
        }

        for (int i = 0; i < ev->attributeCount; i++)
        {
            const EventAttributeData& eventAttr = ev->attributes[i];
            const std::string attrKey = std::to_string(eventAttr.attributeNameID);

            switch (eventAttr.attributeTypeID) {
            case AttributeType::Bool:
                json[attrKey] = static_cast<bool>(eventAttr.value.b);
                break;
            case AttributeType::Int32:
                json[attrKey] = eventAttr.value.i32;
                break;
            case AttributeType::Int64:
                json[attrKey] = eventAttr.value.i64;
                break;
            case AttributeType::Float:
                json[attrKey] = eventAttr.value.f;
                break;
            case AttributeType::Double:
                json[attrKey] = eventAttr.value.d;
                break;
            case AttributeType::FixedStr:
            {
                size_t fixedLen = 0;
                while (fixedLen < sizeof(eventAttr.value.FixedStr) && eventAttr.value.FixedStr[fixedLen] != '\0') {
                    ++fixedLen;
                }
                json[attrKey] = std::string(eventAttr.value.FixedStr, fixedLen);
                break;
            }
            default:
                return false;
            }
        }

        jsonString += json.dump(0);
        if (eventOrderType == ISerializer::LAST) {
            jsonString += "]";
        }

        outChunk.insert(outChunk.end(), jsonString.begin(), jsonString.end());
        return true;
    }
    catch (...) {
        return false;
    }
}