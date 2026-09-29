from __future__ import annotations

import argparse
import struct
import zlib
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable


@dataclass
class FbxNode:
    name: str
    properties: list[Any]
    children: list["FbxNode"]

    def child(self, name: str) -> "FbxNode | None":
        return next((node for node in self.children if node.name == name), None)


def _read_property(data: bytes, offset: int) -> tuple[Any, int]:
    kind = chr(data[offset])
    offset += 1

    scalar_formats = {
        "Y": "<h",
        "C": "<?",
        "I": "<i",
        "F": "<f",
        "D": "<d",
        "L": "<q",
    }
    if kind in scalar_formats:
        fmt = scalar_formats[kind]
        size = struct.calcsize(fmt)
        return struct.unpack_from(fmt, data, offset)[0], offset + size

    if kind in ("S", "R"):
        length = struct.unpack_from("<I", data, offset)[0]
        offset += 4
        value = data[offset:offset + length]
        if kind == "S":
            value = value.decode("utf-8", errors="replace")
        return value, offset + length

    array_formats = {
        "f": ("f", 4),
        "d": ("d", 8),
        "l": ("q", 8),
        "i": ("i", 4),
        "b": ("b", 1),
        "c": ("b", 1),
    }
    if kind not in array_formats:
        raise ValueError(f"Unsupported FBX property type: {kind!r}")

    count, encoding, compressed_length = struct.unpack_from("<III", data, offset)
    offset += 12
    payload = data[offset:offset + compressed_length]
    offset += compressed_length
    if encoding == 1:
        payload = zlib.decompress(payload)
    elif encoding != 0:
        raise ValueError(f"Unsupported FBX array encoding: {encoding}")

    format_code, item_size = array_formats[kind]
    expected = count * item_size
    if len(payload) != expected:
        raise ValueError(f"FBX array size mismatch: kind={kind}, expected={expected}, actual={len(payload)}")
    return list(struct.unpack(f"<{count}{format_code}", payload)), offset


def _read_node(data: bytes, offset: int, version: int) -> tuple[FbxNode | None, int]:
    if version >= 7500:
        end_offset, property_count, property_length = struct.unpack_from("<QQQ", data, offset)
        header_size = 25
        name_length = data[offset + 24]
    else:
        end_offset, property_count, property_length = struct.unpack_from("<III", data, offset)
        header_size = 13
        name_length = data[offset + 12]

    if end_offset == 0:
        return None, offset + header_size

    cursor = offset + header_size
    name = data[cursor:cursor + name_length].decode("utf-8", errors="replace")
    cursor += name_length
    property_end = cursor + property_length
    properties = []
    for _ in range(property_count):
        value, cursor = _read_property(data, cursor)
        properties.append(value)
    if cursor != property_end:
        raise ValueError(f"FBX property length mismatch in node {name}")

    children = []
    null_size = 25 if version >= 7500 else 13
    while cursor < end_offset - null_size:
        child, cursor = _read_node(data, cursor, version)
        if child is None:
            break
        children.append(child)
    return FbxNode(name, properties, children), end_offset


def parse_fbx(path: Path) -> list[FbxNode]:
    data = path.read_bytes()
    if not data.startswith(b"Kaydara FBX Binary  \x00\x1a\x00"):
        raise ValueError(f"Not a binary FBX: {path}")
    version = struct.unpack_from("<I", data, 23)[0]
    cursor = 27
    nodes = []
    while cursor < len(data):
        node, next_cursor = _read_node(data, cursor, version)
        if node is None:
            break
        nodes.append(node)
        cursor = next_cursor
    return nodes


def walk(nodes: Iterable[FbxNode]) -> Iterable[FbxNode]:
    for node in nodes:
        yield node
        yield from walk(node.children)


def mesh_geometries(nodes: list[FbxNode]) -> list[FbxNode]:
    result = []
    for node in walk(nodes):
        if node.name != "Geometry" or len(node.properties) < 3:
            continue
        if node.properties[2] == "Mesh":
            result.append(node)
    return result


def geometry_name(geometry: FbxNode) -> str:
    raw = str(geometry.properties[1]) if len(geometry.properties) > 1 else "Mesh"
    # FBX object names are stored as "Name\0\1Geometry" in these source files.
    return raw.split("::", 1)[-1].split("\x00", 1)[0]


def extract_vertices(geometry: FbxNode) -> list[tuple[float, float, float]]:
    node = geometry.child("Vertices")
    if node is None or not node.properties:
        raise ValueError("FBX Geometry has no Vertices array")
    values = node.properties[0]
    return [tuple(values[index:index + 3]) for index in range(0, len(values), 3)]


def extract_polygons(geometry: FbxNode) -> list[list[int]]:
    node = geometry.child("PolygonVertexIndex")
    if node is None or not node.properties:
        raise ValueError("FBX Geometry has no PolygonVertexIndex array")
    polygons: list[list[int]] = []
    current: list[int] = []
    for encoded in node.properties[0]:
        if encoded < 0:
            current.append(~encoded)
            polygons.append(current)
            current = []
        else:
            current.append(encoded)
    if current:
        raise ValueError("FBX PolygonVertexIndex ended inside a polygon")
    return polygons


def connected_components(polygons: list[list[int]]) -> list[tuple[set[int], list[int]]]:
    parents: dict[int, int] = {}

    def find(value: int) -> int:
        parents.setdefault(value, value)
        while parents[value] != value:
            parents[value] = parents[parents[value]]
            value = parents[value]
        return value

    def union(left: int, right: int) -> None:
        left_root = find(left)
        right_root = find(right)
        if left_root != right_root:
            parents[right_root] = left_root

    for polygon in polygons:
        if not polygon:
            continue
        for vertex in polygon[1:]:
            union(polygon[0], vertex)

    component_vertices: dict[int, set[int]] = {}
    component_faces: dict[int, list[int]] = {}
    for face_index, polygon in enumerate(polygons):
        if not polygon:
            continue
        root = find(polygon[0])
        component_vertices.setdefault(root, set()).update(polygon)
        component_faces.setdefault(root, []).append(face_index)

    components = [
        (vertices, component_faces[root])
        for root, vertices in component_vertices.items()
    ]
    components.sort(key=lambda item: len(item[1]), reverse=True)
    return components


def _child_property(node: FbxNode, name: str) -> Any | None:
    child = node.child(name)
    if child is None or not child.properties:
        return None
    return child.properties[0]


def _layer_values(
    geometry: FbxNode,
    layer_name: str,
    values_name: str,
    indices_name: str,
    item_size: int,
) -> tuple[list[tuple[float, ...]], list[int] | None]:
    layer = geometry.child(layer_name)
    if layer is None:
        return [], None

    mapping = _child_property(layer, "MappingInformationType")
    reference = _child_property(layer, "ReferenceInformationType")
    if mapping != "ByPolygonVertex":
        raise ValueError(f"Unsupported {layer_name} mapping: {mapping!r}")
    if reference not in ("Direct", "IndexToDirect"):
        raise ValueError(f"Unsupported {layer_name} reference: {reference!r}")

    raw_values = _child_property(layer, values_name)
    if raw_values is None:
        return [], None
    values = [
        tuple(raw_values[index:index + item_size])
        for index in range(0, len(raw_values), item_size)
    ]

    if reference == "Direct":
        return values, None

    raw_indices = _child_property(layer, indices_name)
    if raw_indices is None:
        raise ValueError(f"{layer_name} uses IndexToDirect but has no {indices_name}")
    return values, [int(index) for index in raw_indices]


def export_obj(
    source: Path,
    destination: Path,
    excluded_components: set[int] | None = None,
    object_name: str | None = None,
    scale: float = 0.01,
) -> None:
    nodes = parse_fbx(source)
    geometries = mesh_geometries(nodes)
    if len(geometries) != 1:
        raise ValueError(f"Expected exactly one mesh geometry in {source}, found {len(geometries)}")

    geometry = geometries[0]
    vertices = extract_vertices(geometry)
    polygons = extract_polygons(geometry)
    components = connected_components(polygons)
    excluded_components = excluded_components or set()
    invalid_components = excluded_components.difference(range(len(components)))
    if invalid_components:
        raise ValueError(f"Invalid excluded component indices: {sorted(invalid_components)}")

    excluded_faces = {
        face_index
        for component_index, (_, face_indices) in enumerate(components)
        if component_index in excluded_components
        for face_index in face_indices
    }
    selected_faces = [
        face_index
        for face_index in range(len(polygons))
        if face_index not in excluded_faces
    ]
    used_vertex_indices = sorted({
        vertex_index
        for face_index in selected_faces
        for vertex_index in polygons[face_index]
    })
    vertex_remap = {
        source_index: destination_index + 1
        for destination_index, source_index in enumerate(used_vertex_indices)
    }

    normals, normal_indices = _layer_values(
        geometry,
        "LayerElementNormal",
        "Normals",
        "NormalsIndex",
        3,
    )
    uvs, uv_indices = _layer_values(
        geometry,
        "LayerElementUV",
        "UV",
        "UVIndex",
        2,
    )

    corner_offsets: list[int] = []
    corner_offset = 0
    for polygon in polygons:
        corner_offsets.append(corner_offset)
        corner_offset += len(polygon)

    if normal_indices is not None and len(normal_indices) != corner_offset:
        raise ValueError("Normal index count does not match polygon corner count")
    if uv_indices is not None and len(uv_indices) != corner_offset:
        raise ValueError("UV index count does not match polygon corner count")
    if normal_indices is None and normals and len(normals) != corner_offset:
        raise ValueError("Direct normal count does not match polygon corner count")
    if uv_indices is None and uvs and len(uvs) != corner_offset:
        raise ValueError("Direct UV count does not match polygon corner count")

    lines = [
        "# Generated from the project's original rounded armor FBX.",
        f"# Source: {source.as_posix()}",
        f"# Excluded connected components: {','.join(map(str, sorted(excluded_components))) or 'none'}",
        f"o {object_name or geometry_name(geometry)}",
    ]
    for source_index in used_vertex_indices:
        x, y, z = vertices[source_index]
        lines.append(f"v {x * scale:.9g} {y * scale:.9g} {z * scale:.9g}")
    for u, v in uvs:
        lines.append(f"vt {u:.9g} {v:.9g}")
    for x, y, z in normals:
        lines.append(f"vn {x:.9g} {y:.9g} {z:.9g}")
    lines.append("s 1")

    for face_index in selected_faces:
        polygon = polygons[face_index]
        corner_start = corner_offsets[face_index]
        face_tokens: list[str] = []
        for local_corner, source_vertex_index in enumerate(polygon):
            corner_index = corner_start + local_corner
            vertex_index = vertex_remap[source_vertex_index]
            uv_index = 0
            normal_index = 0
            if uvs:
                uv_index = (uv_indices[corner_index] if uv_indices is not None else corner_index) + 1
            if normals:
                normal_index = (normal_indices[corner_index] if normal_indices is not None else corner_index) + 1

            if uv_index and normal_index:
                face_tokens.append(f"{vertex_index}/{uv_index}/{normal_index}")
            elif uv_index:
                face_tokens.append(f"{vertex_index}/{uv_index}")
            elif normal_index:
                face_tokens.append(f"{vertex_index}//{normal_index}")
            else:
                face_tokens.append(str(vertex_index))

        # Unity triangulates imported OBJ polygons, but explicit triangles keep the
        # result deterministic and preserve each polygon corner's UV/normal index.
        for triangle_index in range(1, len(face_tokens) - 1):
            lines.append(f"f {face_tokens[0]} {face_tokens[triangle_index]} {face_tokens[triangle_index + 1]}")

    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")

    selected_vertices = [vertices[index] for index in used_vertex_indices]
    minimum = tuple(min(vertex[axis] for vertex in selected_vertices) * scale for axis in range(3))
    maximum = tuple(max(vertex[axis] for vertex in selected_vertices) * scale for axis in range(3))
    print(
        f"Exported {destination}: vertices={len(used_vertex_indices)}, "
        f"source polygons={len(selected_faces)}, excluded polygons={len(excluded_faces)}, "
        f"bounds min={tuple(round(value, 4) for value in minimum)}, "
        f"max={tuple(round(value, 4) for value in maximum)}"
    )


def component_summary(
    vertices: list[tuple[float, float, float]],
    components: list[tuple[set[int], list[int]]],
) -> list[str]:
    lines = []
    for index, (indices, faces) in enumerate(components):
        minimum = tuple(min(vertices[item][axis] for item in indices) for axis in range(3))
        maximum = tuple(max(vertices[item][axis] for item in indices) for axis in range(3))
        center = tuple((minimum[axis] + maximum[axis]) * 0.5 for axis in range(3))
        size = tuple(maximum[axis] - minimum[axis] for axis in range(3))
        lines.append(
            f"component={index:02d} faces={len(faces):5d} vertices={len(indices):5d} "
            f"center={tuple(round(value, 3) for value in center)} "
            f"size={tuple(round(value, 3) for value in size)}"
        )
    return lines


def inspect(path: Path) -> None:
    nodes = parse_fbx(path)
    geometries = mesh_geometries(nodes)
    print(f"{path}: mesh geometries={len(geometries)}")
    for geometry in geometries:
        vertices = extract_vertices(geometry)
        polygons = extract_polygons(geometry)
        components = connected_components(polygons)
        print(f"geometry={geometry_name(geometry)!r}, vertices={len(vertices)}, polygons={len(polygons)}, components={len(components)}")
        for line in component_summary(vertices, components):
            print("  " + line)
        print("  children=" + ", ".join(child.name for child in geometry.children))


def main() -> None:
    parser = argparse.ArgumentParser(description="Inspect or export a binary FBX armor mesh.")
    parser.add_argument("fbx", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--exclude-components", default="")
    parser.add_argument("--object-name")
    parser.add_argument("--scale", type=float, default=0.01)
    args = parser.parse_args()
    if args.output is None:
        inspect(args.fbx)
        return

    excluded_components = {
        int(value.strip())
        for value in args.exclude_components.split(",")
        if value.strip()
    }
    export_obj(
        args.fbx,
        args.output,
        excluded_components,
        args.object_name,
        args.scale,
    )


if __name__ == "__main__":
    main()
