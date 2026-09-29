"""Generate the complete tiered bullet asset set used by Unity.

The generated prefabs are standalone prefab assets, not prefab variants. Existing
asset meta files are retained so Unity references stay stable when this script is
run again. Icons follow the project's Icon Maker output contract: 512 x 512 RGBA,
transparent background, horizontal presentation, Sprite (2D and UI) / Single.
"""

from __future__ import annotations

import hashlib
import re
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
PREFAB_ROOT = ROOT / "Assets/Prefabs/GameScene/TreasureObject/Bullet"
SO_ROOT = ROOT / "Assets/ScriptableObject/GameScene/GameItem/Bullet"
ICON_ROOT = ROOT / "Assets/Art/GameItem/Icon/Bullet"

ROOT_GAME_OBJECT_FILE_ID = 7611937310320503209
BODY_MATERIAL_GUID = "58d93738923138048904c6c73368d3cf"
HEAD_MATERIAL_GUIDS = {
    1: "3d8764cc9d9797e48abd566ba40debbc",
    2: "ed38650b407ff124ca36157915f657db",
    3: "0dcd4ef6c3e498d4e93d426ba8c9ffc0",
    4: "f4d71b93078267a4ab46b724e2020d91",
    5: "e822c667c8f1eff40ae2aefbbe04639d",
    6: "7d9368afecf9e8c4b88676917321a1dd",
    7: "5de4c239749d8f24b93e91eb66da4fd6",
    8: "88bfa60635bce5b478383f8d3bddd695",
}
HEAD_COLORS = {
    1: (207, 164, 38),
    2: (245, 245, 238),
    3: (200, 156, 48),
    4: (65, 166, 73),
    5: (27, 166, 157),
    6: (52, 112, 218),
    7: (137, 61, 211),
    8: (232, 45, 55),
}


@dataclass(frozen=True)
class FamilySpec:
    name: str
    script_guid: str
    prefab_template: Path
    template_head_material_guid: str
    icon_shape: str
    pellet_count: int | None = None


FAMILIES = (
    FamilySpec(
        "LightBullet",
        "31e6c2435e704bf4ac847a5f88178df0",
        PREFAB_ROOT / "LightBullet/LightBulletLV1.prefab",
        HEAD_MATERIAL_GUIDS[1],
        "light",
    ),
    FamilySpec(
        "MediumBullet",
        "968b7d06d1c34fc799bf16e33c47e3a9",
        PREFAB_ROOT / "MediumBullet/MediumBullet.prefab",
        BODY_MATERIAL_GUID,
        "medium",
    ),
    FamilySpec(
        "HeavyBullet",
        "2f8c7a11598a4676a522ffaf130e20d9",
        PREFAB_ROOT / "HeavyBullet/HeavyBullet.prefab",
        BODY_MATERIAL_GUID,
        "heavy",
    ),
    FamilySpec(
        "SniperBullet",
        "8d41985e436a49f2a8af85bd205f26b3",
        PREFAB_ROOT / "SniperBullet/SniperBullet.prefab",
        BODY_MATERIAL_GUID,
        "sniper",
    ),
    FamilySpec(
        "ShotgunShell",
        "bc0ee8da29db48bfb234442298466091",
        PREFAB_ROOT / "ShotgunShell/ShotgunShell.prefab",
        BODY_MATERIAL_GUID,
        "shotgun",
        pellet_count=8,
    ),
)


def deterministic_guid(key: str) -> str:
    return hashlib.md5(f"board_chase/bullet-tier/{key}".encode("utf-8")).hexdigest()


def read_meta_guid(path: Path) -> str | None:
    meta = path.with_suffix(path.suffix + ".meta")
    if not meta.exists():
        return None
    match = re.search(r"^guid:\s*([0-9a-f]{32})\s*$", meta.read_text(encoding="utf-8"), re.MULTILINE)
    return match.group(1) if match else None


def write_text_if_changed(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists() and path.read_text(encoding="utf-8") == text:
        return
    path.write_text(text, encoding="utf-8")


def ensure_folder_meta(folder: Path) -> None:
    folder.mkdir(parents=True, exist_ok=True)
    meta = folder.with_suffix(".meta")
    if meta.exists():
        return
    guid = deterministic_guid(folder.relative_to(ROOT).as_posix() + "/")
    write_text_if_changed(
        meta,
        f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
    )


def ensure_prefab_meta(path: Path) -> str:
    guid = read_meta_guid(path) or deterministic_guid(path.relative_to(ROOT).as_posix())
    meta = f"""fileFormatVersion: 2
guid: {guid}
PrefabImporter:
  externalObjects: {{}}
  userData: Generated standalone tiered bullet prefab.
  assetBundleName: 
  assetBundleVariant: 
"""
    write_text_if_changed(path.with_suffix(path.suffix + ".meta"), meta)
    return guid


def ensure_native_asset_meta(path: Path) -> str:
    guid = read_meta_guid(path) or deterministic_guid(path.relative_to(ROOT).as_posix())
    meta = f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: Generated tiered bullet data.
  assetBundleName: 
  assetBundleVariant: 
"""
    write_text_if_changed(path.with_suffix(path.suffix + ".meta"), meta)
    return guid


def generate_prefab(spec: FamilySpec, tier: int) -> tuple[Path, str]:
    text = spec.prefab_template.read_text(encoding="utf-8")
    asset_name = f"{spec.name}LV{tier}"

    # Only the root name changes. Body/head object names remain semantic and stable.
    root_name_pattern = re.compile(rf"(^\s*m_Name:\s*){re.escape(spec.name)}(?:LV1)?\s*$", re.MULTILINE)
    text, count = root_name_pattern.subn(rf"\g<1>{asset_name}", text, count=1)
    if count != 1:
        raise RuntimeError(f"Could not identify root object in {spec.prefab_template}")

    text, count = re.subn(
        rf"(m_Materials:\s*\n\s*- \{{fileID: 2100000, guid: ){spec.template_head_material_guid}(, type: 2\}})",
        rf"\g<1>{HEAD_MATERIAL_GUIDS[tier]}\g<2>",
        text,
        count=1 if spec.name != "LightBullet" else 0,
    )
    if count != 1:
        # Light's first renderer is the body. Its head material is the second known
        # renderer value, so direct GUID replacement is unambiguous in the LV1 template.
        text, count = re.subn(
            rf"guid: {spec.template_head_material_guid}",
            f"guid: {HEAD_MATERIAL_GUIDS[tier]}",
            text,
            count=1,
        )
    if count != 1:
        raise RuntimeError(f"Could not replace head material in {spec.prefab_template}")

    out = PREFAB_ROOT / spec.name / f"{asset_name}.prefab"
    write_text_if_changed(out, text)
    return out, ensure_prefab_meta(out)


def scale_color(color: tuple[int, int, int], factor: float) -> tuple[int, int, int, int]:
    return tuple(max(0, min(255, round(channel * factor))) for channel in color) + (255,)


def load_obj(path: Path) -> tuple[list[tuple[float, float, float]], list[list[int]]]:
    vertices: list[tuple[float, float, float]] = []
    faces: list[list[int]] = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("v "):
            vertices.append(tuple(map(float, line.split()[1:4])))
        elif line.startswith("f "):
            faces.append([int(token.split("/")[0]) - 1 for token in line.split()[1:]])
    return vertices, faces


def draw_obj_bullet(family_name: str, head_color: tuple[int, int, int]) -> Image.Image:
    """Render the separated project meshes in the same horizontal icon pose."""
    mesh_root = ROOT / "Assets/Art/Mesh/Bullet/Separated"
    parts = (
        (*load_obj(mesh_root / f"{family_name}Body.obj"), (137, 103, 66)),
        (*load_obj(mesh_root / f"{family_name}Head.obj"), head_color),
    )
    projected: list[tuple[float, list[tuple[float, float]], tuple[int, int, int, int]]] = []
    light = (0.50, 0.45, 0.74)

    for vertices, faces, color in parts:
        for face in faces:
            points = [vertices[index] for index in face]
            if len(points) < 3:
                continue
            screen_points = [(point[1], -0.35 * point[0] - 0.94 * point[2]) for point in points]
            depth = sum(0.94 * point[0] - 0.35 * point[2] for point in points) / len(points)
            a, b, c = points[:3]
            u = tuple(b[axis] - a[axis] for axis in range(3))
            v = tuple(c[axis] - a[axis] for axis in range(3))
            normal = (
                u[1] * v[2] - u[2] * v[1],
                u[2] * v[0] - u[0] * v[2],
                u[0] * v[1] - u[1] * v[0],
            )
            length = max(0.000001, sum(value * value for value in normal) ** 0.5)
            normal = tuple(value / length for value in normal)
            brightness = 0.55 + 0.55 * abs(sum(normal[axis] * light[axis] for axis in range(3)))
            projected.append((depth, screen_points, scale_color(color, brightness)))

    xs = [x for _, points, _ in projected for x, _ in points]
    ys = [y for _, points, _ in projected for _, y in points]
    width = max(xs) - min(xs)
    height = max(ys) - min(ys)
    render_scale = min(1500 / max(width, 0.000001), 360 / max(height, 0.000001))
    offset_x = (2048 - (min(xs) + max(xs)) * render_scale) * 0.5
    offset_y = (2048 - (min(ys) + max(ys)) * render_scale) * 0.5

    image = Image.new("RGBA", (2048, 2048), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image, "RGBA")
    for _, points, color in sorted(projected, key=lambda item: item[0]):
        draw.polygon(
            [(x * render_scale + offset_x, y * render_scale + offset_y) for x, y in points],
            fill=color,
        )
    return image.resize((512, 512), Image.Resampling.LANCZOS)


def draw_faceted_bullet(shape: str, head_color: tuple[int, int, int]) -> Image.Image:
    mesh_families = {
        "medium": "MediumBullet",
        "heavy": "HeavyBullet",
        "sniper": "SniperBullet",
        "shotgun": "ShotgunShell",
    }
    if shape in mesh_families:
        return draw_obj_bullet(mesh_families[shape], head_color)

    scale = 4
    image = Image.new("RGBA", (512 * scale, 512 * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image, "RGBA")

    definitions = {
        "light": (92, 326, 424, 61, 0.74),
        "medium": (66, 309, 446, 55, 0.60),
        "heavy": (102, 312, 418, 70, 0.72),
        "sniper": (48, 294, 466, 45, 0.48),
        "shotgun": (82, 375, 432, 73, 0.86),
    }
    left, join, right, radius, nose_ratio = definitions[shape]
    cx, cy = 256, 256
    left, join, right, radius = [value * scale for value in (left, join, right, radius)]
    cy *= scale
    bevel = 13 * scale
    body_color = (137, 103, 66)

    shadow_box = (left + 12 * scale, cy + radius - 3 * scale, right - 3 * scale, cy + radius + 18 * scale)
    draw.ellipse(shadow_box, fill=(0, 0, 0, 38))

    body = [
        (left + bevel, cy - radius),
        (join, cy - radius),
        (join, cy + radius),
        (left + bevel, cy + radius),
        (left, cy + radius - bevel),
        (left, cy - radius + bevel),
    ]
    draw.polygon(body, fill=scale_color(body_color, 0.92))
    draw.polygon(
        [(left + bevel, cy - radius), (join, cy - radius), (join, cy - radius // 5), (left, cy - radius // 5), (left, cy - radius + bevel)],
        fill=scale_color(body_color, 1.20),
    )
    draw.polygon(
        [(left, cy + radius // 3), (join, cy + radius // 3), (join, cy + radius), (left + bevel, cy + radius), (left, cy + radius - bevel)],
        fill=scale_color(body_color, 0.68),
    )
    draw.line([(join, cy - radius), (join, cy + radius)], fill=(63, 44, 30, 190), width=2 * scale)

    if shape == "shotgun":
        head = [
            (join, cy - radius),
            (right - bevel, cy - radius),
            (right, cy - radius + bevel),
            (right, cy + radius - bevel),
            (right - bevel, cy + radius),
            (join, cy + radius),
        ]
    else:
        shoulder = join + round((right - join) * nose_ratio)
        head = [
            (join, cy - radius),
            (shoulder, cy - radius),
            (right, cy),
            (shoulder, cy + radius),
            (join, cy + radius),
        ]
    draw.polygon(head, fill=scale_color(head_color, 0.92))
    if shape == "shotgun":
        draw.polygon(
            [(join, cy - radius), (right - bevel, cy - radius), (right, cy - radius + bevel), (right, cy - radius // 5), (join, cy - radius // 5)],
            fill=scale_color(head_color, 1.22),
        )
    else:
        shoulder = join + round((right - join) * nose_ratio)
        draw.polygon(
            [(join, cy - radius), (shoulder, cy - radius), (right, cy), (shoulder, cy - radius // 5), (join, cy - radius // 5)],
            fill=scale_color(head_color, 1.20),
        )
    draw.polygon(
        [(join, cy + radius // 3), (right if shape == "shotgun" else shoulder, cy + radius // 3), (right, cy) if shape != "shotgun" else (right, cy + radius - bevel), (right - bevel, cy + radius) if shape == "shotgun" else (shoulder, cy + radius), (join, cy + radius)],
        fill=scale_color(head_color, 0.66),
    )

    # Rim detail makes the low-poly item legible against both light and dark UI.
    outline = tuple(scale_color(head_color, 0.42)[:3]) + (190,)
    draw.line(head + [head[0]], fill=outline, width=2 * scale, joint="curve")
    return image.resize((512, 512), Image.Resampling.LANCZOS)


def texture_meta(guid: str, sprite_id: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
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
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
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
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
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
    spriteID: {sprite_id}
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
  userData: Generated with the bullet Icon Maker batch pipeline (512 RGBA transparent).
  assetBundleName: 
  assetBundleVariant: 
"""


def generate_icon(spec: FamilySpec, tier: int) -> tuple[Path, str]:
    folder = ICON_ROOT / spec.name
    ensure_folder_meta(folder)
    path = folder / f"{spec.name}LV{tier}.png"
    path.parent.mkdir(parents=True, exist_ok=True)
    # Unity's Icon Maker owns the final PNG so its real URP materials and lighting
    # stay consistent with the project. Only create the software placeholder when
    # the icon does not exist yet; rerunning this asset generator must never replace
    # an actual captured icon.
    if not path.exists():
        image = draw_faceted_bullet(spec.icon_shape, HEAD_COLORS[tier])
        image.save(path, "PNG", optimize=True)
    guid = read_meta_guid(path) or deterministic_guid(path.relative_to(ROOT).as_posix())
    sprite_id = deterministic_guid(path.relative_to(ROOT).as_posix() + ":sprite")
    write_text_if_changed(path.with_suffix(path.suffix + ".meta"), texture_meta(guid, sprite_id))
    return path, guid


def bullet_asset_yaml(
    spec: FamilySpec,
    tier: int,
    asset_guid: str,
    prefab_guid: str,
    icon_guid: str,
) -> str:
    name = f"{spec.name}LV{tier}"
    pellet = f"  _pelletCount: {spec.pellet_count}\n" if spec.pellet_count is not None else ""
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {spec.script_guid}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: 
  _guid: {asset_guid}
  _name:
    m_TableReference:
      m_TableCollectionName: 
    m_TableEntryReference:
      m_KeyId: 0
      m_Key: 
    m_FallbackState: 0
    m_WaitForCompletion: 0
    m_LocalVariables: []
  _previewImage: {{fileID: 21300000, guid: {icon_guid}, type: 3}}
  _description:
    m_TableReference:
      m_TableCollectionName: 
    m_TableEntryReference:
      m_KeyId: 0
      m_Key: 
    m_FallbackState: 0
    m_WaitForCompletion: 0
    m_LocalVariables: []
  _rarity: 0
  _purchasePrice: 10
  _worldVisualPrefab: {{fileID: {ROOT_GAME_OBJECT_FILE_ID}, guid: {prefab_guid}, type: 3}}
  _maxStack: 120
  bulletId: {spec.name.lower()}_lv{tier}
  displayName: {name}
  projectilePrefab: {{fileID: {ROOT_GAME_OBJECT_FILE_ID}, guid: {prefab_guid}, type: 3}}
  projectileTrailPrefab: {{fileID: 0}}
  speed: 300
  castRadius: 0.08
  maxLifetime: 3
  spawnOffset: 0.12
  _projectileFlybySoundRadius: 4
  _projectileFlybyEmitDistance: 3
  _projectileFlybySoundIntensity: 0.65
  _projectileImpactSoundRadius: 8
  _projectileImpactSoundIntensity: 1
  _projectileSoundInvestigateDelaySeconds: 0.05
  projectileCollisionMask:
    serializedVersion: 2
    m_Bits: 2248
  projectileTriggerInteraction: 2
  statModifiers: []
  playerDamageableTargetMask:
    serializedVersion: 2
    m_Bits: 64
  enemyDamageableTargetMask:
    serializedVersion: 2
    m_Bits: 8
  penetrationClass: {tier}
  _tier: {tier}
  _healthDamage: 10
  _infectionDamage: 15
  _markAmount: 25
{pellet}  references:
    version: 2
    RefIds: []
"""


def generate_scriptable_object(spec: FamilySpec, tier: int, prefab_guid: str, icon_guid: str) -> Path:
    path = SO_ROOT / f"{spec.name}LV{tier}.asset"
    asset_guid = ensure_native_asset_meta(path)
    write_text_if_changed(path, bullet_asset_yaml(spec, tier, asset_guid, prefab_guid, icon_guid))
    return path


def validate_generated(generated: list[Path]) -> None:
    all_guids: dict[str, Path] = {}
    for meta in ROOT.joinpath("Assets").rglob("*.meta"):
        guid = read_meta_guid(meta.with_suffix(""))
        if guid is None:
            continue
        previous = all_guids.get(guid)
        if previous is not None and previous != meta:
            raise RuntimeError(f"Duplicate GUID {guid}: {previous} and {meta}")
        all_guids[guid] = meta

    for path in generated:
        if not path.exists() or not path.with_suffix(path.suffix + ".meta").exists():
            raise RuntimeError(f"Missing generated asset or meta: {path}")
    for prefab in (path for path in generated if path.suffix == ".prefab"):
        text = prefab.read_text(encoding="utf-8")
        if "--- !u!1001" in text or "m_SourcePrefab:" in text:
            raise RuntimeError(f"Prefab variant marker found in {prefab}")


def main() -> None:
    generated: list[Path] = []
    for spec in FAMILIES:
        ensure_folder_meta(PREFAB_ROOT / spec.name)
        for tier in range(1, 9):
            prefab, prefab_guid = generate_prefab(spec, tier)
            icon, icon_guid = generate_icon(spec, tier)
            asset = generate_scriptable_object(spec, tier, prefab_guid, icon_guid)
            generated.extend((prefab, icon, asset))
            print(f"Generated {spec.name} tier {tier}")
    validate_generated(generated)
    print(f"Validated {len(generated)} generated files (40 prefabs, 40 icons, 40 BulletSO assets).")


if __name__ == "__main__":
    main()
