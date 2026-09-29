"""Normalize and reshape the Plate carrier 1221 FBX without changing its Unity GUID.

The source file was exported from 3ds Max in inch units with Z-up geometry,
per-model pre-rotations, and a geometric offset. ArmorLV7 then compensated for
that with a -90 degree prefab rotation and a separate pouch transform. This tool
bakes those compensations into the two mesh vertex buffers, converts the asset
to centimetre/Y-up coordinates, straightens the vest in side view, and moves the
two shoulder-strap islands outward for the capsule character.

Only the FBX is rewritten. The adjacent Unity .meta file is intentionally left
untouched so its GUID and MeshFilter references remain stable.
"""

import argparse
import math
import shutil
import struct
import zlib
from pathlib import Path

from inspect_binary_fbx import ARRAY_FORMATS, SCALAR_FORMATS, BinaryFbxReader


SOURCE_UNIT_SCALE = 2.54
SIDE_SHEAR = 0.15
DEPTH_SCALE = 1.12
STRAP_OUTWARD_CM = 10.0
SIDE_BAND_START_CM = 34.0
SIDE_BAND_EXTRA_CM = 5.0

# These are the compensating transforms that were stored in ArmorLV7.prefab.
POUCH_LOCAL_POSITION_M = (-0.05, -0.55, -0.30)
POUCH_LOCAL_SCALE = 1.5


def property_values(node):
    return [item.value for item in node.properties]


def clean_name(value):
    return str(value).replace("\x00\x01", "::")


def find_property_node(owner, property_name):
    properties70 = owner.child("Properties70")
    if properties70 is None:
        return None
    for node in properties70.children:
        if node.name == "P" and node.properties and node.properties[0].value == property_name:
            return node
    return None


def get_property_values(owner, property_name, default):
    node = find_property_node(owner, property_name)
    if node is None:
        return tuple(default)
    return tuple(item.value for item in node.properties[4:])


def set_property_values(owner, property_name, values):
    node = find_property_node(owner, property_name)
    if node is None:
        return False
    if len(node.properties) - 4 != len(values):
        raise ValueError(f"Unexpected value count for {property_name}.")
    for item, value in zip(node.properties[4:], values):
        item.value = value
        item.modified = True
    return True


def find_mesh_connections(nodes):
    objects = next(node for node in nodes if node.name == "Objects")
    connections = next(node for node in nodes if node.name == "Connections")
    by_id = {}
    for node in objects.children:
        if node.properties:
            by_id[node.properties[0].value] = node

    geometry_to_model = {}
    for node in connections.children:
        values = property_values(node)
        if node.name != "C" or len(values) < 3 or values[0] != "OO":
            continue
        child = by_id.get(values[1])
        parent = by_id.get(values[2])
        if child is not None and parent is not None and child.name == "Geometry" and parent.name == "Model":
            geometry_to_model[values[1]] = parent
    return objects, geometry_to_model


def connected_components(geometry):
    vertices = geometry.child("Vertices").properties[0].value
    vertex_count = len(vertices) // 3
    polygon_indices = geometry.child("PolygonVertexIndex").properties[0].value
    adjacency = [set() for _ in range(vertex_count)]
    polygon = []
    for raw_index in polygon_indices:
        index = -raw_index - 1 if raw_index < 0 else raw_index
        polygon.append(index)
        if raw_index < 0:
            for first, second in zip(polygon, polygon[1:] + polygon[:1]):
                adjacency[first].add(second)
                adjacency[second].add(first)
            polygon.clear()

    result = []
    visited = set()
    for start in range(vertex_count):
        if start in visited:
            continue
        stack = [start]
        visited.add(start)
        component = []
        while stack:
            current = stack.pop()
            component.append(current)
            for neighbour in adjacency[current]:
                if neighbour not in visited:
                    visited.add(neighbour)
                    stack.append(neighbour)
        result.append(component)
    result.sort(key=len, reverse=True)
    return result


def rotate_x_minus_90(point):
    x, y, z = point
    return x, z, -y


def normalize_plate_vertices(geometry, model):
    geometric_translation = get_property_values(
        model, "GeometricTranslation", (0.0, 0.0, 0.0)
    )
    values = geometry.child("Vertices").properties[0].value
    result = []
    for x, y, z in zip(values[0::3], values[1::3], values[2::3]):
        translated = (
            x + geometric_translation[0],
            y + geometric_translation[1],
            z + geometric_translation[2],
        )
        upright = rotate_x_minus_90(translated)
        result.append(tuple(component * SOURCE_UNIT_SCALE for component in upright))
    return result


def normalize_pouch_vertices(geometry):
    values = geometry.child("Vertices").properties[0].value
    local_position_cm = tuple(component * 100.0 for component in POUCH_LOCAL_POSITION_M)
    result = []
    for x, y, z in zip(values[0::3], values[1::3], values[2::3]):
        scaled = tuple(
            component * SOURCE_UNIT_SCALE * POUCH_LOCAL_SCALE
            for component in (x, y, z)
        )
        local = tuple(
            local_position_cm[index] + scaled[index] for index in range(3)
        )
        result.append(rotate_x_minus_90(local))
    return result


def reshape_plate(points, components):
    if len(components) < 3:
        raise ValueError("Expected the vest body plus two separate shoulder-strap islands.")

    strap_components = components[1:3]
    strap_offsets = {}
    for component in strap_components:
        center_x = sum(points[index][0] for index in component) / len(component)
        offset = STRAP_OUTWARD_CM if center_x >= 0.0 else -STRAP_OUTWARD_CM
        for index in component:
            strap_offsets[index] = offset

    reshaped = []
    for index, (x, y, z) in enumerate(points):
        x += strap_offsets.get(index, 0.0)

        # Open only the lateral wrap/strap area. The centre plate and its pouches
        # keep their proportions instead of receiving another global X scale.
        lateral = max(0.0, min(1.0, (abs(x) - SIDE_BAND_START_CM) / 12.0))
        x += math.copysign(SIDE_BAND_EXTRA_CM * lateral, x) if x != 0.0 else 0.0

        # The original front and back panels lean together in side view. This
        # shear cancels that shared slope while the modest depth expansion makes
        # the carrier sit around the capsule without scaling the whole vest.
        z = (z + SIDE_SHEAR * y) * DEPTH_SCALE
        reshaped.append((x, y, z))
    return reshaped


def reshape_pouches(points):
    return [(x, y, (z + SIDE_SHEAR * y) * DEPTH_SCALE) for x, y, z in points]


def transform_normals(geometry):
    layer = geometry.child("LayerElementNormal")
    if layer is None:
        return
    normals_node = layer.child("Normals")
    if normals_node is None or not normals_node.properties:
        return

    values = normals_node.properties[0].value
    transformed = []
    for x, y, z in zip(values[0::3], values[1::3], values[2::3]):
        x, y, z = rotate_x_minus_90((x, y, z))
        # Inverse-transpose for z' = DEPTH_SCALE * (z + SIDE_SHEAR * y).
        y = y - SIDE_SHEAR * z
        z = z / DEPTH_SCALE
        magnitude = math.sqrt(x * x + y * y + z * z)
        if magnitude > 1e-8:
            x, y, z = x / magnitude, y / magnitude, z / magnitude
        transformed.extend((x, y, z))
    prop = normals_node.properties[0]
    prop.value = transformed
    prop.modified = True


def set_vertices(geometry, points):
    prop = geometry.child("Vertices").properties[0]
    prop.value = [component for point in points for component in point]
    prop.modified = True


def bounds(points):
    minimum = tuple(min(point[axis] for point in points) for axis in range(3))
    maximum = tuple(max(point[axis] for point in points) for axis in range(3))
    size = tuple(maximum[axis] - minimum[axis] for axis in range(3))
    return minimum, maximum, size


def encode_property(prop):
    if not prop.modified:
        return prop.encoded
    kind = prop.kind
    if kind in SCALAR_FORMATS:
        return kind.encode("ascii") + struct.pack(SCALAR_FORMATS[kind], prop.value)
    if kind in ("S", "R"):
        raw = prop.value.encode("utf-8") if kind == "S" else bytes(prop.value)
        return kind.encode("ascii") + struct.pack("<I", len(raw)) + raw
    if kind in ARRAY_FORMATS:
        element_format, _ = ARRAY_FORMATS[kind]
        values = prop.value
        raw = struct.pack(f"<{len(values)}{element_format}", *values)
        compressed = zlib.compress(raw, 9)
        return (
            kind.encode("ascii")
            + struct.pack("<III", len(values), 1, len(compressed))
            + compressed
        )
    raise ValueError(f"Cannot encode FBX property type {kind!r}.")


class BinaryFbxWriter:
    def __init__(self, reader):
        self.reader = reader
        self.wide = reader.wide
        self.null_record = bytes(reader.null_size)
        self.header_format = "<QQQB" if self.wide else "<IIIB"
        self.header_size = struct.calcsize(self.header_format)

    def write(self, nodes):
        output = bytearray(self.reader.data[:27])
        for node in nodes:
            self._write_node(output, node)
        output.extend(self.null_record)
        output.extend(self.reader.footer)
        return bytes(output)

    def _write_node(self, output, node):
        start = len(output)
        output.extend(bytes(self.header_size))
        name = node.name.encode("utf-8")
        output.extend(name)
        encoded_properties = [encode_property(prop) for prop in node.properties]
        property_start = len(output)
        for encoded in encoded_properties:
            output.extend(encoded)
        property_bytes = len(output) - property_start

        for child in node.children:
            self._write_node(output, child)
        if node.children:
            output.extend(self.null_record)

        end = len(output)
        struct.pack_into(
            self.header_format,
            output,
            start,
            end,
            len(node.properties),
            property_bytes,
            len(name),
        )


def reset_model_transform(model):
    for name, values in (
        ("Lcl Translation", (0.0, 0.0, 0.0)),
        ("Lcl Rotation", (0.0, 0.0, 0.0)),
        ("Lcl Scaling", (1.0, 1.0, 1.0)),
        ("PreRotation", (0.0, 0.0, 0.0)),
        ("PostRotation", (0.0, 0.0, 0.0)),
        ("GeometricTranslation", (0.0, 0.0, 0.0)),
        ("GeometricRotation", (0.0, 0.0, 0.0)),
        ("GeometricScaling", (1.0, 1.0, 1.0)),
    ):
        set_property_values(model, name, values)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("fbx", type=Path)
    parser.add_argument("--backup", type=Path)
    args = parser.parse_args()

    reader = BinaryFbxReader(args.fbx)
    nodes = reader.read()
    objects, geometry_to_model = find_mesh_connections(nodes)
    mesh_geometries = [
        node
        for node in objects.children
        if node.name == "Geometry"
        and len(node.properties) >= 3
        and node.properties[2].value == "Mesh"
    ]
    if len(mesh_geometries) != 2:
        raise ValueError(f"Expected exactly two mesh geometries, found {len(mesh_geometries)}.")

    global_settings = next(node for node in nodes if node.name == "GlobalSettings")
    unit_node = find_property_node(global_settings, "UnitScaleFactor")
    source_unit = float(unit_node.properties[4].value)
    if not math.isclose(source_unit, SOURCE_UNIT_SCALE, rel_tol=0.0, abs_tol=1e-6):
        raise ValueError(
            f"Expected untouched source unit {SOURCE_UNIT_SCALE}, found {source_unit}. "
            "The adjuster refuses to run twice."
        )

    plate_geometry = None
    pouch_geometry = None
    plate_model = None
    pouch_model = None
    for geometry in mesh_geometries:
        geometry_id = geometry.properties[0].value
        model = geometry_to_model.get(geometry_id)
        if model is None:
            raise ValueError(f"Geometry {geometry_id} is not connected to a Model.")
        model_name = clean_name(model.properties[1].value).lower()
        if "podsumki" in model_name:
            pouch_geometry, pouch_model = geometry, model
        else:
            plate_geometry, plate_model = geometry, model

    if plate_geometry is None or pouch_geometry is None:
        raise ValueError("Could not identify the plate and pouch mesh models.")

    plate_points = normalize_plate_vertices(plate_geometry, plate_model)
    pouch_points = normalize_pouch_vertices(pouch_geometry)
    plate_components = connected_components(plate_geometry)
    plate_points = reshape_plate(plate_points, plate_components)
    pouch_points = reshape_pouches(pouch_points)

    set_vertices(plate_geometry, plate_points)
    set_vertices(pouch_geometry, pouch_points)
    transform_normals(plate_geometry)
    transform_normals(pouch_geometry)
    reset_model_transform(plate_model)
    reset_model_transform(pouch_model)

    for property_name in ("UnitScaleFactor", "OriginalUnitScaleFactor"):
        set_property_values(global_settings, property_name, (1.0,))
    for property_name in ("OriginalUpAxis", "UpAxis"):
        set_property_values(global_settings, property_name, (1,))
    for property_name in ("OriginalUpAxisSign", "UpAxisSign"):
        set_property_values(global_settings, property_name, (1,))

    output = BinaryFbxWriter(reader).write(nodes)
    backup = args.backup or (
        Path(__file__).resolve().parent / "Backups" / "Plate carrier 1221.original.fbx"
    )
    backup.parent.mkdir(parents=True, exist_ok=True)
    if not backup.exists():
        shutil.copy2(args.fbx, backup)
    temporary = args.fbx.with_suffix(args.fbx.suffix + ".tmp")
    temporary.write_bytes(output)

    # Parse the complete output before replacing the source asset.
    verification = BinaryFbxReader(temporary)
    verification_nodes = verification.read()
    if len(verification_nodes) != len(nodes):
        temporary.unlink(missing_ok=True)
        raise ValueError("Rewritten FBX failed structural verification.")
    temporary.replace(args.fbx)

    plate_min, plate_max, plate_size = bounds(plate_points)
    pouch_min, pouch_max, pouch_size = bounds(pouch_points)
    print(f"Updated: {args.fbx}")
    print(f"Backup:  {backup}")
    print(f"Plate cm min={plate_min} max={plate_max} size={plate_size}")
    print(f"Pouch cm min={pouch_min} max={pouch_max} size={pouch_size}")
    print("Unity .meta was not changed.")


if __name__ == "__main__":
    main()
