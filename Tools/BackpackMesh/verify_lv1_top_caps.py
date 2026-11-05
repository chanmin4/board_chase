"""Read-only mesh checks for LV1's upper strap-mount repair; no Unity menu."""
import collections
import hashlib
import json
import math
import sys
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from remove_shoulder_straps import BinaryFbxReader, REPORT_DIR, ROOT, geometry, mesh_data, preview, components


def snapshot(node):
    return node.name, [p.value for p in node.properties], [snapshot(c) for c in node.children]


def edges(faces):
    result = collections.defaultdict(list)
    for face in faces:
        for a, b in zip(face, face[1:] + face[:1]):
            result[tuple(sorted((a, b)))].append((a, b))
    return result


def verify():
    lines = []
    def check(ok, label):
        if not ok:
            raise AssertionError(label)
        lines.append("PASS " + label)
    old_nodes = BinaryFbxReader(REPORT_DIR / "LV1-before-top-caps.fbx").read()
    new_nodes = BinaryFbxReader(ROOT / "Assets/Art/Mesh/BackPack/BagLv1_NoStraps.fbx").read()
    old, new = geometry(old_nodes), geometry(new_nodes)
    vertices, old_faces = mesh_data(old)
    new_vertices, faces = mesh_data(new)
    check(vertices == new_vertices, "All existing vertex positions and model bounds unchanged")
    check(faces[:len(old_faces)] == old_faces, "All existing body/pocket polygons unchanged")
    caps = faces[len(old_faces):]
    check(len(caps) == 4 and all(len(c) == 4 for c in caps), "Exactly four quad caps added: two mounts on two shells")
    before_edges, after_edges = edges(old_faces), edges(faces)
    check(sum(len(v) == 1 for v in before_edges.values()) == 28 and
          sum(len(v) == 1 for v in after_edges.values()) == 12, "Only the 16 upper boundary edges closed")
    check(max(map(len, after_edges.values())) == 2, "No non-manifold or overlapping added edges")
    old_normals = old.child("LayerElementNormal").child("Normals").properties[0].value
    normals = new.child("LayerElementNormal").child("Normals").properties[0].value
    old_uv = old.child("LayerElementUV")
    uv = new.child("LayerElementUV")
    old_indices = old_uv.child("UVIndex").properties[0].value
    indices = uv.child("UVIndex").properties[0].value
    check(normals[:len(old_normals)] == old_normals and len(normals) == len(old_normals) + 48,
          "Existing smooth normals unchanged; new caps have all 16 corner normals")
    check(snapshot(old_uv.child("UV")) == snapshot(uv.child("UV")) and
          indices[:len(old_indices)] == old_indices and len(indices) == len(old_indices) + 16,
          "Existing atlas UV data unchanged; new cap UVs use existing neighbouring UVs")
    corners = collections.defaultdict(list)
    offset = 0
    for face in old_faces:
        for c, v in enumerate(face):
            corners[v].append(offset + c)
        offset += len(face)
    for i, cap in enumerate(caps):
        check(all(vertices[v][1] > 120 and vertices[v][2] < 25 for v in cap), f"Cap {i + 1} is only at an upper strap mount")
        check(all(len(after_edges[tuple(sorted((a, b)))]) == 2 and
                  after_edges[tuple(sorted((a, b)))][0] == (b, a)
                  for a, b in zip(cap, cap[1:] + cap[:1])), f"Cap {i + 1} seals every rim with correct winding")
        a, b, c = [vertices[v] for v in cap[:3]]
        u, v = [b[k] - a[k] for k in range(3)], [c[k] - a[k] for k in range(3)]
        normal = (u[1]*v[2] - u[2]*v[1], u[2]*v[0] - u[0]*v[2], u[0]*v[1] - u[1]*v[0])
        length = math.sqrt(sum(x*x for x in normal))
        check(length > 1, f"Cap {i + 1} is not degenerate")
        for j, vertex in enumerate(cap):
            at = offset + i * 4 + j
            n = normals[at*3:at*3+3]
            check(any(indices[at] == old_indices[corner] and
                      n == old_normals[corner*3:corner*3+3] for corner in corners[vertex]),
                  f"Cap {i + 1}, corner {j + 1}: UV and smooth normal match its existing boundary")
            check(sum(normal[k] * n[k] for k in range(3)) / length > .85,
                  f"Cap {i + 1}, corner {j + 1}: normal faces the correct shell direction")
    check(snapshot(old.child("Edges")) == snapshot(new.child("Edges")), "Existing edge indices remain valid and unchanged")
    check(snapshot(old.child("LayerElementMaterial")) == snapshot(new.child("LayerElementMaterial")), "No new material or material remapping")
    def unrelated(nodes):
        return [snapshot(n) if n.name != "Objects" else
                (n.name, [p.value for p in n.properties], [snapshot(c) for c in n.children if c.name != "Geometry"])
                for n in nodes]
    check(unrelated(old_nodes) == unrelated(new_nodes), "FBX IDs, model transforms, connections and all unrelated nodes unchanged")
    scope = json.loads((REPORT_DIR / "lv1-top-caps-scope.json").read_text(encoding="utf-8"))
    for p, digest in scope["after"].items():
        check(hashlib.sha256((ROOT / p).read_bytes()).hexdigest() == digest, "Authored asset unchanged after repair: " + p)
    check(scope["changed_asset_files"] == [str(Path("Assets/Art/Mesh/BackPack/BagLv1_NoStraps.fbx"))],
          "Only the LV1 derived mesh changed; all other 210 authored files preserved")
    preview(vertices, old_faces, components(vertices, old_faces, False), REPORT_DIR / "LV1-top-before.png", set(), tilt_degrees=60)
    preview(new_vertices, faces, components(new_vertices, faces, False), REPORT_DIR / "LV1-top-repaired.png", set(), tilt_degrees=60)
    lines.append(f"{len(lines)} passed / 0 failed. Binary topology/layers and asset preservation checks; not Unity render checks.")
    (REPORT_DIR / "lv1-top-caps-verification.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines[:50]) + "\n" + lines[-1])


if __name__ == "__main__":
    verify()
