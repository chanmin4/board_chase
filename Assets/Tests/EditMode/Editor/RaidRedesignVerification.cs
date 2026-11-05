using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Batch-safe regression entry point. No authored asset/scene is mutated.</summary>
public static class RaidRedesignVerification
{
    public static void Run()
    {
        int passed = 0, failed = 0;
        Type[] fixtures = { typeof(RaidMapFoundationTests), typeof(RaidRuntimeContractTests), typeof(RaidRedesignRegressionTests), typeof(RaidRedesignEconomyTests) };
        foreach (var fixture in fixtures) foreach (var method in fixture.GetMethods())
        {
            var cases = method.GetCustomAttributes<TestCaseAttribute>(); bool hadCase = false;
            foreach (var testCase in cases) { hadCase = true; Execute(fixture, method, testCase.Arguments, ref passed, ref failed); }
            if (!hadCase && method.GetCustomAttribute<TestAttribute>() != null)
                Execute(fixture, method, Array.Empty<object>(), ref passed, ref failed);
        }
        Debug.Log($"[Raid redesign verification] PASSED={passed} FAILED={failed}");
        if (Application.isBatchMode) EditorApplication.Exit(failed == 0 ? 0 : 1);
    }

    private static void Execute(Type fixture, MethodInfo method, object[] args, ref int passed, ref int failed)
    {
        object instance = Activator.CreateInstance(fixture);
        try
        {
            foreach (var setup in fixture.GetMethods()) if (setup.GetCustomAttribute<SetUpAttribute>() != null) setup.Invoke(instance, null);
            method.Invoke(instance, args); passed++;
            Debug.Log($"[Raid verification PASS] {fixture.Name}.{method.Name}({string.Join(",", args)})");
        }
        catch (Exception ex) { failed++; Debug.LogError($"[Raid verification FAIL] {fixture.Name}.{method.Name}: {ex.InnerException ?? ex}"); }
        finally { foreach (var teardown in fixture.GetMethods()) if (teardown.GetCustomAttribute<TearDownAttribute>() != null) teardown.Invoke(instance, null); }
    }
}
