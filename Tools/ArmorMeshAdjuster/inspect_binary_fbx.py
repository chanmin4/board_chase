import argparse
import dataclasses
import struct
import zlib
from pathlib import Path


@dataclasses.dataclass
class Property:
    kind: str
    value: object
    encoded: bytes = b""
    modified: bool = False


@dataclasses.dataclass
class Node:
    name: str
    properties: list
    children: list

    def child(self, name):
        return next((item for item in self.children if item.name == name), None)


SCALAR_FORMATS = {
    "Y": "<h",
    "C": "<?",
    "I": "<i",
    "F": "<f",
    "D": "<d",
    "L": "<q",
}

ARRAY_FORMATS = {
    "f": ("f", 4),
    "d": ("d", 8),
    "l": ("q", 8),
    "i": ("i", 4),
    "b": ("B", 1),
}


class BinaryFbxReader:
    def __init__(self, path):
        self.path = Path(path)
        self.data = self.path.read_bytes()
        if not self.data.startswith(b"Kaydara FBX Binary"):
            raise ValueError("Only binary FBX files are supported.")
        self.version = struct.unpack_from("<I", self.data, 23)[0]
        self.wide = self.version >= 7500
        self.null_size = 25 if self.wide else 13
        self.footer = b""

    def read(self):
        position = 27
        nodes = []
        while position < len(self.data):
            node, next_position, is_null = self._read_node(position)
            position = next_position
            if is_null:
                self.footer = self.data[position:]
                break
            nodes.append(node)
        return nodes

    def _read_node(self, position):
        header_format = "<QQQB" if self.wide else "<IIIB"
        header_size = struct.calcsize(header_format)
        end_offset, property_count, property_bytes, name_length = struct.unpack_from(
            header_format, self.data, position
        )
        if end_offset == 0:
            return None, position + self.null_size, True

        cursor = position + header_size
        name = self.data[cursor : cursor + name_length].decode("utf-8", "replace")
        cursor += name_length
        property_end = cursor + property_bytes
        properties = []
        for _ in range(property_count):
            prop, cursor = self._read_property(cursor)
            properties.append(prop)
        if cursor != property_end:
            raise ValueError(f"Property length mismatch at node {name}: {cursor} != {property_end}")

        children = []
        while cursor < end_offset:
            child, next_cursor, is_null = self._read_node(cursor)
            cursor = next_cursor
            if is_null:
                break
            children.append(child)
        if cursor != end_offset:
            raise ValueError(f"Node end mismatch at {name}: {cursor} != {end_offset}")
        return Node(name, properties, children), end_offset, False

    def _read_property(self, position):
        start = position
        kind = chr(self.data[position])
        cursor = position + 1
        if kind in SCALAR_FORMATS:
            fmt = SCALAR_FORMATS[kind]
            value = struct.unpack_from(fmt, self.data, cursor)[0]
            end = cursor + struct.calcsize(fmt)
            return Property(kind, value, self.data[start:end]), end
        if kind in ("S", "R"):
            length = struct.unpack_from("<I", self.data, cursor)[0]
            cursor += 4
            raw = self.data[cursor : cursor + length]
            value = raw.decode("utf-8", "replace") if kind == "S" else raw
            end = cursor + length
            return Property(kind, value, self.data[start:end]), end
        if kind in ARRAY_FORMATS:
            length, encoding, byte_length = struct.unpack_from("<III", self.data, cursor)
            cursor += 12
            raw = self.data[cursor : cursor + byte_length]
            if encoding == 1:
                raw = zlib.decompress(raw)
            element_format, element_size = ARRAY_FORMATS[kind]
            expected = length * element_size
            if len(raw) != expected:
                raise ValueError(f"Array length mismatch for {kind}: {len(raw)} != {expected}")
            values = list(struct.unpack(f"<{length}{element_format}", raw))
            end = cursor + byte_length
            return Property(kind, values, self.data[start:end]), end
        raise ValueError(f"Unknown FBX property type {kind!r} at byte {position}")


def clean_name(value):
    return str(value).replace("\x00\x01", "::")


def property70_values(node):
    result = {}
    props = node.child("Properties70")
    if props is None:
        return result
    for item in props.children:
        if item.name != "P" or not item.properties:
            continue
        values = [prop.value for prop in item.properties]
        result[str(values[0])] = values[4:]
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("fbx")
    args = parser.parse_args()

    reader = BinaryFbxReader(args.fbx)
    nodes = reader.read()
    print(f"FBX version: {reader.version}")

    global_settings = next((node for node in nodes if node.name == "GlobalSettings"), None)
    if global_settings:
        settings = property70_values(global_settings)
        for key in (
            "UpAxis",
            "UpAxisSign",
            "FrontAxis",
            "FrontAxisSign",
            "CoordAxis",
            "CoordAxisSign",
            "UnitScaleFactor",
        ):
            print(f"{key}: {settings.get(key)}")

    objects = next((node for node in nodes if node.name == "Objects"), None)
    if objects is None:
        raise ValueError("Objects node is missing.")

    print("\nModels:")
    for node in objects.children:
        if node.name != "Model":
            continue
        values = [prop.value for prop in node.properties]
        print(f"- id={values[0]} name={clean_name(values[1])} type={values[2]}")
        for key, value in property70_values(node).items():
            if key in (
                "Lcl Translation",
                "Lcl Rotation",
                "Lcl Scaling",
                "GeometricTranslation",
                "GeometricRotation",
                "GeometricScaling",
                "PreRotation",
                "PostRotation",
            ):
                print(f"    {key}: {value}")

    print("\nMesh geometries:")
    for node in objects.children:
        values = [prop.value for prop in node.properties]
        if node.name != "Geometry" or len(values) < 3 or values[2] != "Mesh":
            continue
        vertices_node = node.child("Vertices")
        if vertices_node is None or not vertices_node.properties:
            continue
        vertices = vertices_node.properties[0].value
        triples = list(zip(vertices[0::3], vertices[1::3], vertices[2::3]))
        mins = tuple(min(point[index] for point in triples) for index in range(3))
        maxs = tuple(max(point[index] for point in triples) for index in range(3))
        center = tuple((mins[index] + maxs[index]) * 0.5 for index in range(3))
        size = tuple(maxs[index] - mins[index] for index in range(3))
        print(f"- id={values[0]} name={clean_name(values[1])}")
        print(f"    vertices={len(triples)} min={mins} max={maxs}")
        print(f"    center={center} size={size}")


if __name__ == "__main__":
    main()
