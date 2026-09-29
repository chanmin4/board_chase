import bpy
import json
from mathutils import Vector

SOURCE_FBX = r"E:\github\board_chase\board_chase\Assets\Art\Models\GasMask\AstroJuggernaut_BlackLens_Capsule.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SOURCE_FBX, use_custom_normals=True)

report = {"objects": []}

for obj in bpy.context.scene.objects:
    if obj.type != 'MESH':
        continue

    world_points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    minimum = [min(point[index] for point in world_points) for index in range(3)]
    maximum = [max(point[index] for point in world_points) for index in range(3)]
    material_names = [slot.material.name if slot.material else None for slot in obj.material_slots]
    report["objects"].append({
        "name": obj.name,
        "location": list(obj.location),
        "rotation": list(obj.rotation_euler),
        "scale": list(obj.scale),
        "bounds_min": minimum,
        "bounds_max": maximum,
        "dimensions": [maximum[index] - minimum[index] for index in range(3)],
        "materials": material_names,
        "vertices": len(obj.data.vertices),
        "polygons": len(obj.data.polygons),
    })

print("CODEX_ASTRO_REPORT=" + json.dumps(report, separators=(",", ":")))
