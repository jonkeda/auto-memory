using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoMemory.Analyzers;

/// <summary>
/// Analyzer that detects raw SQL string concatenation/interpolation flowing into SqliteCommand.CommandText.
/// Flags risky patterns; allows parameterized commands and safe constant concatenation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SqlInjectionAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "AUTOMEM001";
    private const string Category = "Security";

    private static readonly LocalizableString Title = "SQL injection risk detected";
    private static readonly LocalizableString MessageFormat = 
        "Potential SQL injection: CommandText assigned with {0}. Use parameterized queries instead.";
    private static readonly LocalizableString Description = 
        "Raw SQL string concatenation or interpolation can lead to SQL injection vulnerabilities. " +
        "Use SqliteCommand parameters (@param) for all dynamic values.";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
    }

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;

        // Check if this is an assignment to CommandText
        if (!IsCommandTextAssignment(assignment, context.SemanticModel))
            return;

        var rightSide = assignment.Right;

        // Check for string interpolation
        if (rightSide is InterpolatedStringExpressionSyntax interpolation)
        {
            // Flag if any interpolation contains non-constant expressions
            if (HasNonConstantInterpolation(interpolation, context.SemanticModel))
            {
                var diagnostic = Diagnostic.Create(Rule, rightSide.GetLocation(), "string interpolation");
                context.ReportDiagnostic(diagnostic);
            }
            return;
        }

        // Check for binary concatenation (string + string)
        if (rightSide is BinaryExpressionSyntax binary && 
            binary.Kind() == SyntaxKind.AddExpression)
        {
            if (HasNonConstantConcatenation(binary, context.SemanticModel))
            {
                var diagnostic = Diagnostic.Create(Rule, rightSide.GetLocation(), "string concatenation");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsCommandTextAssignment(AssignmentExpressionSyntax assignment, SemanticModel model)
    {
        if (assignment.Left is not MemberAccessExpressionSyntax memberAccess)
            return false;

        if (memberAccess.Name.Identifier.Text != "CommandText")
            return false;

        var symbolInfo = model.GetSymbolInfo(memberAccess);
        if (symbolInfo.Symbol is not IPropertySymbol property)
            return false;

        // Check if the containing type is SqliteCommand (or derived)
        var containingType = property.ContainingType;
        return containingType?.Name == "SqliteCommand" &&
               containingType.ContainingNamespace?.ToDisplayString() == "Microsoft.Data.Sqlite";
    }

    private static bool HasNonConstantInterpolation(InterpolatedStringExpressionSyntax interpolation, SemanticModel model)
    {
        // Check if this is a PRAGMA statement - these are often safe when using controlled table names
        var fullText = interpolation.ToString();
        if (fullText.StartsWith("$\"PRAGMA ", StringComparison.OrdinalIgnoreCase))
        {
            // Allow PRAGMA statements where the interpolated value comes from a foreach iteration variable
            // over a known dictionary or collection (common pattern for schema validation)
            foreach (var content in interpolation.Contents)
            {
                if (content is InterpolationSyntax interp)
                {
                    var symbol = model.GetSymbolInfo(interp.Expression).Symbol;
                    if (symbol is ILocalSymbol local)
                    {
                        // Check if this is a foreach iteration variable
                        var declaringSyntax = local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                        if (declaringSyntax is null)
                            continue;

                        // Check parent for foreach patterns
                        var parent = declaringSyntax.Parent;
                        while (parent != null)
                        {
                            if (parent is ForEachStatementSyntax forEachStmt)
                            {
                                // Check if iterating over a readonly field
                                var collectionSymbol = model.GetSymbolInfo(forEachStmt.Expression).Symbol;
                                if (collectionSymbol is IFieldSymbol field && field.IsReadOnly)
                                {
                                    // Iterating over a readonly field - safe for PRAGMA
                                    return false;
                                }
                            }
                            parent = parent.Parent;
                        }
                    }
                }
            }
        }

        foreach (var content in interpolation.Contents)
        {
            if (content is InterpolationSyntax interp)
            {
                var constantValue = model.GetConstantValue(interp.Expression);
                if (!constantValue.HasValue)
                {
                    // Not a compile-time constant - flag it
                    return true;
                }
            }
        }
        return false;
    }

    private static bool HasNonConstantConcatenation(BinaryExpressionSyntax binary, SemanticModel model)
    {
        // Recursively check if any part of the concatenation is non-constant
        if (binary.Kind() != SyntaxKind.AddExpression)
            return false;

        // Check left side
        if (binary.Left is BinaryExpressionSyntax leftBinary)
        {
            if (HasNonConstantConcatenation(leftBinary, model))
                return true;
        }
        else
        {
            if (!IsConstantOrSafeExpression(binary.Left, model))
                return true;
        }

        // Check right side
        if (binary.Right is BinaryExpressionSyntax rightBinary)
        {
            if (HasNonConstantConcatenation(rightBinary, model))
                return true;
        }
        else
        {
            if (!IsConstantOrSafeExpression(binary.Right, model))
                return true;
        }

        return false;
    }

    private static bool IsConstantOrSafeExpression(ExpressionSyntax expr, SemanticModel model)
    {
        // Literal strings are always safe
        if (expr is LiteralExpressionSyntax)
            return true;

        // Check for compile-time constants
        var constantValue = model.GetConstantValue(expr);
        if (constantValue.HasValue)
            return true;

        // Check if it's a reference to a const or readonly static field
        if (expr is IdentifierNameSyntax or MemberAccessExpressionSyntax)
        {
            var symbol = model.GetSymbolInfo(expr).Symbol;
            if (symbol is IFieldSymbol field)
            {
                // Allow const fields and readonly static fields
                if (field.IsConst || (field.IsReadOnly && field.IsStatic))
                    return true;
            }

            // Check if it's a local variable that only gets assigned constant values
            if (symbol is ILocalSymbol local)
            {
                return IsLocalVariableAlwaysConstant(local, model, expr);
            }
        }

        return false;
    }

    private static bool IsLocalVariableAlwaysConstant(ILocalSymbol local, SemanticModel model, ExpressionSyntax currentExpr)
    {
        // Find the declaring syntax for this local
        var declaringSyntax = local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (declaringSyntax is null)
            return false;

        // Find the method or block containing this local
        var containingMethod = currentExpr.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (containingMethod is null)
            return false;

        // Find all assignments to this local
        var assignments = containingMethod.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Where(a => a.Left is IdentifierNameSyntax id && 
                        model.GetSymbolInfo(id).Symbol?.Equals(local, SymbolEqualityComparer.Default) == true)
            .ToList();

        // Also check variable declarator initializer
        if (declaringSyntax is VariableDeclaratorSyntax declarator && declarator.Initializer != null)
        {
            if (!IsConstantOrLiteralString(declarator.Initializer.Value, model))
                return false;
        }

        // Check all assignments - they must all be constant or literal strings
        foreach (var assignment in assignments)
        {
            if (!IsConstantOrLiteralString(assignment.Right, model))
                return false;
        }

        return true;
    }

    private static bool IsConstantOrLiteralString(ExpressionSyntax expr, SemanticModel model)
    {
        // Literal strings
        if (expr is LiteralExpressionSyntax)
            return true;

        // Compile-time constants
        var constantValue = model.GetConstantValue(expr);
        if (constantValue.HasValue)
            return true;

        // Const or readonly static fields
        if (expr is IdentifierNameSyntax or MemberAccessExpressionSyntax)
        {
            var symbol = model.GetSymbolInfo(expr).Symbol;
            if (symbol is IFieldSymbol field && (field.IsConst || (field.IsReadOnly && field.IsStatic)))
                return true;
        }

        return false;
    }
}
