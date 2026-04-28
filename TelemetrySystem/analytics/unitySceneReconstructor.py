from pathlib import Path
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import zipfile
import os
import re
from pathlib import Path
from collections import defaultdict
from matplotlib.colors import ListedColormap
from matplotlib.patches import Patch

DATA_DIR = Path(__file__).resolve().parent / "data"

# -----------------------------------------------------------------------------------

# Esta seccion prepara la configuracion base del sistema de reconstruccion de niveles.

LEVELS_DIR = Path(__file__).resolve().parent / "levels"
GRID_PREFAB_PATH = LEVELS_DIR / "Grid.prefab"
GRID_PREFAB_GUID = "4dddbf204a84a2241b315f01ca8c8f9a"

def _parse_vector3(vector_literal):
    # Convierte un literal tipo {x: a, y: b, z: c} en una tupla de floats.
    match = re.search(r"\{x:\s*([-\d.eE]+),\s*y:\s*([-\d.eE]+),\s*z:\s*([-\d.eE]+)\}", vector_literal)
    if not match:
        return (0.0, 0.0, 0.0)
    return tuple(float(value) for value in match.groups())


def parse_grid_prefab(prefab_path):
    # Lee el prefab completo y separa cada bloque serializado de Unity.
    text = prefab_path.read_text(encoding="utf-8")
    object_pattern = re.compile(
        r"^--- !u!(?P<type>\d+) &(?P<fileid>-?\d+)\n(?P<class>\w+):\n(?P<body>.*?)(?=^--- !u!|\Z)",
        re.MULTILINE | re.DOTALL,
    )

    # Estructuras auxiliares para relacionar componentes con GameObjects.
    gameobjects = {}
    transforms_by_gameobject = {}
    tilemaps = {}
    grid_component = None

    for match in object_pattern.finditer(text):
        file_id = int(match.group("fileid"))
        class_name = match.group("class")
        body = match.group("body")

        # Guardamos nombre y componentes de cada GameObject.
        if class_name == "GameObject":
            name_match = re.search(r"^\s*m_Name:\s*(.+)$", body, re.MULTILINE)
            name = name_match.group(1).strip() if name_match else f"GameObject_{file_id}"
            component_ids = [int(value) for value in re.findall(r"component:\s*\{fileID:\s*(-?\d+)\}", body)]
            gameobjects[file_id] = {
                "name": name,
                "components": component_ids,
            }

        # Relacionamos cada Transform con su GameObject padre.
        elif class_name == "Transform":
            gameobject_match = re.search(r"^\s*m_GameObject:\s*\{fileID:\s*(-?\d+)\}", body, re.MULTILINE)
            if gameobject_match:
                transforms_by_gameobject[int(gameobject_match.group(1))] = file_id

        # Capturamos metadatos de cada Tilemap definido en el prefab.
        elif class_name == "Tilemap":
            gameobject_match = re.search(r"^\s*m_GameObject:\s*\{fileID:\s*(-?\d+)\}", body, re.MULTILINE)
            origin_match = re.search(r"^\s*m_Origin:\s*(\{[^\n]+\})", body, re.MULTILINE)
            size_match = re.search(r"^\s*m_Size:\s*(\{[^\n]+\})", body, re.MULTILINE)
            tile_anchor_match = re.search(r"^\s*m_TileAnchor:\s*(\{[^\n]+\})", body, re.MULTILINE)
            if gameobject_match:
                tilemaps[file_id] = {
                    "gameobject_id": int(gameobject_match.group(1)),
                    "default_origin": _parse_vector3(origin_match.group(1)) if origin_match else (0.0, 0.0, 0.0),
                    "default_size": _parse_vector3(size_match.group(1)) if size_match else (0.0, 0.0, 0.0),
                    "tile_anchor": _parse_vector3(tile_anchor_match.group(1)) if tile_anchor_match else (0.5, 0.5, 0.0),
                }

        # Capturamos configuracion global del componente Grid.
        elif class_name == "Grid":
            gameobject_match = re.search(r"^\s*m_GameObject:\s*\{fileID:\s*(-?\d+)\}", body, re.MULTILINE)
            cell_size_match = re.search(r"^\s*m_CellSize:\s*(\{[^\n]+\})", body, re.MULTILINE)
            cell_gap_match = re.search(r"^\s*m_CellGap:\s*(\{[^\n]+\})", body, re.MULTILINE)
            cell_layout_match = re.search(r"^\s*m_CellLayout:\s*(-?\d+)", body, re.MULTILINE)
            cell_swizzle_match = re.search(r"^\s*m_CellSwizzle:\s*(-?\d+)", body, re.MULTILINE)
            if gameobject_match:
                grid_component = {
                    "component_id": file_id,
                    "gameobject_id": int(gameobject_match.group(1)),
                    "cell_size": _parse_vector3(cell_size_match.group(1)) if cell_size_match else (1.0, 1.0, 0.0),
                    "cell_gap": _parse_vector3(cell_gap_match.group(1)) if cell_gap_match else (0.0, 0.0, 0.0),
                    "cell_layout": int(cell_layout_match.group(1)) if cell_layout_match else 0,
                    "cell_swizzle": int(cell_swizzle_match.group(1)) if cell_swizzle_match else 0,
                }

    if grid_component is None:
        raise ValueError("No se encontro un componente Grid en Grid.prefab")

    # Construimos metadatos consolidados del Grid raiz.
    grid_go_id = grid_component["gameobject_id"]
    grid_transform_file_id = transforms_by_gameobject.get(grid_go_id)

    # Construimos metadatos por capa de Tilemap.
    layers = {}
    for tilemap_file_id, tilemap_data in tilemaps.items():
        gameobject_id = tilemap_data["gameobject_id"]
        layer_name = gameobjects.get(gameobject_id, {}).get("name", f"Layer_{tilemap_file_id}")
        layers[tilemap_file_id] = {
            "name": layer_name,
            "gameobject_id": gameobject_id,
            "transform_file_id": transforms_by_gameobject.get(gameobject_id),
            "default_origin": tilemap_data["default_origin"],
            "default_size": tilemap_data["default_size"],
            "tile_anchor": tilemap_data["tile_anchor"],
        }

    return {
        "grid": {
            "name": gameobjects.get(grid_go_id, {}).get("name", "Grid"),
            "gameobject_id": grid_go_id,
            "transform_file_id": grid_transform_file_id,
            "cell_size": grid_component["cell_size"],
            "cell_gap": grid_component["cell_gap"],
            "cell_layout": grid_component["cell_layout"],
            "cell_swizzle": grid_component["cell_swizzle"],
        },
        "layers": layers,
    }

# Genera la configuracion base del sistema de reconstruccion de niveles.
# 1) Definir rutas y constantes del prefab de Grid.
# 2) Parsear Grid.prefab para obtener metadatos de grid y capas.
# 3) Construir el mapa nivel -> escena y validar resultados por consola.

def get_scene_and_prefab_data(): 
    # Mapeamos nivel numerico a su fichero de escena .unity.
    level_scene_map = {
        int(scene_path.stem): scene_path
        for scene_path in sorted(LEVELS_DIR.glob("*.unity"), key=lambda path: int(path.stem))
        if scene_path.stem.isdigit()
    }

    # Parseamos Grid.prefab una sola vez para reutilizar metadatos en todo el pipeline.
    prefab_meta = parse_grid_prefab(GRID_PREFAB_PATH)

    # Resumen rapido para validar que la configuracion de entrada es correcta.
    print("Niveles detectados:", sorted(level_scene_map.keys()))
    print("Cell size del Grid:", prefab_meta["grid"]["cell_size"])
    print("Capas encontradas en Grid.prefab:")
    for layer_file_id, layer_info in sorted(prefab_meta["layers"].items(), key=lambda item: item[1]["name"]):
        print(f"  - {layer_info['name']} (Tilemap fileID: {layer_file_id})")

    return level_scene_map, prefab_meta


# -----------------------------------------------------------------------------------

# Esta seccion parsea cada escena .unity para obtener datos reales por nivel.
# Responsabilidades de la celda:
# 1) Leer overrides del PrefabInstance que apunta a Grid.prefab.
# 2) Extraer transform del Grid y tiles por capa.
# 3) Construir un resumen tabular para validacion rapida.

def _to_int(value, default=0):
    # Convierte valores de texto a entero de forma segura.
    try:
        return int(float(value))
    except (TypeError, ValueError):
        return default


def _to_float(value, default=0.0):
    # Convierte valores de texto a float de forma segura.
    try:
        return float(value)
    except (TypeError, ValueError):
        return default


def parse_grid_prefab_overrides(scene_path, prefab_guid):
    # Lee la escena serializada de Unity y busca bloques PrefabInstance.
    text = scene_path.read_text(encoding="utf-8")
    instance_pattern = re.compile(
        r"^--- !u!1001 &(?P<instance_id>-?\d+)\nPrefabInstance:\n(?P<body>.*?)(?=^--- !u!|\Z)",
        re.MULTILINE | re.DOTALL,
    )
    property_pattern = re.compile(
        r"- target: \{fileID: (?P<file_id>-?\d+), guid: (?P<guid>[0-9a-f]+), type: 3\}\n"
        r"\s*propertyPath: (?P<property>[^\n]+)\n"
        r"\s*value: (?P<value>[^\n]*)",
        re.MULTILINE,
    )

    # Salida: fileID objetivo -> {propertyPath: value}.
    overrides_by_target = defaultdict(dict)

    for instance_match in instance_pattern.finditer(text):
        body = instance_match.group("body")
        source_match = re.search(r"m_SourcePrefab:\s*\{fileID:\s*100100000,\s*guid:\s*([0-9a-f]+),\s*type:\s*3\}", body)
        if not source_match:
            continue
        if source_match.group(1) != prefab_guid:
            continue

        # Solo guardamos propiedades de este prefab concreto.
        for prop_match in property_pattern.finditer(body):
            if prop_match.group("guid") != prefab_guid:
                continue
            target_file_id = int(prop_match.group("file_id"))
            property_name = prop_match.group("property").strip()
            property_value = prop_match.group("value").strip()
            overrides_by_target[target_file_id][property_name] = property_value

    return overrides_by_target


def parse_scene_grid(scene_path, prefab_meta, prefab_guid):
    # Combina metadatos del prefab con overrides de una escena concreta.
    level_id = int(scene_path.stem)
    overrides = parse_grid_prefab_overrides(scene_path, prefab_guid)

    # Transform final del Grid raiz en este nivel.
    grid_transform_id = prefab_meta["grid"]["transform_file_id"]
    grid_override = overrides.get(grid_transform_id, {})
    grid_position = (
        _to_float(grid_override.get("m_LocalPosition.x", 0.0)),
        _to_float(grid_override.get("m_LocalPosition.y", 0.0)),
        _to_float(grid_override.get("m_LocalPosition.z", 0.0)),
    )
    grid_rotation = (
        _to_float(grid_override.get("m_LocalRotation.x", 0.0)),
        _to_float(grid_override.get("m_LocalRotation.y", 0.0)),
        _to_float(grid_override.get("m_LocalRotation.z", 0.0)),
        _to_float(grid_override.get("m_LocalRotation.w", 1.0)),
    )
    grid_scale = (
        _to_float(grid_override.get("m_LocalScale.x", 1.0)),
        _to_float(grid_override.get("m_LocalScale.y", 1.0)),
        _to_float(grid_override.get("m_LocalScale.z", 1.0)),
    )

    # Recorremos capas conocidas en prefab y aplicamos overrides de escena.
    scene_layers = {}
    for layer_file_id, layer_info in prefab_meta["layers"].items():
        layer_override = overrides.get(layer_file_id, {})

        default_origin = layer_info["default_origin"]
        default_size = layer_info["default_size"]

        origin_x = _to_int(layer_override.get("m_Origin.x", default_origin[0]))
        origin_y = _to_int(layer_override.get("m_Origin.y", default_origin[1]))
        size_x = _to_int(layer_override.get("m_Size.x", default_size[0]))
        size_y = _to_int(layer_override.get("m_Size.y", default_size[1]))
        tile_count = _to_int(layer_override.get("m_Tiles.Array.size", 0))

        # Extraemos coordenadas de cada tile pintado en esta capa.
        tiles = []
        for index in range(tile_count):
            x_key = f"m_Tiles.Array.data[{index}].first.x"
            y_key = f"m_Tiles.Array.data[{index}].first.y"
            if x_key not in layer_override or y_key not in layer_override:
                continue

            tile_x = _to_int(layer_override.get(x_key))
            tile_y = _to_int(layer_override.get(y_key))
            tile_index_key = f"m_Tiles.Array.data[{index}].second.m_TileIndex"
            tile_index = _to_int(layer_override.get(tile_index_key), default=-1)
            tiles.append((tile_x, tile_y, tile_index))

        scene_layers[layer_file_id] = {
            "name": layer_info["name"],
            "origin": (origin_x, origin_y),
            "size": (size_x, size_y),
            "tiles": tiles,
            "tile_count": tile_count,
        }

    return {
        "level_id": level_id,
        "scene_path": str(scene_path),
        "grid": {
            "position": grid_position,
            "rotation": grid_rotation,
            "scale": grid_scale,
            "cell_size": prefab_meta["grid"]["cell_size"],
            "cell_gap": prefab_meta["grid"]["cell_gap"],
            "cell_layout": prefab_meta["grid"]["cell_layout"],
            "cell_swizzle": prefab_meta["grid"]["cell_swizzle"],
        },
        "layers": scene_layers,
    }


def make_dataframe_scene_grid(prefab_meta, level_scene_map):
    # Parseamos todas las escenas detectadas en el paso anterior.
    scene_grid_data = {
        level_id: parse_scene_grid(scene_path, prefab_meta, GRID_PREFAB_GUID)
        for level_id, scene_path in sorted(level_scene_map.items())
    }

    # Construimos una tabla compacta de control por nivel.
    scene_summary_rows = []
    for level_id, level_data in scene_grid_data.items():
        total_tiles = sum(layer["tile_count"] for layer in level_data["layers"].values())
        non_empty_layers = sum(1 for layer in level_data["layers"].values() if layer["tile_count"] > 0)
        grid_x, grid_y, grid_z = level_data["grid"]["position"]
        scene_summary_rows.append(
            {
                "levelID": level_id,
                "gridPosX": grid_x,
                "gridPosY": grid_y,
                "gridPosZ": grid_z,
                "layers": len(level_data["layers"]),
                "nonEmptyLayers": non_empty_layers,
                "totalTiles": total_tiles,
            }
        )

    scene_grid_summary_df = pd.DataFrame(scene_summary_rows).sort_values("levelID").reset_index(drop=True)

    return scene_grid_data

# -----------------------------------------------------------------------------------

# Esta seccion transforma los tiles parseados en matrices binarias por nivel y capa.
# Responsabilidades de la celda:
# 1) Calcular bounds de trabajo por nivel.
# 2) Generar una matriz 2D por capa (0 vacio, 1 tile).
# 3) Publicar un resumen de dimensiones para chequeo rapido.


def _compute_level_bounds(level_data):
    # Ajustamos el marco al contenido real (tiles pintados) para evitar huecos grandes.
    tile_coords = [
        (tile_x, tile_y)
        for layer in level_data["layers"].values()
        for tile_x, tile_y, _ in layer["tiles"]
    ]

    if tile_coords:
        xs, ys = zip(*tile_coords)
        return (int(min(xs)), int(max(xs)), int(min(ys)), int(max(ys)))

    # Fallback: si un nivel no tiene tiles, usamos el bounding por origin/size.
    min_x, min_y = np.inf, np.inf
    max_x, max_y = -np.inf, -np.inf

    for layer in level_data["layers"].values():
        origin_x, origin_y = layer["origin"]
        size_x, size_y = layer["size"]
        if size_x > 0 and size_y > 0:
            min_x = min(min_x, origin_x)
            min_y = min(min_y, origin_y)
            max_x = max(max_x, origin_x + size_x - 1)
            max_y = max(max_y, origin_y + size_y - 1)

    if not np.isfinite(min_x):
        return (0, 0, 0, 0)

    return (int(min_x), int(max_x), int(min_y), int(max_y))


def _build_layer_matrix(layer_data, bounds):
    # Proyecta tiles de una capa a una matriz local segun bounds del nivel.
    min_x, max_x, min_y, max_y = bounds
    width = max_x - min_x + 1
    height = max_y - min_y + 1
    matrix = np.zeros((height, width), dtype=np.uint8)

    for tile_x, tile_y, _ in layer_data["tiles"]:
        col = tile_x - min_x
        row = tile_y - min_y
        if 0 <= row < height and 0 <= col < width:
            matrix[row, col] = 1

    return matrix


def build_level_grid_matrices(scene_data):
    # Construye el diccionario final: level -> bounds + matrices por capa.
    matrix_data = {}
    for level_id, level_data in scene_data.items():
        bounds = _compute_level_bounds(level_data)
        layer_matrices = {}
        for layer_file_id, layer_data in level_data["layers"].items():
            layer_matrices[layer_file_id] = _build_layer_matrix(layer_data, bounds)
        matrix_data[level_id] = {
            "bounds": bounds,
            "layer_matrices": layer_matrices,
        }
    return matrix_data



def make_dataframe_level_grid_matrices(scene_grid_data):
    # Ejecutamos la preparacion de matrices para todos los niveles.
    level_grid_matrices = build_level_grid_matrices(scene_grid_data)

    # Tabla de apoyo para verificar dimensiones y limites por nivel.
    matrix_summary_rows = []
    for level_id in sorted(level_grid_matrices.keys()):
        min_x, max_x, min_y, max_y = level_grid_matrices[level_id]["bounds"]
        matrix_summary_rows.append({
            "levelID": level_id,
            "minX": min_x,
            "maxX": max_x,
            "minY": min_y,
            "maxY": max_y,
            "ancho": max_x - min_x + 1,
            "alto": max_y - min_y + 1,
        })

    matrix_bounds_summary_df = pd.DataFrame(matrix_summary_rows)
    return level_grid_matrices

# -----------------------------------------------------------------------------------

# Esta seccion define funciones de visualizacion reutilizables.
# Responsabilidades de la celda:
# 1) Dibujar layouts de niveles desde matrices binarias.
# 2) Convertir eventos del mundo a coordenadas de grid.
# 3) Mantener la fase de metricas enfocada solo en render.

# Mapa base editable: nombre de capa -> color.
# Se puede anadir aqui cualquier capa del prefab, por ejemplo:
# "Terrain", "Background", "Foreground", "KillOnCollide".


def _world_to_grid_coordinates(world_x, world_y, level_data):
    # Convierte posicion en mundo a coordenada de grid usando offset y cell size del nivel.
    grid_pos_x, grid_pos_y, _ = level_data["grid"]["position"]
    cell_size_x, cell_size_y, _ = level_data["grid"]["cell_size"]
    if cell_size_x == 0 or cell_size_y == 0:
        return (None, None)

    grid_x = (world_x - grid_pos_x) / cell_size_x
    grid_y = (world_y - grid_pos_y) / cell_size_y
    return (grid_x, grid_y)


def build_event_points_by_level(
    events_df,
    scene_data,
    color="black",
    marker="o",
    label=None,
    size=36,
    alpha=0.9,
    edgecolor="none",
    ):
    # Genera el diccionario levelID -> lista de puntos listos para overlay.
    required_columns = {"levelID", "positionX", "positionY"}
    if events_df is None or events_df.empty:
        return {}
    if not required_columns.issubset(set(events_df.columns)):
        return {}

    points_by_level = defaultdict(list)
    valid_rows = events_df.dropna(subset=["levelID", "positionX", "positionY"])
    for _, row in valid_rows.iterrows():
        level_id = int(row["levelID"])
        if level_id not in scene_data:
            continue

        grid_x, grid_y = _world_to_grid_coordinates(
            world_x=float(row["positionX"]),
            world_y=float(row["positionY"]),
            level_data=scene_data[level_id],
        )
        if grid_x is None or grid_y is None:
            continue

        points_by_level[level_id].append(
            {
                "x": grid_x,
                "y": grid_y,
                "color": color,
                "marker": marker,
                "size": size,
                "alpha": alpha,
                "label": label,
                "edgecolor": edgecolor,
            }
        )

    return dict(points_by_level)

# -----------------------------------------------------------------------------------
# Sacar datos de los archivos del juego

def get_database():

    dataframes = []

    #Recorremos los archivos de la carpeta que tiene los datos
    for archivo in os.listdir(DATA_DIR):

        #revisamos que el archivo sea un .zip
        if archivo.endswith(".zip"):

            #Abrimos el archivo .zip y leemos el archivo JSON que contiene
            with zipfile.ZipFile(os.path.join(DATA_DIR, archivo)) as z:

                with z.open(z.namelist()[0]) as f:

                    #Leemos el archivo JSON y lo convertimos en un DataFrame de pandas
                    df = pd.read_json(f, orient="records")
                    dataframes.append(df)

        #Carga JSON directo
        elif archivo.endswith(".json"):

            df = pd.read_json(os.path.join(DATA_DIR, archivo), orient="records")
            dataframes.append(df)

        #Carga CSV directo
        elif archivo.endswith(".csv"):
            df = pd.read_csv(os.path.join(DATA_DIR, archivo))
            dataframes.append(df)

    #Concatenamos todos los DataFrames en uno solo
    database = pd.concat(dataframes)

    print(database.head())
    print(database.shape)

    return database
