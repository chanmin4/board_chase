using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using VSplatter.StateMachine.Editor;

// Pure geometry smoke checks with Unity's actual managed Vector2/Rect types.
// Runs outside the editor; no assets, scene objects, GUI or runtime graph simulation.
internal static class ManagedRoutingTests
{
    private static int _passed;
    private static void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS " + label); _passed++; }
    private static void Validate(Rect[] boxes, Vector2Int[] endpoints, List<Vector2[]> paths)
    {
        Check(paths.Count == endpoints.Length, "one path per original row");
        Check(paths.All(p => p.Length >= 2), "every arrow has endpoints");
        Check(paths.All(p => Enumerable.Range(1, p.Length - 1).All(i => boxes.All(box => !HfsmGraphRouting.CrossesCard(p[i - 1], p[i], box)))), "orthogonal paths never enter cards");
    }
    private static int Main()
    {
        try
        {
            var boxes = Enumerable.Range(0, 7).Select(i => new Rect(110 + i % 2 * 570, 140 + i / 2 * 270, 300, 120)).ToArray();
            var rows = Enumerable.Range(0, 7).SelectMany(from => Enumerable.Range(0, 7)
                .Where(to => from != to && (from + to) % 3 != 0).Select(to => new Vector2Int(from, to)))
                .Concat(new[] { new Vector2Int(1, 1), new Vector2Int(3, 3), new Vector2Int(0, 1), new Vector2Int(0, 1) }).ToArray();
            var timer = Stopwatch.StartNew(); var paths = HfsmGraphRouting.Build(boxes, rows);
            Console.WriteLine("INFO dense " + rows.Length + " routes: " + timer.Elapsed.TotalMilliseconds.ToString("F1") + " ms");
            Validate(boxes, rows, paths);
            var repeat = HfsmGraphRouting.Build(boxes, rows);
            Check(paths.Select((path, i) => path.SequenceEqual(repeat[i])).All(same => same), "routing is deterministic");
            boxes[3].position += new Vector2(31.25f, 22.7f); Validate(boxes, rows, HfsmGraphRouting.Build(boxes, rows));
            var pair = new[] { new Rect(110, 140, 300, 120), new Rect(680, 140, 300, 120), new Rect(480, 110, 80, 190) };
            var pairRows = new[] { new Vector2Int(0, 1), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, 0) };
            var separated = HfsmGraphRouting.Build(pair, pairRows); Validate(pair, pairRows, separated);
            Check(separated[0][0] != separated[1][0], "parallel rows have different ports");
            Check(separated[0][0] != separated[2][separated[2].Length - 1], "reverse rows have different ports");
            Check(separated[3].Length >= 4, "self loop travels outside its card");
            Check(Math.Abs(HfsmGraphRouting.Distance(new Vector2(70, 2), new[] { Vector2.zero, new Vector2(100, 0), new Vector2(100, 80) }) - 2) < .01f, "hit testing follows rendered segments");
            Console.WriteLine(_passed + " passed / 0 failed (managed geometry, no Unity GUI simulation)"); return 0;
        }
        catch (Exception error) { Console.WriteLine("FAIL " + error); return 1; }
    }
}
