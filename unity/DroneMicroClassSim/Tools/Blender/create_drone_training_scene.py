import math
import random
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.geometry import tessellate_polygon


OUTPUT_BLEND = Path(r"K:\FileK\unityprojects\DroneMicroClassSim\BlenderScenes\DroneFigureEightTraining.blend")
TEXTURE_DIR = OUTPUT_BLEND.parent / "Textures"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def save_procedural_texture(name, base, accent, size=512, kind="speckle"):
    TEXTURE_DIR.mkdir(parents=True, exist_ok=True)
    path = TEXTURE_DIR / f"{name}.png"
    rng = random.Random(name)
    pixels = []
    for y in range(size):
        for x in range(size):
            n = rng.random()
            wave = 0.5 + 0.5 * math.sin((x * 0.07) + (y * 0.013))
            if kind == "grass":
                mix = min(1.0, max(0.0, 0.22 * n + 0.22 * wave))
            elif kind == "rubber":
                mix = min(1.0, max(0.0, 0.10 * n + (0.40 if n > 0.965 else 0.0)))
            else:
                mix = min(1.0, max(0.0, 0.16 * n + 0.08 * wave))
            r = base[0] * (1.0 - mix) + accent[0] * mix
            g = base[1] * (1.0 - mix) + accent[1] * mix
            b = base[2] * (1.0 - mix) + accent[2] * mix
            pixels.extend((r, g, b, 1.0))

    image = bpy.data.images.new(name, width=size, height=size)
    image.pixels = pixels
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()
    return path


def make_mat(name, color, roughness=0.75, alpha=1.0, texture_path=None):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (color[0], color[1], color[2], alpha)
    mat.use_nodes = True
    bsdf = next(
        (node for node in mat.node_tree.nodes if node.bl_idname == "ShaderNodeBsdfPrincipled"),
        None,
    )
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (color[0], color[1], color[2], alpha)
        bsdf.inputs["Roughness"].default_value = roughness
        if texture_path:
            image = bpy.data.images.load(str(texture_path), check_existing=True)
            tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
            tex.image = image
            mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        if alpha < 1.0:
            bsdf.inputs["Alpha"].default_value = alpha
            mat.blend_method = "BLEND"
            mat.use_screen_refraction = True
    return mat


def cube_obj(name, loc, scale, mat):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    return obj


def cylinder_obj(name, loc, radius, depth, mat, vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    return obj


def text_obj(name, text, loc, size, mat, align="CENTER"):
    bpy.ops.object.text_add(location=loc, rotation=(0, 0, 0))
    obj = bpy.context.object
    obj.name = name
    obj.data.body = text
    obj.data.align_x = align
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.01
    obj.data.materials.append(mat)
    return obj


def curve_polyline(name, points, mat, bevel_depth=0.04):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 2
    curve.bevel_depth = bevel_depth
    curve.bevel_resolution = 3
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for p, co in zip(spline.points, points):
        p.co = (co[0], co[1], co[2], 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def rectangle_outline(name, width, height, z, mat, bevel_depth=0.035):
    hw, hh = width / 2.0, height / 2.0
    pts = [(-hw, -hh, z), (hw, -hh, z), (hw, hh, z), (-hw, hh, z), (-hw, -hh, z)]
    return curve_polyline(name, pts, mat, bevel_depth)


def annulus_mesh(name, center, inner_radius, outer_radius, z, mat, segments=192):
    verts = []
    faces = []
    cx, cy = center
    for i in range(segments):
        t = 2.0 * math.pi * i / segments
        ct, st = math.cos(t), math.sin(t)
        verts.append((cx + outer_radius * ct, cy + outer_radius * st, z))
        verts.append((cx + inner_radius * ct, cy + inner_radius * st, z))
    for i in range(segments):
        ni = (i + 1) % segments
        faces.append((2 * i, 2 * ni, 2 * ni + 1, 2 * i + 1))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def add_uvs(mesh, verts, min_x, min_y, width, height):
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for poly in mesh.polygons:
        for loop_index in poly.loop_indices:
            vertex = verts[mesh.loops[loop_index].vertex_index]
            uv_layer.data[loop_index].uv = (
                (vertex[0] - min_x) / width,
                (vertex[1] - min_y) / height,
            )


def circle_points(center, radius, start, end, segments):
    cx, cy = center
    return [
        Vector((cx + radius * math.cos(start + (end - start) * i / segments),
                cy + radius * math.sin(start + (end - start) * i / segments),
                0.0))
        for i in range(segments + 1)
    ]


def figure_eight_runway_mesh(name, centers, inner_radius, outer_radius, z, mat, segments=128):
    left_center, right_center = centers
    center_distance = abs(right_center[0] - left_center[0])
    alpha = math.acos(center_distance / (2.0 * outer_radius))

    left_outer = circle_points(left_center, outer_radius, alpha, (2.0 * math.pi) - alpha, segments)
    right_outer = circle_points(right_center, outer_radius, -math.pi + alpha, math.pi - alpha, segments)
    outer_loop = left_outer + right_outer

    # Holes are separate because the inner 4.5 m clear areas only touch the other loop at a point.
    left_hole = circle_points(left_center, inner_radius, 0.0, 2.0 * math.pi, segments)
    right_hole = circle_points(right_center, inner_radius, 2.0 * math.pi, 0.0, segments)
    loops = [outer_loop, left_hole, right_hole]

    flat_verts = []
    for loop in loops:
        flat_verts.extend((v.x, v.y, z) for v in loop)
    faces = tessellate_polygon(loops)

    min_x = min(v[0] for v in flat_verts)
    max_x = max(v[0] for v in flat_verts)
    min_y = min(v[1] for v in flat_verts)
    max_y = max(v[1] for v in flat_verts)
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(flat_verts, [], faces)
    add_uvs(mesh, flat_verts, min_x, min_y, max_x - min_x, max_y - min_y)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def flat_arc_dash(name, center, radius, z, width, start, end, mat, steps=8):
    cx, cy = center
    verts = []
    faces = []
    for i in range(steps + 1):
        t = start + (end - start) * i / steps
        ct, st = math.cos(t), math.sin(t)
        verts.append((cx + (radius + width / 2.0) * ct, cy + (radius + width / 2.0) * st, z))
        verts.append((cx + (radius - width / 2.0) * ct, cy + (radius - width / 2.0) * st, z))
    for i in range(steps):
        faces.append((2 * i, 2 * i + 2, 2 * i + 3, 2 * i + 1))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    add_uvs(mesh, verts, cx - radius, cy - radius, radius * 2.0, radius * 2.0)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def flat_rect_segment(name, p1, p2, z, width, mat):
    v1 = Vector((p1[0], p1[1], 0))
    v2 = Vector((p2[0], p2[1], 0))
    direction = (v2 - v1).normalized()
    normal = Vector((-direction.y, direction.x, 0)) * (width / 2.0)
    a = v1 + normal
    b = v2 + normal
    c = v2 - normal
    d = v1 - normal
    verts = [(a.x, a.y, z), (b.x, b.y, z), (c.x, c.y, z), (d.x, d.y, z)]
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], [(0, 1, 2, 3)])
    add_uvs(mesh, verts, min(p1[0], p2[0]), min(p1[1], p2[1]) - width, abs(p2[0] - p1[0]) + width, abs(p2[1] - p1[1]) + width)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def flat_dashed_circle(name, center, radius, z, mat, dash_count=32, duty=0.58, width=0.13):
    cx, cy = center
    for i in range(dash_count):
        start = 2.0 * math.pi * i / dash_count
        end = start + 2.0 * math.pi * duty / dash_count
        flat_arc_dash(f"{name}_FlatDash_{i + 1:02d}", (cx, cy), radius, z, width, start, end, mat)


def flat_dashed_line(name, start, end, z, mat, width=0.13, dash_len=0.75, gap=0.45):
    p1 = Vector((start[0], start[1], 0))
    p2 = Vector((end[0], end[1], 0))
    length = (p2 - p1).length
    direction = (p2 - p1).normalized()
    cursor = 0.0
    idx = 1
    while cursor < length:
        seg_start = p1 + direction * cursor
        seg_end = p1 + direction * min(length, cursor + dash_len)
        flat_rect_segment(f"{name}_FlatDash_{idx:02d}", (seg_start.x, seg_start.y), (seg_end.x, seg_end.y), z, width, mat)
        cursor += dash_len + gap
        idx += 1


def arrow_triangle(name, loc, angle_rad, size, mat):
    x, y, z = loc
    forward = Vector((math.cos(angle_rad), math.sin(angle_rad), 0))
    right = Vector((-math.sin(angle_rad), math.cos(angle_rad), 0))
    tip = Vector((x, y, z)) + forward * size
    left = Vector((x, y, z)) - forward * size * 0.65 + right * size * 0.45
    right_pt = Vector((x, y, z)) - forward * size * 0.65 - right * size * 0.45
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata([tuple(tip), tuple(left), tuple(right_pt)], [], [(0, 1, 2)])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def build_scene():
    clear_scene()
    textures = {
        "grass": save_procedural_texture("procedural_grass", (0.22, 0.48, 0.18), (0.48, 0.72, 0.32), kind="grass"),
        "flight": save_procedural_texture("procedural_flight_zone", (0.92, 0.84, 0.62), (1.0, 0.95, 0.75), kind="speckle"),
        "runway": save_procedural_texture("procedural_blue_rubber_runway", (0.20, 0.63, 0.82), (0.05, 0.38, 0.60), kind="rubber"),
    }

    mats = {
        "safe": make_mat("Safe_Area_Grass_Texture", (0.36, 0.67, 0.32), texture_path=textures["grass"]),
        "flight": make_mat("Flight_Area_Matte_Sand_Texture", (0.95, 0.86, 0.63), texture_path=textures["flight"]),
        "runway": make_mat("Runway_3m_Blue_Plastic_Rubber_Texture", (0.20, 0.63, 0.82), roughness=0.88, texture_path=textures["runway"]),
        "route": make_mat("Dashed_Route_Blue", (0.02, 0.42, 0.90)),
        "orange": make_mat("Large_Pylon_Orange", (1.0, 0.45, 0.02)),
        "red": make_mat("Center_Pylon_Red", (0.82, 0.05, 0.05)),
        "gray": make_mat("Small_Pylon_Gray", (0.35, 0.35, 0.35)),
        "line_green": make_mat("Safety_Border_Green", (0.18, 0.65, 0.28)),
        "line_orange": make_mat("Flight_Border_Orange", (1.0, 0.55, 0.02)),
        "text": make_mat("Dimension_Text_Dark", (0.08, 0.08, 0.08)),
        "white": make_mat("White_Marker", (0.96, 0.96, 0.92)),
    }

    root = bpy.data.objects.new("Drone_Training_Figure_Eight_Root", None)
    bpy.context.collection.objects.link(root)

    safety = cube_obj("Flying_Safety_Area_40m_x_26m", (0, 0, -0.025), (40, 26, 0.03), mats["safe"])
    flight = cube_obj("Flight_Area_30m_x_16m", (0, 0, 0.005), (30, 16, 0.025), mats["flight"])
    for obj in (safety, flight):
        obj.parent = root

    rectangle_outline("Safety_Area_Dashed_Border_Geometry", 40, 26, 0.065, mats["line_green"], 0.025).parent = root
    rectangle_outline("Flight_Area_Orange_Border", 30, 16, 0.08, mats["line_orange"], 0.035).parent = root

    # Reference dimensions from the sketch: two tangent 6 m radius loops, 3 m runway width.
    center_radius = 6.0
    runway_width = 3.0
    inner_radius = center_radius - runway_width / 2.0
    outer_radius = center_radius + runway_width / 2.0
    left_center = (-6.0, 0.0)
    right_center = (6.0, 0.0)
    figure_eight_runway_mesh(
        "Single_Mesh_Figure_Eight_Runway_3m_Width_No_Overlap",
        (left_center, right_center),
        inner_radius,
        outer_radius,
        0.12,
        mats["runway"],
    ).parent = root

    flat_dashed_circle("Left_8_Route_R6m", left_center, center_radius, 0.155, mats["route"])
    flat_dashed_circle("Right_8_Route_R6m", right_center, center_radius, 0.155, mats["route"])
    flat_dashed_line("Center_Tangent_Route", (-6, 0), (6, 0), 0.156, mats["route"])
    arrow_triangle("Route_Direction_Arrow", (4.7, -5.1, 0.21), math.radians(190), 0.55, mats["route"]).parent = root

    # Large pylons A-F, height 0.9 m.
    large_pylons = {
        "A": (-12, 0), "C": (-6, 6), "D": (-6, -6),
        "B": (12, 0), "E": (6, 6), "F": (6, -6),
    }
    for label, (x, y) in large_pylons.items():
        pylon = cylinder_obj(f"Large_Pylon_{label}_H0p9m", (x, y, 0.45), 0.35, 0.9, mats["orange"])
        pylon.parent = root
        cap = cylinder_obj(f"Large_Pylon_{label}_White_Cap", (x, y, 0.93), 0.28, 0.06, mats["white"])
        cap.parent = root
        text = text_obj(f"Label_{label}", label, (x, y + 0.72, 0.22), 0.55, mats["line_orange"])
        text.parent = root

    small_positions = [
        (-9, 2.4), (-3, 2.4), (-9, -2.4), (-3, -2.4),
        (3, 2.4), (9, 2.4), (3, -2.4), (9, -2.4),
    ]
    for idx, (x, y) in enumerate(small_positions, 1):
        small = cylinder_obj(f"Small_Pylon_{idx:02d}_H0p6m", (x, y, 0.3), 0.22, 0.6, mats["gray"])
        small.parent = root

    center = cylinder_obj("Center_Pylon_O_H0p9m", (0, 0, 0.45), 0.42, 0.9, mats["red"])
    center.parent = root
    text_obj("Center_Label", "中心桩 O", (0, -1.18, 0.2), 0.42, mats["text"]).parent = root

    # Dimension annotations on the ground.
    curve_polyline("Total_Length_24m_Dimension_Line", [(-12, -9.2, 0.13), (12, -9.2, 0.13)], mats["text"], 0.025).parent = root
    text_obj("Total_Length_Label", "总长度 ≈ 24m（两圆外切）", (0, -9.75, 0.2), 0.42, mats["text"]).parent = root
    curve_polyline("Radius_6m_Dimension_Line", [(-6, 6, 0.2), (-6, 0, 0.2)], mats["text"], 0.02).parent = root
    text_obj("Radius_Label", "r = 6m", (-8.1, 3.2, 0.22), 0.38, mats["text"]).parent = root
    text_obj("Runway_Width_Label", "跑道宽度 3m", (0, 8.7, 0.2), 0.42, mats["text"]).parent = root

    clean_labels = {
        "Center_Label": "Center O",
        "Total_Length_Label": "Total length approx 24m",
        "Runway_Width_Label": "Runway width 3m",
    }
    for label_name, body in clean_labels.items():
        label_obj = bpy.data.objects.get(label_name)
        if label_obj and hasattr(label_obj.data, "body"):
            label_obj.data.body = body

    bpy.ops.object.light_add(type="SUN", location=(0, -5, 12))
    sun = bpy.context.object
    sun.name = "Sun_Key_Light"
    sun.data.energy = 2.0
    sun.rotation_euler = (math.radians(45), 0, math.radians(35))
    sun.parent = root

    bpy.ops.object.light_add(type="AREA", location=(0, 0, 18))
    area = bpy.context.object
    area.name = "Top_Area_Fill_Light"
    area.data.energy = 450
    area.data.size = 30
    area.parent = root

    bpy.ops.object.camera_add(location=(0, 0, 36))
    camera = bpy.context.object
    camera.name = "Camera_Top_Overview"
    camera.rotation_euler = (0, 0, 0)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 42
    bpy.context.scene.camera = camera

    # Add a slightly oblique secondary camera for quick scene inspection.
    bpy.ops.object.camera_add(location=(0, -24, 18))
    oblique = bpy.context.object
    oblique.name = "Camera_Oblique_Training_View"
    look_at(oblique, (0, 0, 0))
    oblique.data.lens = 28
    oblique.parent = root

    bpy.context.scene.render.engine = "BLENDER_EEVEE_NEXT"
    bpy.context.scene.world.color = (1.0, 1.0, 1.0)
    bpy.context.scene.view_settings.view_transform = "Standard"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.view_settings.exposure = 0.0
    bpy.context.scene.view_settings.gamma = 1.0
    bpy.context.scene.render.resolution_x = 1400
    bpy.context.scene.render.resolution_y = 900
    bpy.context.scene.render.image_settings.file_format = "PNG"
    bpy.context.scene.render.image_settings.color_mode = "RGB"
    bpy.context.scene.render.image_settings.color_depth = "8"

    OUTPUT_BLEND.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND))


if __name__ == "__main__":
    build_scene()
