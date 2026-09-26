import bpy
import json
import os
import re


PROJECT_ROOT = r"K:\FileK\unityprojects\DroneMicroClassSim"
SOURCE_BLEND = os.path.join(PROJECT_ROOT, "BlenderScenes", "02.blend")
TEXTURE_DIR = os.path.join(PROJECT_ROOT, "Assets", "DroneMicroClass", "Textures", "Urban02")
MODEL_DIR = os.path.join(PROJECT_ROOT, "Assets", "DroneMicroClass", "Models", "UrbanBuildings")
OUTPUT_FBX = os.path.join(MODEL_DIR, "02_Corrected.fbx")
REPORT_PATH = os.path.join(PROJECT_ROOT, "BlenderScenes", "02_UnityExportReport.json")


def safe_filename(name):
    cleaned = re.sub(r'[<>:"/\\|?*]', "_", name)
    return cleaned.strip().rstrip(".") or "texture"


def image_format_from_extension(path):
    extension = os.path.splitext(path)[1].lower()
    if extension in {".jpg", ".jpeg"}:
        return "JPEG"
    if extension == ".tga":
        return "TARGA"
    if extension == ".bmp":
        return "BMP"
    if extension == ".tif" or extension == ".tiff":
        return "TIFF"
    return "PNG"


def extract_images():
    os.makedirs(TEXTURE_DIR, exist_ok=True)
    extracted = []

    for image in bpy.data.images:
        if image.source != "FILE" or image.name in {"Render Result", "Viewer Node"}:
            continue

        original_name = os.path.basename(image.filepath) if image.filepath else image.name
        filename = safe_filename(original_name)
        if not os.path.splitext(filename)[1]:
            filename += ".png"
        output_path = os.path.join(TEXTURE_DIR, filename)

        previous_filepath = image.filepath_raw
        previous_format = image.file_format
        try:
            image.filepath_raw = output_path
            image.file_format = image_format_from_extension(output_path)
            image.save()
        except Exception:
            # save_render is a reliable fallback for packed images whose original
            # external source no longer exists.
            image.save_render(output_path, scene=bpy.context.scene)
        finally:
            image.filepath_raw = output_path
            if os.path.exists(output_path):
                image.reload()
            else:
                image.filepath_raw = previous_filepath
                image.file_format = previous_format

        extracted.append(
            {
                "image": image.name,
                "path": output_path,
                "exists": os.path.exists(output_path),
                "bytes": os.path.getsize(output_path) if os.path.exists(output_path) else 0,
            }
        )

    return extracted


def prepare_export_materials():
    prepared = []
    for material in bpy.data.materials:
        if not material.use_nodes or not material.node_tree:
            continue

        principled_nodes = [
            node for node in material.node_tree.nodes
            if node.bl_idname == "ShaderNodeBsdfPrincipled"
        ]
        image_nodes = [
            node for node in material.node_tree.nodes
            if node.bl_idname == "ShaderNodeTexImage" and node.image
        ]

        for shader in principled_nodes:
            base_color = shader.inputs.get("Base Color")
            alpha = shader.inputs.get("Alpha")
            normal = shader.inputs.get("Normal")

            if base_color and not base_color.is_linked:
                material.diffuse_color = tuple(base_color.default_value)
            elif image_nodes:
                # FBX exports the linked image as diffuse texture. Keep the tint
                # neutral so Unity displays the original image colors.
                material.diffuse_color = (1.0, 1.0, 1.0, 1.0)

            # The imported C4D materials contain Normal Map nodes whose Color
            # input is not connected to any texture. Remove only these invalid
            # links in the temporary export session.
            if normal and normal.is_linked:
                for link in list(normal.links):
                    source = link.from_node
                    if (
                        source.bl_idname == "ShaderNodeNormalMap"
                        and source.inputs.get("Color")
                        and not source.inputs["Color"].is_linked
                    ):
                        material.node_tree.links.remove(link)

            if alpha and alpha.is_linked:
                material.diffuse_color = (
                    material.diffuse_color[0],
                    material.diffuse_color[1],
                    material.diffuse_color[2],
                    1.0,
                )

        prepared.append(
            {
                "material": material.name,
                "imageTextures": [node.image.name for node in image_nodes],
                "hasLinkedAlpha": any(
                    node.inputs.get("Alpha") and node.inputs["Alpha"].is_linked
                    for node in principled_nodes
                ),
            }
        )
    return prepared


def export_fbx():
    os.makedirs(MODEL_DIR, exist_ok=True)
    export_objects = [
        obj for obj in bpy.context.scene.objects
        if obj.type in {"MESH", "EMPTY"}
    ]

    if not export_objects:
        raise RuntimeError("No mesh or empty objects found for export")

    bpy.ops.export_scene.fbx(
        filepath=OUTPUT_FBX,
        use_selection=False,
        object_types={"MESH", "EMPTY"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,
        use_mesh_modifiers=False,
        mesh_smooth_type="FACE",
        use_tspace=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
    )
    return export_objects


def main():
    if bpy.data.filepath != SOURCE_BLEND:
        bpy.ops.wm.open_mainfile(filepath=SOURCE_BLEND)

    extracted = extract_images()
    prepared_materials = prepare_export_materials()
    export_objects = export_fbx()

    report = {
        "sourceBlend": SOURCE_BLEND,
        "outputFbx": OUTPUT_FBX,
        "exportObjectCount": len(export_objects),
        "meshCount": sum(obj.type == "MESH" for obj in export_objects),
        "emptyCount": sum(obj.type == "EMPTY" for obj in export_objects),
        "materialCount": len(bpy.data.materials),
        "textures": extracted,
        "materials": prepared_materials,
        "fbxBytes": os.path.getsize(OUTPUT_FBX),
        "settings": {
            "axisForward": "-Z",
            "axisUp": "Y",
            "applyUnitScale": True,
            "applyModifiers": False,
            "pathMode": "COPY",
            "embedTextures": True,
        },
    }
    with open(REPORT_PATH, "w", encoding="utf-8") as report_file:
        json.dump(report, report_file, ensure_ascii=False, indent=2)

    print("URBAN_02_CORRECTED_EXPORT_COMPLETE")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
