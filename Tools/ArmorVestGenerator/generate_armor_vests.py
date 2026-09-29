from __future__ import annotations

import hashlib
import math
import random
import struct
import zlib
from dataclasses import dataclass
from pathlib import Path

from fbx_mesh_export import export_obj


ROOT = Path(__file__).resolve().parents[2]
TEXTURE_DIR = ROOT / "Assets/Art/Texture/ArmorGenerated"
MATERIAL_DIR = ROOT / "Assets/Art/Materials/GameScene/ArmorGenerated"
MESH_DIR = ROOT / "Assets/Art/Mesh/ArmorGenerated"
PREFAB_DIR = ROOT / "Assets/Prefabs/GameScene/TreasureObject/Vest"

URP_LIT_SHADER_GUID = "933532a4fcc9baf4fa0491de14d08ed7"
BUILTIN_MESH_GUID = "0000000000000000e000000000000000"

GUIDS = {
    "texture_folder": "3bd961189c15410a81545ef85869481d",
    "material_folder": "3d1c2fe2a4ac469fb055e7bcb6395bb4",
    "mesh_folder": "b0c53d4bcb3d4cf7b976d84c34dcf34f",
    "light_mesh": "cd0ea54c201f408e910175f5fc60be33",
    "assault_mesh": "58e13424aeb34ff1a2ff863b944a5fba",
    "heavy_mesh": "da5a6959e47f43cf8224f3424276ec49",
    "light_albedo": "0c5cfaf1e55d4b5a82ca36246fb92073",
    "light_normal": "210d880ab04d4a229e37d40dab239fc2",
    "assault_albedo": "e1dc43a5e1754489b03d1ac72f439f02",
    "assault_normal": "ba8b6fd96ef44daf86a34bec69d5317e",
    "heavy_albedo": "156b71f5a6ec4aebae580c51577001ad",
    "heavy_normal": "69798f53be2d44b9b367e2bbc7273978",
    "light_fabric": "645082d3c50c480cae4deae410e2e81a",
    "light_webbing": "6e163b7285f94009b2c622437cd661d9",
    "light_hardware": "536418755b85460aa96467dad3acdf8c",
    "assault_fabric": "89f160ed0a034986be66cac59c5de7c4",
    "assault_plate": "7fecc0663e244284bd750eb627816fb0",
    "assault_webbing": "fc78ff67303d45efad0e890afc24e8f4",
    "assault_hardware": "f03df31a61064befa60b3b835ec4b722",
    "heavy_fabric": "6184a49f50934290a2611f4bd957aaee",
    "heavy_plate": "dbdf9ee41b8c42b1aa867952cc86ce7c",
    "heavy_webbing": "f0be61a1619d47bfb0a63af25d71fc64",
    "heavy_hardware": "8316ca351abc49ab8d678e930ec26070",
    "armor_lv7_prefab": "f9c9463342224fb4b465e96bd1fd5ab2",
    "armor_lv8_prefab": "64c68e45d30b426c93a8736e5a49b8ea",
}


@dataclass(frozen=True)
class Part:
    name: str
    position: tuple[float, float, float]
    scale: tuple[float, float, float]
    material: str
    rotation: tuple[float, float, float] = (0.0, 0.0, 0.0)


def part(
    name: str,
    position: tuple[float, float, float],
    scale: tuple[float, float, float],
    material: str,
    rotation: tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> Part:
    return Part(name, position, scale, material, rotation)


def build_light_parts() -> list[Part]:
    parts = [
        part("FrontLowerPanel", (0, -0.10, 0.125), (0.36, 0.25, 0.07), "light_fabric"),
        part("FrontChestPanel", (0, 0.12, 0.12), (0.29, 0.22, 0.065), "light_fabric"),
        part("BackLowerPanel", (0, -0.10, -0.125), (0.36, 0.25, 0.07), "light_fabric"),
        part("BackChestPanel", (0, 0.12, -0.12), (0.29, 0.22, 0.065), "light_fabric"),
        part("LeftCummerbund", (-0.205, -0.03, 0), (0.07, 0.18, 0.27), "light_webbing"),
        part("RightCummerbund", (0.205, -0.03, 0), (0.07, 0.18, 0.27), "light_webbing"),
        part("LeftShoulderFrontStrap", (-0.12, 0.265, 0.075), (0.065, 0.18, 0.05), "light_webbing", (12, 0, 0)),
        part("RightShoulderFrontStrap", (0.12, 0.265, 0.075), (0.065, 0.18, 0.05), "light_webbing", (12, 0, 0)),
        part("LeftShoulderBridge", (-0.12, 0.34, 0), (0.065, 0.045, 0.19), "light_webbing"),
        part("RightShoulderBridge", (0.12, 0.34, 0), (0.065, 0.045, 0.19), "light_webbing"),
        part("FrontWaistWebbing", (0, -0.22, 0.17), (0.39, 0.032, 0.028), "light_webbing"),
        part("BackWaistWebbing", (0, -0.22, -0.17), (0.39, 0.032, 0.028), "light_webbing"),
        part("CenterBuckle", (0, 0.20, 0.162), (0.05, 0.035, 0.02), "light_hardware"),
        part("LeftSideBuckle", (-0.225, -0.03, 0.10), (0.026, 0.05, 0.045), "light_hardware"),
        part("RightSideBuckle", (0.225, -0.03, 0.10), (0.026, 0.05, 0.045), "light_hardware"),
    ]
    for index, y in enumerate((0.065, 0.015, -0.035)):
        parts.append(part(f"FrontMolleRow_{index + 1}", (0, y, 0.166), (0.28, 0.018, 0.018), "light_webbing"))
    for index, x in enumerate((-0.11, 0.0, 0.11)):
        parts.extend([
            part(f"MagazinePouch_{index + 1}", (x, -0.155, 0.198), (0.095, 0.135, 0.075), "light_fabric"),
            part(f"MagazinePouchFlap_{index + 1}", (x, -0.083, 0.225), (0.095, 0.03, 0.025), "light_webbing", (-18, 0, 0)),
        ])
    return parts


def build_assault_parts() -> list[Part]:
    parts = [
        part("FrontCarrier", (0, -0.04, 0.135), (0.39, 0.43, 0.075), "assault_fabric"),
        part("FrontCeramicPlate", (0, 0.06, 0.18), (0.31, 0.32, 0.055), "assault_plate"),
        part("BackCarrier", (0, -0.04, -0.135), (0.39, 0.43, 0.075), "assault_fabric"),
        part("BackCeramicPlate", (0, 0.06, -0.18), (0.31, 0.32, 0.055), "assault_plate"),
        part("LeftSidePlate", (-0.215, -0.04, 0), (0.07, 0.23, 0.26), "assault_plate"),
        part("RightSidePlate", (0.215, -0.04, 0), (0.07, 0.23, 0.26), "assault_plate"),
        part("LeftShoulderFrontStrap", (-0.13, 0.265, 0.075), (0.075, 0.18, 0.06), "assault_webbing", (10, 0, 0)),
        part("RightShoulderFrontStrap", (0.13, 0.265, 0.075), (0.075, 0.18, 0.06), "assault_webbing", (10, 0, 0)),
        part("LeftShoulderBridge", (-0.13, 0.345, 0), (0.075, 0.05, 0.21), "assault_webbing"),
        part("RightShoulderBridge", (0.13, 0.345, 0), (0.075, 0.05, 0.21), "assault_webbing"),
        part("AdminPouch", (0, 0.175, 0.225), (0.19, 0.075, 0.045), "assault_fabric"),
        part("AdminPouchFlap", (0, 0.22, 0.248), (0.19, 0.025, 0.018), "assault_webbing"),
        part("RadioPouch", (-0.205, 0.08, 0.145), (0.065, 0.16, 0.075), "assault_fabric"),
        part("RadioAntenna", (-0.21, 0.205, 0.145), (0.014, 0.17, 0.014), "assault_hardware", (0, 0, -8)),
        part("UtilityPouch", (0.205, 0.065, 0.15), (0.065, 0.13, 0.08), "assault_fabric"),
        part("BackDragHandle", (0, 0.325, -0.205), (0.12, 0.025, 0.025), "assault_webbing"),
    ]
    for index, y in enumerate((0.115, 0.065, 0.015, -0.035)):
        parts.append(part(f"FrontMolleRow_{index + 1}", (0, y, 0.218), (0.31, 0.016, 0.017), "assault_webbing"))
        parts.append(part(f"BackMolleRow_{index + 1}", (0, y, -0.218), (0.31, 0.016, 0.017), "assault_webbing"))
    for index, x in enumerate((-0.12, -0.04, 0.04, 0.12)):
        parts.extend([
            part(f"RifleMagazinePouch_{index + 1}", (x, -0.165, 0.225), (0.068, 0.145, 0.065), "assault_fabric"),
            part(f"RifleMagazineFlap_{index + 1}", (x, -0.09, 0.247), (0.068, 0.026, 0.022), "assault_webbing", (-16, 0, 0)),
        ])
    for x in (-0.12, 0.12):
        parts.append(part(f"FrontQuickRelease_{'L' if x < 0 else 'R'}", (x, 0.275, 0.18), (0.035, 0.045, 0.025), "assault_hardware"))
    return parts


def build_heavy_parts() -> list[Part]:
    parts = [
        part("FrontCarrier", (0, -0.035, 0.145), (0.40, 0.46, 0.085), "heavy_fabric"),
        part("FrontCompositePlate", (0, 0.055, 0.20), (0.34, 0.35, 0.075), "heavy_plate"),
        part("BackCarrier", (0, -0.035, -0.145), (0.40, 0.46, 0.085), "heavy_fabric"),
        part("BackCompositePlate", (0, 0.055, -0.20), (0.34, 0.35, 0.075), "heavy_plate"),
        part("AbdomenPlate", (0, -0.255, 0.19), (0.30, 0.12, 0.075), "heavy_plate"),
        part("LeftSidePlate", (-0.225, -0.035, 0), (0.085, 0.27, 0.29), "heavy_plate"),
        part("RightSidePlate", (0.225, -0.035, 0), (0.085, 0.27, 0.29), "heavy_plate"),
        part("LeftShoulderFrontStrap", (-0.13, 0.27, 0.08), (0.085, 0.19, 0.07), "heavy_webbing", (10, 0, 0)),
        part("RightShoulderFrontStrap", (0.13, 0.27, 0.08), (0.085, 0.19, 0.07), "heavy_webbing", (10, 0, 0)),
        part("LeftShoulderBridge", (-0.13, 0.355, 0), (0.085, 0.055, 0.22), "heavy_webbing"),
        part("RightShoulderBridge", (0.13, 0.355, 0), (0.085, 0.055, 0.22), "heavy_webbing"),
        part("LeftFrontCollar", (-0.075, 0.285, 0.16), (0.12, 0.09, 0.045), "heavy_plate", (0, 0, -10)),
        part("RightFrontCollar", (0.075, 0.285, 0.16), (0.12, 0.09, 0.045), "heavy_plate", (0, 0, 10)),
        part("RearCollar", (0, 0.30, -0.17), (0.24, 0.08, 0.05), "heavy_plate"),
        part("LeftLowerUtilityPouch", (-0.225, -0.18, 0.11), (0.09, 0.14, 0.10), "heavy_fabric"),
        part("RightLowerUtilityPouch", (0.225, -0.18, 0.11), (0.09, 0.14, 0.10), "heavy_fabric"),
        part("BackDragHandle", (0, 0.345, -0.235), (0.13, 0.028, 0.028), "heavy_webbing"),
        part("CenterQuickRelease", (0, 0.245, 0.248), (0.055, 0.05, 0.03), "heavy_hardware"),
    ]
    for index, y in enumerate((0.13, 0.08, 0.03, -0.02)):
        parts.append(part(f"FrontMolleRow_{index + 1}", (0, y, 0.25), (0.33, 0.018, 0.02), "heavy_webbing"))
        parts.append(part(f"BackMolleRow_{index + 1}", (0, y, -0.25), (0.33, 0.018, 0.02), "heavy_webbing"))
    for index, x in enumerate((-0.11, 0, 0.11)):
        parts.extend([
            part(f"ArmoredMagazinePouch_{index + 1}", (x, -0.16, 0.26), (0.095, 0.15, 0.078), "heavy_fabric"),
            part(f"ArmoredMagazineFlap_{index + 1}", (x, -0.082, 0.287), (0.095, 0.03, 0.024), "heavy_webbing", (-15, 0, 0)),
        ])
    for x in (-0.135, 0.135):
        side = "L" if x < 0 else "R"
        parts.append(part(f"VerticalReinforcement_{side}", (x, 0.065, 0.252), (0.028, 0.33, 0.025), "heavy_hardware"))
        parts.append(part(f"ShoulderQuickRelease_{side}", (x, 0.275, 0.215), (0.04, 0.05, 0.03), "heavy_hardware"))
    return parts


def stable_file_id(*values: str) -> int:
    digest = hashlib.blake2b("|".join(values).encode("utf-8"), digest_size=8).digest()
    return 1_000_000_000_000_000_000 + int.from_bytes(digest, "little") % 7_500_000_000_000_000_000


def quaternion(euler: tuple[float, float, float]) -> tuple[float, float, float, float]:
    x, y, z = (math.radians(value) * 0.5 for value in euler)
    sx, cx = math.sin(x), math.cos(x)
    sy, cy = math.sin(y), math.cos(y)
    sz, cz = math.sin(z), math.cos(z)
    return (
        sx * cy * cz + cx * sy * sz,
        cx * sy * cz - sx * cy * sz,
        cx * cy * sz + sx * sy * cz,
        cx * cy * cz - sx * sy * sz,
    )


def f(value: float) -> str:
    if abs(value) < 0.0000005:
        return "0"
    return f"{value:.7g}"


def vec3(values: tuple[float, float, float]) -> str:
    return f"{{x: {f(values[0])}, y: {f(values[1])}, z: {f(values[2])}}}"


def rotate_vector(
    value: tuple[float, float, float],
    rotation: tuple[float, float, float],
) -> tuple[float, float, float]:
    x, y, z, w = quaternion(rotation)
    matrix = (
        (1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)),
        (2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)),
        (2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)),
    )
    return tuple(sum(matrix[row][column] * value[column] for column in range(3)) for row in range(3))


def build_combined_mesh(
    parts: list[Part],
    material_order: list[str],
) -> tuple[
    list[tuple[float, float, float]],
    list[tuple[float, float, float]],
    list[tuple[float, float, float, float]],
    list[tuple[float, float]],
    list[list[int]],
    tuple[float, float, float],
    tuple[float, float, float],
]:
    # Each face gets distinct vertices for crisp low-poly edges and stable box UVs.
    faces = (
        (((-1, -1, 1), (1, -1, 1), (1, 1, 1), (-1, 1, 1)), (0, 0, 1), (1, 0, 0)),
        (((1, -1, -1), (-1, -1, -1), (-1, 1, -1), (1, 1, -1)), (0, 0, -1), (-1, 0, 0)),
        (((1, -1, 1), (1, -1, -1), (1, 1, -1), (1, 1, 1)), (1, 0, 0), (0, 0, -1)),
        (((-1, -1, -1), (-1, -1, 1), (-1, 1, 1), (-1, 1, -1)), (-1, 0, 0), (0, 0, 1)),
        (((-1, 1, 1), (1, 1, 1), (1, 1, -1), (-1, 1, -1)), (0, 1, 0), (1, 0, 0)),
        (((-1, -1, -1), (1, -1, -1), (1, -1, 1), (-1, -1, 1)), (0, -1, 0), (1, 0, 0)),
    )
    uvs = ((0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0))
    vertices: list[tuple[float, float, float]] = []
    normals: list[tuple[float, float, float]] = []
    tangents: list[tuple[float, float, float, float]] = []
    vertex_uvs: list[tuple[float, float]] = []
    submesh_indices = [[] for _ in material_order]
    material_lookup = {material: index for index, material in enumerate(material_order)}

    for mesh_part in parts:
        half = tuple(axis * 0.5 for axis in mesh_part.scale)
        target_indices = submesh_indices[material_lookup[mesh_part.material]]
        for corners, local_normal, local_tangent in faces:
            first_vertex = len(vertices)
            for corner, uv in zip(corners, uvs):
                local = (
                    corner[0] * half[0],
                    corner[1] * half[1],
                    corner[2] * half[2],
                )
                rotated = rotate_vector(local, mesh_part.rotation)
                vertices.append(tuple(rotated[index] + mesh_part.position[index] for index in range(3)))
                normals.append(rotate_vector(local_normal, mesh_part.rotation))
                tangent = rotate_vector(local_tangent, mesh_part.rotation)
                tangents.append((tangent[0], tangent[1], tangent[2], 1.0))
                vertex_uvs.append(uv)
            target_indices.extend((
                first_vertex,
                first_vertex + 1,
                first_vertex + 2,
                first_vertex,
                first_vertex + 2,
                first_vertex + 3,
            ))

    minimum = tuple(min(vertex[index] for vertex in vertices) for index in range(3))
    maximum = tuple(max(vertex[index] for vertex in vertices) for index in range(3))
    center = tuple((minimum[index] + maximum[index]) * 0.5 for index in range(3))
    extent = tuple((maximum[index] - minimum[index]) * 0.5 for index in range(3))
    return vertices, normals, tangents, vertex_uvs, submesh_indices, center, extent


def write_mesh_asset(
    path: Path,
    mesh_name: str,
    parts: list[Part],
    material_order: list[str],
) -> None:
    vertices, normals, tangents, uvs, submeshes, center, extent = build_combined_mesh(parts, material_order)
    if len(vertices) > 65535:
        raise ValueError(f"{mesh_name} exceeds the UInt16 index limit")

    index_buffer = bytearray()
    submesh_yaml = []
    index_offset = 0
    for indices in submeshes:
        submesh_yaml.extend([
            "  - serializedVersion: 2",
            f"    firstByte: {index_offset * 2}",
            f"    indexCount: {len(indices)}",
            "    topology: 0",
            "    firstVertex: 0",
            f"    vertexCount: {len(vertices)}",
            "    localAABB:",
            f"      m_Center: {vec3(center)}",
            f"      m_Extent: {vec3(extent)}",
        ])
        for index in indices:
            index_buffer.extend(struct.pack("<H", index))
        index_offset += len(indices)

    vertex_buffer = bytearray()
    for position, normal, tangent, uv in zip(vertices, normals, tangents, uvs):
        vertex_buffer.extend(struct.pack(
            "<3f3f4f2f",
            position[0], position[1], position[2],
            normal[0], normal[1], normal[2],
            tangent[0], tangent[1], tangent[2], tangent[3],
            uv[0], uv[1],
        ))

    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!43 &4300000",
        "Mesh:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        f"  m_Name: {mesh_name}",
        "  serializedVersion: 8",
        "  m_SubMeshes:",
    ]
    lines.extend(submesh_yaml)
    lines.extend([
        "  m_Shapes:",
        "    vertices: []",
        "    shapes: []",
        "    channels: []",
        "    fullWeights: []",
        "  m_BindPose: []",
        "  m_BoneNameHashes:",
        "  m_RootBoneNameHash: 0",
        "  m_MeshCompression: 0",
        "  m_IsReadable: 0",
        "  m_KeepVertices: 0",
        "  m_KeepIndices: 0",
        f"  m_IndexBuffer: {index_buffer.hex()}",
        "  m_Skin: []",
        "  m_VertexData:",
        "    m_CurrentChannels: 23",
        f"    m_VertexCount: {len(vertices)}",
        "    m_Channels:",
        "    - stream: 0",
        "      offset: 0",
        "      format: 0",
        "      dimension: 3",
        "    - stream: 0",
        "      offset: 12",
        "      format: 0",
        "      dimension: 3",
        "    - stream: 0",
        "      offset: 24",
        "      format: 0",
        "      dimension: 4",
        "    - stream: 0",
        "      offset: 0",
        "      format: 0",
        "      dimension: 0",
        "    - stream: 0",
        "      offset: 40",
        "      format: 0",
        "      dimension: 2",
        "    - stream: 0",
        "      offset: 0",
        "      format: 0",
        "      dimension: 0",
        "    - stream: 0",
        "      offset: 0",
        "      format: 0",
        "      dimension: 0",
        "    - stream: 0",
        "      offset: 0",
        "      format: 0",
        "      dimension: 0",
        f"    m_DataSize: {len(vertex_buffer)}",
        f"    _typelessdata: {vertex_buffer.hex()}",
        "  m_CompressedMesh:",
        "    m_Vertices:",
        "      m_NumItems: 0",
        "      m_Range: 0",
        "      m_Start: 0",
        "      m_Data:",
        "      m_BitSize: 0",
        "    m_UV:",
        "      m_NumItems: 0",
        "      m_Range: 0",
        "      m_Start: 0",
        "      m_Data:",
        "      m_BitSize: 0",
        "    m_Normals:",
        "      m_NumItems: 0",
        "      m_Range: 0",
        "      m_Start: 0",
        "      m_Data:",
        "      m_BitSize: 0",
        "    m_Tangents:",
        "      m_NumItems: 0",
        "      m_Range: 0",
        "      m_Start: 0",
        "      m_Data:",
        "      m_BitSize: 0",
        "    m_Weights:",
        "      m_NumItems: 0",
        "      m_Data:",
        "      m_BitSize: 0",
        "    m_NormalSigns:",
        "      m_NumItems: 0",
        "      m_Data:",
        "      m_BitSize: 0",
        "    m_TangentSigns:",
        "      m_NumItems: 0",
        "      m_Data:",
        "      m_BitSize: 0",
        "    m_FloatColors:",
        "      m_NumItems: 0",
        "      m_Range: 0",
        "      m_Start: 0",
        "      m_Data:",
        "      m_BitSize: 0",
        "    m_BoneIndices:",
        "      m_NumItems: 0",
        "      m_Data:",
        "      m_BitSize: 0",
        "    m_Triangles:",
        "      m_NumItems: 0",
        "      m_Data:",
        "      m_BitSize: 0",
        "    m_UVInfo: 0",
        f"  m_LocalAABB:\n    m_Center: {vec3(center)}\n    m_Extent: {vec3(extent)}",
        "  m_MeshUsageFlags: 0",
        "  m_BakedConvexCollisionMesh:",
        "  m_BakedTriangleCollisionMesh:",
        "  m_MeshOptimized: 0",
    ])
    path.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")


def write_combined_prefab(
    path: Path,
    prefab_name: str,
    mesh_guid: str,
    material_order: list[str],
    root_game_object_id: int | None = None,
    root_transform_id: int | None = None,
    mesh_filter_id: int | None = None,
    mesh_renderer_id: int | None = None,
) -> None:
    root_game_object_id = root_game_object_id or stable_file_id(prefab_name, "root", "go")
    root_transform_id = root_transform_id or stable_file_id(prefab_name, "root", "transform")
    mesh_filter_id = mesh_filter_id or stable_file_id(prefab_name, "root", "filter")
    mesh_renderer_id = mesh_renderer_id or stable_file_id(prefab_name, "root", "renderer")
    materials = "\n".join(
        f"  - {{fileID: 2100000, guid: {GUIDS[role]}, type: 2}}"
        for role in material_order
    )
    content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &{root_game_object_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {root_transform_id}}}
  - component: {{fileID: {mesh_filter_id}}}
  - component: {{fileID: {mesh_renderer_id}}}
  m_Layer: 0
  m_Name: {prefab_name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{root_transform_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_game_object_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!33 &{mesh_filter_id}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_game_object_id}}}
  m_Mesh: {{fileID: 4300000, guid: {mesh_guid}, type: 2}}
--- !u!23 &{mesh_renderer_id}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_game_object_id}}}
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
{materials}
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
    path.write_text(content, encoding="utf-8", newline="\n")


def write_prefab(
    path: Path,
    prefab_name: str,
    parts: list[Part],
    root_game_object_id: int | None = None,
    root_transform_id: int | None = None,
) -> None:
    root_game_object_id = root_game_object_id or stable_file_id(prefab_name, "root", "go")
    root_transform_id = root_transform_id or stable_file_id(prefab_name, "root", "transform")
    ids = []
    for item in parts:
        ids.append((
            stable_file_id(prefab_name, item.name, "go"),
            stable_file_id(prefab_name, item.name, "transform"),
            stable_file_id(prefab_name, item.name, "filter"),
            stable_file_id(prefab_name, item.name, "renderer"),
        ))

    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        f"--- !u!1 &{root_game_object_id}",
        "GameObject:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  serializedVersion: 6",
        "  m_Component:",
        f"  - component: {{fileID: {root_transform_id}}}",
        "  m_Layer: 0",
        f"  m_Name: {prefab_name}",
        "  m_TagString: Untagged",
        "  m_Icon: {fileID: 0}",
        "  m_NavMeshLayer: 0",
        "  m_StaticEditorFlags: 0",
        "  m_IsActive: 1",
        f"--- !u!4 &{root_transform_id}",
        "Transform:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        f"  m_GameObject: {{fileID: {root_game_object_id}}}",
        "  serializedVersion: 2",
        "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}",
        "  m_LocalPosition: {x: 0, y: 0, z: 0}",
        "  m_LocalScale: {x: 1, y: 1, z: 1}",
        "  m_ConstrainProportionsScale: 0",
        "  m_Children:",
    ]
    lines.extend(f"  - {{fileID: {item_ids[1]}}}" for item_ids in ids)
    lines.extend([
        "  m_Father: {fileID: 0}",
        "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}",
    ])

    for item, (go_id, transform_id, filter_id, renderer_id) in zip(parts, ids):
        qx, qy, qz, qw = quaternion(item.rotation)
        lines.extend([
            f"--- !u!1 &{go_id}",
            "GameObject:",
            "  m_ObjectHideFlags: 0",
            "  m_CorrespondingSourceObject: {fileID: 0}",
            "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}",
            "  serializedVersion: 6",
            "  m_Component:",
            f"  - component: {{fileID: {transform_id}}}",
            f"  - component: {{fileID: {filter_id}}}",
            f"  - component: {{fileID: {renderer_id}}}",
            "  m_Layer: 0",
            f"  m_Name: {item.name}",
            "  m_TagString: Untagged",
            "  m_Icon: {fileID: 0}",
            "  m_NavMeshLayer: 0",
            "  m_StaticEditorFlags: 0",
            "  m_IsActive: 1",
            f"--- !u!4 &{transform_id}",
            "Transform:",
            "  m_ObjectHideFlags: 0",
            "  m_CorrespondingSourceObject: {fileID: 0}",
            "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}",
            f"  m_GameObject: {{fileID: {go_id}}}",
            "  serializedVersion: 2",
            f"  m_LocalRotation: {{x: {f(qx)}, y: {f(qy)}, z: {f(qz)}, w: {f(qw)}}}",
            f"  m_LocalPosition: {vec3(item.position)}",
            f"  m_LocalScale: {vec3(item.scale)}",
            "  m_ConstrainProportionsScale: 0",
            "  m_Children: []",
            f"  m_Father: {{fileID: {root_transform_id}}}",
            f"  m_LocalEulerAnglesHint: {vec3(item.rotation)}",
            f"--- !u!33 &{filter_id}",
            "MeshFilter:",
            "  m_ObjectHideFlags: 0",
            "  m_CorrespondingSourceObject: {fileID: 0}",
            "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}",
            f"  m_GameObject: {{fileID: {go_id}}}",
            f"  m_Mesh: {{fileID: 10202, guid: {BUILTIN_MESH_GUID}, type: 0}}",
            f"--- !u!23 &{renderer_id}",
            "MeshRenderer:",
            "  m_ObjectHideFlags: 0",
            "  m_CorrespondingSourceObject: {fileID: 0}",
            "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}",
            f"  m_GameObject: {{fileID: {go_id}}}",
            "  m_Enabled: 1",
            "  m_CastShadows: 1",
            "  m_ReceiveShadows: 1",
            "  m_DynamicOccludee: 1",
            "  m_StaticShadowCaster: 0",
            "  m_MotionVectors: 1",
            "  m_LightProbeUsage: 1",
            "  m_ReflectionProbeUsage: 1",
            "  m_RayTracingMode: 2",
            "  m_RayTraceProcedural: 0",
            "  m_RayTracingAccelStructBuildFlagsOverride: 0",
            "  m_RayTracingAccelStructBuildFlags: 1",
            "  m_SmallMeshCulling: 1",
            "  m_RenderingLayerMask: 1",
            "  m_RendererPriority: 0",
            "  m_Materials:",
            f"  - {{fileID: 2100000, guid: {GUIDS[item.material]}, type: 2}}",
            "  m_StaticBatchInfo:",
            "    firstSubMesh: 0",
            "    subMeshCount: 0",
            "  m_StaticBatchRoot: {fileID: 0}",
            "  m_ProbeAnchor: {fileID: 0}",
            "  m_LightProbeVolumeOverride: {fileID: 0}",
            "  m_ScaleInLightmap: 1",
            "  m_ReceiveGI: 1",
            "  m_PreserveUVs: 0",
            "  m_IgnoreNormalsForChartDetection: 0",
            "  m_ImportantGI: 0",
            "  m_StitchLightmapSeams: 1",
            "  m_SelectedEditorRenderState: 3",
            "  m_MinimumChartSize: 4",
            "  m_AutoUVMaxDistance: 0.5",
            "  m_AutoUVMaxAngle: 89",
            "  m_LightmapParameters: {fileID: 0}",
            "  m_SortingLayerID: 0",
            "  m_SortingLayer: 0",
            "  m_SortingOrder: 0",
            "  m_AdditionalVertexStreams: {fileID: 0}",
        ])
    path.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")


def png_chunk(kind: bytes, data: bytes) -> bytes:
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)


def write_png(path: Path, width: int, height: int, pixels: bytes) -> None:
    raw = bytearray()
    stride = width * 4
    for y in range(height):
        raw.append(0)
        raw.extend(pixels[y * stride:(y + 1) * stride])
    data = b"\x89PNG\r\n\x1a\n"
    data += png_chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
    data += png_chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    data += png_chunk(b"IEND", b"")
    path.write_bytes(data)


def generate_texture_set(prefix: str, seed: int, warmth: float, wear: float) -> None:
    size = 256
    rng = random.Random(seed)
    heights = [0.0] * (size * size)
    albedo = bytearray(size * size * 4)
    for y in range(size):
        for x in range(size):
            weave = math.sin(x * math.pi * 0.5) * 0.55 + math.cos(y * math.pi * 0.5) * 0.45
            diagonal = math.sin((x + y) * math.pi / 8.0) * 0.18
            speck = rng.uniform(-1.0, 1.0) * 0.20
            seam = -0.32 if y % 64 in (0, 1) else 0.0
            value = weave * 0.32 + diagonal + speck + seam
            heights[y * size + x] = value
            grime = rng.random()
            scuff = wear if grime > 0.992 else 0.0
            base = 190 + int(value * 18 + scuff * 32)
            r = max(0, min(255, base + int(warmth * 9)))
            g = max(0, min(255, base + int(warmth * 4)))
            b = max(0, min(255, base - int(warmth * 8)))
            offset = (y * size + x) * 4
            albedo[offset:offset + 4] = bytes((r, g, b, 255))

    normal = bytearray(size * size * 4)
    strength = 2.1
    for y in range(size):
        for x in range(size):
            left = heights[y * size + (x - 1) % size]
            right = heights[y * size + (x + 1) % size]
            down = heights[((y - 1) % size) * size + x]
            up = heights[((y + 1) % size) * size + x]
            nx = (left - right) * strength
            ny = (down - up) * strength
            nz = 1.0
            length = math.sqrt(nx * nx + ny * ny + nz * nz)
            nx, ny, nz = nx / length, ny / length, nz / length
            offset = (y * size + x) * 4
            normal[offset:offset + 4] = bytes((
                int((nx * 0.5 + 0.5) * 255),
                int((ny * 0.5 + 0.5) * 255),
                int((nz * 0.5 + 0.5) * 255),
                255,
            ))

    write_png(TEXTURE_DIR / f"{prefix}_Albedo.png", size, size, bytes(albedo))
    write_png(TEXTURE_DIR / f"{prefix}_Normal.png", size, size, bytes(normal))


def texture_meta(guid: str, normal: bool) -> str:
    srgb = 0 if normal else 1
    texture_type = 1 if normal else 0
    alpha_usage = 0 if normal else 1
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: {srgb}
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 512
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 2
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 70
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: {alpha_usage}
  alphaIsTransparency: 0
  spriteTessellationDetail: -1
  textureType: {texture_type}
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 512
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 70
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def material_yaml(
    name: str,
    albedo_guid: str,
    normal_guid: str,
    color: tuple[float, float, float, float],
    metallic: float,
    smoothness: float,
    texture_scale: float,
) -> str:
    color_text = f"{{r: {f(color[0])}, g: {f(color[1])}, b: {f(color[2])}, a: {f(color[3])}}}"
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Shader: {{fileID: 4800000, guid: {URP_LIT_SHADER_GUID}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords:
  - _NORMALMAP
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap:
    RenderType: Opaque
  disabledShaderPasses:
  - MOTIONVECTORS
  m_LockedProperties:
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {albedo_guid}, type: 3}}
        m_Scale: {{x: {f(texture_scale)}, y: {f(texture_scale)}}}
        m_Offset: {{x: 0, y: 0}}
    - _BumpMap:
        m_Texture: {{fileID: 2800000, guid: {normal_guid}, type: 3}}
        m_Scale: {{x: {f(texture_scale)}, y: {f(texture_scale)}}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {albedo_guid}, type: 3}}
        m_Scale: {{x: {f(texture_scale)}, y: {f(texture_scale)}}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _AlphaClip: 0
    - _Blend: 0
    - _BumpScale: 0.55
    - _Cull: 2
    - _Metallic: {f(metallic)}
    - _ReceiveShadows: 1
    - _Smoothness: {f(smoothness)}
    - _Surface: 0
    - _WorkflowMode: 1
    - _ZWrite: 1
    m_Colors:
    - _BaseColor: {color_text}
    - _Color: {color_text}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
"""


def simple_meta(guid: str, importer: str, main_object_file_id: int | None = None) -> str:
    main_object = f"  mainObjectFileID: {main_object_file_id}\n" if main_object_file_id is not None else ""
    return f"""fileFormatVersion: 2
guid: {guid}
{importer}:
  externalObjects: {{}}
{main_object}  userData:
  assetBundleName:
  assetBundleVariant:
"""


def folder_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def write_text(path: Path, content: str) -> None:
    path.write_text(content, encoding="utf-8", newline="\n")


def main() -> None:
    TEXTURE_DIR.mkdir(parents=True, exist_ok=True)
    MATERIAL_DIR.mkdir(parents=True, exist_ok=True)

    write_text(TEXTURE_DIR.with_suffix(".meta"), folder_meta(GUIDS["texture_folder"]))
    write_text(MATERIAL_DIR.with_suffix(".meta"), folder_meta(GUIDS["material_folder"]))

    texture_sets = [
        ("ArmorVestAssault", "assault_albedo", "assault_normal", 2911, -0.10, 0.65),
        ("ArmorVestHeavy", "heavy_albedo", "heavy_normal", 8107, -0.35, 0.90),
    ]
    for prefix, albedo_key, normal_key, seed, warmth, wear in texture_sets:
        generate_texture_set(prefix, seed, warmth, wear)
        write_text(TEXTURE_DIR / f"{prefix}_Albedo.png.meta", texture_meta(GUIDS[albedo_key], False))
        write_text(TEXTURE_DIR / f"{prefix}_Normal.png.meta", texture_meta(GUIDS[normal_key], True))

    materials = [
        ("ArmorVestAssault_Fabric", "assault_fabric", "assault_albedo", "assault_normal", (0.28, 0.34, 0.24, 1), 0.0, 0.18, 5),
        ("ArmorVestAssault_Plate", "assault_plate", "assault_albedo", "assault_normal", (0.20, 0.25, 0.18, 1), 0.04, 0.28, 4),
        ("ArmorVestAssault_Webbing", "assault_webbing", "assault_albedo", "assault_normal", (0.13, 0.16, 0.12, 1), 0.0, 0.2, 6),
        ("ArmorVestAssault_Hardware", "assault_hardware", "assault_albedo", "assault_normal", (0.10, 0.11, 0.10, 1), 0.62, 0.46, 4),
        ("ArmorVestHeavy_Fabric", "heavy_fabric", "heavy_albedo", "heavy_normal", (0.19, 0.22, 0.18, 1), 0.0, 0.16, 5),
        ("ArmorVestHeavy_Plate", "heavy_plate", "heavy_albedo", "heavy_normal", (0.105, 0.125, 0.105, 1), 0.12, 0.33, 4),
        ("ArmorVestHeavy_Webbing", "heavy_webbing", "heavy_albedo", "heavy_normal", (0.08, 0.09, 0.075, 1), 0.0, 0.2, 6),
        ("ArmorVestHeavy_Hardware", "heavy_hardware", "heavy_albedo", "heavy_normal", (0.16, 0.17, 0.16, 1), 0.72, 0.5, 4),
    ]
    for name, guid_key, albedo_key, normal_key, color, metallic, smoothness, scale in materials:
        path = MATERIAL_DIR / f"{name}.mat"
        write_text(path, material_yaml(name, GUIDS[albedo_key], GUIDS[normal_key], color, metallic, smoothness, scale))
        write_text(path.with_suffix(".mat.meta"), simple_meta(GUIDS[guid_key], "NativeFormatImporter", 2100000))

    MESH_DIR.mkdir(parents=True, exist_ok=True)
    write_text(MESH_DIR.with_suffix(".meta"), folder_meta(GUIDS["mesh_folder"]))

    # Reuse the project's authored, rounded armor geometry. ArmorLV8 removes only
    # vest5's disconnected sleeve islands; the body, straps, pouches, UVs and normals
    # remain intact. Prefab/meta files are intentionally not regenerated here so their
    # stable Unity object IDs and item references cannot be replaced by blockout parts.
    export_obj(
        ROOT / "Assets/Art/Mesh/Armor/vest4.fbx",
        MESH_DIR / "ArmorLV7_Rounded.obj",
        object_name="Vest_001",
    )
    export_obj(
        ROOT / "Assets/Art/Mesh/Armor/vest5.fbx",
        MESH_DIR / "ArmorLV8_Sleeveless.obj",
        excluded_components={3, 4, 11, 12},
        object_name="Vest_1_Prem",
    )

    print("Generated rounded ArmorLV7 and sleeveless ArmorLV8 source meshes.")


if __name__ == "__main__":
    main()
