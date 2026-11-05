"""Copy the five backpack meshes and remove only reviewed shoulder-strap islands.

The original FBX files, retained faces, UV channels, normals, colors, object IDs,
model transforms and importer settings remain unchanged. No Unity Tools menu.
Run --inspect to render numbered mesh components before approving a selection.
LV1's exposed upper strap mounts are capped on both shells after strap removal.
"""
import argparse
import copy
import hashlib
import json
import math
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Tools/ArmorMeshAdjuster"))
from inspect_binary_fbx import BinaryFbxReader
from adjust_plate_carrier import BinaryFbxWriter

SPECS = [(1, "12"), (2, "10"), (3, "18"), (4, "07"), (5, "03")]
# Reviewed in the numbered four-view source previews. LV4 has no shoulder straps:
# components 2/3 are sleeping-roll retaining straps, not straps around a character.
# LV1 has duplicate inner/outer strap shells. Use ORIGINAL control-point
# connectivity: position-welding would attach its inner straps to the bag shell.
REMOVE = {1: [2, 3, 4, 5], 2: [20, 21], 3: [1], 4: [], 5: [4, 5]}
REPORT_DIR = ROOT / "Library/BackpackNoStraps-20261002"


def source_path(number):
    return ROOT / f"Assets/Art/Mesh/BackPack/SM_BackPack_{number}.fbx"


def geometry(nodes):
    objects = next(n for n in nodes if n.name == "Objects")
    meshes = [n for n in objects.children if n.name == "Geometry" and n.properties[2].value == "Mesh"]
    if len(meshes) != 1:
        raise ValueError("Expected a single backpack geometry")
    return meshes[0]


def mesh_data(mesh):
    values = mesh.child("Vertices").properties[0].value
    vertices = list(zip(values[0::3], values[1::3], values[2::3]))
    polygons = []
    face = []
    for raw in mesh.child("PolygonVertexIndex").properties[0].value:
        face.append(~raw if raw < 0 else raw)
        if raw < 0:
            polygons.append(face)
            face = []
    if face:
        raise ValueError("Unterminated polygon")
    return vertices, polygons


def components(vertices, polygons, weld=True):
    # Weld coincident control points ONLY for classification, never in output.
    parents = list(range(len(vertices)))
    def find(v):
        while parents[v] != v:
            parents[v] = parents[parents[v]]
            v = parents[v]
        return v
    def join(a, b):
        parents[find(a)] = find(b)
    positions = {}
    for i, point in enumerate(vertices) if weld else []:
        key = tuple(round(v, 4) for v in point)
        if key in positions:
            join(i, positions[key])
        else:
            positions[key] = i
    for face in polygons:
        for v in face[1:]:
            join(face[0], v)
    result = {}
    for i, face in enumerate(polygons):
        result.setdefault(find(face[0]), []).append(i)
    return sorted(result.values(), key=len, reverse=True)


def bounds(vertices, indices):
    points = [vertices[i] for i in indices]
    return [[round(f(p[a] for p in points), 6) for a in range(3)] for f in (min, max)]


def preview(vertices, polygons, groups, output, highlight=None, isolated=None, tilt_degrees=8):
    # Deterministic orthographic diagnostic, not a modification to any texture.
    from PIL import Image, ImageDraw
    colors = [(126, 167, 194), (210, 175, 107), (192, 114, 127), (122, 180, 124),
              (166, 134, 201), (209, 147, 83), (110, 180, 177), (171, 165, 108)]
    image = Image.new("RGB", (1760, 680), (22, 28, 35))
    draw = ImageDraw.Draw(image)
    by_face = {face: index for index, group in enumerate(groups) for face in group}
    shown = [i for i in range(len(polygons)) if isolated is None or by_face[i] in isolated]
    for view, angle in enumerate((0, 90, 180, 240)):
        yaw = math.radians(angle)
        # Slightly elevated view exposes handles and the shoulder-side surface.
        tilt = math.radians(tilt_degrees)
        projected = []
        for x, y, z in vertices:
            px = x * math.cos(yaw) + z * math.sin(yaw)
            depth = -x * math.sin(yaw) + z * math.cos(yaw)
            projected.append((px, y * math.cos(tilt) - depth * math.sin(tilt), depth * math.cos(tilt) + y * math.sin(tilt)))
        used = {v for f in shown for v in polygons[f]}
        xmin, xmax = min(projected[v][0] for v in used), max(projected[v][0] for v in used)
        ymin, ymax = min(projected[v][1] for v in used), max(projected[v][1] for v in used)
        scale = min(370 / max(1, xmax - xmin), 565 / max(1, ymax - ymin))
        def screen(v):
            p = projected[v]
            return (view * 440 + 220 + (p[0] - (xmin + xmax) / 2) * scale,
                    350 - (p[1] - (ymin + ymax) / 2) * scale)
        draw.text((view * 440 + 12, 12), f"yaw {angle} / {output.stem}", fill="white")
        for f in sorted(shown, key=lambda i: sum(projected[v][2] for v in polygons[i]) / len(polygons[i])):
            group = by_face[f]
            base = colors[group % len(colors)] if highlight is None else ((238, 88, 90) if group in highlight else (154, 169, 179))
            # Normals for shading only; source normals are never recomputed.
            a, b, c = [vertices[v] for v in polygons[f][:3]]
            u, v = [b[k] - a[k] for k in range(3)], [c[k] - a[k] for k in range(3)]
            normal = (u[1]*v[2]-u[2]*v[1], u[2]*v[0]-u[0]*v[2], u[0]*v[1]-u[1]*v[0])
            length = math.sqrt(sum(n*n for n in normal)) or 1
            shade = .55 + .45 * abs(sum(normal[k]*(-.4,.5,.75)[k] for k in range(3))/length)
            fill = tuple(min(255, int(value * shade)) for value in base)
            draw.polygon([screen(v) for v in polygons[f]], fill=fill, outline=(45, 51, 58))
        if highlight is None:
            for i, group in enumerate(groups):
                if isolated is not None and i not in isolated:
                    continue
                indices = {v for f in group for v in polygons[f]}
                px = sum(screen(v)[0] for v in indices) / len(indices)
                py = sum(screen(v)[1] for v in indices) / len(indices)
                draw.text((px, py), str(i), fill="white", stroke_width=2, stroke_fill="black")
    output.parent.mkdir(parents=True, exist_ok=True)
    image.save(output)


def changed(prop, values):
    prop.value = values
    prop.modified = True


def seal_lv1_top_mounts(mesh):
    """Close only the two upper strap mounts, on the inner and outer shells.

    Reuse their existing boundary vertices, UV indices and smooth corner normals.
    No vertex movement, atlas repaint, new material or unrelated hole filling.
    The reviewed vertex sets also stop a changed source mesh being patched blind.
    """
    vertices, faces = mesh_data(mesh)
    reviewed = ({68, 69, 70, 71}, {167, 168, 170, 171},
                {240, 241, 242, 243}, {339, 340, 342, 343})
    edges, corners = {}, {}
    offset = 0
    for face in faces:
        for c, a in enumerate(face):
            b = face[(c + 1) % len(face)]
            edges.setdefault(tuple(sorted((a, b))), []).append((a, b))
            corners.setdefault(a, offset + c)
        offset += len(face)
    caps = []
    for rim in reviewed:
        if not all(vertices[v][1] > 120 and vertices[v][2] < 25 for v in rim):
            raise ValueError("LV1 upper strap-mount topology has changed")
        following = {}
        for edge, owners in edges.items():
            if set(edge) <= rim:
                if len(owners) != 1:
                    raise ValueError("Expected an open, unpatched LV1 upper strap mount")
                a, b = owners[0]
                following[b] = a  # Opposite winding to the adjacent existing face.
        if set(following) != rim or set(following.values()) != rim:
            raise ValueError("Expected a four-edge LV1 upper strap-mount rim")
        cap = [min(rim)]
        while following[cap[-1]] != cap[0]:
            v = following[cap[-1]]
            if v in cap:
                raise ValueError("Invalid LV1 upper strap-mount loop")
            cap.append(v)
        if len(cap) != 4:
            raise ValueError("Only the reviewed quad openings may be capped")
        caps.append(cap)
    values = list(mesh.child("PolygonVertexIndex").properties[0].value)
    for cap in caps:
        values.extend(cap[:-1] + [~cap[-1]])
    changed(mesh.child("PolygonVertexIndex").properties[0], values)
    for layer in mesh.children:
        if not layer.name.startswith("LayerElement"):
            continue
        mapping = layer.child("MappingInformationType").properties[0].value
        reference = layer.child("ReferenceInformationType").properties[0].value
        if mapping == "AllSame":
            continue
        if mapping != "ByPolygonVertex":
            raise ValueError("Unsupported LV1 cap layer mapping")
        if reference == "IndexToDirect":
            prop = next(c for c in layer.children if c.name in ("UVIndex", "ColorIndex", "NormalsIndex")).properties[0]
            old = list(prop.value)
            changed(prop, old + [old[corners[v]] for cap in caps for v in cap])
        elif reference == "Direct":
            node = next(c for c in layer.children if c.name in ("Normals", "UV", "Colors"))
            size = {"Normals": 3, "UV": 2, "Colors": 4}[node.name]
            old = list(node.properties[0].value)
            changed(node.properties[0], old + [old[corners[v] * size + k] for cap in caps for v in cap for k in range(size)])
        else:
            raise ValueError("Unsupported LV1 cap layer reference")
    # Every cap edge already belongs to a neighbouring polygon. Keep the existing
    # Edges array: its corner indices remain valid, with no extra vertices/edges.
    return caps


def remove_faces(mesh, retained_faces):
    vertices, polygons = mesh_data(mesh)
    used = sorted({v for f in retained_faces for v in polygons[f]})
    remap = {old: new for new, old in enumerate(used)}
    offsets = []
    at = 0
    for face in polygons:
        offsets.append(at)
        at += len(face)
    corners = [offsets[f] + c for f in retained_faces for c in range(len(polygons[f]))]
    changed(mesh.child("Vertices").properties[0], [x for i in used for x in vertices[i]])
    flattened = []
    for f in retained_faces:
        face = [remap[v] for v in polygons[f]]
        flattened.extend(face[:-1] + [~face[-1]])
    changed(mesh.child("PolygonVertexIndex").properties[0], flattened)
    # Regenerate FBX edge-to-polygon-corner indices after polygon removal.
    seen = set()
    edges = []
    at = 0
    for f in retained_faces:
        face = [remap[v] for v in polygons[f]]
        for c, a in enumerate(face):
            edge = tuple(sorted((a, face[(c + 1) % len(face)])))
            if edge not in seen:
                seen.add(edge)
                edges.append(at + c)
        at += len(face)
    if mesh.child("Edges") is not None:
        changed(mesh.child("Edges").properties[0], edges)
    for layer in mesh.children:
        if not layer.name.startswith("LayerElement"):
            continue
        mapping = layer.child("MappingInformationType").properties[0].value
        reference = layer.child("ReferenceInformationType").properties[0].value
        if mapping == "AllSame":
            continue
        if mapping != "ByPolygonVertex":
            raise ValueError(f"Refusing unsupported layer {layer.name}: {mapping}")
        if reference == "IndexToDirect":
            indices = next(c for c in layer.children if c.name in ("UVIndex", "ColorIndex", "NormalsIndex"))
            original = indices.properties[0].value
            if len(original) != sum(len(f) for f in polygons):
                raise ValueError("Layer corner count mismatch")
            changed(indices.properties[0], [original[c] for c in corners])
        elif reference == "Direct":
            values = next(c for c in layer.children if c.name in ("Normals", "UV", "Colors"))
            size = {"Normals": 3, "UV": 2, "Colors": 4}[values.name]
            original = values.properties[0].value
            if len(original) != sum(len(f) for f in polygons) * size:
                raise ValueError("Direct layer corner count mismatch")
            changed(values.properties[0], [original[c*size+k] for c in corners for k in range(size)])
        else:
            raise ValueError("Unsupported layer reference")
    return used, corners


def generate(level, number):
    src = source_path(number)
    reader = BinaryFbxReader(src)
    nodes = reader.read()
    original = copy.deepcopy(nodes)
    mesh = geometry(nodes)
    vertices, polygons = mesh_data(mesh)
    groups = components(vertices, polygons, False)
    if not REMOVE[level] and level != 4:
        raise ValueError(f"LV{level} strap selection has not been reviewed")
    deleted = {f for g in REMOVE[level] for f in groups[g]}
    retained = [i for i in range(len(polygons)) if i not in deleted]
    if deleted:
        used, corners = remove_faces(mesh, retained)
    else:
        used, corners = list(range(len(vertices))), list(range(sum(len(f) for f in polygons)))
    caps = seal_lv1_top_mounts(mesh) if level == 1 else []
    output = ROOT / f"Assets/Art/Mesh/BackPack/BagLv{level}_NoStraps.fbx"
    data = BinaryFbxWriter(reader).write(nodes) if deleted else reader.data
    if output.exists() and output.read_bytes() != data:
        prior = json.loads((REPORT_DIR/"mesh-verification.json").read_text(encoding="utf-8"))
        record = next(p for p in prior if p["level"] == level)
        if hashlib.sha256(output.read_bytes()).hexdigest() != record["output_sha256"]:
            raise ValueError(f"Refusing to overwrite a modified generated mesh: {output}")
    # Validate the encoded binary in Library before replacing the active mesh.
    candidate = REPORT_DIR/f"LV{level}-candidate.fbx"
    candidate.write_bytes(data)
    parsed = BinaryFbxReader(candidate).read()
    verify = geometry(parsed)
    new_vertices, new_faces = mesh_data(verify)
    assert new_vertices == [vertices[i] for i in used], "Retained vertex coordinates changed"
    assert [[new_vertices[v] for v in f] for f in new_faces[:len(retained)]] == [[vertices[v] for v in polygons[f]] for f in retained], "Retained polygons changed"
    assert new_faces[len(retained):] == caps, "Only reviewed upper strap-mount caps may be added"
    # Every unrelated FBX node and every retained material/UV/normal/color value must match.
    def snapshot(node):
        return (node.name, [p.value for p in node.properties], [snapshot(c) for c in node.children])
    expected_mesh = geometry(nodes)
    assert snapshot(verify) == snapshot(expected_mesh), "Serialized geometry differs from expected deletion"
    objects_before = next(n for n in original if n.name == "Objects")
    objects_after = next(n for n in parsed if n.name == "Objects")
    assert [snapshot(n) for n in objects_before.children if n.name != "Geometry"] == [snapshot(n) for n in objects_after.children if n.name != "Geometry"]
    assert [snapshot(n) for n in original if n.name != "Objects"] == [snapshot(n) for n in parsed if n.name != "Objects"]
    old_layers = [n for n in geometry(original).children if n.name.startswith("LayerElement")]
    for old, new in zip(old_layers, [n for n in verify.children if n.name.startswith("LayerElement")]):
        assert old.name == new.name
        for old_child, new_child in zip(old.children, new.children):
            if old_child.name in ("Normals",):
                values = old_child.properties[0].value
                expected = [values[c*3+k] for c in corners for k in range(3)]
                assert new_child.properties[0].value[:len(expected)] == expected
                assert len(new_child.properties[0].value) == len(expected) + sum(map(len, caps)) * 3
            elif old_child.name in ("UVIndex", "ColorIndex", "NormalsIndex"):
                expected = [old_child.properties[0].value[c] for c in corners]
                assert new_child.properties[0].value[:len(expected)] == expected
                assert len(new_child.properties[0].value) == len(expected) + sum(map(len, caps))
            else:
                assert snapshot(old_child) == snapshot(new_child), f"Unrelated layer changed: {old_child.name}"
    output.write_bytes(data)
    preview(vertices, polygons, groups, REPORT_DIR/f"LV{level}_removed-red.png", set(REMOVE[level]))
    new_groups = components(new_vertices, new_faces, False)
    preview(new_vertices, new_faces, new_groups, REPORT_DIR/f"LV{level}_no-straps.png", set())
    return {"level": level, "source": str(src.relative_to(ROOT)), "output": str(output.relative_to(ROOT)),
            "source_sha256": hashlib.sha256(src.read_bytes()).hexdigest(), "output_sha256": hashlib.sha256(data).hexdigest(),
            "removed_components": REMOVE[level], "removed_polygons": len(deleted), "retained_polygons": len(retained),
            "retained_vertices": len(used), "filled_upper_strap_mounts": len(caps), "added_polygons": len(caps),
            "all_retained_geometry_uv_normals_colors_and_unrelated_nodes_preserved": True}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--inspect", action="store_true")
    parser.add_argument("--generate", action="store_true")
    parser.add_argument("--level", type=int, choices=range(1, 6), help="Generate only this level; leave the other meshes untouched")
    args = parser.parse_args()
    REPORT_DIR.mkdir(parents=True, exist_ok=True)
    if args.inspect:
        for lv, number in SPECS:
            mesh = geometry(BinaryFbxReader(source_path(number)).read())
            vertices, polygons = mesh_data(mesh)
            groups = components(vertices, polygons, False)
            print(f"LV{lv} SM_BackPack_{number}: {len(groups)} components")
            for i, group in enumerate(groups):
                print(i, len(group), bounds(vertices, {v for f in group for v in polygons[f]}))
            preview(vertices, polygons, groups, REPORT_DIR/f"LV{lv}_components.png")
    if args.generate:
        results = [generate(lv, num) for lv, num in SPECS if args.level is None or lv == args.level]
        report = REPORT_DIR/"mesh-verification.json"
        prior = json.loads(report.read_text(encoding="utf-8")) if args.level is not None and report.exists() else []
        replaced = {p["level"] for p in results}
        combined = sorted([p for p in prior if p["level"] not in replaced] + results, key=lambda p: p["level"])
        report.write_text(json.dumps(combined, indent=2), encoding="utf-8")
        print(json.dumps(results, indent=2))


if __name__ == "__main__":
    main()
