import shutil
from pathlib import Path

import bpy


PROJECT_ROOT = Path(r"K:\FileK\unityprojects\DroneMicroClassSim")
SOURCE_BLEND = PROJECT_ROOT / "BlenderScenes" / "DroneFigureEightTraining.blend"
EXPORT_BLEND = PROJECT_ROOT / "BlenderScenes" / "DroneFigureEightTraining_UnityClean.blend"
UNITY_MODEL = PROJECT_ROOT / "Assets" / "DroneMicroClass" / "Models" / "Training" / "DroneFigureEightTraining.fbx"
UNITY_TEXTURE_DIR = PROJECT_ROOT / "Assets" / "DroneMicroClass" / "Textures" / "Training"

REMOVE_PREFIXES = (
    "Total_Length_24m_Dimension_Line",
    "Radius_6m_Dimension_Line",
)


def remove_non_scene_annotations():
    for obj in list(bpy.data.objects):
        if obj.type == "FONT" or obj.name.startswith(REMOVE_PREFIXES):
            bpy.data.objects.remove(obj, do_unlink=True)


def copy_texture_assets():
    UNITY_TEXTURE_DIR.mkdir(parents=True, exist_ok=True)
    for image in bpy.data.images:
        if not image.filepath:
            continue
        source = Path(bpy.path.abspath(image.filepath))
        if not source.exists() or source.suffix.lower() not in {".png", ".jpg", ".jpeg", ".tga"}:
            continue
        destination = UNITY_TEXTURE_DIR / source.name
        shutil.copy2(source, destination)
        image.filepath = bpy.path.relpath(str(destination))


def convert_supported_curves_to_mesh():
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.data.objects:
        if obj.type == "CURVE":
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.convert(target="MESH")
            obj.select_set(False)


def export_fbx():
    UNITY_MODEL.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    exportable_types = {"MESH", "EMPTY"}
    for obj in bpy.context.scene.objects:
        if obj.type in exportable_types:
            obj.select_set(True)

    bpy.ops.export_scene.fbx(
        filepath=str(UNITY_MODEL),
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        path_mode="COPY",
        embed_textures=False,
        use_mesh_modifiers=True,
    )


def main():
    if not SOURCE_BLEND.exists():
        raise FileNotFoundError(SOURCE_BLEND)

    remove_non_scene_annotations()
    copy_texture_assets()
    convert_supported_curves_to_mesh()
    bpy.ops.wm.save_as_mainfile(filepath=str(EXPORT_BLEND))
    export_fbx()


if __name__ == "__main__":
    main()
