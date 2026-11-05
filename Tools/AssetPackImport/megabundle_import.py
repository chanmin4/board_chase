"""Inspect Unity packages and preserve existing assets by GUID, not folder spelling."""
from __future__ import annotations

import argparse
import collections
import concurrent.futures
import copy
import hashlib
import io
import json
import pathlib
import re
import tarfile

GUID = re.compile(rb"^guid:\s*([0-9a-fA-F]{32})\s*$", re.M)
CONTENT_EXTENSIONS = {".prefab", ".mat", ".fbx", ".obj", ".png", ".tga", ".jpg", ".jpeg", ".tif", ".psd"}


def project_guids(project: pathlib.Path):
    result = collections.defaultdict(list)
    for meta in (project / "Assets").rglob("*.meta"):
        data = meta.read_bytes()
        match = GUID.search(data)
        if match:
            result[match.group(1).decode("ascii").lower()].append(meta.relative_to(project).as_posix()[:-5])
    return dict(result)


def package_entries(package: pathlib.Path):
    with tarfile.open(package, "r:gz") as archive:
        groups = collections.OrderedDict()
        for member in archive.getmembers():
            parts = member.name.split("/")
            if len(parts) != 2 or not re.fullmatch(r"[0-9a-fA-F]{32}", parts[0]):
                continue
            groups.setdefault(parts[0].lower(), {})[parts[1]] = member
        entries = []
        for guid, members in groups.items():
            if "pathname" not in members:
                continue
            meta = archive.extractfile(members["asset.meta"]).read() if "asset.meta" in members else b""
            path = archive.extractfile(members["pathname"]).read().decode("utf-8-sig").strip()
            if "\\" in path or ".." in pathlib.PurePosixPath(path).parts or not path.startswith("Assets/"):
                raise ValueError(f"Unsafe or out-of-scope package pathname: {package.name}: {path!r}")
            meta_guid = GUID.search(meta)
            if meta_guid is None or meta_guid.group(1).decode("ascii").lower() != guid:
                raise ValueError(f"Package GUID/meta mismatch: {package.name}: {path}")
            entries.append({"guid": guid, "path": path, "folder": b"folderAsset: yes" in meta,
                            "bytes": members["asset"].size if "asset" in members else 0,
                            "has_asset": "asset" in members})
        return entries


def analyze_package(package: pathlib.Path, project: pathlib.Path, known: dict, folders: set):
    entries = package_entries(package)
    root_counts = collections.Counter()
    matched = []
    nested = []
    scripts = []
    for entry in entries:
        parts = pathlib.PurePosixPath(entry["path"]).parts
        if len(parts) > 2 and not entry["folder"]:
            root_counts[parts[1]] += 1
        suffix = pathlib.PurePosixPath(entry["path"]).suffix.lower()
        hits = [p for p in known.get(entry["guid"], []) if "/LowPolyMegaBundle/" not in p]
        if hits and suffix in CONTENT_EXTENSIONS and not entry["folder"]:
            matched.append({"source": entry["path"], "existing": hits})
        if suffix == ".unitypackage":
            nested.append({"path": entry["path"], "bytes": entry["bytes"], "guid": entry["guid"]})
        if suffix == ".cs":
            scripts.append(entry["path"])
    pack_name = package.parent.name
    root_names = set(root_counts)
    aliases = set()
    for name in {pack_name} | root_names:
        if name.casefold() in folders:
            aliases.add(name)
    # Root-folder GUID also recognizes a pack moved or renamed by the user.
    root_guid_hits = []
    for entry in entries:
        if entry["folder"] and len(pathlib.PurePosixPath(entry["path"]).parts) == 2:
            hits = [p for p in known.get(entry["guid"], []) if "/LowPolyMegaBundle/" not in p]
            if hits:
                root_guid_hits.append({"source": entry["path"], "existing": hits})
    existing = bool(aliases or root_guid_hits or matched)
    return {"pack": pack_name, "package": package.relative_to(project).as_posix(),
            "status": "skip_existing" if existing else "candidate_new",
            "existing_folder_names": sorted(aliases), "existing_root_guids": root_guid_hits,
            "existing_content_count": len(matched), "existing_content_examples": matched[:8],
            "root_file_counts": dict(root_counts), "entry_count": len(entries),
            "nested_packages": nested, "scripts": scripts,
            "uncompressed_asset_bytes": sum(e["bytes"] for e in entries), "entries": entries}


def analyze(project: pathlib.Path, output: pathlib.Path):
    bundle = project / "Assets/AssetsPack/LowPolyMegaBundle"
    if not bundle.is_dir():
        raise ValueError("Expected LowPolyMegaBundle source folder is missing.")
    known = project_guids(project)
    folders = {p.name.casefold() for p in (project / "Assets/AssetsPack").iterdir() if p.is_dir()}
    packages = sorted(bundle.glob("*/*.unitypackage"))
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        rows = list(pool.map(lambda p: analyze_package(p, project, known, folders), packages))
    report = {"project": str(project), "guid_count": len(known), "packages": rows}
    output.mkdir(parents=True, exist_ok=True)
    (output / "analysis.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    for row in rows:
        print(f'{row["status"]:16} {row["pack"]:30} roots={list(row["root_file_counts"])} '
              f'GUID_hits={row["existing_content_count"]} nested={len(row["nested_packages"])}', flush=True)
    print(f"Packages={len(rows)} new={sum(r['status'] == 'candidate_new' for r in rows)} "
          f"skip={sum(r['status'] == 'skip_existing' for r in rows)} GUIDs={len(known)}", flush=True)


def snapshot(project: pathlib.Path):
    files = []
    for root in ("Assets", "Packages", "ProjectSettings"):
        files.extend(p for p in (project / root).rglob("*") if p.is_file())
    def digest(path):
        sha = hashlib.sha256()
        with path.open("rb") as stream:
            for block in iter(lambda: stream.read(1024 * 1024), b""):
                sha.update(block)
        return {"path": path.relative_to(project).as_posix(), "sha256": sha.hexdigest()}
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        return list(pool.map(digest, files))


def stage_package(source, target, selected):
    """Repack unchanged binary payloads; change only the Unity package pathname table."""
    by_guid = {e["guid"]: e for e in selected}
    with tarfile.open(source, "r:gz") as incoming, tarfile.open(target, "w:gz", compresslevel=1, format=tarfile.GNU_FORMAT) as outgoing:
        for member in incoming:
            guid = member.name.split("/")[0].lower()
            if guid not in by_guid:
                continue
            header = copy.copy(member)
            if member.name.endswith("/pathname"):
                payload = by_guid[guid]["path"].encode("utf-8")
                header.size = len(payload)
                outgoing.addfile(header, io.BytesIO(payload))
            else:
                outgoing.addfile(header, incoming.extractfile(member) if member.isfile() else None)


def prepare(project: pathlib.Path, output: pathlib.Path):
    report = json.loads((output / "analysis.json").read_text(encoding="utf-8"))
    known = project_guids(project)
    # Package-owned shader GUIDs are also already available to Unity; never duplicate them in Assets.
    for meta in (project / "Library/PackageCache").rglob("*.meta"):
        match = GUID.search(meta.read_bytes())
        if match:
            known.setdefault(match.group(1).decode("ascii").lower(), ["Packages/" + meta.relative_to(project / "Library/PackageCache").as_posix()[:-5]])
    staged = output / "staged"
    staged.mkdir(parents=True, exist_ok=True)
    owned = {}
    occupied = {}
    plans = []
    for row in report["packages"]:
        if row["status"] != "candidate_new":
            continue
        roots = list(row["root_file_counts"])
        if len(roots) != 1:
            raise ValueError(f"Ambiguous pack roots: {row['pack']}: {roots}")
        source_root = "Assets/" + roots[0]
        target_root = "Assets/AssetsPack/" + roots[0]
        if (project / target_root).exists() or (project / (target_root + ".meta")).exists():
            raise ValueError(f"Existing destination must not be touched: {target_root}")
        urp_candidates = [p for p in row["nested_packages"] if "_urp" in p["path"].lower()]
        if not urp_candidates:
            raise ValueError(f"No vendor URP package found: {row['pack']}")
        # TropicalEnvironment ships both 2020.3 and 2021.3. Use its newest supplied URP package.
        urp = max(urp_candidates, key=lambda p: tuple(int(x) for x in re.findall(r"\d+", pathlib.PurePosixPath(p["path"]).name)))
        inner_path = staged / (row["pack"] + "-vendor-urp.unitypackage")
        with tarfile.open(project / row["package"], "r:gz") as archive:
            payload = archive.extractfile(urp["guid"] + "/asset")
            with inner_path.open("wb") as destination:
                for block in iter(lambda: payload.read(1024 * 1024), b""):
                    destination.write(block)
        inner_entries = package_entries(inner_path)
        inner_roots = collections.Counter(pathlib.PurePosixPath(e["path"]).parts[1]
                                         for e in inner_entries if not e["folder"])
        inner_name = inner_roots.most_common(1)[0][0]
        inner_root = "Assets/" + inner_name
        if "urp" not in inner_name.lower() and inner_name.casefold() != roots[0].casefold():
            raise ValueError(f"Unexpected vendor URP root: {row['pack']}: {inner_roots}")
        inner_existing = [e for e in inner_entries if not e["folder"]
                          and pathlib.PurePosixPath(e["path"]).suffix.lower() in CONTENT_EXTENSIONS
                          and any(p.startswith("Assets/") and "/LowPolyMegaBundle/" not in p
                                  for p in known.get(e["guid"], []))]
        if inner_existing:
            row["status"] = "skip_existing"
            print(f"SKIP {row['pack']}: its URP assets already exist by GUID ({len(inner_existing)} matches).", flush=True)
            continue
        original_prefabs = {e["path"][len(source_root):].casefold() for e in row["entries"]
                            if e["path"].startswith(source_root + "/") and e["path"].endswith(".prefab")}
        converted_prefabs = {e["path"][len(inner_root):].casefold() for e in inner_entries
                             if e["path"].startswith(inner_root + "/") and e["path"].endswith(".prefab")}
        missing_prefabs = original_prefabs - converted_prefabs
        fallback = []
        if missing_prefabs:
            # Some vendor URP editions omit default-material particle-system prefabs.
            # Retain all original prefab content and recursively include only missing dependencies.
            by_guid = {e["guid"]: e for e in row["entries"]}
            urp_guids = {e["guid"] for e in inner_entries}
            urp_paths = {e["path"][len(inner_root):].casefold() for e in inner_entries
                         if e["path"].startswith(inner_root + "/")}
            needed = [e for e in row["entries"] if e["path"].startswith(source_root + "/")
                      and e["path"][len(source_root):].casefold() in missing_prefabs]
            visited = set()
            with tarfile.open(project / row["package"], "r:gz") as archive:
                while needed:
                    entry = needed.pop()
                    if entry["guid"] in visited or entry["guid"] in urp_guids:
                        continue
                    visited.add(entry["guid"])
                    relative = entry["path"][len(source_root):]
                    if relative.casefold() in urp_paths:
                        relative = "/_URP_Compatibility" + relative
                    fallback.append({**entry, "target_relative": relative})
                    if entry["has_asset"]:
                        payload = archive.extractfile(entry["guid"] + "/asset").read()
                        for match in re.findall(rb"guid:\s*([0-9a-f]{32})", payload):
                            guid = match.decode("ascii")
                            if guid in by_guid and guid not in known and guid not in urp_guids:
                                needed.append(by_guid[guid])
            print(f"RETAIN {row['pack']}: {len(missing_prefabs)} omitted prefabs, {len(fallback)} fallback assets (new-pack-only URP default-material repair).", flush=True)
        skipped = []
        def select(entries, from_root, conversion):
            selected = []
            for entry in entries:
                path = entry["path"]
                if path != from_root and not path.startswith(from_root + "/"):
                    skipped.append({"path": path, "reason": "outside this pack (global settings are not imported)"})
                    continue
                target = target_root + entry.get("target_relative", path[len(from_root):])
                guid = entry["guid"]
                if guid in known:
                    skipped.append({"path": path, "reason": "existing GUID", "existing": known[guid]})
                    continue
                if guid in owned and owned[guid] != target:
                    skipped.append({"path": path, "reason": "shared GUID already planned", "existing": [owned[guid]]})
                    continue
                key = target.casefold()
                if key in occupied and occupied[key] != guid:
                    raise ValueError(f"Different GUIDs share a destination: {target}")
                if (project / target).exists() or (project / (target + ".meta")).exists():
                    raise ValueError(f"Destination appeared after analysis: {target}")
                if guid in owned and not conversion:
                    raise ValueError(f"Duplicate base asset: {target}")
                owned[guid] = target
                occupied[key] = guid
                selected.append({**entry, "source_path": path, "path": target})
            return selected
        # These vendor URP archives are complete editions with different GUIDs and a *_URP root,
        # not material-only overlays. Import the complete URP edition at the desired pack root.
        # Retain the outer package's raw conversion archives and documentation, not duplicate Built-in visuals.
        documentation = [e for e in row["entries"] if not e["folder"] and
                         pathlib.PurePosixPath(e["path"]).suffix.lower() in {".unitypackage", ".txt", ".pdf", ".md", ".html"}]
        base_entries = select(documentation + fallback, source_root, False)
        urp_entries = select(inner_entries, inner_root, True)
        if not any(e["path"].endswith(".mat") for e in urp_entries):
            raise ValueError(f"Vendor URP overlay has no new-pack materials: {row['pack']}")
        base_path = staged / (row["pack"] + "-base.unitypackage")
        urp_path = staged / (row["pack"] + "-urp.unitypackage")
        stage_package(project / row["package"], base_path, base_entries)
        stage_package(inner_path, urp_path, urp_entries)
        merged = {e["guid"]: e for e in base_entries + urp_entries}
        plans.append({"name": row["pack"], "root": target_root,
                      "basePackage": base_path.relative_to(project).as_posix(),
                      "urpPackage": urp_path.relative_to(project).as_posix(),
                      "vendorUrp": urp["path"], "assets": list(merged.values()), "skipped": skipped,
                      "original_prefabs": len(original_prefabs), "urp_prefabs": len(converted_prefabs)})
        print(f"STAGED {row['pack']}: base={len(base_entries)} URP={len(urp_entries)} excluded={len(skipped)} -> {target_root}", flush=True)
    print("Hashing all pre-existing Assets / Packages / ProjectSettings files...", flush=True)
    originals = snapshot(project)
    plan = {"project": str(project), "packs": plans, "originals": originals,
            "skipExisting": [r["pack"] for r in report["packages"] if r["status"] == "skip_existing"]}
    (output / "plan.json").write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"READY: {len(plans)} new packs; {len(originals)} original file hashes; {len(owned)} owned GUIDs.", flush=True)


def extract_reviewed(project: pathlib.Path, output: pathlib.Path, resume=False):
    """Extract binary package payloads to only absent, reviewed roots; retain all original GUIDs."""
    plan = json.loads((output / "plan.json").read_text(encoding="utf-8"))
    known = project_guids(project)
    expected = {entry["path"]: entry["sha256"] for entry in plan["originals"]}
    for pack in plan["packs"]:
        root = project / pack["root"]
        if (root.exists() or pathlib.Path(str(root) + ".meta").exists()) and not resume:
            raise ValueError(f"Reviewed new root is no longer absent: {pack['root']}")
        for entry in pack["assets"]:
            if entry["path"] in expected or entry["path"] + ".meta" in expected:
                raise ValueError(f"Reviewed target was present before this operation: {entry['path']}")
            if entry["guid"] in known and (not resume or known[entry["guid"]] != [entry["path"]]):
                raise ValueError(f"GUID now exists; refusing extraction: {entry['guid']}")
    actual = {entry["path"]: entry["sha256"] for entry in snapshot(project)}
    changed = [path for path, sha in expected.items() if actual.get(path) != sha]
    if changed:
        raise ValueError(f"Protected files changed; refusing extraction: {changed[:10]}")
    written = {}
    counts = []
    for pack in plan["packs"]:
        owned = {entry["guid"]: entry for entry in pack["assets"]}
        root = (project / pack["root"]).resolve()
        for key in ("basePackage", "urpPackage"):
            package = project / pack[key]
            entries = package_entries(package)
            destinations = {}
            for entry in entries:
                reviewed = owned.get(entry["guid"])
                if reviewed is None or reviewed["path"] != entry["path"]:
                    raise ValueError(f"Package entry differs from reviewed plan: {entry}")
                target = (project / entry["path"]).resolve()
                if target != root and root not in target.parents:
                    raise ValueError(f"Extraction target leaves new root: {target}")
                meta = pathlib.Path(str(target) + ".meta")
                if meta.exists():
                    match = GUID.search(meta.read_bytes())
                    if match and match.group(1).decode("ascii").lower() != entry["guid"]:
                        raise ValueError(f"Unrelated GUID at resumed target: {target}")
                destinations[entry["guid"] + "/asset.meta"] = meta
                if entry["folder"]:
                    target.mkdir(parents=True, exist_ok=True)
                elif entry["has_asset"]:
                    target.parent.mkdir(parents=True, exist_ok=True)
                    destinations[entry["guid"] + "/asset"] = target
                else:
                    raise ValueError(f"Non-folder asset has no payload: {entry['path']}")
            # Reading compressed archives in physical order avoids a full gzip rewind per file.
            with tarfile.open(package, "r|gz") as archive:
                for member in archive:
                    destination = destinations.get(member.name)
                    if destination is None:
                        continue
                    guid = member.name.split("/")[0]
                    if destination.exists() and not resume and written.get(destination) != guid:
                        raise ValueError(f"Refusing to overwrite an existing file: {destination}")
                    payload = archive.extractfile(member)
                    temporary = output / "extraction-payload.tmp"
                    sha = hashlib.sha256()
                    with temporary.open("wb") as stream:
                        for block in iter(lambda: payload.read(1024 * 1024), b""):
                            stream.write(block)
                            sha.update(block)
                    if destination.exists() and hashlib.sha256(destination.read_bytes()).digest() == sha.digest():
                        temporary.unlink()
                    else:
                        temporary.replace(destination)
                    written[destination] = guid
        counts.append({"pack": pack["name"], "root": pack["root"], "assets": len(owned)})
        print(f"EXTRACTED {pack['name']}: {len(owned)} asset GUIDs -> {pack['root']}", flush=True)
    (output / "extraction.json").write_text(json.dumps(counts, indent=2), encoding="utf-8")
    print(f"EXTRACTED {len(counts)} reviewed roots; {len(written)} new files; originals verified before extraction.", flush=True)


def audit_references(project: pathlib.Path, output: pathlib.Path):
    """Read-only GUID audit of newly imported serialized prefab/material/scene data."""
    plan = json.loads((output / "plan.json").read_text(encoding="utf-8"))
    known = project_guids(project)
    for meta in (project / "Library/PackageCache").rglob("*.meta"):
        match = GUID.search(meta.read_bytes())
        if match:
            known.setdefault(match.group(1).decode("ascii").lower(), [str(meta)])
    missing = collections.defaultdict(list)
    files = references = 0
    for pack in plan["packs"]:
        for entry in pack["assets"]:
            if pathlib.PurePosixPath(entry["path"]).suffix.lower() not in {".mat", ".prefab", ".unity", ".asset"}:
                continue
            path = project / entry["path"]
            if not path.is_file():
                continue
            data = path.read_bytes()
            if not data.startswith(b"%YAML"):
                continue
            files += 1
            for match in set(re.findall(rb"guid:\s*([0-9a-fA-F]{32})", data)):
                guid = match.decode("ascii").lower()
                if guid.startswith("0000000000000000"):
                    continue
                references += 1
                if guid not in known:
                    missing[guid].append(entry["path"])
    report = {"files": files, "references": references, "unresolved": dict(missing)}
    (output / "reference-audit.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"GUID references: {files} serialized files, {references} references, {len(missing)} unresolved GUIDs.")
    for guid, paths in missing.items():
        print(guid, paths[:4])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["analyze", "prepare", "finalize", "repack", "refresh_helper", "extract", "extract_resume", "audit"])
    parser.add_argument("--project", type=pathlib.Path, required=True)
    parser.add_argument("--output", type=pathlib.Path, required=True)
    args = parser.parse_args()
    project = args.project.resolve()
    output = args.output.resolve()
    if project not in output.parents:
        raise ValueError("Reports must stay inside the explicitly selected workspace.")
    if args.mode == "analyze":
        analyze(project, output)
    elif args.mode == "prepare":
        prepare(project, output)
    elif args.mode == "extract":
        extract_reviewed(project, output)
    elif args.mode == "extract_resume":
        extract_reviewed(project, output, resume=True)
    elif args.mode == "audit":
        audit_references(project, output)
    elif args.mode == "repack":
        plan = json.loads((output / "plan.json").read_text(encoding="utf-8"))
        for pack in plan["packs"]:
            for key in ("basePackage", "urpPackage"):
                source = project / pack[key]
                target = source.with_suffix(".gnu.unitypackage")
                stage_package(source, target, package_entries(source))
                target.replace(source)
            print(f"GNU archive format: {pack['name']}", flush=True)
    elif args.mode == "refresh_helper":
        # Only the importer authored for this operation may change after the initial snapshot.
        # Never rebaseline pre-existing project data after importing has started.
        plan_path = output / "plan.json"
        plan = json.loads(plan_path.read_text(encoding="utf-8"))
        helper = "Assets/Editor/MegaBundleMissingPackImporter.cs"
        record = next(item for item in plan["originals"] if item["path"] == helper)
        record["sha256"] = hashlib.sha256((project / helper).read_bytes()).hexdigest()
        plan_path.write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding="utf-8")
        print("Refreshed only agent-owned importer hash; original project hashes unchanged.", flush=True)
    else:
        plan_path = output / "plan.json"
        plan = json.loads(plan_path.read_text(encoding="utf-8"))
        prefix = output.relative_to(project).as_posix()
        for pack in plan["packs"]:
            for key in ("basePackage", "urpPackage"):
                pack[key] = prefix + "/staged/" + pathlib.PurePosixPath(pack[key]).name
                if not (project / pack[key]).is_file():
                    raise ValueError(f"Staged package is missing: {pack[key]}")
        plan["originals"] = snapshot(project)
        plan_path.write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"FINALIZED: {len(plan['packs'])} packs, {len(plan['originals'])} protected files.", flush=True)


if __name__ == "__main__":
    main()
