using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VSplatter.StateMachine.Editor
{
    /// <summary>
    /// Display-only orthogonal routing. One path per ORIGINAL transition row.
    /// Ports are separated, cards are obstacles, and occupied corridors cost extra.
    /// Never changes definitions, priorities, endpoints or the runtime graph.
    /// </summary>
    internal static class HfsmGraphRouting
    {
        private const float Clearance = 24, BendCost = 26, Epsilon = .05f;
        private sealed class Port
        {
            public int Box, Side, Row;
            public bool Source;
            public float Sort;
            public Vector2 Point, Lead;
        }
        private readonly struct Segment
        {
            public readonly Vector2 A, B;
            public Segment(Vector2 a, Vector2 b) { A = a; B = b; }
        }
        private sealed class Grid
        {
            public float[] X, Y;
            public bool[] Right, Down;
            public int Width => X.Length;
            public int Count => X.Length * Y.Length;
            public Vector2 Point(int id) => new(X[id % Width], Y[id / Width]);
            public int Index(Vector2 point) => Array.BinarySearch(Y, point.y) * Width + Array.BinarySearch(X, point.x);
        }
        private sealed class Heap
        {
            private readonly List<(int id, float score)> _items = new();
            public int Count => _items.Count;
            public void Push(int id, float score)
            {
                _items.Add((id, score)); int at = _items.Count - 1;
                while (at > 0)
                {
                    int parent = (at - 1) / 2;
                    if (_items[parent].score <= score) break;
                    _items[at] = _items[parent]; at = parent;
                }
                _items[at] = (id, score);
            }
            public int Pop()
            {
                int result = _items[0].id;
                var last = _items[_items.Count - 1]; _items.RemoveAt(_items.Count - 1);
                if (_items.Count == 0) return result;
                int at = 0;
                while (at * 2 + 1 < _items.Count)
                {
                    int child = at * 2 + 1;
                    if (child + 1 < _items.Count && _items[child + 1].score < _items[child].score) child++;
                    if (_items[child].score >= last.score) break;
                    _items[at] = _items[child]; at = child;
                }
                _items[at] = last; return result;
            }
        }

        internal static List<Vector2[]> Build(Rect[] boxes, Vector2Int[] endpoints)
        {
            var result = new List<Vector2[]>();
            if (endpoints.Length == 0) return result;
            var ports = new List<Port>();
            for (int i = 0; i < endpoints.Length; i++)
            {
                var pair = endpoints[i];
                AddPort(pair.x, pair.y, true, i); AddPort(pair.y, pair.x, false, i);
            }
            void AddPort(int box, int other, bool source, int row)
            {
                var a = boxes[box]; var b = boxes[other]; var delta = b.center - a.center;
                // 0 left, 1 right, 2 top, 3 bottom. Facing ports keep adjacent flow short.
                int side = box == other ? 2 : Mathf.Abs(delta.x) > a.width * .5f ? delta.x > 0 ? 1 : 0 : delta.y > 0 ? 3 : 2;
                ports.Add(new Port { Box = box, Side = side, Row = row, Source = source,
                    Sort = side < 2 ? b.center.y : b.center.x + (box == other ? source ? a.width * .3f : -a.width * .3f : 0) });
            }
            foreach (var group in ports.GroupBy(p => (p.Box, p.Side)))
            {
                var ordered = group.OrderBy(p => p.Sort).ThenBy(p => p.Row).ThenBy(p => p.Source).ToArray();
                var rect = boxes[group.Key.Box];
                for (int i = 0; i < ordered.Length; i++)
                {
                    var port = ordered[i]; float fraction = (i + 1f) / (ordered.Length + 1f);
                    float y = Mathf.Lerp(rect.yMin + 16, rect.yMax - 16, fraction);
                    float x = Mathf.Lerp(rect.xMin + 20, rect.xMax - 20, fraction);
                    port.Point = port.Side == 0 ? new Vector2(rect.xMin, y) : port.Side == 1 ? new Vector2(rect.xMax, y)
                        : port.Side == 2 ? new Vector2(x, rect.yMin) : new Vector2(x, rect.yMax);
                    var normal = port.Side == 0 ? Vector2.left : port.Side == 1 ? Vector2.right : port.Side == 2 ? Vector2.down : Vector2.up;
                    // GUI y grows downwards. Self loops have a separate overhead lane.
                    float lead = Clearance;
                    if (endpoints[port.Row].x == endpoints[port.Row].y)
                        lead += 18 + endpoints.Take(port.Row).Count(e => e == endpoints[port.Row]) * 16;
                    port.Lead = port.Point + normal * lead;
                }
            }
            var obstacles = boxes.Select(Expand).ToArray();
            var grid = MakeGrid(obstacles, ports);
            var traffic = new List<Segment>();
            // Deterministic order: short local routes first, then longer cross-row routes.
            // Results are restored to original row order; this is NOT execution ordering.
            var routes = new Vector2[endpoints.Length][];
            foreach (int row in Enumerable.Range(0, endpoints.Length).OrderBy(i =>
                Vector2.Distance(boxes[endpoints[i].x].center, boxes[endpoints[i].y].center)).ThenBy(i => i))
            {
                var a = ports.First(p => p.Row == row && p.Source); var b = ports.First(p => p.Row == row && !p.Source);
                var inner = FindPath(grid, a.Lead, b.Lead, traffic);
                var points = new List<Vector2> { a.Point, a.Lead };
                points.AddRange(inner.Skip(1)); points.Add(b.Point);
                routes[row] = Simplify(points);
                for (int i = 1; i < routes[row].Length; i++) traffic.Add(new Segment(routes[row][i - 1], routes[row][i]));
            }
            result.AddRange(routes); return result;
        }

        private static Rect Expand(Rect rect) => new(rect.x - Clearance, rect.y - Clearance, rect.width + Clearance * 2, rect.height + Clearance * 2);
        private static Grid MakeGrid(Rect[] obstacles, List<Port> ports)
        {
            var x = new List<float>(); var y = new List<float>();
            foreach (var box in obstacles) { x.Add(box.xMin); x.Add(box.xMax); y.Add(box.yMin); y.Add(box.yMax); }
            foreach (var port in ports) { x.Add(port.Lead.x); y.Add(port.Lead.y); }
            float left = x.Min() - 64, right = x.Max() + 64, top = y.Min() - 64, bottom = y.Max() + 64;
            for (float at = left; at <= right; at += 32) x.Add(at);
            for (float at = top; at <= bottom; at += 32) y.Add(at);
            x.Add(right); y.Add(bottom);
            var grid = new Grid { X = x.Distinct().OrderBy(v => v).ToArray(), Y = y.Distinct().OrderBy(v => v).ToArray() };
            grid.Right = new bool[grid.Count]; grid.Down = new bool[grid.Count];
            for (int i = 0; i < grid.Count; i++)
            {
                var point = grid.Point(i);
                if (i % grid.Width < grid.Width - 1) grid.Right[i] = Clear(point, grid.Point(i + 1), obstacles);
                if (i / grid.Width < grid.Y.Length - 1) grid.Down[i] = Clear(point, grid.Point(i + grid.Width), obstacles);
            }
            return grid;
        }
        private static bool Clear(Vector2 a, Vector2 b, IEnumerable<Rect> obstacles)
            => !obstacles.Any(rect => CrossesCard(a, b, rect));

        internal static bool CrossesCard(Vector2 a, Vector2 b, Rect rect)
        {
            if (Mathf.Abs(a.y - b.y) < Epsilon)
                return a.y > rect.yMin + Epsilon && a.y < rect.yMax - Epsilon
                    && Mathf.Max(a.x, b.x) > rect.xMin + Epsilon && Mathf.Min(a.x, b.x) < rect.xMax - Epsilon;
            if (Mathf.Abs(a.x - b.x) < Epsilon)
                return a.x > rect.xMin + Epsilon && a.x < rect.xMax - Epsilon
                    && Mathf.Max(a.y, b.y) > rect.yMin + Epsilon && Mathf.Min(a.y, b.y) < rect.yMax - Epsilon;
            return true; // Routes are orthogonal; a diagonal is never an acceptable bypass.
        }

        private static Vector2[] FindPath(Grid grid, Vector2 from, Vector2 to, List<Segment> traffic)
        {
            int start = grid.Index(from), goal = grid.Index(to);
            var g = Enumerable.Repeat(float.PositiveInfinity, grid.Count * 3).ToArray();
            var parent = Enumerable.Repeat(-1, g.Length).ToArray(); var closed = new bool[g.Length];
            var open = new Heap(); int first = start * 3; g[first] = 0; open.Push(first, Manhattan(from, to));
            int found = -1;
            while (open.Count > 0)
            {
                int current = open.Pop(); if (closed[current]) continue; closed[current] = true;
                int node = current / 3, direction = current % 3;
                if (node == goal) { found = current; break; }
                if (node % grid.Width < grid.Width - 1 && grid.Right[node]) Visit(node + 1, 1);
                if (node % grid.Width > 0 && grid.Right[node - 1]) Visit(node - 1, 1);
                if (node / grid.Width < grid.Y.Length - 1 && grid.Down[node]) Visit(node + grid.Width, 2);
                if (node / grid.Width > 0 && grid.Down[node - grid.Width]) Visit(node - grid.Width, 2);
                void Visit(int next, int nextDirection)
                {
                    int id = next * 3 + nextDirection; if (closed[id]) return;
                    var a = grid.Point(node); var b = grid.Point(next);
                    float cost = g[current] + Manhattan(a, b) + (direction > 0 && direction != nextDirection ? BendCost : 0) + TrafficCost(a, b, traffic);
                    if (cost >= g[id]) return;
                    g[id] = cost; parent[id] = current; open.Push(id, cost + Manhattan(b, to));
                }
            }
            // Overlapping manually dragged cards can block every port. Keep the edge
            // identifiable rather than dropping a real transition from the view.
            if (found < 0) return new[] { from, new Vector2(from.x, to.y), to };
            var points = new List<Vector2>();
            for (int at = found; at >= 0; at = parent[at]) points.Add(grid.Point(at / 3));
            points.Reverse(); return points.ToArray();
        }
        private static float Manhattan(Vector2 a, Vector2 b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        private static float TrafficCost(Vector2 a, Vector2 b, List<Segment> traffic)
        {
            float cost = 0; bool horizontal = Mathf.Abs(a.y - b.y) < Epsilon;
            foreach (var line in traffic)
            {
                bool otherHorizontal = Mathf.Abs(line.A.y - line.B.y) < Epsilon;
                if (horizontal == otherHorizontal)
                {
                    float distance = horizontal ? Mathf.Abs(a.y - line.A.y) : Mathf.Abs(a.x - line.A.x);
                    if (distance >= 12) continue;
                    float overlap = horizontal ? Overlap(a.x, b.x, line.A.x, line.B.x) : Overlap(a.y, b.y, line.A.y, line.B.y);
                    cost += overlap * (distance < Epsilon ? 3 : .35f);
                }
                else
                {
                    var h1 = horizontal ? a : line.A; var h2 = horizontal ? b : line.B;
                    var v1 = horizontal ? line.A : a; var v2 = horizontal ? line.B : b;
                    if (v1.x > Mathf.Min(h1.x, h2.x) + Epsilon && v1.x < Mathf.Max(h1.x, h2.x) - Epsilon
                        && h1.y > Mathf.Min(v1.y, v2.y) + Epsilon && h1.y < Mathf.Max(v1.y, v2.y) - Epsilon) cost += 28;
                }
            }
            return cost;
        }
        private static float Overlap(float a, float b, float c, float d)
            => Mathf.Max(0, Mathf.Min(Mathf.Max(a, b), Mathf.Max(c, d)) - Mathf.Max(Mathf.Min(a, b), Mathf.Min(c, d)));
        private static Vector2[] Simplify(List<Vector2> points)
        {
            var result = new List<Vector2>();
            foreach (var point in points)
            {
                if (result.Count > 0 && Vector2.Distance(result[result.Count - 1], point) < Epsilon) continue;
                if (result.Count > 1)
                {
                    var a = result[result.Count - 2]; var b = result[result.Count - 1];
                    if (Mathf.Abs(a.x - b.x) < Epsilon && Mathf.Abs(b.x - point.x) < Epsilon
                        || Mathf.Abs(a.y - b.y) < Epsilon && Mathf.Abs(b.y - point.y) < Epsilon) result.RemoveAt(result.Count - 1);
                }
                result.Add(point);
            }
            return result.ToArray();
        }
        internal static float Distance(Vector2 point, Vector2[] route)
        {
            float result = float.PositiveInfinity;
            for (int i = 1; i < route.Length; i++)
            {
                var a = route[i - 1]; var delta = route[i] - a;
                float t = delta.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude) : 0;
                result = Mathf.Min(result, Vector2.Distance(point, a + delta * t));
            }
            return result;
        }
    }
}
