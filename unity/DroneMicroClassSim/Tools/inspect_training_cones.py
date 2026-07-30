import json

import bpy
from mathutils import Vector


def world_bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    min_corner = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    max_corner = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return min_corner, max_corner


objects = []
for obj in bpy.context.scene.objects:
    if obj.type != "MESH" or "_Cone_" not in obj.name:
        continue

    min_corner, max_corner = world_bounds(obj)
    material_counts = {}
    for polygon in obj.data.polygons:
        material_name = obj.data.materials[polygon.material_index].name if obj.data.materials else "<none>"
        material_counts[material_name] = material_counts.get(material_name, 0) + 1

    objects.append(
        {
            "name": obj.name,
            "worldMinZ": round(min_corner.z, 4),
            "worldMaxZ": round(max_corner.z, 4),
            "vertices": len(obj.data.vertices),
            "polygons": len(obj.data.polygons),
            "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
            "materialFaceCounts": material_counts,
        }
    )

print("TRAINING_CONES_JSON_BEGIN")
print(json.dumps(objects, ensure_ascii=False, indent=2))
print("TRAINING_CONES_JSON_END")
