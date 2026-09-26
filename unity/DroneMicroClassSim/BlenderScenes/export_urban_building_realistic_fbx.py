import bpy
import os


SOURCE_BLEND = r"K:\FileK\unityprojects\DroneMicroClassSim\BlenderScenes\UrbanBuildingMassing_Realistic.blend"
OUTPUT_DIR = r"K:\FileK\unityprojects\DroneMicroClassSim\Assets\DroneMicroClass\Models\UrbanBuildings"
OUTPUT_FBX = os.path.join(OUTPUT_DIR, "UrbanBuildingMassing_Realistic.fbx")


def main():
    if bpy.data.filepath != SOURCE_BLEND:
        bpy.ops.wm.open_mainfile(filepath=SOURCE_BLEND)

    os.makedirs(OUTPUT_DIR, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")

    export_objects = []
    for obj in bpy.data.objects:
        if obj.get("preview_only"):
            continue
        if obj.name.startswith("Preview_"):
            continue
        if obj.type not in {"EMPTY", "MESH"}:
            continue
        if obj.type == "EMPTY" and obj.get("category") not in {"MiddleSchool", "Residential"}:
            continue
        if obj.type == "MESH" and obj.get("role") not in {
            "WallsAndRoof",
            "WindowGlass",
            "WindowFrames",
        }:
            continue
        obj.select_set(True)
        export_objects.append(obj)

    if len(export_objects) != 64:
        raise RuntimeError(f"Expected 64 export objects (16 roots + 48 meshes), found {len(export_objects)}")

    bpy.context.view_layer.objects.active = next(obj for obj in export_objects if obj.type == "MESH")
    bpy.ops.export_scene.fbx(
        filepath=OUTPUT_FBX,
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_tspace=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )
    print("URBAN_FBX_EXPORT_COMPLETE")
    print(f"FBX={OUTPUT_FBX}")
    print(f"OBJECTS={len(export_objects)}")
    print("UNITY_AXES=-Z forward, Y up")


if __name__ == "__main__":
    main()
