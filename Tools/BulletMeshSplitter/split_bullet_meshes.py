"""Split selected meshes from the source bullet FBX into editable body/head OBJ files.

The project keeps the source asset intact. Generated OBJ files preserve the original
vertex positions and UVs so the body can continue using the source atlas material.
"""

from __future__ import annotations

import argparse
import math
import struct
import zlib
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Iterable


@dataclass(frozen=True)
class BulletSplitSpec:
    model_name: str
    asset_name: str
    split_y: float
    body_guid: str
    head_guid: str
    body_mesh_file_id: int
    head_mesh_file_id: int
    visual_scale: float
    head_length_scale: float = 1.0


# split_y values are existing vertex-ring heights in the source model. Keeping the
# cut on a real ring avoids open triangles and makes both pieces meet exactly.
BULLET_SPLITS = (
    BulletSplitSpec(
        "SM_Bullet_01",
        "HeavyBullet",
        116.4693,
        "b17c2da5f40a4fa19a6dc50100000001",
        "b17c2da5f40a4fa19a6dc50100000002",
        -2432090755550338912,
        -2432090755550338912,
        0.35,
    ),
    BulletSplitSpec(
        "SM_Bullet_03",
        "SniperBullet",
        76.6712,
        "b17c2da5f40a4fa19a6dc50100000003",
        "b17c2da5f40a4fa19a6dc50100000004",
        -2432090755550338912,
        -2432090755550338912,
        0.5,
        head_length_scale=1.4,
    ),
    BulletSplitSpec(
        "SM_Bullet_05",
        "MediumBullet",
        31.2676,
        "b17c2da5f40a4fa19a6dc50100000005",
        "b17c2da5f40a4fa19a6dc50100000006",
        -2432090755550338912,
        -2432090755550338912,
        0.9,
    ),
    BulletSplitSpec(
        "SM_Bullet_09",
        "ShotgunShell",
        32.7271,
        "b17c2da5f40a4fa19a6dc50100000007",
        "b17c2da5f40a4fa19a6dc50100000008",
        -2432090755550338912,
        -2432090755550338912,
        0.55,
    ),
)


@dataclass
class FbxNode:
    name: str
    properties: list[Any] = field(default_factory=list)
    children: list["FbxNode"] = field(default_factory=list)

    def child(self, name: str) -> "FbxNode | None":
        return next((node for node in self.children if node.name == name), None)


class BinaryFbxReader:
    def __init__(self, data: bytes):
        if not data.startswith(b"Kaydara FBX Binary"):
            raise ValueError("Only binary FBX files are supported.")

        self.data = data
        self.version = struct.unpack_from("<I", data, 23)[0]
        self.offset = 27
        self.wide_offsets = self.version >= 7500

    def read(self) -> list[FbxNode]:
        nodes: list[FbxNode] = []
        while self.offset < len(self.data):
            node = self._read_node()
            if node is None:
                break
            nodes.append(node)
        return nodes

    def _read_node(self) -> FbxNode | None:
        header_format = "<QQQB" if self.wide_offsets else "<IIIB"
        header_size = struct.calcsize(header_format)
        values = struct.unpack_from(header_format, self.data, self.offset)
        self.offset += header_size
        end_offset, property_count, _property_bytes, name_length = values

        if end_offset == 0:
            return None

        name = self.data[self.offset : self.offset + name_length].decode("utf-8", "replace")
        self.offset += name_length
        properties = [self._read_property() for _ in range(property_count)]
        children: list[FbxNode] = []

        while self.offset < end_offset:
            child = self._read_node()
            if child is None:
                break
            children.append(child)

        self.offset = end_offset
        return FbxNode(name, properties, children)

    def _read_property(self) -> Any:
        kind = chr(self.data[self.offset])
        self.offset += 1
        scalar_formats = {
            "Y": "<h",
            "C": "<?",
            "I": "<i",
            "F": "<f",
            "D": "<d",
            "L": "<q",
        }

        if kind in scalar_formats:
            value = struct.unpack_from(scalar_formats[kind], self.data, self.offset)[0]
            self.offset += struct.calcsize(scalar_formats[kind])
            return value

        if kind in ("S", "R"):
            length = struct.unpack_from("<I", self.data, self.offset)[0]
            self.offset += 4
            value = self.data[self.offset : self.offset + length]
            self.offset += length
            return value.decode("utf-8", "replace") if kind == "S" else value

        array_formats = {
            "f": ("f", 4),
            "d": ("d", 8),
            "l": ("q", 8),
            "i": ("i", 4),
            "b": ("?", 1),
        }
        if kind not in array_formats:
            raise ValueError(f"Unsupported FBX property type: {kind}")

        count, encoding, compressed_length = struct.unpack_from("<III", self.data, self.offset)
        self.offset += 12
        payload = self.data[self.offset : self.offset + compressed_length]
        self.offset += compressed_length
        if encoding == 1:
            payload = zlib.decompress(payload)
        elif encoding != 0:
            raise ValueError(f"Unsupported FBX array encoding: {encoding}")

        element_format, element_size = array_formats[kind]
        expected_length = count * element_size
        if len(payload) != expected_length:
            raise ValueError(f"Invalid FBX array length for {kind}: {len(payload)} != {expected_length}")
        return list(struct.unpack(f"<{count}{element_format}", payload))


def find_node(nodes: Iterable[FbxNode], name: str) -> FbxNode | None:
    return next((node for node in nodes if node.name == name), None)


def geometry_nodes(nodes: list[FbxNode]) -> list[FbxNode]:
    objects = find_node(nodes, "Objects")
    if objects is None:
        return []
    return [node for node in objects.children if node.name == "Geometry" and len(node.properties) >= 3]


def object_name(node: FbxNode) -> str:
    raw_name = str(node.properties[1])
    return raw_name.split("\x00", 1)[0].split("::", 1)[-1]


def geometry_model_names(nodes: list[FbxNode]) -> dict[int, str]:
    objects = find_node(nodes, "Objects")
    connections = find_node(nodes, "Connections")
    if objects is None or connections is None:
        return {}

    model_names = {
        int(node.properties[0]): object_name(node)
        for node in objects.children
        if node.name == "Model" and len(node.properties) >= 2
    }
    result: dict[int, str] = {}
    for connection in connections.children:
        if connection.name != "C" or len(connection.properties) < 3:
            continue
        if connection.properties[0] != "OO":
            continue
        child_id = int(connection.properties[1])
        parent_id = int(connection.properties[2])
        if parent_id in model_names:
            result[child_id] = model_names[parent_id]
    return result


def model_nodes(nodes: list[FbxNode]) -> dict[str, FbxNode]:
    objects = find_node(nodes, "Objects")
    if objects is None:
        return {}
    return {
        object_name(node): node
        for node in objects.children
        if node.name == "Model" and len(node.properties) >= 2
    }


def model_pivot_xz(model: FbxNode | None) -> tuple[float, float] | None:
    properties = model.child("Properties70") if model is not None else None
    if properties is None:
        return None
    for node in properties.children:
        if node.name != "P" or len(node.properties) < 7:
            continue
        if node.properties[0] == "RotationPivot":
            return float(node.properties[4]), float(node.properties[6])
    return None


def polygon_faces(indices: list[int]) -> list[list[int]]:
    faces: list[list[int]] = []
    face: list[int] = []
    for index in indices:
        is_end = index < 0
        face.append(-index - 1 if is_end else index)
        if is_end:
            faces.append(face)
            face = []
    if face:
        faces.append(face)
    return faces


def polygon_faces_with_corners(indices: list[int]) -> list[list[tuple[int, int]]]:
    faces: list[list[tuple[int, int]]] = []
    face: list[tuple[int, int]] = []
    for corner_index, index in enumerate(indices):
        is_end = index < 0
        vertex_index = -index - 1 if is_end else index
        face.append((vertex_index, corner_index))
        if is_end:
            faces.append(face)
            face = []
    if face:
        faces.append(face)
    return faces


def child_property(node: FbxNode, child_name: str, default: Any = None) -> Any:
    child = node.child(child_name)
    if child is None or not child.properties:
        return default
    return child.properties[0]


def layer_element(geometry: FbxNode, name: str) -> FbxNode | None:
    return next((node for node in geometry.children if node.name == name), None)


def normalized(vector: tuple[float, float, float]) -> tuple[float, float, float]:
    length = math.sqrt(sum(value * value for value in vector))
    if length <= 0.000001:
        return 0.0, 1.0, 0.0
    return tuple(value / length for value in vector)


def write_obj_part(
    output_path: Path,
    mesh_name: str,
    vertices: list[tuple[float, float, float]],
    faces: list[list[tuple[int, int]]],
    uv_values: list[float],
    uv_indices: list[int],
    normal_values: list[float],
    center_xz: tuple[float, float],
    split_y: float,
    is_head: bool,
    head_length_scale: float,
) -> None:
    used_vertices = sorted({vertex_index for face in faces for vertex_index, _ in face})
    vertex_remap = {source: target + 1 for target, source in enumerate(used_vertices)}
    center_x, center_z = center_xz

    lines = [f"# Generated from WeaponsPack-Bullets.fbx by {Path(__file__).name}", f"o {mesh_name}"]
    for source_index in used_vertices:
        x, y, z = vertices[source_index]
        if is_head and head_length_scale != 1.0 and y > split_y:
            y = split_y + (y - split_y) * head_length_scale
        # FBX source units are centimetres. OBJ has no file-unit metadata, so bake
        # the conversion Unity's FBX importer previously applied.
        lines.append(f"v {(x - center_x) * 0.01:.9f} {y * 0.01:.9f} {(z - center_z) * 0.01:.9f}")

    uv_pairs = list(zip(uv_values[0::2], uv_values[1::2]))
    for u, v in uv_pairs:
        lines.append(f"vt {u:.9f} {v:.9f}")

    normal_triplets = list(zip(normal_values[0::3], normal_values[1::3], normal_values[2::3]))
    for x, y, z in normal_triplets:
        if is_head and head_length_scale != 1.0:
            x, y, z = normalized((x, y / head_length_scale, z))
        lines.append(f"vn {x:.9f} {y:.9f} {z:.9f}")

    lines.append("s 1")
    for face in faces:
        corners: list[str] = []
        for vertex_index, corner_index in face:
            uv_index = uv_indices[corner_index] + 1
            normal_index = corner_index + 1
            corners.append(f"{vertex_remap[vertex_index]}/{uv_index}/{normal_index}")
        lines.append("f " + " ".join(corners))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_obj_meta(path: Path, guid: str, mesh_file_id: int, mesh_name: str) -> None:
    meta = f"""fileFormatVersion: 2
guid: {guid}
ModelImporter:
  serializedVersion: 22200
  internalIDToNameTable:
  - first:
      43: {mesh_file_id}
    second: default
  externalObjects: {{}}
  materials:
    materialImportMode: 0
    materialName: 0
    materialSearch: 1
    materialLocation: 1
  animations:
    legacyGenerateAnimations: 4
    bakeSimulation: 0
    resampleCurves: 1
    optimizeGameObjects: 0
    removeConstantScaleCurves: 0
    motionNodeName: 
    animationImportErrors: 
    animationImportWarnings: 
    animationRetargetingWarnings: 
    animationDoRetargetingWarnings: 0
    importAnimatedCustomProperties: 0
    importConstraints: 0
    animationCompression: 1
    animationRotationError: 0.5
    animationPositionError: 0.5
    animationScaleError: 0.5
    animationWrapMode: 0
    extraExposedTransformPaths: []
    extraUserProperties: []
    clipAnimations: []
    isReadable: 0
  meshes:
    lODScreenPercentages: []
    globalScale: 1
    meshCompression: 0
    addColliders: 0
    useSRGBMaterialColor: 1
    sortHierarchyByName: 1
    importPhysicalCameras: 0
    importVisibility: 0
    importBlendShapes: 0
    importCameras: 0
    importLights: 0
    nodeNameCollisionStrategy: 1
    fileIdsGeneration: 2
    swapUVChannels: 0
    generateSecondaryUV: 0
    useFileUnits: 1
    keepQuads: 0
    weldVertices: 1
    bakeAxisConversion: 0
    preserveHierarchy: 0
    skinWeightsMode: 0
    maxBonesPerVertex: 4
    minBoneWeight: 0.001
    optimizeBones: 1
    meshOptimizationFlags: -1
    indexFormat: 0
    secondaryUVAngleDistortion: 8
    secondaryUVAreaDistortion: 15.000001
    secondaryUVHardAngle: 88
    secondaryUVMarginMethod: 1
    secondaryUVMinLightmapResolution: 40
    secondaryUVMinObjectScale: 1
    secondaryUVPackMargin: 4
    useFileScale: 1
    strictVertexDataChecks: 0
  tangentSpace:
    normalSmoothAngle: 60
    normalImportMode: 0
    tangentImportMode: 3
    normalCalculationMode: 4
    legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes: 0
    blendShapeNormalImportMode: 1
    normalSmoothingSource: 0
  referencedClips: []
  importAnimation: 0
  humanDescription:
    serializedVersion: 3
    human: []
    skeleton: []
    armTwist: 0.5
    foreArmTwist: 0.5
    upperLegTwist: 0.5
    legTwist: 0.5
    armStretch: 0.05
    legStretch: 0.05
    feetSpacing: 0
    globalScale: 1
    rootMotionBoneName: 
    hasTranslationDoF: 0
    hasExtraRoot: 0
    skeletonHasParents: 1
  lastHumanDescriptionAvatarSource: {{instanceID: 0}}
  autoGenerateAvatarMappingIfUnspecified: 1
  animationType: 0
  humanoidOversampling: 1
  avatarSetup: 0
  addHumanoidExtraRootOnlyWhenUsingAvatar: 1
  importBlendShapeDeformPercent: 1
  remapMaterialsIfMaterialImportModeIsNone: 0
  additionalBone: 0
  userData: Generated editable bullet part. Regenerate with Tools/BulletMeshSplitter/split_bullet_meshes.py.
  assetBundleName: 
  assetBundleVariant: 
"""
    path.with_suffix(path.suffix + ".meta").write_text(meta, encoding="utf-8")


def renderer_yaml(file_id: int, game_object_id: int, material_guid: str) -> str:
    return f"""--- !u!23 &{file_id}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_object_id}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: {material_guid}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
"""


def mesh_part_yaml(
    name: str,
    game_object_id: int,
    transform_id: int,
    mesh_filter_id: int,
    renderer_id: int,
    visual_transform_id: int,
    mesh_guid: str,
    mesh_file_id: int,
    material_guid: str,
) -> str:
    return f"""--- !u!1 &{game_object_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {transform_id}}}
  - component: {{fileID: {mesh_filter_id}}}
  - component: {{fileID: {renderer_id}}}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{transform_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_object_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {visual_transform_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!33 &{mesh_filter_id}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_object_id}}}
  m_Mesh: {{fileID: {mesh_file_id}, guid: {mesh_guid}, type: 3}}
""" + renderer_yaml(renderer_id, game_object_id, material_guid)


def write_standalone_prefab(prefab_dir: Path, spec: BulletSplitSpec) -> None:
    # These are deliberately ordinary prefab objects. There is no PrefabInstance
    # or m_SourcePrefab block, so the generated asset cannot become a Variant.
    root_go = 7611937310320503209
    root_transform = 7127183956105130771
    bullet_component = -2465942000021760626
    visual_go = 6765858400618962384
    visual_transform = 2431488147881566020
    body_ids = (1970341548401841480, 3515975867584503454, 667353481389275905, 8644951915767358777)
    head_ids = (810000000000000001, 820000000000000001, 830000000000000001, 840000000000000001)
    body_material_guid = "097b1f66a5fa4004b8d3e02254385fb0"
    head_material_guid = "58d93738923138048904c6c73368d3cf"

    body = mesh_part_yaml(
        spec.asset_name + "Body",
        *body_ids,
        visual_transform,
        spec.body_guid,
        spec.body_mesh_file_id,
        body_material_guid,
    )
    head = mesh_part_yaml(
        spec.asset_name + "Head",
        *head_ids,
        visual_transform,
        spec.head_guid,
        spec.head_mesh_file_id,
        head_material_guid,
    )
    scale = f"{spec.visual_scale:.6g}"
    prefab = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
{body}{head}--- !u!1 &{visual_go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {visual_transform}}}
  m_Layer: 0
  m_Name: Visual
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{visual_transform}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {visual_go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0.7071068, y: 0, z: 0, w: 0.7071068}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: {scale}, y: {scale}, z: {scale}}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: {body_ids[1]}}}
  - {{fileID: {head_ids[1]}}}
  m_Father: {{fileID: {root_transform}}}
  m_LocalEulerAnglesHint: {{x: 90, y: 0, z: 0}}
--- !u!1 &{root_go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {root_transform}}}
  - component: {{fileID: {bullet_component}}}
  m_Layer: 0
  m_Name: {spec.asset_name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{root_transform}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: {visual_transform}}}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{bullet_component}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: f9b7ed7bc840a0e428f54458efe43754, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  _debugHit: 1
"""
    prefab_dir.mkdir(parents=True, exist_ok=True)
    path = prefab_dir / f"{spec.asset_name}.prefab"
    path.write_text(prefab, encoding="utf-8")
    print(f"Generated standalone prefab {path}")


def write_standalone_prefabs(prefab_dir: Path) -> None:
    for spec in BULLET_SPLITS:
        write_standalone_prefab(prefab_dir, spec)


def export_parts(nodes: list[FbxNode], output_dir: Path) -> None:
    names = geometry_model_names(nodes)
    geometries = {names.get(int(node.properties[0]), object_name(node)): node for node in geometry_nodes(nodes)}
    models = model_nodes(nodes)

    for spec in BULLET_SPLITS:
        geometry = geometries.get(spec.model_name)
        if geometry is None:
            raise ValueError(f"Geometry not found: {spec.model_name}")

        vertex_values = child_property(geometry, "Vertices")
        polygon_indices = child_property(geometry, "PolygonVertexIndex")
        uv_layer = layer_element(geometry, "LayerElementUV")
        normal_layer = layer_element(geometry, "LayerElementNormal")
        if not vertex_values or not polygon_indices or uv_layer is None or normal_layer is None:
            raise ValueError(f"Incomplete mesh data: {spec.model_name}")
        if child_property(uv_layer, "MappingInformationType") != "ByPolygonVertex":
            raise ValueError(f"Unsupported UV mapping: {spec.model_name}")
        if child_property(uv_layer, "ReferenceInformationType") != "IndexToDirect":
            raise ValueError(f"Unsupported UV reference mode: {spec.model_name}")
        if child_property(normal_layer, "MappingInformationType") != "ByPolygonVertex":
            raise ValueError(f"Unsupported normal mapping: {spec.model_name}")
        if child_property(normal_layer, "ReferenceInformationType") != "Direct":
            raise ValueError(f"Unsupported normal reference mode: {spec.model_name}")

        vertices = list(zip(vertex_values[0::3], vertex_values[1::3], vertex_values[2::3]))
        faces = polygon_faces_with_corners(polygon_indices)
        body_faces = [face for face in faces if sum(vertices[index][1] for index, _ in face) / len(face) < spec.split_y]
        head_faces = [face for face in faces if face not in body_faces]
        if not body_faces or not head_faces:
            raise ValueError(f"Split produced an empty part: {spec.model_name}")

        pivot = model_pivot_xz(models.get(spec.model_name))
        if pivot is None:
            pivot = (
                (min(vertex[0] for vertex in vertices) + max(vertex[0] for vertex in vertices)) * 0.5,
                (min(vertex[2] for vertex in vertices) + max(vertex[2] for vertex in vertices)) * 0.5,
            )

        uv_values = child_property(uv_layer, "UV")
        uv_indices = child_property(uv_layer, "UVIndex")
        normal_values = child_property(normal_layer, "Normals")
        for part_name, part_faces, guid, mesh_file_id, is_head in (
            ("Body", body_faces, spec.body_guid, spec.body_mesh_file_id, False),
            ("Head", head_faces, spec.head_guid, spec.head_mesh_file_id, True),
        ):
            mesh_name = spec.asset_name + part_name
            path = output_dir / f"{mesh_name}.obj"
            write_obj_part(
                path,
                mesh_name,
                vertices,
                part_faces,
                uv_values,
                uv_indices,
                normal_values,
                pivot,
                spec.split_y,
                is_head,
                spec.head_length_scale,
            )
            write_obj_meta(path, guid, mesh_file_id, mesh_name)
            print(f"Exported {path} ({len(part_faces)} faces)")


def connected_face_components(faces: list[list[int]]) -> list[list[int]]:
    vertex_faces: dict[int, list[int]] = {}
    for face_index, face in enumerate(faces):
        for vertex_index in face:
            vertex_faces.setdefault(vertex_index, []).append(face_index)

    remaining = set(range(len(faces)))
    components: list[list[int]] = []
    while remaining:
        start = remaining.pop()
        component = [start]
        stack = [start]
        while stack:
            face_index = stack.pop()
            for vertex_index in faces[face_index]:
                for neighbor in vertex_faces[vertex_index]:
                    if neighbor in remaining:
                        remaining.remove(neighbor)
                        component.append(neighbor)
                        stack.append(neighbor)
        components.append(component)
    return sorted(components, key=len, reverse=True)


def report(nodes: list[FbxNode]) -> None:
    names = geometry_model_names(nodes)
    for geometry in geometry_nodes(nodes):
        vertices_node = geometry.child("Vertices")
        polygon_node = geometry.child("PolygonVertexIndex")
        if vertices_node is None or polygon_node is None:
            continue
        values = vertices_node.properties[0]
        vertices = list(zip(values[0::3], values[1::3], values[2::3]))
        faces = polygon_faces(polygon_node.properties[0])
        mins = tuple(min(vertex[axis] for vertex in vertices) for axis in range(3))
        maxs = tuple(max(vertex[axis] for vertex in vertices) for axis in range(3))
        geometry_id = int(geometry.properties[0])
        print(
            f"{names.get(geometry_id, object_name(geometry))}: vertices={len(vertices)} faces={len(faces)} "
            f"bounds=({mins})..({maxs})"
        )
        for component_index, component in enumerate(connected_face_components(faces)):
            used = {vertex_index for face_index in component for vertex_index in faces[face_index]}
            component_mins = tuple(min(vertices[index][axis] for index in used) for axis in range(3))
            component_maxs = tuple(max(vertices[index][axis] for index in used) for axis in range(3))
            print(
                f"  component {component_index}: faces={len(component)} vertices={len(used)} "
                f"bounds=({component_mins})..({component_maxs})"
            )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("fbx", type=Path)
    parser.add_argument("--report", action="store_true")
    parser.add_argument("--export-dir", type=Path)
    parser.add_argument("--prefab-dir", type=Path)
    args = parser.parse_args()
    nodes = BinaryFbxReader(args.fbx.read_bytes()).read()
    if args.report:
        report(nodes)
    if args.export_dir is not None:
        export_parts(nodes, args.export_dir)
    if args.prefab_dir is not None:
        write_standalone_prefabs(args.prefab_dir)


if __name__ == "__main__":
    main()
