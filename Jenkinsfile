pipeline {
    agent any
    stages {

        stage('Build') {
            steps {
                echo pwd()
                echo "Building Game ..."
                bat '''
                %UNITY_PATH% -executeMethod CustomBuild.BuildWindowsPlayer -buildTarget StandaloneWindows64 -batchmode -quit -projectPath "%WORKSPACE%/ChronoPunk" -logFile "%WORKSPACE%/build%BUILD_NUMBER%.log" -buildPath "%WORKSPACE%/%BUILD_NUMBER%"
                '''
            }
        }
        stage('Deliver'){
            steps {
                echo "Delivering zip file ..."
                script{
                    zip archive: true, dir: "${BUILD_NUMBER}", zipFile: "Builds/build${BUILD_NUMBER}.zip"
                } 
            }
        }
    }
}