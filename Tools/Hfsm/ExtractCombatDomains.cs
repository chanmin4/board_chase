// One-time, reviewable source transformation. Emits an apply_patch patch.
// --verify is read-only; an explicit --format also normalizes the extracted sources.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class ExtractCombatDomains
{
    private const string FilePath = "Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs";
    private static string Tokens(SyntaxNode node) => node == null ? "" : string.Join("|", node.DescendantTokens().Select(t => t.RawKind + ":" + t.Text));
    private static string Group(MethodDeclarationSyntax m)
    {
        int l=m.GetLocation().GetLineSpan().StartLinePosition.Line+1;
        string n=m.Identifier.Text;
        if(new[]{"Reset","Awake","OnEnable","OnDisable","LateUpdate","ResolveRefs","ResolveComponentInOwnerHierarchy"}.Contains(n))return null;
        if(m.Modifiers.Any(SyntaxKind.StaticKeyword))return "Utilities";
        if(l>=4769||l>=4191&&l<4249||n=="TryPickPatrolDestination"||n=="HasReached"||n=="GetPlanarDistanceToTarget"||n=="ResolveMoveSpeed"||n=="ResolveChaseStoppingDistance"||n=="RecoverStalledChase"||n=="StopMovement")return "Navigation";
        if(l>=4270&&l<4611)return "Hearing";
        if(l>=3206&&l<3333||l>=3378&&l<3754)return "Evasion";
        if(l>=1310&&l<1936)return "Tactics";
        if(l>=2135&&l<2253)return "Status";
        if(l>=1977&&l<2129||l>=2793&&l<2978||l>=3782&&l<3925)return "Investigation";
        if(l>=2400&&l<2702||l>=569&&l<724)return "EquipmentAttacks";
        if(l>=1070&&l<1310)return "PatrolChase";
        if(l>=2253&&l<2400||l>=3043&&l<3206||l>=3925&&l<4191||n=="ResolveShotsPerSecond")return "RangedAttack";
        return "Perception";
    }

    private sealed class OwnerRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _model;
        private readonly HashSet<string> _owners;
        public OwnerRewriter(SemanticModel model,INamedTypeSymbol owner)
        { _model=model;_owners=new();for(var t=owner;t!=null;t=t.BaseType)_owners.Add(t.ToDisplayString()); }
        private SyntaxNode Qualify(SimpleNameSyntax node)
        {
            if(node.Parent is MemberAccessExpressionSyntax ma&&ma.Name==node)return node;
            if(node.Parent is MemberBindingExpressionSyntax||node.Parent is QualifiedNameSyntax||node.Parent is NameColonSyntax)return node;
            var info=_model.GetSymbolInfo(node);var symbol=info.Symbol??info.CandidateSymbols.FirstOrDefault();
            if(symbol==null||symbol.ContainingType==null||!_owners.Contains(symbol.ContainingType.ToDisplayString()))return node;
            if(!(symbol is IFieldSymbol||symbol is IPropertySymbol||symbol is IMethodSymbol||symbol is IEventSymbol))return node;
            string receiver=symbol.IsStatic?symbol.ContainingType.ToDisplayString():"_owner";
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,SyntaxFactory.ParseExpression(receiver),node.WithoutTrivia()).WithTriviaFrom(node);
        }
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)=>Qualify(node);
        public override SyntaxNode VisitGenericName(GenericNameSyntax node)=>Qualify(node);
        public override SyntaxNode VisitThisExpression(ThisExpressionSyntax node)=>SyntaxFactory.IdentifierName("_owner").WithTriviaFrom(node);
    }

    public static void Main(string[] commandArgs)
    {
        bool verify=commandArgs.Contains("--verify");
        string original=System.IO.File.ReadAllText(verify ? "Library/HfsmRefactor-20260929/original-EnemyShooterCombatController.cs" : FilePath);
        var tree=CSharpSyntaxTree.ParseText(original);var root=tree.GetCompilationUnitRoot();
        var type=root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c=>c.Identifier.Text=="EnemyShooterCombatController");
        if (type.Members.OfType<MethodDeclarationSyntax>().Count() < 140)
            throw new InvalidOperationException("Extraction requires the original controller snapshot, not the already extracted facade. Use --verify.");
        var refs=System.IO.File.ReadAllLines("Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp")
            .Where(l=>l.StartsWith("-r:")).Select(l=>l.Substring(3).Trim('"')).Where(System.IO.File.Exists).Distinct().Select(p=>MetadataReference.CreateFromFile(p)).ToList();
        refs.Add(MetadataReference.CreateFromFile("Library/ScriptAssemblies/Assembly-CSharp.dll"));
        var diagnostics=CSharpSyntaxTree.ParseText(System.IO.File.ReadAllText(Path.ChangeExtension(FilePath,null)+".Diagnostics.cs"));
        var compilation=CSharpCompilation.Create("CombatExtraction",new[]{tree,diagnostics},refs,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model=compilation.GetSemanticModel(tree);var rewriter=new OwnerRewriter(model,model.GetDeclaredSymbol(type));
        if(verify)
        {
            string prefix=Path.ChangeExtension(FilePath,null);
            var targets=Directory.GetFiles(Path.GetDirectoryName(FilePath),"EnemyShooterCombatController*.cs").Where(p=>!p.EndsWith(".Diagnostics.cs")).ToArray();
            foreach(var target in commandArgs.Contains("--format") ? targets : Array.Empty<string>())
            {
                var formatted=CSharpSyntaxTree.ParseText(System.IO.File.ReadAllText(target)).GetCompilationUnitRoot().NormalizeWhitespace().ToFullString();
                System.IO.File.WriteAllText(target,formatted+Environment.NewLine);
            }
            int checkedCount=0;
            foreach(var method in type.Members.OfType<MethodDeclarationSyntax>())
            {
                string group=Group(method);
                var actualRoot=CSharpSyntaxTree.ParseText(System.IO.File.ReadAllText(group==null?FilePath:prefix+"."+group+".cs")).GetCompilationUnitRoot();
                var owner=actualRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c=>c.Identifier.Text==(group==null||group=="Utilities"?"EnemyShooterCombatController":group+"Domain"));
                var actual=owner.Members.OfType<MethodDeclarationSyntax>().Single(m=>m.Identifier.Text==method.Identifier.Text&&m.ParameterList.Parameters.Count==method.ParameterList.Parameters.Count);
                var expected=group==null||group=="Utilities"?method:(MethodDeclarationSyntax)rewriter.Visit(method);
                if(Tokens(expected.Body)!=Tokens(actual.Body)||Tokens(expected.ExpressionBody)!=Tokens(actual.ExpressionBody))throw new Exception("Body changed: "+method.Identifier.Text+"\nEXPECTED "+expected.Body+"\nACTUAL "+actual.Body);
                checkedCount++;
            }
            var current=CSharpSyntaxTree.ParseText(System.IO.File.ReadAllText(FilePath)).GetCompilationUnitRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            foreach(var field in type.Members.OfType<FieldDeclarationSyntax>())
            {
                var found=current.Members.OfType<FieldDeclarationSyntax>().Single(f=>f.Declaration.Variables[0].Identifier.Text==field.Declaration.Variables[0].Identifier.Text);
                if(Tokens(field)!=Tokens(found))throw new Exception("Field contract changed: "+field.Declaration);
            }
            var allMembers=targets.SelectMany(p=>CSharpSyntaxTree.ParseText(File.ReadAllText(p)).GetCompilationUnitRoot().DescendantNodes()
                .OfType<ClassDeclarationSyntax>().Where(c=>c.Identifier.Text=="EnemyShooterCombatController").SelectMany(c=>c.Members)).ToArray();
            foreach(var property in type.Members.OfType<PropertyDeclarationSyntax>())
            {
                var actual=allMembers.OfType<PropertyDeclarationSyntax>().Single(p=>p.Identifier.Text==property.Identifier.Text);
                if(Tokens(property)!=Tokens(actual))throw new Exception("Property contract changed: "+property.Identifier.Text);
            }
            foreach(var method in type.Members.OfType<MethodDeclarationSyntax>())
            {
                var facade=allMembers.OfType<MethodDeclarationSyntax>().Single(m=>m.Identifier.Text==method.Identifier.Text&&m.ParameterList.Parameters.Count==method.ParameterList.Parameters.Count);
                string Signature(MethodDeclarationSyntax m)=>Tokens(m.WithBody(null).WithExpressionBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
                if(Signature(method)!=Signature(facade))throw new Exception("Method signature changed: "+method.Identifier.Text);
            }
            Console.WriteLine("Verified "+checkedCount+" original method bodies/signatures (owner qualification only), all original fields and properties.");
            return;
        }
        var kept=new List<MemberDeclarationSyntax>();var groups=new Dictionary<string,List<MethodDeclarationSyntax>>();var properties=new List<MemberDeclarationSyntax>();
        foreach(var member in type.Members)
        {
            if(member is PropertyDeclarationSyntax){properties.Add(member);continue;}
            if(!(member is MethodDeclarationSyntax method)||Group(method)==null){kept.Add(member);continue;}
            string g=Group(method);if(!groups.ContainsKey(g))groups[g]=new();groups[g].Add(method);
        }
        var files=new Dictionary<string,string>();string header="// Behavior domain extracted without changing serialized component identity or call order.\nusing System.Collections.Generic;\nusing UnityEngine;\nusing UnityEngine.AI;\n\npublic partial class EnemyShooterCombatController\n{\n";
        foreach(var pair in groups)
        {
            string g=pair.Key;var text=new StringBuilder(header);
            if(g=="Utilities") {foreach(var m in pair.Value)text.Append(m.ToFullString());text.Append("}\n");files[g]=text.ToString();continue;}
            text.Append($"    private {g}Domain _{char.ToLowerInvariant(g[0])+g.Substring(1)}Domain;\n    private {g}Domain {g} => _{char.ToLowerInvariant(g[0])+g.Substring(1)}Domain ??= new {g}Domain(this);\n\n");
            foreach(var method in pair.Value)
            {
                string args=string.Join(", ",method.ParameterList.Parameters.Select(p=>(p.Modifiers.Any(SyntaxKind.OutKeyword)?"out ":p.Modifiers.Any(SyntaxKind.RefKeyword)?"ref ":p.Modifiers.Any(SyntaxKind.InKeyword)?"in ":"")+p.Identifier.Text));
                string call=g+"."+method.Identifier.Text+(method.TypeParameterList?.ToString()??"")+"("+args+")";
                var wrapper=method.WithBody(null).WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.ParseExpression(call))).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                text.Append(wrapper.NormalizeWhitespace().ToFullString()).Append('\n');
            }
            text.Append($"\n    private sealed class {g}Domain\n    {{\n        private readonly EnemyShooterCombatController _owner;\n        public {g}Domain(EnemyShooterCombatController owner) {{ _owner = owner; }}\n");
            foreach(var method in pair.Value)
            {
                var moved=(MethodDeclarationSyntax)rewriter.Visit(method);
                moved=moved.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
                text.Append(moved.NormalizeWhitespace().ToFullString()).Append('\n');
            }
            text.Append("    }\n}\n");files[g]=text.ToString();
        }
        files["Queries"]=header+string.Concat(properties.Select(p=>p.ToFullString()))+"}\n";
        var updated=root.ReplaceNode(type,type.WithMembers(SyntaxFactory.List(kept))).ToFullString();
        var patch=new StringBuilder("*** Begin Patch\n*** Update File: "+FilePath+"\n@@\n");
        foreach(string line in original.Replace("\r\n","\n").TrimEnd('\n').Split('\n'))patch.Append('-').AppendLine(line);
        foreach(string line in updated.Replace("\r\n","\n").TrimEnd('\n').Split('\n'))patch.Append('+').AppendLine(line);
        foreach(var pair in files)
        {
            patch.AppendLine("*** Add File: "+Path.ChangeExtension(FilePath,null)+"."+pair.Key+".cs");
            foreach(string line in pair.Value.Replace("\r\n","\n").TrimEnd('\n').Split('\n'))patch.Append('+').AppendLine(line);
        }
        patch.AppendLine("*** End Patch");
        Directory.CreateDirectory("Library/HfsmRefactor-20260929");
        System.IO.File.WriteAllText("Library/HfsmRefactor-20260929/controller.patch",patch.ToString());
        System.IO.File.WriteAllText("Library/HfsmRefactor-20260929/controller-methods.txt",string.Join("\n",groups.SelectMany(g=>g.Value.Select(m=>g.Key+"/"+m.Identifier.Text))));
        Console.WriteLine("Generated "+groups.Sum(g=>g.Value.Count)+" method mappings in "+groups.Count+" domains; retained "+kept.Count+" component members.");
    }
}
