import bpy
import importlib.util
import math
import os
from mathutils import Vector


BASE_DIR = r"K:\FileK\unityprojects\DroneMicroClassSim\BlenderScenes"
BASE_SCRIPT = os.path.join(BASE_DIR, "create_urban_building_massing.py")
BLEND_PATH = os.path.join(BASE_DIR, "UrbanBuildingMassing_Realistic.blend")
PREVIEW_PATH = os.path.join(BASE_DIR, "UrbanBuildingMassing_Realistic_Preview.png")


spec = importlib.util.spec_from_file_location("urban_massing_base", BASE_SCRIPT)
base = importlib.util.module_from_spec(spec)
spec.loader.exec_module(base)


# Normalized, counter-clockwise polyline footprints. Each template spans -0.5..0.5
# on both axes, so the approved outer dimensions remain exact after scaling.
FOOTPRINTS = {
    "school_step_right": [
        (-0.50, -0.50), (0.50, -0.50), (0.50, 0.25),
        (0.38, 0.25), (0.38, 0.50), (-0.50, 0.50),
    ],
    "school_step_left": [
        (-0.50, -0.50), (0.50, -0.50), (0.50, 0.50),
        (-0.38, 0.50), (-0.38, 0.28), (-0.50, 0.28),
    ],
    "school_u": [
        (-0.50, -0.50), (0.50, -0.50), (0.50, 0.50),
        (0.18, 0.50), (0.18, 0.22), (-0.18, 0.22),
        (-0.18, 0.50), (-0.50, 0.50),
    ],
    "school_vertical_step": [
        (-0.50, -0.50), (0.30, -0.50), (0.30, -0.35),
        (0.50, -0.35), (0.50, 0.50), (-0.50, 0.50),
        (-0.50, 0.18), (-0.35, 0.18), (-0.35, -0.18),
        (-0.50, -0.18),
    ],
    "school_offset": [
        (-0.50, -0.50), (0.42, -0.50), (0.42, -0.30),
        (0.50, -0.30), (0.50, 0.50), (-0.32, 0.50),
        (-0.32, 0.35), (-0.50, 0.35),
    ],
    "residential_wings": [
        (-0.35, -0.50), (0.35, -0.50), (0.35, -0.38),
        (0.50, -0.38), (0.50, 0.38), (0.35, 0.38),
        (0.35, 0.50), (-0.35, 0.50), (-0.35, 0.38),
        (-0.50, 0.38), (-0.50, -0.38), (-0.35, -0.38),
    ],
    "residential_asym": [
        (-0.50, -0.50), (0.22, -0.50), (0.22, -0.40),
        (0.50, -0.40), (0.50, 0.35), (0.38, 0.35),
        (0.38, 0.50), (-0.42, 0.50), (-0.42, 0.28),
        (-0.50, 0.28),
    ],
    "residential_core": [
        (-0.40, -0.50), (0.40, -0.50), (0.40, -0.30),
        (0.50, -0.30), (0.50, 0.30), (0.40, 0.30),
        (0.40, 0.50), (-0.40, 0.50), (-0.40, 0.30),
        (-0.50, 0.30), (-0.50, -0.30), (-0.40, -0.30),
    ],
    "residential_notched": [
        (-0.50, -0.50), (0.50, -0.50), (0.50, 0.28),
        (0.36, 0.28), (0.36, 0.50), (0.06, 0.50),
        (0.06, 0.38), (-0.34, 0.38), (-0.34, 0.50),
        (-0.50, 0.50),
    ],
}


BUILDING_TEMPLATE = {
    "T01_Main_West": "school_step_right",
    "T02_Main_East": "school_step_left",
    "T03_Lab_Comprehensive": "school_u",
    "T04_East_Teaching": "school_vertical_step",
    "T05_Northwest_Auxiliary": "school_offset",
    "R01_West_South": "residential_wings",
    "R02_West_Central": "residential_asym",
    "R03_West_North": "residential_core",
    "R04_East_South": "residential_notched",
    "R05_East_Central": "residential_wings",
    "R06_East_North": "residential_asym",
    "R07_Southwest": "residential_core",
    "R08_South_WestCentral": "residential_notched",
    "R09_South_Central": "residential_wings",
    "R10_South_EastCentral": "residential_asym",
    "R11_Southeast": "residential_core",
}


def polygon_area(points):
    return 0.5 * sum(
        points[i][0] * points[(i + 1) % len(points)][1]
        - points[(i + 1) % len(points)][0] * points[i][1]
        for i in range(len(points))
    )


def scaled_footprint(template_name, sx, sy):
    points = [(x * sx, y * sy) for x, y in FOOTPRINTS[template_name]]
    if polygon_area(points) < 0.0:
        points.reverse()
    return points


def make_glass_material():
    material = bpy.data.materials.new("MAT_Window_Glass_BlueGrey")
    material.use_nodes = True
    material.diffuse_color = (0.025, 0.075, 0.095, 1.0)
    nodes = material.node_tree.nodes
    shader = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
    base.set_principled_input(shader, "Base Color", (0.018, 0.055, 0.075, 1.0))
    base.set_principled_input(shader, "Metallic", 0.22)
    base.set_principled_input(shader, "Roughness", 0.16)
    base.set_principled_input(shader, "IOR", 1.45)
    base.set_principled_input(shader, "Transmission Weight", 0.12)
    material["zone"] = "Window glass"
    material["reference"] = "BlenderScenes/材质合集.png - 玻璃（窗户/阳台）"
    return material


def make_metal_material():
    material = bpy.data.materials.new("MAT_Window_Frame_DarkAluminum")
    material.use_nodes = True
    material.diffuse_color = (0.035, 0.042, 0.045, 1.0)
    nodes = material.node_tree.nodes
    shader = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
    base.set_principled_input(shader, "Base Color", (0.025, 0.030, 0.032, 1.0))
    base.set_principled_input(shader, "Metallic", 0.78)
    base.set_principled_input(shader, "Roughness", 0.27)
    material["zone"] = "Window frame"
    material["reference"] = "BlenderScenes/材质合集.png - 窗框（铝合金）"
    return material


def make_ground_material():
    material = bpy.data.materials.new("MAT_Preview_Ground")
    material.use_nodes = True
    material.diffuse_color = (0.055, 0.065, 0.078, 1.0)
    shader = next(node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED")
    base.set_principled_input(shader, "Base Color", (0.045, 0.055, 0.068, 1.0))
    base.set_principled_input(shader, "Roughness", 0.92)
    material["preview_only"] = True
    return material


def create_collections():
    scene_root = bpy.context.scene.collection
    master = bpy.data.collections.new("Urban_Building_Realistic")
    schools = bpy.data.collections.new("01_School_Buildings")
    residential = bpy.data.collections.new("02_Residential_Buildings")
    preview = bpy.data.collections.new("99_Preview_Setup")
    scene_root.children.link(master)
    master.children.link(schools)
    master.children.link(residential)
    scene_root.children.link(preview)

    master["coordinate_system"] = "Blender: X east-west, Y north-south, Z up"
    master["unity_mapping"] = "Blender (X,Y,Z) -> Unity (X,Z,Y)"
    master["stadium_reference_center"] = (0.0, 0.0, 0.0)
    master["stadium_outer_bounds_m"] = (176.91, 92.52)
    master["content_scope"] = "Polyline buildings with separate wall, window glass and frame geometry"
    master["source_layout"] = "Latest approved 16-building dimension plan"
    preview["export_exclude"] = True
    return master, schools, residential, preview


def create_body_mesh(name, footprint, height, facade, end_wall, roof, foundation, long_axis):
    n = len(footprint)
    vertices = [(x, y, 0.0) for x, y in footprint] + [(x, y, height) for x, y in footprint]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    material_indices = [3, 2]

    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
        dx = footprint[j][0] - footprint[i][0]
        dy = footprint[j][1] - footprint[i][1]
        if long_axis == "X":
            material_indices.append(0 if abs(dx) >= abs(dy) else 1)
        else:
            material_indices.append(0 if abs(dy) >= abs(dx) else 1)

    mesh = bpy.data.meshes.new(f"{name}_Walls_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    for material in (facade, end_wall, roof, foundation):
        mesh.materials.append(material)
    for polygon, material_index in zip(mesh.polygons, material_indices):
        polygon.material_index = material_index
        polygon.use_smooth = False
    return mesh


def append_oriented_box(vertices, faces, center_xy, tangent, outward, width, depth, z0, z1):
    tx, ty = tangent
    nx, ny = outward
    cx, cy = center_xy
    corners = [
        (-width * 0.5, -depth * 0.5, z0),
        (width * 0.5, -depth * 0.5, z0),
        (width * 0.5, depth * 0.5, z0),
        (-width * 0.5, depth * 0.5, z0),
        (-width * 0.5, -depth * 0.5, z1),
        (width * 0.5, -depth * 0.5, z1),
        (width * 0.5, depth * 0.5, z1),
        (-width * 0.5, depth * 0.5, z1),
    ]
    start = len(vertices)
    for u, v, z in corners:
        vertices.append((cx + tx * u + nx * v, cy + ty * u + ny * v, z))
    faces.extend([
        (start + 0, start + 3, start + 2, start + 1),
        (start + 4, start + 5, start + 6, start + 7),
        (start + 0, start + 1, start + 5, start + 4),
        (start + 1, start + 2, start + 6, start + 5),
        (start + 2, start + 3, start + 7, start + 6),
        (start + 3, start + 0, start + 4, start + 7),
    ])


def create_window_geometry(name, footprint, height, floors, category):
    glass_vertices = []
    glass_faces = []
    frame_vertices = []
    frame_faces = []
    window_count = 0

    floor_height = height / floors
    sill = 0.82 if category == "MiddleSchool" else 0.92
    window_height = min(1.72 if category == "MiddleSchool" else 1.48, floor_height - 1.18)
    target_module = 3.05 if category == "MiddleSchool" else 3.20
    target_width = 1.78 if category == "MiddleSchool" else 1.58
    frame_width = 0.075
    frame_depth = 0.105
    glass_depth = 0.055

    n = len(footprint)
    for edge_index in range(n):
        p0 = Vector(footprint[edge_index])
        p1 = Vector(footprint[(edge_index + 1) % n])
        edge = p1 - p0
        length = edge.length
        if length < 2.6:
            continue
        tangent_vec = edge.normalized()
        tangent = (tangent_vec.x, tangent_vec.y)
        outward = (tangent_vec.y, -tangent_vec.x)

        margin = min(1.05, max(0.40, length * 0.16))
        usable = length - 2.0 * margin
        if usable < 1.0:
            continue
        modules = max(1, int(math.floor(usable / target_module)) + 1)
        spacing = usable / modules
        window_width = min(target_width, spacing * 0.72)

        for floor_index in range(floors):
            z0 = floor_index * floor_height + sill
            z1 = z0 + window_height
            if z1 > height - 0.42:
                z1 = height - 0.42
            if z1 <= z0 + 0.35:
                continue

            for module_index in range(modules):
                along = margin + spacing * (module_index + 0.5)
                center = p0 + tangent_vec * along
                center_xy = (
                    center.x + outward[0] * 0.055,
                    center.y + outward[1] * 0.055,
                )
                append_oriented_box(
                    glass_vertices, glass_faces, center_xy, tangent, outward,
                    window_width, glass_depth, z0, z1,
                )

                # Four-sided aluminum perimeter plus a central mullion.
                full_height = z1 - z0
                frame_z0 = z0 - frame_width
                frame_z1 = z1 + frame_width
                append_oriented_box(
                    frame_vertices, frame_faces,
                    (
                        center_xy[0] - tangent[0] * (window_width * 0.5 + frame_width * 0.5),
                        center_xy[1] - tangent[1] * (window_width * 0.5 + frame_width * 0.5),
                    ),
                    tangent, outward, frame_width, frame_depth, frame_z0, frame_z1,
                )
                append_oriented_box(
                    frame_vertices, frame_faces,
                    (
                        center_xy[0] + tangent[0] * (window_width * 0.5 + frame_width * 0.5),
                        center_xy[1] + tangent[1] * (window_width * 0.5 + frame_width * 0.5),
                    ),
                    tangent, outward, frame_width, frame_depth, frame_z0, frame_z1,
                )
                append_oriented_box(
                    frame_vertices, frame_faces,
                    center_xy, tangent, outward,
                    window_width + frame_width * 2.0, frame_depth, z0 - frame_width, z0,
                )
                append_oriented_box(
                    frame_vertices, frame_faces,
                    center_xy, tangent, outward,
                    window_width + frame_width * 2.0, frame_depth, z1, z1 + frame_width,
                )
                append_oriented_box(
                    frame_vertices, frame_faces,
                    center_xy, tangent, outward,
                    frame_width * 0.78, frame_depth * 1.02,
                    z0, z1,
                )
                window_count += 1

    glass_mesh = bpy.data.meshes.new(f"{name}_Windows_Glass_Mesh")
    glass_mesh.from_pydata(glass_vertices, [], glass_faces)
    glass_mesh.update()
    frame_mesh = bpy.data.meshes.new(f"{name}_Window_Frames_Mesh")
    frame_mesh.from_pydata(frame_vertices, [], frame_faces)
    frame_mesh.update()
    return glass_mesh, frame_mesh, window_count


def create_realistic_building(collection, entry, category, materials, glass_material, frame_material):
    name, cx, cy, sx, sy, height, floors, facade_name, end_name = entry
    template_name = BUILDING_TEMPLATE[name]
    footprint = scaled_footprint(template_name, sx, sy)
    long_axis = "X" if sx >= sy else "Y"

    root = bpy.data.objects.new(name, None)
    collection.objects.link(root)
    root.location = (cx, cy, 0.0)
    root.empty_display_type = "CUBE"
    root.empty_display_size = 1.0

    body_mesh = create_body_mesh(
        name,
        footprint,
        height,
        materials[facade_name],
        materials[end_name],
        materials["MAT_Roof_Waterproof_Dark"],
        materials["MAT_Foundation_Concrete"],
        long_axis,
    )
    body = bpy.data.objects.new(f"{name}_Walls", body_mesh)
    collection.objects.link(body)
    body.parent = root
    body["role"] = "WallsAndRoof"

    glass_mesh, frame_mesh, window_count = create_window_geometry(
        name, footprint, height, floors, category
    )
    glass = bpy.data.objects.new(f"{name}_Windows_Glass", glass_mesh)
    collection.objects.link(glass)
    glass.parent = root
    glass.data.materials.append(glass_material)
    glass["role"] = "WindowGlass"

    frames = bpy.data.objects.new(f"{name}_Window_Frames", frame_mesh)
    collection.objects.link(frames)
    frames.parent = root
    frames.data.materials.append(frame_material)
    frames["role"] = "WindowFrames"

    root["category"] = category
    root["footprint_type"] = template_name
    root["footprint_vertex_count"] = len(footprint)
    root["outer_dimensions_m"] = (sx, sy, height)
    root["planned_floors"] = floors
    root["floor_height_m"] = round(height / floors, 3)
    root["window_count"] = window_count
    root["unity_ground_center_xz"] = (cx, cy)
    root["unity_center_xyz"] = (cx, height * 0.5, cy)
    root["wall_material"] = facade_name
    root["end_wall_material"] = end_name
    root["window_material"] = glass_material.name
    root["window_frame_material"] = frame_material.name
    return root


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_preview(preview_collection, ground_material):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1200
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.filepath = PREVIEW_PATH
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = 0.45

    world = bpy.data.worlds.new("Urban_Realistic_World")
    world.use_nodes = True
    nodes = world.node_tree.nodes
    links = world.node_tree.links
    nodes.clear()
    background = nodes.new("ShaderNodeBackground")
    output = nodes.new("ShaderNodeOutputWorld")
    background.inputs["Color"].default_value = (0.065, 0.085, 0.115, 1.0)
    background.inputs["Strength"].default_value = 0.48
    links.new(background.outputs["Background"], output.inputs["Surface"])
    scene.world = world

    bpy.ops.mesh.primitive_plane_add(size=650.0, location=(0.0, 0.0, -0.035))
    ground = bpy.context.object
    for parent_collection in list(ground.users_collection):
        parent_collection.objects.unlink(ground)
    preview_collection.objects.link(ground)
    ground.name = "Preview_Ground_ExportExcluded"
    ground.data.materials.append(ground_material)
    ground["preview_only"] = True

    camera_data = bpy.data.cameras.new("Preview_Camera")
    camera = bpy.data.objects.new("Preview_Camera", camera_data)
    preview_collection.objects.link(camera)
    camera.location = (300.0, -350.0, 325.0)
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = 330.0
    look_at(camera, (0.0, -4.0, 12.0))
    scene.camera = camera

    sun_data = bpy.data.lights.new("Preview_Key_Sun", type="SUN")
    sun_data.energy = 2.7
    sun_data.angle = math.radians(9.0)
    sun = bpy.data.objects.new("Preview_Key_Sun", sun_data)
    preview_collection.objects.link(sun)
    sun.rotation_euler = (math.radians(30.0), math.radians(-22.0), math.radians(-38.0))

    fill_data = bpy.data.lights.new("Preview_Fill_Area", type="AREA")
    fill_data.energy = 1700.0
    fill_data.shape = "DISK"
    fill_data.size = 170.0
    fill = bpy.data.objects.new("Preview_Fill_Area", fill_data)
    preview_collection.objects.link(fill)
    fill.location = (-100.0, -70.0, 190.0)
    look_at(fill, (0.0, 0.0, 18.0))


def validate_scene():
    roots = [
        obj for obj in bpy.data.objects
        if obj.type == "EMPTY" and obj.get("category") in {"MiddleSchool", "Residential"}
    ]
    walls = [obj for obj in bpy.data.objects if obj.get("role") == "WallsAndRoof"]
    windows = [obj for obj in bpy.data.objects if obj.get("role") == "WindowGlass"]
    frames = [obj for obj in bpy.data.objects if obj.get("role") == "WindowFrames"]
    if (len(roots), len(walls), len(windows), len(frames)) != (16, 16, 16, 16):
        raise RuntimeError(
            f"Object validation failed: roots={len(roots)}, walls={len(walls)}, "
            f"windows={len(windows)}, frames={len(frames)}"
        )
    for root in roots:
        if root["footprint_vertex_count"] < 6:
            raise RuntimeError(f"{root.name} footprint is not a multi-segment polyline")
        if root["window_count"] <= 0:
            raise RuntimeError(f"{root.name} has no generated windows")
    for wall in walls:
        if len(wall.data.materials) != 4:
            raise RuntimeError(f"{wall.name} does not have four wall/roof material zones")
    return roots, walls, windows, frames


def main():
    base.clear_scene()
    scene = bpy.context.scene
    scene.name = "UrbanBuildingMassing_Realistic"
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.length_unit = "METERS"
    scene.unit_settings.scale_length = 1.0

    _, schools, residential, preview = create_collections()
    materials = base.create_materials()
    glass_material = make_glass_material()
    frame_material = make_metal_material()
    ground_material = make_ground_material()

    for entry in base.SCHOOL_BUILDINGS:
        create_realistic_building(
            schools, entry, "MiddleSchool", materials, glass_material, frame_material
        )
    for entry in base.RESIDENTIAL_BUILDINGS:
        create_realistic_building(
            residential, entry, "Residential", materials, glass_material, frame_material
        )

    setup_preview(preview, ground_material)
    roots, walls, windows, frames = validate_scene()

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    total_windows = sum(root["window_count"] for root in roots)
    print("URBAN_REALISTIC_BUILD_COMPLETE")
    print(f"BLEND={BLEND_PATH}")
    print(f"PREVIEW={PREVIEW_PATH}")
    print(f"BUILDINGS={len(roots)}")
    print(f"WALL_OBJECTS={len(walls)}")
    print(f"WINDOW_OBJECTS={len(windows)}")
    print(f"FRAME_OBJECTS={len(frames)}")
    print(f"WINDOW_UNITS={total_windows}")
    print(f"MATERIALS={len(bpy.data.materials)}")


if __name__ == "__main__":
    main()
