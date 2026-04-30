pipeline {
    agent any
    stages {

        stage('Build') {
            steps {
                def workingDir = pwd()
                echo "Building Game on ${workingDir}..."
                //bat '''
                //%UNITY_PATH% -executeMethod CustomBuild.BuildWindowsPlayer -buildTarget StandaloneWindows64 -batchmode -quit -projectPath "C:/Users/lenri/Workplace/Usabilidad/ProyectoFinal/JenkinsUAJ/ChronoPunk" -logFile "%WORKSPACE%/build%BUILD_NUMBER%.log" -buildPath "%WORKSPACE%/%BUILD_NUMBER%"
                //'''
            }
        }
        stage('Deliver'){
            steps {
                echo "Delivering zip file ..."
                //script{
                //    zip archive: true, dir: "${BUILD_NUMBER}", zipFile: "build${BUILD_NUMBER}.zip"
                //} 
            }
        }
    }
}