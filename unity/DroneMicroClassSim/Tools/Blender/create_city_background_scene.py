from __future__ import annotations

import math
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(r"K:\FileK\unityprojects\DroneMicroClassSim")
ASSET_DIR = PROJECT_ROOT / "素材"
BLENDER_OUTPUT = PROJECT_ROOT / "BlenderScenes" / "CityBackground.blend"
FBX_OUTPUT = PROJECT_ROOT / "Assets" / "DroneMicroClass" / "Models" / "Environment" / "CityBackground.fbx"
PREVIEW_OUTPUT = PROJECT_ROOT / "BlenderScenes" / "CityBackgroundPreview.png"


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def material(name: str, color: tuple[float, float, float, float], roughness: float = 0.7) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
    return mat


def cube(name: str, location: tuple[float, float, float], scale: tuple[float, float, float], mat: bpy.types.Material) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    obj.location = location
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


def cylinder(
    name: str,
    location: tuple[float, float, float],
    radius: float,
    depth: float,
    mat: bpy.types.Material,
    vertices: int = 12,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    return obj


def add_window_grid(
    name: str,
    x: float,
    y: float,
    width: float,
    depth: float,
    height: float,
    face: str,
    mat: bpy.types.Material,
) -> None:
    vertices: list[tuple[float, float, float]] = []
    faces: list[tuple[int, int, int, int]] = []

    def add_rect(corners: tuple[tuple[float, float, float], tuple[float, float, float], tuple[float, float, float], tuple[float, float, float]]) -> None:
        start = len(vertices)
        vertices.extend(corners)
        faces.append((start, start + 1, start + 2, start + 3))

    if face in {"front", "back"}:
        cols = max(3, min(9, int(width / 0.9)))
        rows = max(3, min(12, int(height / 1.55)))
        face_y = y - depth * 0.506 if face == "front" else y + depth * 0.506
        for row in range(rows):
            for col in range(cols):
                wx = x - width * 0.36 + width * 0.72 * (col / max(1, cols - 1))
                wz = 1.15 + (height - 2.2) * (row / max(1, rows - 1))
                half_w = 0.18
                half_h = 0.16
                if face == "front":
                    add_rect(((wx - half_w, face_y, wz - half_h), (wx - half_w, face_y, wz + half_h), (wx + half_w, face_y, wz + half_h), (wx + half_w, face_y, wz - half_h)))
                else:
                    add_rect(((wx - half_w, face_y, wz - half_h), (wx + half_w, face_y, wz - half_h), (wx + half_w, face_y, wz + half_h), (wx - half_w, face_y, wz + half_h)))
    else:
        cols = max(3, min(8, int(depth / 0.9)))
        rows = max(3, min(12, int(height / 1.55)))
        face_x = x - width * 0.506 if face == "left" else x + width * 0.506
        for row in range(rows):
            for col in range(cols):
                wy = y - depth * 0.36 + depth * 0.72 * (col / max(1, cols - 1))
                wz = 1.15 + (height - 2.2) * (row / max(1, rows - 1))
                half_w = 0.18
                half_h = 0.16
                if face == "left":
                    add_rect(((face_x, wy - half_w, wz - half_h), (face_x, wy + half_w, wz - half_h), (face_x, wy + half_w, wz + half_h), (face_x, wy - half_w, wz + half_h)))
                else:
                    add_rect(((face_x, wy - half_w, wz - half_h), (face_x, wy - half_w, wz + half_h), (face_x, wy + half_w, wz + half_h), (face_x, wy + half_w, wz - half_h)))

    mesh = bpy.data.meshes.new(f"{name}_GridWindows_{face}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(f"{name}_GridWindows_{face}", mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)


def add_building(
    name: str,
    x: float,
    y: float,
    width: float,
    depth: float,
    height: float,
    mat: bpy.types.Material,
    window: bpy.types.Material,
    faces: tuple[str, ...],
) -> None:
    body = cube(name, (x, y, height * 0.5), (width, depth, height), mat)
    bevel = body.modifiers.new("Subtle Edge Bevel", "BEVEL")
    bevel.width = 0.06
    bevel.segments = 1
    body.modifiers.new("Weighted Normals", "WEIGHTED_NORMAL")

    for face in faces:
        add_window_grid(name, x, y, width, depth, height, face, window)


def normalized_tree_source(source: bpy.types.Object, trunk_mat: bpy.types.Material, foliage_mat: bpy.types.Material) -> bpy.types.Object:
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = source.evaluated_get(depsgraph)
    source_mesh = evaluated.to_mesh()
    try:
        world_vertices = [source.matrix_world @ vertex.co for vertex in source_mesh.vertices]
        min_x = min(vertex.x for vertex in world_vertices)
        max_x = max(vertex.x for vertex in world_vertices)
        min_y = min(vertex.y for vertex in world_vertices)
        max_y = max(vertex.y for vertex in world_vertices)
        min_z = min(vertex.z for vertex in world_vertices)
        center = Vector(((min_x + max_x) * 0.5, (min_y + max_y) * 0.5, min_z))
        vertices = [tuple(vertex - center) for vertex in world_vertices]
        faces = [[vertex for vertex in polygon.vertices] for polygon in source_mesh.polygons]

        mesh = bpy.data.meshes.new(source.name + "_NormalizedMesh")
        mesh.from_pydata(vertices, [], faces)
        mesh.update()
        mesh.materials.append(trunk_mat)
        mesh.materials.append(foliage_mat)

        for index, polygon in enumerate(mesh.polygons):
            source_material = None
            source_polygon = source_mesh.polygons[index]
            if source_polygon.material_index < len(source.material_slots):
                source_material = source.material_slots[source_polygon.material_index].material

            material_name = source_material.name.lower() if source_material is not None else ""
            polygon.material_index = 0 if "trunk" in material_name else 1

        obj = bpy.data.objects.new(source.name + "_Normalized", mesh)
        bpy.context.collection.objects.link(obj)
        obj.hide_viewport = True
        obj.hide_render = True
        return obj
    finally:
        evaluated.to_mesh_clear()


def import_tree_sources(trunk_mat: bpy.types.Material, foliage_mat: bpy.types.Material) -> list[bpy.types.Object]:
    plant_fbx = next((path for path in ASSET_DIR.glob("*.fbx") if "plants" in path.name.lower()), None)
    if plant_fbx is None:
        return []

    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(plant_fbx))
    imported = [obj for obj in bpy.data.objects if obj not in before and obj.type == "MESH"]
    keep_prefixes = ("Tree-01-4", "Tree-02-4", "Tree-03-4", "Tree-01-3")
    kept: list[bpy.types.Object] = []
    for obj in imported:
        if obj.name.startswith(keep_prefixes):
            obj.name = "TreeSource_" + obj.name
            kept.append(normalized_tree_source(obj, trunk_mat, foliage_mat))

        bpy.data.objects.remove(obj, do_unlink=True)
    return kept


def place_tree(source: bpy.types.Object, name: str, x: float, y: float, scale: float, yaw_degrees: float) -> bpy.types.Object:
    obj = source.copy()
    obj.data = source.data
    obj.animation_data_clear()
    bpy.context.collection.objects.link(obj)
    obj.name = name
    obj.location = (x, y, 0)
    obj.rotation_euler = (0, 0, math.radians(yaw_degrees))
    obj.scale = (scale, scale, scale)
    obj.hide_viewport = False
    obj.hide_render = False
    return obj


def add_tree_rows(tree_sources: list[bpy.types.Object]) -> None:
    if not tree_sources:
        return

    positions: list[tuple[float, float]] = []
    for x in range(-74, 75, 5):
        positions.append((x, -33.5))
        positions.append((x, 36.5))
    for y in range(-28, 33, 5):
        positions.append((-47.5, y))
        positions.append((47.5, y))
    for x in range(-66, 67, 8):
        positions.append((x, -58.5))
        positions.append((x, 62.5))

    for index, (x, y) in enumerate(positions):
        source = tree_sources[index % len(tree_sources)]
        scale = 0.48 + (index % 5) * 0.035
        place_tree(source, f"City_LowPolyTree_{index:02d}", x, y, scale, (index * 41) % 360)


def add_roads(road: bpy.types.Material, sidewalk: bpy.types.Material, grass: bpy.types.Material) -> None:
    cube("City_Background_Ground", (0, 4, -0.035), (178, 152, 0.06), grass)
    cube("City_Background_Back_Road", (0, 49, 0.012), (168, 8.0, 0.035), road)
    cube("City_Background_Front_Road", (0, -47, 0.012), (168, 8.0, 0.035), road)
    cube("City_Background_Left_Road", (-61, 3, 0.014), (7.0, 98, 0.035), road)
    cube("City_Background_Right_Road", (61, 3, 0.014), (7.0, 98, 0.035), road)
    cube("City_Background_Back_Block_Road", (0, 69, 0.012), (168, 6.0, 0.035), road)
    cube("City_Background_Front_Block_Road", (0, -67, 0.012), (168, 6.0, 0.035), road)

    cube("City_Background_Back_Sidewalk", (0, 43.9, 0.04), (168, 1.2, 0.05), sidewalk)
    cube("City_Background_Front_Sidewalk", (0, -41.9, 0.04), (168, 1.2, 0.05), sidewalk)
    cube("City_Background_Left_Sidewalk", (-55.9, 3, 0.04), (1.2, 98, 0.05), sidewalk)
    cube("City_Background_Right_Sidewalk", (55.9, 3, 0.04), (1.2, 98, 0.05), sidewalk)
    cube("City_Background_Back_Outer_Sidewalk", (0, 64.7, 0.04), (168, 1.0, 0.05), sidewalk)
    cube("City_Background_Front_Outer_Sidewalk", (0, -62.7, 0.04), (168, 1.0, 0.05), sidewalk)


def add_city_blocks(building_mats: list[bpy.types.Material], window: bpy.types.Material) -> None:
    block_specs = [
        (-72, 60, 6.5, 6.2, 9, ("front", "right")), (-61, 58, 7.5, 6.0, 15, ("front",)),
        (-49, 59, 6.0, 5.5, 12, ("front",)), (-37, 60, 8.0, 6.4, 19, ("front",)),
        (-24, 58, 6.5, 5.8, 11, ("front",)), (-13, 61, 7.4, 6.2, 22, ("front",)),
        (0, 59, 6.8, 6.2, 14, ("front",)), (12, 60, 7.8, 5.8, 20, ("front",)),
        (25, 58, 6.5, 6.0, 12, ("front",)), (37, 61, 8.2, 6.5, 17, ("front",)),
        (50, 59, 6.0, 5.5, 10, ("front",)), (62, 60, 7.5, 6.2, 18, ("front",)),
        (74, 58, 6.5, 5.8, 13, ("front", "left")),

        (-72, -56, 6.0, 5.6, 14, ("back", "right")), (-60, -58, 7.0, 6.0, 9, ("back",)),
        (-47, -57, 6.5, 5.5, 18, ("back",)), (-34, -59, 8.0, 6.0, 12, ("back",)),
        (-22, -56, 6.0, 5.4, 16, ("back",)), (-10, -58, 7.4, 6.0, 11, ("back",)),
        (3, -57, 6.2, 5.6, 20, ("back",)), (16, -59, 8.0, 6.2, 13, ("back",)),
        (28, -56, 6.0, 5.2, 17, ("back",)), (41, -58, 7.4, 6.0, 10, ("back",)),
        (54, -57, 6.6, 5.6, 19, ("back",)), (67, -59, 7.8, 6.4, 12, ("back", "left")),

        (-77, -31, 5.6, 7.2, 13, ("right",)), (-78, -18, 6.0, 6.8, 20, ("right",)),
        (-76, -5, 5.8, 7.0, 11, ("right",)), (-79, 9, 6.5, 7.4, 17, ("right",)),
        (-77, 23, 5.8, 7.0, 14, ("right",)), (-78, 36, 6.4, 6.8, 21, ("right",)),

        (77, -31, 6.0, 7.0, 18, ("left",)), (79, -18, 5.6, 6.8, 12, ("left",)),
        (76, -4, 6.4, 7.4, 21, ("left",)), (78, 10, 5.8, 7.0, 14, ("left",)),
        (77, 24, 6.2, 6.8, 17, ("left",)), (79, 37, 5.8, 7.2, 11, ("left",)),
    ]

    for index, (x, y, width, depth, height, faces) in enumerate(block_specs):
        add_building(f"City_Background_Building_{index:02d}", x, y, width, depth, height, building_mats[index % len(building_mats)], window, faces)

    low_specs = []
    for index, x in enumerate(range(-70, 71, 10)):
        low_specs.append((x, 39.5, 5.2, 3.4, 3.4 + (index % 4) * 0.6, ("front",)))
        low_specs.append((x, -38.5, 5.2, 3.4, 3.2 + ((index + 1) % 4) * 0.6, ("back",)))

    for index, (x, y, width, depth, height, faces) in enumerate(low_specs):
        add_building(f"City_Background_LowBlock_{index:02d}", x, y, width, depth, height, building_mats[(index + 2) % len(building_mats)], window, faces)


def add_street_lights(metal: bpy.types.Material, glow: bpy.types.Material) -> None:
    for index, x in enumerate(range(-70, 71, 14)):
        for y in (-41.2, 43.2):
            pole = cylinder(f"City_StreetLight_Pole_{index}_{int(y)}", (x, y, 1.45), 0.055, 2.9, metal, vertices=8)
            cube(f"City_StreetLight_Head_{index}_{int(y)}", (x, y - 0.18, 2.94), (0.55, 0.18, 0.12), glow)
    for index, y in enumerate(range(-30, 31, 15)):
        for x in (-55.2, 55.2):
            cylinder(f"City_StreetLight_SidePole_{index}_{int(x)}", (x, y, 1.45), 0.055, 2.9, metal, vertices=8)
            cube(f"City_StreetLight_SideHead_{index}_{int(x)}", (x + 0.18, y, 2.94), (0.18, 0.55, 0.12), glow)


def add_composition_guides(accent: bpy.types.Material) -> None:
    # Small ground details only: no in-scene text annotations.
    for index, x in enumerate(range(-66, 67, 12)):
        cube(f"City_Background_Curb_Marker_Front_{index}", (x, -41.9, 0.075), (1.7, 0.12, 0.045), accent)
        cube(f"City_Background_Curb_Marker_Back_{index}", (x, 43.9, 0.075), (1.7, 0.12, 0.045), accent)


def setup_camera_and_lights() -> None:
    bpy.ops.object.light_add(type="SUN", location=(0, -10, 30))
    sun = bpy.context.object
    sun.name = "City_Background_Sun"
    sun.rotation_euler = (math.radians(50), 0, math.radians(35))
    sun.data.energy = 3.0

    bpy.ops.object.camera_add(location=(0, -105, 48), rotation=(math.radians(62), 0, 0))
    cam = bpy.context.object
    bpy.context.scene.camera = cam
    cam.name = "City_Background_Preview_Camera"
    cam.data.lens = 26


def export_scene() -> None:
    BLENDER_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    FBX_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_OUTPUT))

    for obj in bpy.data.objects:
        obj.select_set(not obj.hide_render and obj.type in {"MESH", "EMPTY"})
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_OUTPUT),
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        apply_unit_scale=True,
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
    )

    bpy.context.scene.render.resolution_x = 1000
    bpy.context.scene.render.resolution_y = 620
    bpy.context.scene.eevee.taa_render_samples = 8
    bpy.context.scene.render.filepath = str(PREVIEW_OUTPUT)
    bpy.ops.render.render(write_still=True)


def main() -> None:
    reset_scene()

    road = material("City_Mat_Asphalt", (0.19, 0.2, 0.2, 1))
    sidewalk = material("City_Mat_Concrete", (0.54, 0.54, 0.49, 1))
    grass = material("City_Mat_Grass", (0.20, 0.44, 0.22, 1))
    metal = material("City_Mat_DarkMetal", (0.11, 0.12, 0.13, 1))
    glow = material("City_Mat_LampWarm", (1.0, 0.77, 0.34, 1))
    window = material("City_Mat_SoftWindow", (0.10, 0.18, 0.24, 1), roughness=0.34)
    accent = material("City_Mat_CurbYellow", (0.95, 0.66, 0.08, 1))
    tree_trunk = material("City_Mat_TreeTrunk", (0.23, 0.14, 0.08, 1))
    tree_foliage = material("City_Mat_TreeFoliage", (0.12, 0.34, 0.16, 1))
    building_mats = [
        material("City_Mat_Building_Stone", (0.50, 0.49, 0.44, 1)),
        material("City_Mat_Building_GlassGrey", (0.34, 0.42, 0.48, 1), roughness=0.45),
        material("City_Mat_Building_WarmGrey", (0.58, 0.55, 0.50, 1)),
        material("City_Mat_Building_CoolGrey", (0.40, 0.44, 0.47, 1)),
        material("City_Mat_Building_SageGrey", (0.42, 0.48, 0.42, 1)),
        material("City_Mat_Building_Charcoal", (0.30, 0.33, 0.35, 1)),
    ]

    add_roads(road, sidewalk, grass)
    add_city_blocks(building_mats, window)
    add_street_lights(metal, glow)
    add_composition_guides(accent)
    add_tree_rows(import_tree_sources(tree_trunk, tree_foliage))
    setup_camera_and_lights()
    export_scene()


if __name__ == "__main__":
    main()
