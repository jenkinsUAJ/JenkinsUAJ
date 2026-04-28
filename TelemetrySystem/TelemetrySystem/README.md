# TelemetrySystem

En esta carpeta se encuentra la solucion de visual studio del sistema de telemetria


## Detalles de configuracion

- La configuracion de "ReleaseToEngine" es exactamente igual a la configuracion de Release pero coloca la salida en el directorio : "$(SolutionDir)..\..\ChronoPunk\Assets\Plugins\" para poder probarlo directamente desde Unity

- Los ficheros temporales estan en la carpeta $(SolutionDir)/tmp/

- La salida del proyecto va a la carpeta $(SolutionDir)/bin/$(Configuration)/

- El codigo fuente del proyecto se encuentra en el directorio $(SolutionDir)/src/, dividido en 2 directorios de "include" y "cpp" respectivamente



