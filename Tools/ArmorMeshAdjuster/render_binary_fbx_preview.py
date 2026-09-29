"""Render simple orthographic wireframe previews without external packages."""

import argparse
import struct
import zlib
from pathlib import Path

from inspect_binary_fbx import BinaryFbxReader


WIDTH = 1500
HEIGHT = 620
PANEL_WIDTH = WIDTH // 3
MARGIN = 45
BACKGROUND = (25, 31, 39)
GRID = (54, 64, 76)
COLORS = ((214, 179, 111), (161, 112, 65), (118, 176, 210))


def mesh_data(nodes):
    objects = next(node for node in nodes if node.name == "Objects")
    result = []
    for geometry in objects.children:
        if geometry.name != "Geometry":
            continue
        vertices_node = geometry.child("Vertices")
        indices_node = geometry.child("PolygonVertexIndex")
        if vertices_node is None or indices_node is None:
            continue
        values = vertices_node.properties[0].value
        points = list(zip(values[0::3], values[1::3], values[2::3]))
        edges = set()
        polygon = []
        for raw_index in indices_node.properties[0].value:
            index = -raw_index - 1 if raw_index < 0 else raw_index
            polygon.append(index)
            if raw_index < 0:
                for first, second in zip(polygon, polygon[1:] + polygon[:1]):
                    edges.add(tuple(sorted((first, second))))
                polygon.clear()
        result.append((points, sorted(edges)))
    return result


def put_pixel(image, x, y, color):
    if 0 <= x < WIDTH and 0 <= y < HEIGHT:
        index = (y * WIDTH + x) * 3
        image[index : index + 3] = bytes(color)


def draw_line(image, x0, y0, x1, y1, color):
    dx = abs(x1 - x0)
    sx = 1 if x0 < x1 else -1
    dy = -abs(y1 - y0)
    sy = 1 if y0 < y1 else -1
    error = dx + dy
    while True:
        put_pixel(image, x0, y0, color)
        if x0 == x1 and y0 == y1:
            break
        twice = 2 * error
        if twice >= dy:
            error += dy
            x0 += sx
        if twice <= dx:
            error += dx
            y0 += sy


def draw_preview(meshes):
    image = bytearray(BACKGROUND * (WIDTH * HEIGHT))
    views = ((0, 1), (2, 1), (0, 2))
    for panel, axes in enumerate(views):
        panel_x = panel * PANEL_WIDTH
        for grid_index in range(1, 10):
            x = panel_x + grid_index * PANEL_WIDTH // 10
            draw_line(image, x, 0, x, HEIGHT - 1, GRID)
            y = grid_index * HEIGHT // 10
            draw_line(image, panel_x, y, panel_x + PANEL_WIDTH - 1, y, GRID)

        all_points = [point for points, _ in meshes for point in points]
        minimum = tuple(min(point[axis] for point in all_points) for axis in axes)
        maximum = tuple(max(point[axis] for point in all_points) for axis in axes)
        span_x = max(1e-6, maximum[0] - minimum[0])
        span_y = max(1e-6, maximum[1] - minimum[1])
        scale = min(
            (PANEL_WIDTH - MARGIN * 2) / span_x,
            (HEIGHT - MARGIN * 2) / span_y,
        )
        center_x = (minimum[0] + maximum[0]) * 0.5
        center_y = (minimum[1] + maximum[1]) * 0.5

        def project(point):
            x = panel_x + PANEL_WIDTH * 0.5 + (point[axes[0]] - center_x) * scale
            y = HEIGHT * 0.5 - (point[axes[1]] - center_y) * scale
            return round(x), round(y)

        for mesh_index, (points, edges) in enumerate(meshes):
            color = COLORS[mesh_index % len(COLORS)]
            projected = [project(point) for point in points]
            for first, second in edges:
                draw_line(image, *projected[first], *projected[second], color)
    return image


def png_chunk(kind, data):
    return (
        struct.pack(">I", len(data))
        + kind
        + data
        + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)
    )


def write_png(path, image):
    scanlines = b"".join(
        b"\x00" + bytes(image[y * WIDTH * 3 : (y + 1) * WIDTH * 3])
        for y in range(HEIGHT)
    )
    data = bytearray(b"\x89PNG\r\n\x1a\n")
    data.extend(png_chunk(b"IHDR", struct.pack(">IIBBBBB", WIDTH, HEIGHT, 8, 2, 0, 0, 0)))
    data.extend(png_chunk(b"IDAT", zlib.compress(scanlines, 9)))
    data.extend(png_chunk(b"IEND", b""))
    path.write_bytes(data)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("fbx", type=Path)
    parser.add_argument("png", type=Path)
    args = parser.parse_args()
    nodes = BinaryFbxReader(args.fbx).read()
    args.png.parent.mkdir(parents=True, exist_ok=True)
    write_png(args.png, draw_preview(mesh_data(nodes)))
    print(args.png)


if __name__ == "__main__":
    main()
