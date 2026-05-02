pipeline {
    agent {label 'unity-build'}
    environment {
        ZIP_PATH = "%WORKSPACE%/ZippedBuilds" 
        BUILD_PATH = "%WORKSPACE%/Builds" 
    }
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
                    zip archive: true, dir: "%BUILD_PATH%/%BUILD_NUMBER%", zipFile: "%ZIP_PATH%/build${BUILD_NUMBER}.zip"
                } 
            }
        }
    }
}