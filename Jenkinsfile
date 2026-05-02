// esto es una variable para definir la conexión ssh para subir el zip al server
def remote = [:]
remote.name = 'sftpgo-server'
remote.host = 'localhost'
remote.port = 2022
remote.user = 'jenkins'
remote.password = 'jenkinspassword'
remote.allowAnyHosts = true


pipeline {
    agent {label 'unity-build'}
    stages {

        stage('Build') {
            steps {
                echo pwd()
                echo "Building Game ..."
                bat '''
                %UNITY_PATH% -executeMethod CustomBuild.BuildWindowsPlayer -buildTarget StandaloneWindows64 -batchmode -quit -projectPath "%WORKSPACE%/ChronoPunk" -logFile "%WORKSPACE%/Builds/%BUILD_NUMBER%/build.log" -buildPath "%WORKSPACE%/Builds/%BUILD_NUMBER%"
                '''
            }
        }
        stage('Zip'){
            steps {
                echo "Empaquetando build..."
                script {
                    zip archive: true, 
                        dir: "Builds/${BUILD_NUMBER}", 
                        zipFile: "ZippedBuilds/build${BUILD_NUMBER}.zip"

                    dir("ZippedBuilds") {
                        stash name: 'build-zip', includes: "build${BUILD_NUMBER}.zip"
                    }
                } 
            }
        }

        stage('Deliver'){
            agent { label 'built-in' }
            steps {
                echo "Moviendo build a la carpeta de Filebrowser..."
                script {
                    unstash 'build-zip'
                    sh "mv build${BUILD_NUMBER}.zip /var/jenkins_home/BuildDatabase/files/"
                }
            }
        }
    }
    post {
        success {
            script {
                emailext (
                    subject: "Build Exitosa de Cronopunk - ${BUILD_NUMBER}",
                    body: """Hola,
                    Aqui tienes build de Cronopunk
                    Puedes revisar los detalles aquí: ${BUILD_URL}

                    Enlace a la build: http://localhost:1000/files/build${BUILD_NUMBER}.zip
                    
                    Un coordial saludo, 
                    Jenkins.""",
                    to: emailextrecipients([
                        [$class: 'DevelopersRecipientProvider']
                    ])
                )
            }
        }
        failure {
            script{
                emailext (
                    subject: "Build Fallida de Cronopunk - ${BUILD_NUMBER}",
                    body: """Hola, 
                    Eres un ruina. La build ha fallado. 
                    Revisa los logs en: ${BUILD_URL}
                    
                    Un coordial saludo, 
                    Jenkins.""",
                    to: emailextrecipients([
                        [$class: 'DevelopersRecipientProvider']
                    ])
                )
            }
        }
    }
}