@echo off

:: Ir a la carpeta donde esta el bat
cd /d "%~dp0"

:: Verificar si DockerEngine esta ejecutandose
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Docker no esta iniciado. Abre Docker Desktop y espera a que este listo y ejecuta el bat de nuevo.
    pause
    exit /b
)


docker build -t myjenkins-blueocean:2.555.1-1 .

docker run --name jenkins-blueocean --restart=on-failure ^
  --network jenkins --env DOCKER_HOST=tcp://docker:2376 ^
  --env DOCKER_CERT_PATH=/certs/client --env DOCKER_TLS_VERIFY=1 ^
  --volume jenkins-data:/var/jenkins_home ^
  --volume jenkins-docker-certs:/certs/client:ro ^
  --publish 8080:8080 --publish 50000:50000 myjenkins-blueocean:2.555.1-1


pause