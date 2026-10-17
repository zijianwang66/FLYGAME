import bpy
import math
import os
from mathutils import Vector


BASE_DIR = r"K:\FileK\unityprojects\DroneMicroClassSim\BlenderScenes"
BLEND_PATH = os.path.join(BASE_DIR, "UrbanBuildingMassing_Whitebox.blend")
PREVIEW_PATH = os.path.join(BASE_DIR, "UrbanBuildingMassing_Whitebox_Preview.png")


SCHOOL_BUILDINGS = [
    # name, center_x, center_y, size_x, size_y, height, floors, facade, end_wall
    ("T01_Main_West", -35.0, 70.0, 54.0, 16.0, 21.6, 6, "MAT_School_Plaster_Light", "MAT_School_Tile_Beige"),
    ("T02_Main_East", 25.0, 70.0, 52.0, 16.0, 21.6, 6, "MAT_School_Plaster_Yellow", "MAT_School_Plaster_Light"),
    ("T03_Lab_Comprehensive", 73.0, 69.0, 30.0, 16.0, 18.0, 5, "MAT_School_Tile_Beige", "MAT_School_Plaster_Light"),
    ("T04_East_Teaching", 108.0, 18.0, 18.0, 48.0, 25.2, 7, "MAT_School_Plaster_Light", "MAT_School_Plaster_Yellow"),
    ("T05_Northwest_Auxiliary", -78.0, 69.0, 28.0, 16.0, 18.0, 5, "MAT_School_Plaster_Yellow", "MAT_School_Tile_Beige"),
]


RESIDENTIAL_BUILDINGS = [
    # name, center_x, center_y, size_x, size_y, height, floors, facade, end_wall
    ("R01_West_South", -116.0, -43.0, 22.0, 34.0, 39.0, 13, "MAT_Residential_Tile_Beige", "MAT_Residential_Endwall_Concrete"),
    ("R02_West_Central", -122.0, 4.0, 24.0, 40.0, 45.0, 15, "MAT_Residential_Mosaic_Light", "MAT_Residential_Endwall_Concrete"),
    ("R03_West_North", -115.0, 50.0, 22.0, 32.0, 36.0, 12, "MAT_Residential_Paint_New", "MAT_Residential_Endwall_Concrete"),
    ("R04_East_South", 119.0, -43.0, 22.0, 34.0, 42.0, 14, "MAT_Residential_Tile_Beige", "MAT_Residential_Endwall_Concrete"),
    ("R05_East_Central", 128.0, 3.0, 24.0, 42.0, 48.0, 16, "MAT_Residential_Mosaic_Light", "MAT_Residential_Endwall_Concrete"),
    ("R06_East_North", 128.0, 57.0, 22.0, 28.0, 39.0, 13, "MAT_Residential_Paint_New", "MAT_Residential_Endwall_Concrete"),
    ("R07_Southwest", -72.0, -82.0, 34.0, 20.0, 36.0, 12, "MAT_Residential_Tile_Beige", "MAT_Residential_Endwall_Concrete"),
    ("R08_South_WestCentral", -30.0, -86.0, 28.0, 22.0, 30.0, 10, "MAT_Residential_Mosaic_Light", "MAT_Residential_Endwall_Concrete"),
    ("R09_South_Central", 10.0, -81.0, 30.0, 20.0, 42.0, 14, "MAT_Residential_Paint_New", "MAT_Residential_Endwall_Concrete"),
    ("R10_South_EastCentral", 50.0, -87.0, 28.0, 22.0, 33.0, 11, "MAT_Residential_Tile_Beige", "MAT_Residential_Endwall_Concrete"),
    ("R11_Southeast", 82.0, -79.0, 24.0, 20.0, 39.0, 13, "MAT_Residential_Mosaic_Light", "MAT_Residential_Endwall_Concrete"),
]


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights, bpy.data.materials):
        for datablock in list(datablocks):
            datablocks.remove(datablock)
    for collection in list(bpy.data.collections):
        bpy.data.collections.remove(collection)


def set_principled_input(shader, name, value):
    socket = shader.inputs.get(name)
    if socket is not None:
        socket.default_value = value


def make_plaster_material(name, base_color, roughness=0.82):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = (*base_color, 1.0)

    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    noise = nodes.new("ShaderNodeTexNoise")
    ramp = nodes.new("ShaderNodeValToRGB")
    bump = nodes.new("ShaderNodeBump")

    output.location = (620, 0)
    shader.location = (350, 0)
    ramp.location = (-40, 80)
    noise.location = (-310, 80)
    bump.location = (100, -160)

    noise.inputs["Scale"].default_value = 8.0
    noise.inputs["Detail"].default_value = 3.0
    noise.inputs["Roughness"].default_value = 0.65

    darker = tuple(max(0.0, c * 0.86) for c in base_color)
    lighter = tuple(min(1.0, c * 1.08) for c in base_color)
    ramp.color_ramp.elements[0].position = 0.30
    ramp.color_ramp.elements[0].color = (*darker, 1.0)
    ramp.color_ramp.elements[1].position = 0.72
    ramp.color_ramp.elements[1].color = (*lighter, 1.0)

    bump.inputs["Strength"].default_value = 0.12
    bump.inputs["Distance"].default_value = 0.06
    set_principled_input(shader, "Roughness", roughness)

    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], shader.inputs["Base Color"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def make_tile_material(name, color_a, color_b, mortar_color, roughness=0.78, scale=7.0):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = (*color_a, 1.0)

    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    texcoord = nodes.new("ShaderNodeTexCoord")
    mapping = nodes.new("ShaderNodeMapping")
    brick = nodes.new("ShaderNodeTexBrick")
    bump = nodes.new("ShaderNodeBump")

    output.location = (740, 0)
    shader.location = (470, 0)
    brick.location = (60, 70)
    mapping.location = (-180, 70)
    texcoord.location = (-410, 70)
    bump.location = (240, -170)

    mapping.inputs["Scale"].default_value = (1.0, 1.0, 1.0)
    brick.inputs["Color1"].default_value = (*color_a, 1.0)
    brick.inputs["Color2"].default_value = (*color_b, 1.0)
    brick.inputs["Mortar"].default_value = (*mortar_color, 1.0)
    brick.inputs["Scale"].default_value = scale
    brick.inputs["Mortar Size"].default_value = 0.035
    brick.inputs["Mortar Smooth"].default_value = 0.01
    brick.inputs["Brick Width"].default_value = 0.62
    brick.inputs["Row Height"].default_value = 0.30

    bump.inputs["Strength"].default_value = 0.18
    bump.inputs["Distance"].default_value = 0.04
    set_principled_input(shader, "Roughness", roughness)

    links.new(texcoord.outputs["Generated"], mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], brick.inputs["Vector"])
    links.new(brick.outputs["Color"], shader.inputs["Base Color"])
    links.new(brick.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def create_materials():
    materials = {}
    materials["MAT_School_Plaster_Light"] = make_plaster_material(
        "MAT_School_Plaster_Light", (0.72, 0.66, 0.55), 0.84
    )
    materials["MAT_School_Plaster_Yellow"] = make_plaster_material(
        "MAT_School_Plaster_Yellow", (0.78, 0.57, 0.25), 0.83
    )
    materials["MAT_School_Tile_Beige"] = make_tile_material(
        "MAT_School_Tile_Beige",
        (0.57, 0.37, 0.23),
        (0.72, 0.51, 0.33),
        (0.30, 0.25, 0.21),
        0.77,
        8.0,
    )
    materials["MAT_Residential_Tile_Beige"] = make_tile_material(
        "MAT_Residential_Tile_Beige",
        (0.58, 0.45, 0.31),
        (0.72, 0.58, 0.42),
        (0.32, 0.30, 0.27),
        0.80,
        8.5,
    )
    materials["MAT_Residential_Mosaic_Light"] = make_tile_material(
        "MAT_Residential_Mosaic_Light",
        (0.62, 0.64, 0.59),
        (0.44, 0.54, 0.48),
        (0.25, 0.27, 0.25),
        0.82,
        13.0,
    )
    materials["MAT_Residential_Paint_New"] = make_plaster_material(
        "MAT_Residential_Paint_New", (0.67, 0.62, 0.54), 0.80
    )
    materials["MAT_Residential_Endwall_Concrete"] = make_plaster_material(
        "MAT_Residential_Endwall_Concrete", (0.42, 0.43, 0.42), 0.88
    )
    materials["MAT_Roof_Waterproof_Dark"] = make_plaster_material(
        "MAT_Roof_Waterproof_Dark", (0.075, 0.082, 0.086), 0.92
    )
    materials["MAT_Foundation_Concrete"] = make_plaster_material(
        "MAT_Foundation_Concrete", (0.24, 0.25, 0.25), 0.91
    )

    for material in materials.values():
        material["reference"] = "BlenderScenes/材质合集.png"
        material["workflow"] = "Procedural PBR approximation for massing phase"
    return materials


def create_building(collection, entry, category, materials):
    name, cx, cy, sx, sy, height, floors, facade_name, end_name = entry
    hx = sx * 0.5
    hy = sy * 0.5

    vertices = [
        (-hx, -hy, 0.0),
        (hx, -hy, 0.0),
        (hx, hy, 0.0),
        (-hx, hy, 0.0),
        (-hx, -hy, height),
        (hx, -hy, height),
        (hx, hy, height),
        (-hx, hy, height),
    ]
    # bottom, top, south, east, north, west
    faces = [
        (0, 3, 2, 1),
        (4, 5, 6, 7),
        (0, 1, 5, 4),
        (1, 2, 6, 5),
        (2, 3, 7, 6),
        (3, 0, 4, 7),
    ]

    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.location = (cx, cy, 0.0)

    facade = materials[facade_name]
    end_wall = materials[end_name]
    roof = materials["MAT_Roof_Waterproof_Dark"]
    foundation = materials["MAT_Foundation_Concrete"]
    for material in (facade, end_wall, roof, foundation):
        mesh.materials.append(material)

    # The primary facade follows the long axis; short ends use the secondary material.
    mesh.polygons[0].material_index = 3
    mesh.polygons[1].material_index = 2
    if sx >= sy:
        mesh.polygons[2].material_index = 0
        mesh.polygons[4].material_index = 0
        mesh.polygons[3].material_index = 1
        mesh.polygons[5].material_index = 1
        main_facing = "North/South"
    else:
        mesh.polygons[3].material_index = 0
        mesh.polygons[5].material_index = 0
        mesh.polygons[2].material_index = 1
        mesh.polygons[4].material_index = 1
        main_facing = "East/West"

    obj["category"] = category
    obj["dimensions_m"] = (sx, sy, height)
    obj["planned_floors"] = floors
    obj["floor_height_m"] = round(height / floors, 3)
    obj["unity_ground_center_xz"] = (cx, cy)
    obj["unity_center_xyz"] = (cx, height * 0.5, cy)
    obj["main_facade_orientation"] = main_facing
    obj["material_zone_primary"] = facade_name
    obj["material_zone_end_wall"] = end_name
    obj["material_zone_roof"] = roof.name
    obj["model_phase"] = "Urban building massing"

    for polygon in mesh.polygons:
        polygon.use_smooth = False
    return obj


def make_collections():
    scene_root = bpy.context.scene.collection
    master = bpy.data.collections.new("Urban_Building_Massing")
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
    master["content_scope"] = "Building masses only"
    preview["export_exclude"] = True
    return master, schools, residential, preview


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_preview(preview_collection):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = PREVIEW_PATH
    scene.render.film_transparent = False

    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = 0.7

    world = bpy.data.worlds.new("Urban_Massing_World")
    world.use_nodes = True
    world_nodes = world.node_tree.nodes
    world_links = world.node_tree.links
    world_nodes.clear()
    background = world_nodes.new("ShaderNodeBackground")
    world_output = world_nodes.new("ShaderNodeOutputWorld")
    background.location = (-220, 0)
    world_output.location = (60, 0)
    background.inputs["Color"].default_value = (0.035, 0.045, 0.060, 1.0)
    background.inputs["Strength"].default_value = 0.85
    world_links.new(background.outputs["Background"], world_output.inputs["Surface"])
    scene.world = world

    camera_data = bpy.data.cameras.new("Preview_Camera")
    camera = bpy.data.objects.new("Preview_Camera", camera_data)
    preview_collection.objects.link(camera)
    camera.location = (300.0, -350.0, 340.0)
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = 330.0
    look_at(camera, (0.0, -4.0, 12.0))
    scene.camera = camera

    key_data = bpy.data.lights.new("Preview_Key_Sun", type="SUN")
    key_data.energy = 2.8
    key_data.angle = math.radians(18.0)
    key = bpy.data.objects.new("Preview_Key_Sun", key_data)
    preview_collection.objects.link(key)
    key.rotation_euler = (math.radians(28.0), math.radians(-18.0), math.radians(-32.0))

    fill_data = bpy.data.lights.new("Preview_Fill_Area", type="AREA")
    fill_data.energy = 1450.0
    fill_data.shape = "DISK"
    fill_data.size = 150.0
    fill = bpy.data.objects.new("Preview_Fill_Area", fill_data)
    preview_collection.objects.link(fill)
    fill.location = (-85.0, -50.0, 180.0)
    look_at(fill, (0.0, 0.0, 20.0))


def validate_scene():
    expected_names = {entry[0] for entry in SCHOOL_BUILDINGS + RESIDENTIAL_BUILDINGS}
    actual = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    actual_names = {obj.name for obj in actual}
    if actual_names != expected_names:
        missing = sorted(expected_names - actual_names)
        extra = sorted(actual_names - expected_names)
        raise RuntimeError(f"Building validation failed. Missing={missing}, Extra={extra}")
    if len(actual) != 16:
        raise RuntimeError(f"Expected 16 building meshes, found {len(actual)}")
    for obj in actual:
        if len(obj.data.materials) != 4:
            raise RuntimeError(f"{obj.name} has {len(obj.data.materials)} material slots; expected 4")
        if min(v.co.z for v in obj.data.vertices) != 0.0:
            raise RuntimeError(f"{obj.name} is not grounded at local Z=0")


def main():
    clear_scene()
    scene = bpy.context.scene
    scene.name = "UrbanBuildingMassing_Whitebox"
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.length_unit = "METERS"
    scene.unit_settings.scale_length = 1.0

    _, schools, residential, preview = make_collections()
    materials = create_materials()

    for entry in SCHOOL_BUILDINGS:
        create_building(schools, entry, "MiddleSchool", materials)
    for entry in RESIDENTIAL_BUILDINGS:
        create_building(residential, entry, "Residential", materials)

    setup_preview(preview)
    validate_scene()

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    mesh_objects = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    print("URBAN_MASSING_BUILD_COMPLETE")
    print(f"BLEND={BLEND_PATH}")
    print(f"PREVIEW={PREVIEW_PATH}")
    print(f"BUILDINGS={len(mesh_objects)}")
    print(f"SCHOOLS={len(SCHOOL_BUILDINGS)}")
    print(f"RESIDENTIAL={len(RESIDENTIAL_BUILDINGS)}")
    print(f"MATERIALS={len(bpy.data.materials)}")


if __name__ == "__main__":
    main()
