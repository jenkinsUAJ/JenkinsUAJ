@echo off

:: Ir a la carpeta donde esta el bat
cd /d "%~dp0"

:: Comprobar que DockerEngine este ejecutandose
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Docker no esta iniciado. Abre Docker Desktop y espera a que este listo y ejecuta el bat de nuevo.
    pause
    exit /b
)

:: Build del contenedor jenkins
docker build -t myjenkins-blueocean:2.555.1-1 .

:: Ejecutarlo
docker run -d --name jenkins-blueocean --restart=on-failure ^
  --network jenkins --env DOCKER_HOST=tcp://docker:2376 ^
  --env DOCKER_CERT_PATH=/certs/client --env DOCKER_TLS_VERIFY=1 ^
  -v jenkins-data:/var/jenkins_home ^
  -v jenkins-docker-certs:/certs/client:ro ^
  -p 8080:8080 -p 50000:50000 myjenkins-blueocean:2.555.1-1

:: Carpetas para sacar builds y filebrowser
docker exec -u root jenkins-blueocean mkdir -p /var/jenkins_home/BuildDatabase/config /var/jenkins_home/BuildDatabase/files
docker exec -u root jenkins-blueocean chmod -R 777 /var/jenkins_home/BuildDatabase
:: Contenedor de fileBrowser
docker run -d --name build-file-server ^
  --network jenkins ^
  -v jenkins-data:/database ^
  -p 1000:80 ^
  -e FB_DATABASE=/database/BuildDatabase/config/filebrowser.db ^
  -e FB_ROOT=/database/BuildDatabase/files ^
  filebrowser/filebrowser