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
    if obj.type in {"MESH", "EMPTY"}:
        min_corner, max_corner = world_bounds(obj) if obj.type == "MESH" else (obj.location, obj.location)
        objects.append(
            {
                "name": obj.name,
                "type": obj.type,
                "location": [round(v, 4) for v in obj.location],
                "rotation": [round(v, 4) for v in obj.rotation_euler],
                "scale": [round(v, 4) for v in obj.scale],
                "dimensions": [round(v, 4) for v in obj.dimensions],
                "worldMin": [round(v, 4) for v in min_corner],
                "worldMax": [round(v, 4) for v in max_corner],
                "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
            }
        )

print("BLENDER_SCENE_OBJECTS_JSON_BEGIN")
print(json.dumps(objects, ensure_ascii=False, indent=2))
print("BLENDER_SCENE_OBJECTS_JSON_END")
