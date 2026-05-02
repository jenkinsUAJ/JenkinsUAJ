# Proyecto final de Usabilidad

## Instalación y primeros pasos

### 1- Instalar jenkins en un contenedor de Docker
1- Tener instalado docker desktop, sino descargarlo de aquí https://docs.docker.com/desktop/setup/install/windows-install/

2- Tener docker desktop corriendo y luego ejecutar `installContainers.bat`, este bat solo se tiene que ejecutar una vez para poder tener el contenedor en docker engine, luego una vez instaldo, para abrirlo simplemente activar el contenedor desde docker desktop.

3- Mientras la ventana siga abierta seguirá ejecutandose el servidor de jenkins, ahora acceder a la interfaz visual yendo a http://localhost:8080 

4- La primera vez que se ejecute pedirá introducir una contraseña de administrador que estará en un archivo dentro del docker, para poder acceder a él y leerlo:  

Ir a docker desktop -> containers -> clicar en jenkins-blueocean -> Files. El archivo estará en `/var/jenkins_home/secrets/initialAdminPassword` -> se hace clic derecho -> edit  

Una vez obtenida la contraseña, guardarla, copiarla y pegarla en la caja de texto, luego pedirá instalar plugins y le daremos a la opción de plugins recomendados, a la izquierda

5- Ahora pedirá crear un usuario de admin, como vamos a hacer una prueba de concepto, le damos a `Skip and continue as admin` y la url ponemos la default (http://localhost:8080/) y le damos a `Save And Finish`

6- Para que funcionen ciertas funciones del pipeline, hay que instalar una serie de plugins, vamos a Administrar Jenkins (el engranaje / configuración) -> plugins -> available plugins -> Instalamos `Pipeline Utility Steps` y `SSH Pipeline Steps` -> Reiniciamos cuando termine

DISCLAIMER: En caso de que se cierre sesión en Jenkins, el usuario es `admin` y la contraseña es la contraseña obtenida en el paso 4
### 2- Configurar el pipeline dentro de Jenkins
Ahora vamosa a crear y configurar un pipeline para que coja automaticamente el Jenkinsfile de este repo  

1- Vamos a nueva tarea, le ponemos de nombre `JenkinsUAJ`, y ponemos el tipo `Pipeline`  

2- Ahora en el menú de configuración, vamos a Triggers->Pipeline, elegimos `Pipeline script from SCM` (SCM = "Source Code Management") -> SCM ponemos Git -> en repository Url ponemos `https://github.com/jenkinsUAJ/JenkinsUAJ` -> Credentials +Add -> Global -> Username with password  

3- Ahora tendremos que poner nuestro nombre de usuario de GitHub y un Personal Access Token (la contraseña de la cuenta NO), para ello abrimos github en una pestaña aparte -> click al icono del usuario -> settings -> Developer settings -> Personal Access Tokens -> Tokens (classic) -> Generate new token -> Generate new token (classic) -> Ahora ponemos de nota `JenkinsUAJ`, 30 dias de expiración, y ahora marcar las casillas: `[x] repo, [x]admin:repo_hook, [x]user` -> Ahora generar el token y copiarlo y pegarlo en la sección de contraseña en Jenkins.  

4- Ahora seleccionar los credenciales generados y cambiar el branch specifier a `*/main`

5- Activamos ahora en la sección de triggers Consultar repositorio (SCM), y en el texto ponemos `* * * * *`

### 3- Configurar nodo creador de builds de Unity
Jenkins necesita de un directorio con los archivos de Editor de Unity para poder generar builds y hacer tests con ella. Habría que hacer una imagen docker personalizada, con la versión de Unity, luego activar la licencia e iniciar sesión, lo que aumenta demasiado la complejidad. Por lo que asumimos que el PC que ejecuta el contenedor Jenkins es Windows, y tiene descargado Unity y el editor de la versión del juego (6000.0.60f1)  
Ahora para que Jenkins delegue el trabajo de crear la build y los tests en otro tenemos que crear un "Agente"

1- Vamos a ajustes de jenkins (Administrar jenkins), al engranaje  

2- Vamos a Nodes -> New Node  

3- En nombre del nodo ponemos `unity-build` y activamos la opción `permanent agent` -> Create

4- Ahora en directorio de raíz remoto ponemos `{rutaAlRepositorio}\JenkinsUnityAgentWorkspace`  (en ruta al repositorio ponemos la ruta de nuestro pc a este repo) -> en método de ejecución ponemos `Launch agent by connecting it to the controller` -> y en variables de entorno añadimos una que sea: nombre: `UNITY_PATH` valor: `"C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe"` (en caso de tener la ruta al editor de Unity de la versión de Cronopunk en otro directorio, cambiarlo) -> Guardar  

5- Ahora se habrá creado el agente, pero saldrá un icono de una cruz roja indicando que no está conectado, hay que hacer clic en el nodo, y copiar el comando a ejecutar para windows, y ir a un CMD y ejecutarlo  

6- Ahora ya funcionaría, cada vez que se quiera ejecutar el pipeline debe estar el agente activo, de lo contrario saldrá la cruz roja y fallará la creación de build

### 4- Configurar servidor almacenador de builds
El almacenamiento de los zip de las builds generadas se hace en un servidor aparte, que tendrá todas las builds generadas y cada build mandará un enlace por correo al fichero en el server.  
En caso de querer mirar o acceder a cualquier fichero se accederá mediante `localhost:1000`  
Para ello hay que configurar y activar un contenedor de docker para hostear el servidor, que será con 'FileBrowser'

1- Si el bat ejecutado previamente `installContainers.bat` funcionó bien, ahora en docker Desktop deberá haber un nuevo contenedor llamado `build-file-server`, ejecutarlo  

2- Ahora podemos acceder para ver los ficheros yendo a `localhost:1000`. No obstante, pide usuario y contraseña. El usuario es `admin` y la contraseña se encuentra en los logs del contenedor de docker, que habrá logueado una linea de este tipo: `2026/05/02 10:01:48 User 'admin' initialized with randomly generated password: yCO9lRqgG3eMvj17`. Coger esa contraseña y guardarla.

3- Ahora hay que generar un usuario para permitir que jenkins suba archivos. Dentro de file browser vamos a admin -> User Management -> New -> En username ponemos tal cual `jenkins` y en password ponemos `jenkinspassword` -> Save

### 5- Configurar correo

1- Ir a Administrar jenkins -> Credentials -> Add username with password -> En username ponemos: `jenkinsuaj@gmail.com` en password: `ozdt vilw emxl itqr` en ID: `gmail` -> Ahora volvemos a Administrar Jenkins -> System -> Bajamos hasta Extended E-mail Notification  
2- En SMTP Server ponemos `smtp.gmail.com` en SMTP Port ponemos `465` -> Avanzado -> En credentials ponemos las credenciales que acabamos de crear -> Activamos la casilla de use SSL-> apply, save 


## Como usar

Una vez instalado y configurado, el pipeline se ejecutará cuando detecte un commit en este repositorio, realizará una build, mandará un correo a los desarrolladores del repo y posteará la build en el servidor de archivos de builds.

El jenkins es accesible a través de http://localhost:8080  
El server de builds es accesible a través de http://localhost:1000

## Como crear nuevos pasos en el pipeline

todo
 
asdf