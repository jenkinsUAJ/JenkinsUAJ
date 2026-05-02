# Proyecto final de Usabilidad

## Instalación y primeros pasos

### 1- Instalar jenkins en un contenedor de Docker
1- Tener instalado docker desktop, sino descargarlo de aquí https://docs.docker.com/desktop/setup/install/windows-install/

2- Tener docker desktop corriendo y luego ejecutar runJenkins.bat  

3- Mientras la ventana siga abierta seguirá ejecutandose el servidor de jenkins, ahora acceder a la interfaz visual yendo a http://localhost:8080 

4- La primera vez que se ejecute pedirá introducir una contraseña de administrador que estará en un archivo dentro del docker, para poder acceder a él y leerlo:  

Ir a docker desktop -> containers -> clicar en jenkins-blueocean -> Files. El archivo estará en /var/jenkins_home/secrets/initialAdminPassword -> se hace clic derecho -> edit  

Una vez obtenida la contraseña, copiarla y pegarla en la caja de texto, luego pedirá instalar plugins y le daremos a la opción de plugins recomendados, a la izquierda  

5- Ahora pedirá crear un usuario de admin, como vamos a hacer una prueba de concepto, le damos a `Skip and continue as admin` y la url ponemos la default (http://localhost:8080/) y le damos a `Save And Finish`

DISCLAIMER: En caso de que se cierre sesión en Jenkins, el usuario es `admin` y la contraseña es la contraseña obtenida en el paso 4
### 2- Configurar el pipeline dentro de Jenkins
Ahora vamosa a crear y configurar un pipeline para que coja automaticamente el Jenkinsfile de este repo  

1- Vamos a nueva tarea, le ponemos de nombre `JenkinsUAJ`, y ponemos el tipo `Pipeline`  

2- Ahora en el menú de configuración, vamos a Triggers->Pipeline, elegimos `Pipeline script from SCM` (SCM = "Source Code Management") -> SCM ponemos Git -> Credentials +Add -> Global -> Username with password  

3- Ahora tendremos que poner nuestro nombre de usuario de GitHub y un Personal Access Token (la contraseña de la cuenta NO), para ello abrimos github en una pestaña aparte -> click al icono del usuario -> settings -> Developer settings -> Personal Access Tokens -> Tokens (classic) -> Generate new token -> Generate new token (classic) -> Ahora ponemos de nota `JenkinsUAJ`, 30 dias de expiración, y ahora marcar las casillas: `[x] repo, [x]admin:repo_hook, [x]user` -> Ahora generar el token y copiarlo y pegarlo en la sección de contraseña en Jenkins.  

4- Ahora seleccionar los credenciales generados y cambiar el branch specifier a `*/main`

### 3- Configurar nodo creador de builds de Unity
Jenkins necesita de un directorio con los archivos de Editor de Unity para poder generar builds y hacer tests con ella. Habría que hacer una imagen docker personalizada, con la versión de Unity, luego activar la licencia e iniciar sesión, lo que aumenta demasiado la complejidad. Por lo que asumimos que el PC que ejecuta el contenedor Jenkins es Windows, y tiene descargado Unity y el editor de la versión del juego (6000.0.60f1)  
Ahora para que Jenkins delegue el trabajo de crear la build y los tests en otro tenemos que crear un "Agente"

1- Vamos a ajustes de jenkins (Administrar jenkins), al engranaje  

2- Vamos a Nodes -> New Node  

3- En nombre del nodo ponemos `unity-build` y activamos la opción `permanent agent` -> Create

4- Ahora en directorio de raíz remoto ponemos `{rutaAlRepositorio}\JenkinsUnityAgentWorkspace`  (en ruta al repositorio ponemos la ruta de nuestro pc a este repo) -> en método de ejecución ponemos `Launch agent by connecting it to the controller` -> y en variables de entorno añadimos una que sea: nombre: `UNITY_PATH` valor: `C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe` (en caso de tener la ruta al editor de Unity de la versión de Cronopunk en otro directorio, cambiarlo) -> Guardar  

5- Ahora se habrá creado el agente, pero saldrá un icono de una cruz roja indicando que no está conectado, hay que hacer clic en el nodo, y copiar el comando a ejecutar para windows, y ir a un CMD y ejecutarlo  

6- Ahora ya funcionaría, cada vez que se quiera ejecutar el pipeline debe estar el agente activo, de lo contrario saldrá la cruz roja y fallará la creación de build


## Como crear nuevos pasos en el pipeline

