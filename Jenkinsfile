pipeline {
    agent {label 'unity-build'}
    stages {

        stage('Build') {
            steps {
                echo pwd()
                echo "Building Game ..."
                bat '''
                %UNITY_PATH% -executeMethod CustomBuild.BuildWindowsPlayer -buildTarget StandaloneWindows64 -batchmode -quit -projectPath "%WORKSPACE%/ChronoPunk" -logFile "%BUILD_PATH%/%BUILD_NUMBER%/build.log" -buildPath "%BUILD_PATH%/%BUILD_NUMBER%"
                '''
            }
        }
        stage('Deliver'){
            steps {
                echo "Delivering zip file ..."
                script{
                    zip archive: true, dir: "%WORKSPACE%/Builds/%BUILD_NUMBER%", zipFile: "%WORKSPACE%/ZippedBuilds/build%BUILD_NUMBER%.zip"
                } 
            }
        }
    }

    post {
        success {
            script {
                emailext (
                    subject: "Build Exitosa de Cronopunk - %BUILD_NUMBER%",
                    body: """Hola,
                    Aqui tienes build de Cronopunk
                    Puedes revisar los detalles aquí: %BUILD_URL%
                    
                    Un coordial saludo, 
                    Jenkins.""",
                    to: ccem(culprits: true),
                    attachmentsPattern: "%WORKSPACE%/ZippedBuilds/build%BUILD_NUMBER%.zip"
                )
            }
        }
        failure {
            script{
                emailext (
                    subject: "Build Fallida de Cronopunk - %BUILD_NUMBER%",
                    body: """Hola, 
                    La build ha fallado. 
                    Revisa los logs en: %BUILD_URL%
                    
                    Un coordial saludo, 
                    Jenkins.""",
                    to: ccem(culprits: true)
                )
            }
        }
    }
}