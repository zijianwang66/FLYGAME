import math
import os

import bpy


PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
BLEND_OUTPUT = os.path.join(
    PROJECT_ROOT,
    "BlenderScenes",
    "DroneRectangleTraining_UnityClean.blend",
)
FBX_OUTPUT = os.path.join(
    PROJECT_ROOT,
    "Assets",
    "DroneMicroClass",
    "Models",
    "Training",
    "DroneRectangleTraining.fbx",
)

TRACK_CENTER_WIDTH = 30.0
TRACK_CENTER_HEIGHT = 16.0
RUNWAY_WIDTH = 3.0
RUNWAY_Z = 0.12
DASH_Z = 0.155
DASH_LENGTH = 0.70
DASH_WIDTH = 0.18


def delete_figure_eight_visuals():
    prefixes = (
        "Left_8_Route_",
        "Right_8_Route_",
        "Center_Tangent_Route_",
    )
    names = {
        "Single_Mesh_Figure_Eight_Runway_3m_Width_No_Overlap",
    }

    for obj in list(bpy.data.objects):
        if obj.name in names or obj.name.startswith(prefixes):
            bpy.data.objects.remove(obj, do_unlink=True)


def sync_principled_base_colors():
    for material in bpy.data.materials:
        if not material.use_nodes or not material.node_tree:
            continue

        for node in material.node_tree.nodes:
            if node.type != "BSDF_PRINCIPLED":
                continue
            base_color = node.inputs.get("Base Color")
            if base_color is not None:
                base_color.default_value = material.diffuse_color


def create_mesh_object(name, vertices, faces, material, parent, z):
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata([(x, y, z) for x, y in vertices], [], faces)
    mesh.update(calc_edges=True)

    uv_layer = mesh.uv_layers.new(name="UVMap")
    for polygon in mesh.polygons:
        for loop_index in polygon.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_index].vertex_index]
            uv_layer.data[loop_index].uv = (vertex.co.x / 3.0, vertex.co.y / 3.0)

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    obj.data.materials.append(material)
    return obj


def create_rectangular_runway(root):
    half_center_x = TRACK_CENTER_WIDTH * 0.5
    half_center_y = TRACK_CENTER_HEIGHT * 0.5
    half_runway = RUNWAY_WIDTH * 0.5

    outer_x = half_center_x + half_runway
    outer_y = half_center_y + half_runway
    inner_x = half_center_x - half_runway
    inner_y = half_center_y - half_runway

    vertices = [
        (-outer_x, -outer_y),
        (outer_x, -outer_y),
        (outer_x, outer_y),
        (-outer_x, outer_y),
        (-inner_x, -inner_y),
        (inner_x, -inner_y),
        (inner_x, inner_y),
        (-inner_x, inner_y),
    ]
    faces = [
        (0, 1, 5, 4),
        (1, 2, 6, 5),
        (2, 3, 7, 6),
        (3, 0, 4, 7),
    ]

    material = bpy.data.materials["Runway_3m_Blue_Plastic_Rubber_Texture"]
    return create_mesh_object(
        "Single_Mesh_Rectangle_Runway_3m_Width",
        vertices,
        faces,
        material,
        root,
        RUNWAY_Z,
    )


def append_dash(vertices, faces, center_x, center_y, length, width, horizontal):
    start = len(vertices)
    half_length = length * 0.5
    half_width = width * 0.5

    if horizontal:
        corners = [
            (center_x - half_length, center_y - half_width),
            (center_x + half_length, center_y - half_width),
            (center_x + half_length, center_y + half_width),
            (center_x - half_length, center_y + half_width),
        ]
    else:
        corners = [
            (center_x - half_width, center_y - half_length),
            (center_x + half_width, center_y - half_length),
            (center_x + half_width, center_y + half_length),
            (center_x - half_width, center_y + half_length),
        ]

    vertices.extend(corners)
    faces.append((start, start + 1, start + 2, start + 3))


def evenly_spaced_centers(half_extent, count, corner_margin):
    if count <= 1:
        return [0.0]
    start = -half_extent + corner_margin
    end = half_extent - corner_margin
    step = (end - start) / (count - 1)
    return [start + step * index for index in range(count)]


def create_rectangular_centerline_dashes(root):
    vertices = []
    faces = []
    half_x = TRACK_CENTER_WIDTH * 0.5
    half_y = TRACK_CENTER_HEIGHT * 0.5

    for x in evenly_spaced_centers(half_x, 21, 0.8):
        append_dash(vertices, faces, x, -half_y, DASH_LENGTH, DASH_WIDTH, True)
        append_dash(vertices, faces, x, half_y, DASH_LENGTH, DASH_WIDTH, True)

    for y in evenly_spaced_centers(half_y, 11, 0.8):
        append_dash(vertices, faces, -half_x, y, DASH_LENGTH, DASH_WIDTH, False)
        append_dash(vertices, faces, half_x, y, DASH_LENGTH, DASH_WIDTH, False)

    material = bpy.data.materials["Dashed_Route_Blue"]
    return create_mesh_object(
        "Rectangle_Route_Centerline_Dashes",
        vertices,
        faces,
        material,
        root,
        DASH_Z,
    )


def validate_geometry(runway, dashes):
    expected_runway = (33.0, 19.0)
    actual_runway = (round(runway.dimensions.x, 4), round(runway.dimensions.y, 4))
    if actual_runway != expected_runway:
        raise RuntimeError(
            f"Unexpected runway dimensions: {actual_runway}, expected {expected_runway}"
        )

    if len(dashes.data.polygons) != 64:
        raise RuntimeError(
            f"Unexpected dash count: {len(dashes.data.polygons)}, expected 64"
        )


def save_and_export():
    os.makedirs(os.path.dirname(BLEND_OUTPUT), exist_ok=True)
    os.makedirs(os.path.dirname(FBX_OUTPUT), exist_ok=True)

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUTPUT)
    bpy.ops.export_scene.fbx(
        filepath=FBX_OUTPUT,
        use_selection=False,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
    )


def main():
    delete_figure_eight_visuals()
    sync_principled_base_colors()

    root = bpy.data.objects.get("Drone_Training_Figure_Eight_Root")
    if root is None:
        raise RuntimeError("Training field root object was not found")
    root.name = "Drone_Training_Rectangle_Root"

    runway = create_rectangular_runway(root)
    dashes = create_rectangular_centerline_dashes(root)
    validate_geometry(runway, dashes)
    save_and_export()

    print(f"Created Blender source: {BLEND_OUTPUT}")
    print(f"Exported Unity model: {FBX_OUTPUT}")


if __name__ == "__main__":
    main()
