import bpy
import math
import os

PROJECT = r"E:\github\board_chase\board_chase"
SOURCE_FBX = os.path.join(PROJECT, "Assets", "Art", "Models", "GasMask", "AstroJuggernaut_BlackLens_Capsule.fbx")
OUTPUT_FBX = os.path.join(PROJECT, "Assets", "Art", "Models", "GasMask", "AstroJuggernaut_BlackLens_Remodeled.fbx")
RENDER_DIR = os.path.join(PROJECT, "_codex_blender", "renders", "astro_remodeled")


def make_material(name, color, metallic, roughness):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    links.new(bsdf.outputs[0], output.inputs["Surface"])
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    material.diffuse_color = (*color, 1.0)
    return material


def make_lens_backing():
    # A single recessed backing sits behind the entire eye area. The original
    # shell defines the actual irregular eye-hole outlines, so this cannot drift
    # away from either opening like two independent proxy spheres could.
    bpy.ops.mesh.primitive_cube_add(location=(0.0, 0.120, 0.062))
    backing = bpy.context.object
    backing.name = "IntegratedEyeLensBacking"
    backing.dimensions = (0.325, 0.020, 0.205)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = backing.modifiers.new("LensBackingSoftening", "BEVEL")
    bevel.width = 0.012
    bevel.segments = 4
    bpy.context.view_layer.objects.active = backing
    bpy.ops.object.modifier_apply(modifier="LensBackingSoftening")
    return backing


def add_capsule_preview():
    preview_material = make_material("PreviewCapsuleWhite", (0.76, 0.79, 0.82), 0.0, 0.62)
    # Rear/preview-only simple capsule. It is not selected for FBX export.
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, location=(0.0, 0.60, 0.30))
    top = bpy.context.object
    top.name = "PreviewCapsuleTop"
    top.scale = (0.37, 0.37, 0.37)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.37, depth=0.92, location=(0.0, 0.60, -0.16))
    center = bpy.context.object
    center.name = "PreviewCapsuleBody"
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, location=(0.0, 0.60, -0.62))
    bottom = bpy.context.object
    bottom.name = "PreviewCapsuleBottom"
    bottom.scale = (0.37, 0.37, 0.37)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for obj in (top, center, bottom):
        obj.data.materials.append(preview_material)
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    return [top, center, bottom]


def setup_camera(location, rotation, ortho_scale):
    bpy.ops.object.camera_add(location=location, rotation=rotation)
    camera = bpy.context.object
    bpy.context.scene.camera = camera
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho_scale
    return camera


def setup_lighting():
    bpy.ops.object.light_add(type="AREA", location=(-1.4, -1.6, 1.5))
    key = bpy.context.object
    key.data.energy = 250
    key.data.shape = "DISK"
    key.data.size = 1.6
    key.rotation_euler = (math.radians(35), 0.0, math.radians(-35))
    bpy.ops.object.light_add(type="AREA", location=(1.2, -1.2, 0.5))
    fill = bpy.context.object
    fill.data.energy = 80
    fill.data.size = 1.2
    fill.rotation_euler = (math.radians(80), 0.0, math.radians(135))
    bpy.ops.object.light_add(type="AREA", location=(0.0, 0.6, 1.6))
    rim = bpy.context.object
    rim.data.energy = 120
    rim.data.size = 1.0
    rim.rotation_euler = (math.radians(25), 0.0, math.radians(180))


def render(camera, name):
    bpy.context.scene.camera = camera
    bpy.context.scene.render.filepath = os.path.join(RENDER_DIR, name + ".png")
    bpy.ops.render.render(write_still=True)


os.makedirs(RENDER_DIR, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

bpy.ops.import_scene.fbx(filepath=SOURCE_FBX, use_manual_orientation=False)
mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if len(mesh_objects) != 1:
    raise RuntimeError("Expected one source mesh, got %d" % len(mesh_objects))
mask = mesh_objects[0]
mask.name = "AstroJuggernaut_Remodeled"
bpy.context.view_layer.objects.active = mask
mask.select_set(True)
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

rubber = make_material("Astro_RubberBlack", (0.003, 0.004, 0.006), 0.02, 0.68)
lens_material = make_material("Astro_LensBlack", (0.0005, 0.001, 0.002), 0.18, 0.22)
mask.data.materials.clear()
mask.data.materials.append(rubber)
mask.data.materials.append(lens_material)
for polygon in mask.data.polygons:
    polygon.material_index = 0
    polygon.use_smooth = True

lens_backing = make_lens_backing()
lens_backing.data.materials.append(lens_material)
for polygon in lens_backing.data.polygons:
    polygon.use_smooth = True

# Join lenses into the actual model so Unity receives a real multi-material mesh,
# not prefab-only proxy components.
bpy.ops.object.select_all(action="DESELECT")
mask.select_set(True)
lens_backing.select_set(True)
bpy.context.view_layer.objects.active = mask
bpy.ops.object.join()
mask.name = "AstroJuggernaut_Remodeled"

# Save source stats before render/export for verification.
stats = {
    "mesh": mask.name,
    "vertices": len(mask.data.vertices),
    "polygons": len(mask.data.polygons),
    "materials": [slot.name for slot in mask.data.materials],
    "dimensions": [round(v, 5) for v in mask.dimensions],
}
print("CODEX_REMODELED_ASTRO=" + str(stats))

preview_objects = add_capsule_preview()
setup_lighting()
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 900
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.view_settings.look = 'None'
scene.view_settings.view_transform = 'Standard'
if scene.world is None:
    scene.world = bpy.data.worlds.new("PreviewWorld")
scene.world.color = (0.025, 0.028, 0.035)

front_camera = setup_camera((0.0, -2.35, -0.03), (math.radians(90), 0.0, 0.0), 1.85)
render(front_camera, "front_capsule")
front_camera.location = (1.65, -1.75, -0.04)
front_camera.rotation_euler = (math.radians(78), 0.0, math.radians(43))
render(front_camera, "three_quarter_capsule")
front_camera.location = (2.25, 0.20, -0.02)
front_camera.rotation_euler = (math.radians(90), 0.0, math.radians(90))
render(front_camera, "side_capsule")

# Exclude the preview body from the actual FBX.
bpy.ops.object.select_all(action="DESELECT")
mask.select_set(True)
bpy.context.view_layer.objects.active = mask
bpy.ops.export_scene.fbx(
    filepath=OUTPUT_FBX,
    use_selection=True,
    object_types={"MESH"},
    use_mesh_modifiers=True,
    add_leaf_bones=False,
    path_mode="AUTO",
    embed_textures=False,
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
)
print("CODEX_REMODELED_ASTRO_FBX=" + OUTPUT_FBX)
