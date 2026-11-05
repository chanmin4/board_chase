using System;
using System.Reflection;
using NUnit.Framework;

internal static class ManagedCombatDecisionTests
{
    public static int Main()
    {
        int passed = 0, failed = 0;
        foreach (var method in typeof(HfsmCombatDecisionPolicyTests).GetMethods())
        {
            if (!method.IsDefined(typeof(TestAttribute))) continue;
            try { method.Invoke(new HfsmCombatDecisionPolicyTests(), null); Console.WriteLine("PASS " + method.Name); passed++; }
            catch (Exception ex) { Console.WriteLine("FAIL " + method.Name + " " + (ex.InnerException ?? ex)); failed++; }
        }
        Console.WriteLine($"{passed} passed / {failed} failed (combat decisions, no Unity simulation)");
        return failed == 0 ? 0 : 1;
    }
}
