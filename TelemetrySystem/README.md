# Sistema de telemetría UAJ 25/26

## Cómo usar el analizador
### Windows:
1- Meter los archivos a analizar dentro de la carpeta ``data``  
2- Abrir un terminal dentro de la carpeta ``analytics`` y ejecutar:
 ```python -m venv venv && .\venv\Scripts\activate```  
3- Ejecutar ```pip install -r requirements.txt```  
4- Ejecutar ```python main.py```

### MacOS / Linux:
1- Meter los archivos a analizar dentro de la carpeta `data`  
2- Abrir un terminal dentro de la carpeta `analytics` y ejecutar: 
 ```python3 -m venv venv && source venv/bin/activate```  
3- Ejecutar ```pip install -r requirements.txt```  
4- Ejecutar ```python3 main.py```

## Estructura del repositorio

- En el directorio `analitics` se encuentra el codigo relacionado con el procesamiento de datos en Python.
- En el directorio `TelemetrySystem` se encuentra la solucion de Visual Studio que genera la DLL con el sistema de telemetria.

