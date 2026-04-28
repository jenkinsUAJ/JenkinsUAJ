import sys
from pathlib import Path
from unitySceneReconstructor import *
from visualize import (
    render_spatial_metric_player_deaths,
    render_spatial_metric_player_iteration_points,
    render_spatial_metric_failure_points,
    render_abandonment_rate_by_level,
    render_abandoment_rate_game,
    render_iteration_rate_by_level,
    render_interaction_with_interactables_rate_by_level,
    render_interaction_with_button_by_level,
    render_interaction_with_levers_by_level
)

DATA_DIR = Path(__file__).resolve().parent / "data"
RESULTS_DIR = Path(__file__).resolve().parent / "results"

def check_data_folders():
    # Comprobar si existe la carpeta, si no existe, crearla y salir
    if not DATA_DIR.exists():
        DATA_DIR.mkdir(parents=True, exist_ok=True)
        print("Error, no hay archivos de datos, añadir datos .json a la carpeta /data.")
        sys.exit(1)

    # Comprobar si hay archivos
    has_files = any(f.is_file() for f in DATA_DIR.iterdir())

    if not has_files:
        print("Error, no hay archivos de datos, añadir datos .json a la carpeta /data.")
        sys.exit(1)

    # Check carpeta results
    if not RESULTS_DIR.exists():
        RESULTS_DIR.mkdir(parents=True, exist_ok=True)

        
def main():
    check_data_folders()

    # Cogemos las bases de datos
    database = get_database()

    database = database.rename(columns={
        "1": "levelID",
        "2": "userID",
        "3": "sessionID",
        "5": "positionX",
        "6": "positionY",
        "7": "shadowID",
        "8": "buttonID",
        "9": "leverID",        
    })

    deathDB = database[database["eventType"] == 0]
    endIterationDB = database[database["eventType"] == 1]
    detFailureDB = database[database["eventType"] == 2]
    leftGameDB = database[database["eventType"] == 3]
    leftLevelDB = database[database["eventType"] == 4]
    shadowSelectDB = database[database["eventType"] == 5]
    buttonPressDB = database[database["eventType"] == 6]
    leverActionDB = database[database["eventType"] == 7]
    levelStartDB = database[database["eventType"] == 8]
    levelEndDB = database[database["eventType"] == 9]

    # Hacemos una lista de los usuarios, almacenando un dataframe por cada uno de estos.
    datasetUsers = [df for _, df in database.groupby("userID")]

    #Obtenemos todos los valores unicos que se han obtenido en levelID (Basicamente los niveles que se han testeado del juego)
    levelIdsValues = database["levelID"].unique()

    # grid para las metricas espaciales
    level_scene_map, prefab_meta = get_scene_and_prefab_data()
    scene_grid_data = make_dataframe_scene_grid(prefab_meta, level_scene_map)
    level_grid_matrices = make_dataframe_level_grid_matrices(scene_grid_data)

    # Renderizado de metricas visualizadas espacialmente
    render_spatial_metric_player_deaths(deathDB, scene_grid_data, level_grid_matrices)
    render_spatial_metric_player_iteration_points(endIterationDB, scene_grid_data, level_grid_matrices)
    render_spatial_metric_failure_points(detFailureDB, scene_grid_data, level_grid_matrices)

    # Renderizado de metricas visualizadas de barras
    render_abandonment_rate_by_level(leftLevelDB)
    render_abandoment_rate_game(leftGameDB)
    render_iteration_rate_by_level(endIterationDB, levelIdsValues)
    render_interaction_with_interactables_rate_by_level(buttonPressDB, leverActionDB)
    render_interaction_with_button_by_level(buttonPressDB, levelIdsValues)
    render_interaction_with_levers_by_level(leverActionDB, levelIdsValues)




if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\nAnalisis interrumpido por el usuario.")
        sys.exit(0)