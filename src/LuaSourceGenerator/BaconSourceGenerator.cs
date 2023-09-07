using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.CompilerServices;

namespace LuaSourceGenerator;

[Generator]
public class BaconSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(PostInitCallback);

        var pipeline = context.SyntaxProvider.CreateSyntaxProvider(
                       predicate: (s, cancellationToken) => IsSyntaxTarget(s, cancellationToken),
                       transform: (ctx, cancellationToken) => GetSymbol(ctx, cancellationToken))
                       .Select((type, _) => CreateModel(type));

        context.RegisterSourceOutput(pipeline, Execute);

    }
    private void Execute(SourceProductionContext context, GeneratorModel model)
    {
        var source = LuaBuilder.Compile(model);

        context.AddSource("Roslyn.Generated.BaconSource.g.cs", source);
    }

    private GeneratorModel CreateModel(INamedTypeSymbol typeSymbol)
    {
        string className = typeSymbol.Name;
            
        return new GeneratorModel(className);
    }

    private void PostInitCallback(IncrementalGeneratorPostInitializationContext context)
    {
        string attributeText = @"using System;
namespace Bacon;

[AttributeUsage(AttributeTargets.Class)]
public class BaconLuaAttribute : Attribute
{
    public string Name { get; set; }
    public BaconLuaAttribute(string name = null)
    {
        Name = name ?? ""BaconScript"";
    }
}
";
        context.AddSource("Roslyn.Generated.BaconLuaAttribute.g.cs", attributeText);
    }

    private bool IsSyntaxTarget(SyntaxNode s, System.Threading.CancellationToken cancellationToken)
    {
        return !cancellationToken.IsCancellationRequested
            && s is ClassDeclarationSyntax c
            && c.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)
            && !c.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword);
    }

    private INamedTypeSymbol GetSymbol(GeneratorSyntaxContext ctx, System.Threading.CancellationToken cancellationToken)
    {
        var candidate = Unsafe.As<ClassDeclarationSyntax>(ctx.Node);
        var symbol = ctx.SemanticModel.GetDeclaredSymbol(candidate, cancellationToken);

        return symbol as INamedTypeSymbol;
    }
}
