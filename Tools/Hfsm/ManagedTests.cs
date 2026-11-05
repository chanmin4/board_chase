using System;
using System.Reflection;
using NUnit.Framework;
internal static class ManagedTests
{
    public static int Main()
    {
        int pass=0,fail=0;
        foreach(var method in typeof(HfsmRuntimeTests).GetMethods())
        {
            if(!method.IsDefined(typeof(TestAttribute)))continue;
            try {method.Invoke(new HfsmRuntimeTests(),null);Console.WriteLine("PASS "+method.Name);pass++;}
            catch(Exception ex){Console.WriteLine("FAIL "+method.Name+" "+(ex.InnerException??ex));fail++;}
        }
        Console.WriteLine($"{pass} passed / {fail} failed (managed core, no Unity simulation)");
        return fail==0?0:1;
    }
}
