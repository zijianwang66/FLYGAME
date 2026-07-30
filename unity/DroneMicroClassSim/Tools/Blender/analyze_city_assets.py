import json
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(r"K:\FileK\unityprojects\DroneMicroClassSim")
SOURCE_DIR = PROJECT_ROOT / "素材"
OUTPUT_PATH = PROJECT_ROOT / "BlenderScenes" / "CityAssetAnalysis.json"

ASSETS = {
    "plants": SOURCE_DIR / "shapespark-low-poly-plants-kit.fbx",
    "dji_drone": SOURCE_DIR / "dji无人机.fbx",
}


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def import_fbx(path):
    clear_scene()
    error = None
    try:
        bpy.ops.import_scene.fbx(filepath=str(path))
    except Exception as exc:
        error = str(exc)
    return error


def mesh_triangle_count(mesh):
    return sum(len(poly.vertices) - 2 for poly in mesh.polygons)


def object_bounds(objects):
    min_v = [float("inf"), float("inf"), float("inf")]
    max_v = [float("-inf"), float("-inf"), float("-inf")]
    found = False
    for obj in objects:
        if obj.type != "MESH":
            continue
        found = True
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            min_v[0] = min(min_v[0], world.x)
            min_v[1] = min(min_v[1], world.y)
            min_v[2] = min(min_v[2], world.z)
            max_v[0] = max(max_v[0], world.x)
            max_v[1] = max(max_v[1], world.y)
            max_v[2] = max(max_v[2], world.z)
    if not found:
        return None
    return {
        "min": min_v,
        "max": max_v,
        "size": [max_v[i] - min_v[i] for i in range(3)],
    }


def analyze_asset(name, path):
    importError = import_fbx(path)
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    object_stats = []
    total_vertices = 0
    total_polygons = 0
    total_triangles = 0
    for obj in mesh_objects:
        mesh = obj.data
        vertices = len(mesh.vertices)
        polygons = len(mesh.polygons)
        triangles = mesh_triangle_count(mesh)
        total_vertices += vertices
        total_polygons += polygons
        total_triangles += triangles
        object_stats.append(
            {
                "name": obj.name,
                "vertices": vertices,
                "polygons": polygons,
                "triangles": triangles,
                "materials": [slot.material.name for slot in obj.material_slots if slot.material],
            }
        )

    object_stats.sort(key=lambda item: item["triangles"])
    return {
        "asset": name,
        "path": str(path),
        "importError": importError,
        "meshObjectCount": len(mesh_objects),
        "totalVertices": total_vertices,
        "totalPolygons": total_polygons,
        "totalTriangles": total_triangles,
        "bounds": object_bounds(mesh_objects),
        "lowestTriangleObjects": object_stats[:20],
        "highestTriangleObjects": object_stats[-20:],
    }


def main():
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    report = {}
    for name, path in ASSETS.items():
        report[name] = analyze_asset(name, path)
    OUTPUT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
