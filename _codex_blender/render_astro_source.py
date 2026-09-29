import bpy
from mathutils import Vector

SOURCE_FBX = r"E:\github\board_chase\board_chase\Assets\Art\Models\GasMask\AstroJuggernaut_BlackLens_Capsule.fbx"
OUTPUT_FOLDER = r"E:\github\board_chase\board_chase\_codex_blender\renders"


def look_at(camera, point):
    direction = Vector(point) - camera.location
    camera.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()


def make_material():
    material = bpy.data.materials.new("Preview_RubberBlack")
    material.diffuse_color = (0.025, 0.03, 0.038, 1.0)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    principled = nodes.new("ShaderNodeBsdfPrincipled")
    material.node_tree.links.new(principled.outputs["BSDF"], output.inputs["Surface"])
    principled.inputs["Base Color"].default_value = (0.018, 0.022, 0.03, 1.0)
    principled.inputs["Roughness"].default_value = 0.56
    principled.inputs["Metallic"].default_value = 0.04
    return material


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SOURCE_FBX, use_custom_normals=True)
mask = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH')
black = make_material()
mask.data.materials.clear()
mask.data.materials.append(black)

scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
if scene.world is None:
    scene.world = bpy.data.worlds.new("PreviewWorld")
scene.world.color = (0.035, 0.035, 0.045)

bpy.ops.object.light_add(type='AREA', location=(0.0, -1.1, 0.75))
key = bpy.context.object
key.data.energy = 400
key.data.shape = 'DISK'
key.data.size = 1.0
look_at(key, (0.0, 0.0, 0.0))

bpy.ops.object.light_add(type='AREA', location=(-0.75, 0.1, 0.35))
fill = bpy.context.object
fill.data.energy = 120
fill.data.size = 0.8
look_at(fill, (0.0, 0.0, 0.0))

bpy.ops.object.camera_add()
camera = bpy.context.object
scene.camera = camera

views = {
    "front_y_negative": (0.0, -1.5, 0.0),
    "front_y_positive": (0.0, 1.5, 0.0),
    "side_x_negative": (-1.5, 0.0, 0.0),
    "side_x_positive": (1.5, 0.0, 0.0),
}

for name, location in views.items():
    camera.location = location
    look_at(camera, (0.0, 0.0, 0.0))
    scene.render.filepath = OUTPUT_FOLDER + "\\" + name + ".png"
    bpy.ops.render.render(write_still=True)

print("CODEX_ASTRO_RENDERED=" + OUTPUT_FOLDER)
