from unitySceneReconstructor import build_event_points_by_level
import seaborn as sns
import matplotlib.pyplot as plt
import pandas as pd
import numpy as np
from pathlib import Path
from matplotlib.colors import ListedColormap
from matplotlib.patches import Patch

RESULTS_DIR = Path(__file__).resolve().parent / "results"
RESULTS_DIR.mkdir(parents=True, exist_ok=True)

DEFAULT_LAYER_COLORS = {
    "KillOnCollide": "#FFCF4B",
}

def _plot_optional_events(axis, level_id, event_points_by_level=None):
    # Dibuja puntos de evento solo si se reciben datos para el nivel.
    if not event_points_by_level:
        return

    level_events = event_points_by_level.get(level_id, [])
    for event in level_events:
        x = event.get("x")
        y = event.get("y")
        if x is None or y is None:
            continue

        marker = event.get("marker", "o")
        edgecolor = event.get("edgecolor", "none")
        scatter_kwargs = {
            "c": event.get("color", "black"),
            "marker": marker,
            "s": event.get("size", 36),
            "alpha": event.get("alpha", 0.9),
            "label": event.get("label", None),
        }

        # Evitamos warning de matplotlib con marcadores no rellenables como 'x'.
        if marker not in {"x", "+", "1", "2", "3", "4", "|", "_"}:
            scatter_kwargs["edgecolors"] = edgecolor

        axis.scatter(x, y, **scatter_kwargs)

def plot_reconstructed_grids(
    scene_data,
    matrix_data,
    max_cols=1,
    include_empty_layers=False,
    margin_cells=1.0,
    event_points_by_level=None,
    show_event_legend=False,
    represented_data_label=None,
    layer_colors=None,
    palette_name="tab10",
    file_output_name=""
    ):
    # Funcion principal de render por nivel.
    level_ids = sorted(scene_data.keys())
    if not level_ids:
        raise ValueError("No hay niveles parseados para visualizar")

    # Combina colores por defecto con overrides opcionales del usuario.
    resolved_layer_colors = dict(DEFAULT_LAYER_COLORS)
    if layer_colors:
        resolved_layer_colors.update(layer_colors)

    max_cols = max(1, max_cols)
    rows = int(np.ceil(len(level_ids) / max_cols))
    axes = plt.subplots(rows, max_cols, figsize=(12 * max_cols, 4.8 * rows), squeeze=False)[1]
    axes_flat = axes.ravel()

    for axis, level_id in zip(axes_flat, level_ids):
        level_data = scene_data[level_id]
        bounds = matrix_data[level_id]["bounds"]
        min_x, max_x, min_y, max_y = bounds

        layer_items = list(level_data["layers"].items())
        palette = plt.get_cmap(palette_name, max(1, len(layer_items)))

        legend_handles = []
        plotted_layers = 0

        # Pintamos cada capa con color configurable por nombre de capa.
        for layer_index, (layer_file_id, layer_info) in enumerate(layer_items):
            matrix = matrix_data[level_id]["layer_matrices"][layer_file_id]
            if matrix.size == 0:
                continue
            if not include_empty_layers and matrix.sum() == 0:
                continue

            layer_label = layer_info["name"] if layer_info["name"] else f"Layer {layer_file_id}"
            layer_color = resolved_layer_colors.get(layer_label, palette(layer_index))
            mask = np.ma.masked_where(matrix == 0, matrix)

            axis.imshow(
                mask,
                origin="lower",
                interpolation="nearest",
                extent=(min_x - 0.5, max_x + 0.5, min_y - 0.5, max_y + 0.5),
                cmap=ListedColormap([layer_color]),
                alpha=0.72,
            )

            legend_handles.append(Patch(facecolor=layer_color, edgecolor="none", label=layer_label))
            plotted_layers += 1

        # Hook para eventos telemetricos (muertes, fin iteracion, etc.).
        _plot_optional_events(axis, level_id, event_points_by_level=event_points_by_level)

        grid_pos_x, grid_pos_y, _ = level_data["grid"]["position"]
        title_parts = [f"Nivel {level_id}", f"Grid Pos: ({grid_pos_x:.2f}, {grid_pos_y:.2f})"]
        if represented_data_label:
            title_parts.append(represented_data_label)
        axis.set_title(" | ".join(title_parts))
        axis.set_xlabel("Grid X")
        axis.set_ylabel("Grid Y")
        axis.set_aspect("equal")
        axis.set_xlim(min_x - 0.5 - margin_cells, max_x + 0.5 + margin_cells)
        axis.set_ylim(min_y - 0.5 - margin_cells, max_y + 0.5 + margin_cells)
        axis.grid(True, color="lightgray", linewidth=0.3, alpha=0.4)

        if legend_handles:
            axis.legend(handles=legend_handles, loc="upper right", fontsize=8, framealpha=0.95)
        if show_event_legend and event_points_by_level:
            axis.legend(loc="upper left", fontsize=8, framealpha=0.95)
        if plotted_layers == 0:
            axis.text(0.5, 0.5, "Sin capas con tiles", ha="center", va="center", transform=axis.transAxes)

    # Ocultamos ejes sobrantes cuando la rejilla de subplots tiene huecos.
    for axis in axes_flat[len(level_ids):]:
        axis.axis("off")

    plt.tight_layout()
    plt.savefig(RESULTS_DIR / f"spatial_{file_output_name}.png")
    plt.close()

# Render de la metrica espacial de muertes del jugador.
def render_spatial_metric_player_deaths(deathDB, scene_grid_data, level_grid_matrices):

    death_points_by_level = build_event_points_by_level(
        events_df=deathDB,
        scene_data=scene_grid_data,
        color="#FF3300",
        marker="x",
        label="Muerte",
        size=50,
        alpha=1.0,
        edgecolor="white",
        )

    plot_reconstructed_grids(
        scene_data=scene_grid_data,
        matrix_data=level_grid_matrices,
        max_cols=1,
        include_empty_layers=False,
        margin_cells=1.0,
        event_points_by_level=death_points_by_level,
        show_event_legend=False,
        represented_data_label="Death Points",
        file_output_name="death_points"
        )
    

def render_spatial_metric_player_iteration_points(endIterationDB, scene_grid_data, level_grid_matrices):
    # Render de la metrica espacial de fin de iteracion.
    end_iteration_points_by_level = build_event_points_by_level(
    events_df=endIterationDB,
    scene_data=scene_grid_data,
    color="#00FF1A",
    marker="o",
    label="Fin iteracion",
    size=50,
    alpha=1.0,
    edgecolor="white",
    )

    plot_reconstructed_grids(
        scene_data=scene_grid_data,
        matrix_data=level_grid_matrices,
        max_cols=1,
        include_empty_layers=False,
        margin_cells=1.0,
        event_points_by_level=end_iteration_points_by_level,
        show_event_legend=False,
        represented_data_label="End Iteration Points",
        file_output_name="end_iteration_points"
        )
    

def render_spatial_metric_failure_points(detFailureDB, scene_grid_data, level_grid_matrices):
    # Render de la metrica espacial de fallos de determinismo.
    det_failure_points_by_level = build_event_points_by_level(
        events_df=detFailureDB,
        scene_data=scene_grid_data,
        color="#FF00D4",
        marker="^",
        label="Fallo determinismo",
        size=50,
        alpha=1.0,
        edgecolor="white",
        )

    plot_reconstructed_grids(
        scene_data=scene_grid_data,
        matrix_data=level_grid_matrices,
        max_cols=1,
        include_empty_layers=False,
        margin_cells=1.0,
        event_points_by_level=det_failure_points_by_level,
        show_event_legend=False,
        represented_data_label="Determinism Failure Points",
        file_output_name="determinism_failure_points"
        )
    
def render_abandonment_rate_by_level(leftLevelDB):
    ax = sns.countplot(x="levelID", data=leftLevelDB, hue="levelID", palette = "crest", order=sorted(leftLevelDB["levelID"].unique()))
    ax.set_title("Tasa de abandono de nivel")
    ax.tick_params(axis='x', rotation=0)
    plt.savefig(RESULTS_DIR / "abandonment_rate_by_level.png")
    plt.close()

def render_abandoment_rate_game(leftGameDB):
    ax = sns.countplot(x="levelID", data=leftGameDB, hue="levelID", palette = "crest", order=sorted(leftGameDB["levelID"].unique()))
    ax.set_title("Tasa de abandono de juego")
    ax.tick_params(axis='x', rotation=0)
    plt.savefig(RESULTS_DIR / "abandonment_rate_game.png")
    plt.close()

def render_iteration_rate_by_level(endIterationDB, levelIdsValues):
    plt.figure()
    ax = sns.countplot(x="levelID", data=endIterationDB, hue="levelID", palette = "crest")
    ax.set_title("Tasa de iteraciones por nivel")
    ax.tick_params(axis='x', rotation=0)
    plt.savefig(RESULTS_DIR / "iteration_rate_by_level_general.png")
    plt.close()

    endIterationLevelsDS = []
    for level in levelIdsValues:
        endIterationLevelsDS.append(endIterationDB[endIterationDB["levelID"] == level])

    for dataset in endIterationLevelsDS:

        if not dataset.empty:
            plt.figure()
            ax = sns.countplot(x="shadowID", data=dataset, hue="shadowID", palette = "crest")
            level_id = dataset["levelID"].iloc[0]
            ax.set_title("Tasa de iteraciones por sombra en nivel " + str(level_id))
            ax.tick_params(axis='x', rotation=0)
            plt.savefig(RESULTS_DIR / f"iteration_rate_shadow_level_{level_id}.png")
            plt.close()


def render_interaction_with_interactables_rate_by_level(buttonPressDB, leverActionDB):
    buttonPressDB["type"] = "Boton"
    leverActionDB["type"] = "Palanca"

    combined = pd.concat([buttonPressDB, leverActionDB])

    ax = sns.countplot(
        x="levelID",
        hue="type",
        data=combined,
        palette=["#4CA7AF", "#C64444"],
        order=sorted(combined["unique"].unique()) if "unique" in combined.columns else sorted(combined["levelID"].unique())
    )

    ax.set_title("Tasa de iteracciones de elementos activables por nivel")
    ax.tick_params(axis='x', rotation=0)

    plt.savefig(RESULTS_DIR / "interaction_interactables_rate.png")
    plt.close()


def render_interaction_with_button_by_level(buttonPressDB, levelIdsValues):
    buttonPressLevelsDS = []

    for level in levelIdsValues:
        buttonPressLevelsDS.append(buttonPressDB[buttonPressDB["levelID"] == level])

    for dataset in buttonPressLevelsDS:
        if not dataset.empty:
            plt.figure()
            ax = sns.countplot(x="buttonID", data=dataset, hue="buttonID", palette = "crest")
            level_id = dataset["levelID"].iloc[0]
            ax.set_title("Tasa de iteraciones de botón en nivel " + str(level_id))
            ax.tick_params(axis='x', rotation=0)
            plt.savefig(RESULTS_DIR / f"interaction_button_level_{level_id}.png")
            plt.close()

def render_interaction_with_levers_by_level(leverActionDB, levelIdsValues):
    leverActionLevelsDS = []

    for level in levelIdsValues:
        leverActionLevelsDS.append(leverActionDB[leverActionDB["levelID"] == level])

    for dataset in leverActionLevelsDS:
        if not dataset.empty:
            plt.figure()
            ax = sns.countplot(x="leverID", data=dataset, hue="leverID", palette = "crest")
            level_id = dataset["levelID"].iloc[0]
            ax.set_title("Tasa de iteraciones de palanca en nivel " + str(level_id))
            ax.tick_params(axis='x', rotation=0)
            plt.savefig(RESULTS_DIR / f"interaction_lever_level_{level_id}.png")
            plt.close()