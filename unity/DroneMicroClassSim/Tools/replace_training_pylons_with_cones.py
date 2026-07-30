import math
import os
import re

import bpy
from mathutils import Vector


PROJECT_ROOT = r"K:\FileK\unityprojects\DroneMicroClassSim"
BLENDER_SCENE_PATH = os.path.join(PROJECT_ROOT, "BlenderScenes", "DroneFigureEightTraining_UnityClean.blend")
SOURCE_SCENE_PATH = os.path.join(PROJECT_ROOT, "BlenderScenes", "DroneFigureEightTraining.blend")
EXPORT_FBX_PATH = os.path.join(PROJECT_ROOT, "Assets", "DroneMicroClass", "Models", "Training", "DroneFigureEightTraining.fbx")
SURFACE_CLEARANCE = 0.006
LOW_POLY_CONE_SIDES = 16


def set_bsdf_material(material, color, roughness=0.62):
    material.diffuse_color = color
    material.use_nodes = True
    nodes = material.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return

    if "Base Color" in bsdf.inputs:
        bsdf.inputs["Base Color"].default_value = color
    if "Roughness" in bsdf.inputs:
        bsdf.inputs["Roughness"].default_value = roughness


def get_or_create_material(name, color, roughness=0.62):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    set_bsdf_material(material, color, roughness)
    return material


def load_latest_training_scene():
    source_mtime = os.path.getmtime(SOURCE_SCENE_PATH) if os.path.exists(SOURCE_SCENE_PATH) else 0
    clean_mtime = os.path.getmtime(BLENDER_SCENE_PATH) if os.path.exists(BLENDER_SCENE_PATH) else 0
    scene_path = SOURCE_SCENE_PATH if source_mtime > clean_mtime else BLENDER_SCENE_PATH
    bpy.ops.wm.open_mainfile(filepath=scene_path)
    return scene_path


def make_cone_materials():
    orange = get_or_create_material("Traffic_Cone_Orange", (0.95, 0.47, 0.06, 1.0), 0.55)
    white = get_or_create_material("Traffic_Cone_White_Band", (0.92, 0.90, 0.84, 1.0), 0.5)
    dark = get_or_create_material("Traffic_Cone_Dark_Base", (0.06, 0.065, 0.06, 1.0), 0.72)
    return orange, white, dark


def circle_vertices(z, radius, sides):
    vertices = []
    for index in range(sides):
        angle = (math.tau * index) / sides
        vertices.append((math.cos(angle) * radius, math.sin(angle) * radius, z))
    return vertices


def create_low_poly_cone_source():
    orange, white, dark = make_cone_materials()
    sides = LOW_POLY_CONE_SIDES
    vertices = []
    faces = []
    material_indices = []

    base_half = 0.65
    base_top_z = 0.08
    base_bottom = [
        (-base_half, -base_half, 0.0),
        (base_half, -base_half, 0.0),
        (base_half, base_half, 0.0),
        (-base_half, base_half, 0.0),
    ]
    base_top = [(x, y, base_top_z) for x, y, _ in base_bottom]
    vertices.extend(base_bottom + base_top)
    faces.extend(
        [
            (0, 3, 2, 1),
            (4, 5, 6, 7),
            (0, 1, 5, 4),
            (1, 2, 6, 5),
            (2, 3, 7, 6),
            (3, 0, 4, 7),
        ]
    )
    material_indices.extend([2, 2, 2, 2, 2, 2])

    rings = [
        (base_top_z, 0.38),
        (0.22, 0.335),
        (0.32, 0.305),
        (0.52, 0.242),
        (0.62, 0.210),
        (1.0, 0.085),
    ]
    ring_indices = []
    for z, radius in rings:
        start = len(vertices)
        vertices.extend(circle_vertices(z, radius, sides))
        ring_indices.append(list(range(start, start + sides)))

    for ring_index in range(len(ring_indices) - 1):
        lower = ring_indices[ring_index]
        upper = ring_indices[ring_index + 1]
        z_mid = (rings[ring_index][0] + rings[ring_index + 1][0]) * 0.5
        material_index = 1 if 0.22 <= z_mid <= 0.32 or 0.52 <= z_mid <= 0.62 else 0

        for side_index in range(sides):
            next_index = (side_index + 1) % sides
            faces.append((lower[side_index], lower[next_index], upper[next_index], upper[side_index]))
            material_indices.append(material_index)

    top_center_index = len(vertices)
    vertices.append((0.0, 0.0, rings[-1][0]))
    top_ring = ring_indices[-1]
    for side_index in range(sides):
        next_index = (side_index + 1) % sides
        faces.append((top_ring[side_index], top_ring[next_index], top_center_index))
        material_indices.append(0)

    mesh = bpy.data.meshes.new("_Traffic_Cone_LowPoly_Source_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    mesh.materials.append(orange)
    mesh.materials.append(white)
    mesh.materials.append(dark)

    for polygon, material_index in zip(mesh.polygons, material_indices):
        polygon.material_index = material_index

    cone = bpy.data.objects.new("_Traffic_Cone_LowPoly_Source", mesh)
    bpy.context.collection.objects.link(cone)
    cone.hide_viewport = True
    cone.hide_render = True
    return cone


def apply_cone_materials(cone):
    orange, white, dark = make_cone_materials()
    cone.data.materials.clear()
    cone.data.materials.append(orange)
    cone.data.materials.append(white)
    cone.data.materials.append(dark)

    if not cone.data.vertices:
        return

    min_z = min(vertex.co.z for vertex in cone.data.vertices)
    max_z = max(vertex.co.z for vertex in cone.data.vertices)
    height = max(0.001, max_z - min_z)

    for polygon in cone.data.polygons:
        center_z = sum(cone.data.vertices[index].co.z for index in polygon.vertices) / len(polygon.vertices)
        normalized_z = (center_z - min_z) / height

        if normalized_z < 0.08:
            polygon.material_index = 2
        elif 0.22 <= normalized_z <= 0.34 or 0.53 <= normalized_z <= 0.65:
            polygon.material_index = 1
        else:
            polygon.material_index = 0


def world_bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    min_corner = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    max_corner = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return min_corner, max_corner


def target_pylons():
    pylons = []
    cap_names = []
    for obj in list(bpy.context.scene.objects):
        name = obj.name
        if "_White_Cap" in name:
            cap_names.append(name)
            continue

        if obj.type == "MESH" and (
            name.startswith("Large_Pylon_")
            or name.startswith("Small_Pylon_")
            or name.startswith("Center_Pylon_")
        ) and not name.startswith("_Traffic_Cone_Source"):
            pylons.append(obj)

    return pylons, cap_names


def is_support_surface(obj):
    if obj.type != "MESH" or obj.hide_render:
        return False

    name = obj.name
    if any(token in name for token in ("Pylon", "Cone", "Route", "Dash", "Border", "Beacon", "Marker")):
        return False

    return any(token in name for token in ("Runway", "Flight_Area", "Flying_Safety_Area", "Takeoff", "Landing", "Pad"))


def surface_height_at_xy(x, y):
    origin = Vector((x, y, 20.0))
    direction = Vector((0.0, 0.0, -1.0))
    best_z = None

    for obj in bpy.context.scene.objects:
        if not is_support_surface(obj):
            continue

        inverse = obj.matrix_world.inverted()
        local_origin = inverse @ origin
        local_direction = (inverse.to_3x3() @ direction).normalized()
        hit, location, _, _ = obj.ray_cast(local_origin, local_direction, distance=60.0)
        if not hit:
            continue

        world_location = obj.matrix_world @ location
        if best_z is None or world_location.z > best_z:
            best_z = world_location.z

    return best_z


def cone_name_for(old_name):
    old_name = re.sub(r"\.\d{3}$", "", old_name)
    if "_Cone_" in old_name:
        return old_name
    return old_name.replace("_H0p9m", "_Cone_H0p9m").replace("_H0p6m", "_Cone_H0p6m")


def make_cone_instance(source_cone, old_obj, root):
    old_min, old_max = world_bounds(old_obj)
    old_size = old_max - old_min
    target_height = max(0.1, old_size.z)
    target_radius = max(old_size.x, old_size.y) * 0.5

    source_min, source_max = world_bounds(source_cone)
    source_size = source_max - source_min
    source_height = max(0.001, source_size.z)
    source_radius = max(source_size.x, source_size.y) * 0.5

    scale_z = target_height / source_height
    scale_xy = target_radius / max(0.001, source_radius)

    cone = source_cone.copy()
    cone.data = source_cone.data.copy()
    bpy.context.collection.objects.link(cone)
    cone.name = cone_name_for(old_obj.name)
    cone.hide_viewport = False
    cone.hide_render = False
    center_x = (old_min.x + old_max.x) * 0.5
    center_y = (old_min.y + old_max.y) * 0.5
    cone.location = (center_x, center_y, 0.0)
    cone.rotation_euler = old_obj.rotation_euler
    cone.scale = (scale_xy, scale_xy, scale_z)
    cone.parent = root

    bpy.context.view_layer.update()
    cone_min, _ = world_bounds(cone)
    support_z = surface_height_at_xy(center_x, center_y)
    if support_z is None:
        support_z = old_min.z
    cone.location.z += support_z + SURFACE_CLEARANCE - cone_min.z
    return cone


def replace_pylons():
    root = bpy.data.objects.get("Drone_Training_Figure_Eight_Root")
    source_cone = create_low_poly_cone_source()
    pylons, cap_names = target_pylons()

    made = []
    for old_obj in pylons:
        desired_name = cone_name_for(old_obj.name)
        cone = make_cone_instance(source_cone, old_obj, root)
        bpy.data.objects.remove(old_obj, do_unlink=True)
        cone.name = desired_name
        cone.data.name = desired_name + "_Mesh"
        made.append(cone)

    for cap_name in cap_names:
        cap = bpy.data.objects.get(cap_name)
        if cap is not None:
            bpy.data.objects.remove(cap, do_unlink=True)

    bpy.data.objects.remove(source_cone, do_unlink=True)
    return made


def export_unity_fbx():
    os.makedirs(os.path.dirname(EXPORT_FBX_PATH), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.scene.objects:
        if obj.type in {"MESH", "EMPTY"} and not obj.hide_render:
            obj.select_set(True)

    bpy.ops.export_scene.fbx(
        filepath=EXPORT_FBX_PATH,
        use_selection=True,
        apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=False,
        object_types={"EMPTY", "MESH"},
        path_mode="COPY",
        embed_textures=True,
        add_leaf_bones=False,
    )


loaded_scene = load_latest_training_scene()
created = replace_pylons()
bpy.ops.wm.save_as_mainfile(filepath=BLENDER_SCENE_PATH)
export_unity_fbx()
print(f"Loaded training scene: {loaded_scene}")
print(f"Replaced pylons with traffic cones: {len(created)}")
print(f"Saved clean blend: {BLENDER_SCENE_PATH}")
print(f"Exported FBX: {EXPORT_FBX_PATH}")
