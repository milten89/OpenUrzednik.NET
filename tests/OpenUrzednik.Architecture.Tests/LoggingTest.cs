using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// ADR-0025 and CLAUDE.md, Telemetry: IOpenUrzednikLogger calls use constant message templates, never interpolation, and a Debug or
// Trace entry is built only after IsEnabled said the level is on. Checked on src/ as syntax, with no compilation:
// - A file with #if is parsed once per build it can be part of: netstandard2.0, net472 (which compiles the shared polyfills in
//   the tests) and each .NET target of the root Directory.Build.props, in Debug and Release, so every branch any build compiles
//   is checked. A symbol none of those builds defines fails PreprocessorSymbol_InSrc_IsKnown: its branch would go unchecked.
// - A call counts as IOpenUrzednikLogger.Log when it's named Log (logger.Log, logger?.Log or an unqualified Log), has at least
//   four arguments (positional or named: level, eventId, exception, messageTemplate) and its level isn't Microsoft's LogLevel.
//   Its level must be written as OpenUrzednikLogLevel.<Level>, so the guard can be checked. Microsoft ILogger calls aren't
//   IOpenUrzednikLogger calls: the adapter src/OpenUrzednik.Extensions.Logging is left out, and of the DI packages'
//   LoggerMessage only Define's template is checked.
// - A template is a string literal, a const field or const local (resolved by namespace and containing type), or a conditional
//   of those; a name that isn't such a const (ex.Message, a local built by interpolation) fails.
// - The guard must check the same logger, and must sit inside the same lambda or local function as the call.
public class LoggingTest
{
    // The ILogger adapter: it implements IOpenUrzednikLogger over Microsoft's ILogger, whose Log takes the level in a variable.
    private const string AdapterFolder = "src/OpenUrzednik.Extensions.Logging/";

    private static readonly Lazy<IReadOnlyList<string[]>> BuildSymbols = new(() => Builds().ToList());

    private static readonly Lazy<IReadOnlyList<SourceFile>> Sources = new(ReadSources);

    private static readonly Lazy<ConstantNames> Constants = new(() => ConstantNames.From(Sources.Value));

    [Fact]
    public void LogCall_Always_HasAConstantTemplate()
    {
        // Arrange
        var calls = LogCalls().ToList();

        // Act
        var violations = calls
            .Where(call => call.Template is null || !IsConstant(call.Template))
            .Select(call => call.Describe())
            .Distinct()
            .ToList();

        // Assert
        calls.ShouldNotBeEmpty("no IOpenUrzednikLogger.Log call found: the detection broke");
        violations.ShouldBeEmpty("a message template is a constant: pass values as arguments, never interpolate, concatenate or log ex.Message");
    }

    [Fact]
    public void LogCall_Always_NamesItsLevelLiterally()
    {
        // Act
        var violations = LogCalls().Where(call => call.Level is null).Select(call => call.Describe()).Distinct().ToList();

        // Assert
        violations.ShouldBeEmpty("write the level as OpenUrzednikLogLevel.<Level>, so the IsEnabled guard can be checked");
    }

    [Fact]
    public void DebugOrTraceLogCall_Always_IsGuardedByIsEnabled()
    {
        // Arrange
        var calls = LogCalls().Where(call => call.Level is "Debug" or "Trace").ToList();

        // Act
        var violations = calls.Where(call => !IsGuarded(call)).Select(call => call.Describe()).Distinct().ToList();

        // Assert
        calls.ShouldNotBeEmpty("no Debug log call found: the detection broke");
        violations.ShouldBeEmpty("check <logger>.IsEnabled(OpenUrzednikLogLevel.<level>) on the same logger before a Debug or Trace entry");
    }

    [Fact]
    public void LoggerMessageDefine_Always_HasAConstantTemplate()
    {
        // Arrange
        var defines = Sources.Value
            .SelectMany(source => source.Trees.SelectMany(tree => Invocations(tree)
                .Where(invocation => NameOf(invocation) == "Define" && invocation.Expression is MemberAccessExpressionSyntax member
                                     && RightmostName(member.Expression) == "LoggerMessage")
                .Select(invocation => new LogCall(source.Path, invocation, null, null, Argument(invocation, 2, "formatString")))))
            .ToList();

        // Act
        var violations = defines.Where(call => call.Template is null || !IsConstant(call.Template)).Select(call => call.Describe()).Distinct().ToList();

        // Assert
        defines.ShouldNotBeEmpty("no LoggerMessage.Define call found: the detection broke");
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void PreprocessorSymbol_InSrc_IsKnown()
    {
        // Arrange
        var known = BuildSymbols.Value.SelectMany(symbols => symbols).Concat(["DEBUG", "RELEASE", "TRACE"]).ToHashSet(StringComparer.Ordinal);

        // Act
        var unknown = Sources.Value
            .SelectMany(source => Regex.Matches(source.Text, @"^[ \t]*#[ \t]*(?:if|elif)\b(?<condition>[^\r\n]*)", RegexOptions.Multiline)
                .Cast<Match>()
                .SelectMany(directive => Regex.Matches(directive.Groups["condition"].Value, @"[A-Za-z_]\w*").Cast<Match>()
                    .Where(symbol => symbol.Value is not ("true" or "false") && !known.Contains(symbol.Value))
                    .Select(symbol => $"{source.Path}:{LineOf(source.Text, directive.Index)}: {symbol.Value}")))
            .Distinct()
            .ToList();

        // Assert
        unknown.ShouldBeEmpty("no parsed build defines this symbol, so its #if branch goes unchecked: add the build to LoggingTest.Builds");
    }

    private static IEnumerable<LogCall> LogCalls()
        => Sources.Value.Where(source => !source.Path.StartsWith(AdapterFolder, StringComparison.Ordinal))
            .SelectMany(source => source.Trees.SelectMany(tree => Invocations(tree)
            .Where(invocation => NameOf(invocation) == "Log" && invocation.ArgumentList.Arguments.Count >= 4)
            .Select(invocation => (invocation, level: Argument(invocation, 0, "level")))
            .Where(call => call.level is null || !IsMicrosoftLevel(call.level))
            .Select(call => new LogCall(source.Path, call.invocation, call.level is null ? null : LevelOf(call.level), Receiver(call.invocation),
                Argument(call.invocation, 3, "messageTemplate")))));

    private static IEnumerable<InvocationExpressionSyntax> Invocations(SyntaxTree tree)
        => tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>();

    // logger.Log(...), logger?.Log(...), Log(...).
    private static string? NameOf(InvocationExpressionSyntax invocation)
        => invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            MemberBindingExpressionSyntax binding => binding.Name.Identifier.Text,
            SimpleNameSyntax name => name.Identifier.Text,
            _ => null,
        };

    // The logger a call goes to, as text without whitespace: `_telemetry.Logger` for both _telemetry.Logger.Log and
    // _telemetry.Logger?.Log; "this" for an unqualified call.
    private static string? Receiver(InvocationExpressionSyntax invocation)
        => invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => Compact(member.Expression),
            MemberBindingExpressionSyntax => invocation.Parent is ConditionalAccessExpressionSyntax access ? Compact(access.Expression) : null,
            SimpleNameSyntax => "this",
            _ => null,
        };

    private static string Compact(SyntaxNode node) => Regex.Replace(node.ToString(), @"\s+", "");

    // The argument named `name`, else the positional one at `index` (named arguments may come in any order after it).
    private static ExpressionSyntax? Argument(InvocationExpressionSyntax invocation, int index, string name)
    {
        var arguments = invocation.ArgumentList.Arguments;
        var named = arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.Text == name);
        if (named is not null)
            return named.Expression;
        return index < arguments.Count && arguments[index].NameColon is null ? arguments[index].Expression : null;
    }

    // OpenUrzednikLogLevel.Debug, also qualified (OpenUrzednik.Core.Telemetry.OpenUrzednikLogLevel.Debug).
    private static string? LevelOf(ExpressionSyntax expression)
        => Unwrap(expression) is MemberAccessExpressionSyntax member && RightmostName(member.Expression) == "OpenUrzednikLogLevel"
            ? member.Name.Identifier.Text
            : null;

    // Microsoft.Extensions.Logging.LogLevel.Information: an ILogger.Log call, not ours.
    private static bool IsMicrosoftLevel(ExpressionSyntax expression)
        => Unwrap(expression) is MemberAccessExpressionSyntax member && RightmostName(member.Expression) == "LogLevel";

    private static string? RightmostName(ExpressionSyntax expression)
        => Unwrap(expression) switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text,
            _ => null,
        };

    private static bool IsConstant(ExpressionSyntax expression)
        => Unwrap(expression) switch
        {
            LiteralExpressionSyntax literal => literal.IsKind(SyntaxKind.StringLiteralExpression) || literal.IsKind(SyntaxKind.Utf8StringLiteralExpression),
            ConditionalExpressionSyntax conditional => IsConstant(conditional.WhenTrue) && IsConstant(conditional.WhenFalse),
            IdentifierNameSyntax identifier => Constants.Value.IsConstant(identifier),
            MemberAccessExpressionSyntax member => Constants.Value.IsConstantField(member),
            _ => false,
        };

    // Inside an if whose condition calls IsEnabled(OpenUrzednikLogLevel.<level>) on the same logger (directly or in an && chain),
    // or after an early exit (`if (!logger.IsEnabled(...)) return;`, or continue, break, throw) earlier in an enclosing statement
    // list (a block or a switch section). The search stops at a lambda or local function: a guard outside it doesn't run with it.
    private static bool IsGuarded(LogCall call)
    {
        foreach (var ancestor in call.Invocation.Ancestors())
        {
            if (ancestor is IfStatementSyntax { Statement: var body, Condition: var condition }
                && body.Span.Contains(call.Invocation.Span) && ChecksEnabled(condition, call))
                return true;

            if (Statements(ancestor) is { } list
                && list.TakeWhile(statement => !statement.Span.Contains(call.Invocation.Span)).Any(statement => IsEarlyExit(statement, call)))
                return true;

            if (ancestor is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax)
                return false;
        }

        return false;
    }

    private static SyntaxList<StatementSyntax>? Statements(SyntaxNode node)
        => node switch
        {
            BlockSyntax block => block.Statements,
            SwitchSectionSyntax section => section.Statements,
            _ => null,
        };

    private static bool ChecksEnabled(ExpressionSyntax condition, LogCall call)
        => Unwrap(condition) switch
        {
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression)
                => ChecksEnabled(binary.Left, call) || ChecksEnabled(binary.Right, call),
            InvocationExpressionSyntax invocation => IsEnabledCall(invocation, call),
            _ => false,
        };

    private static bool IsEarlyExit(StatementSyntax statement, LogCall call)
        => statement is IfStatementSyntax { Else: null } early
           && Unwrap(early.Condition) is PrefixUnaryExpressionSyntax not && not.IsKind(SyntaxKind.LogicalNotExpression)
           && Unwrap(not.Operand) is InvocationExpressionSyntax invocation && IsEnabledCall(invocation, call)
           && IsExit(early.Statement);

    private static bool IsExit(StatementSyntax statement)
        => statement switch
        {
            ReturnStatementSyntax or ContinueStatementSyntax or BreakStatementSyntax or ThrowStatementSyntax => true,
            BlockSyntax block => block.Statements.Count == 1 && IsExit(block.Statements[0]),
            _ => false,
        };

    private static bool IsEnabledCall(InvocationExpressionSyntax invocation, LogCall call)
        => NameOf(invocation) == "IsEnabled"
           && invocation.ArgumentList.Arguments.Count == 1
           && LevelOf(invocation.ArgumentList.Arguments[0].Expression) == call.Level
           && Receiver(invocation) == call.Receiver;

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
        => expression is ParenthesizedExpressionSyntax parenthesized ? Unwrap(parenthesized.Expression) : expression;

    private static int LineOf(string text, int index) => text.Take(index).Count(c => c == '\n') + 1;

    private static List<SourceFile> ReadSources()
    {
        var views = BuildSymbols.Value
            .SelectMany(symbols => new[] { symbols.Append("DEBUG"), symbols.Append("RELEASE") })
            .Select(symbols => new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: symbols.Append("TRACE")))
            .ToList();
        return new DirectoryInfo(RepositoryFiles.PathOf("src")).EnumerateFiles("*.cs", SearchOption.AllDirectories)
            .Select(file => (file, path: RepositoryFiles.Relative(file)))
            .Where(source => !source.path.Contains("/obj/") && !source.path.Contains("/bin/"))
            .Select(source =>
            {
                var text = File.ReadAllText(source.file.FullName);
                // Without #if every build reads the same tree.
                var options = Regex.IsMatch(text, @"^[ \t]*#[ \t]*if\b", RegexOptions.Multiline) ? views : views.Take(1);
                return new SourceFile(source.path, text, options.Select(view => CSharpSyntaxTree.ParseText(text, view)).ToArray());
            })
            .ToList();
    }

    // The symbols the SDK defines for each build a src/ file can be part of: netstandard2.0 (src/Directory.Build.props), net472
    // (tests/Directory.Build.props; it compiles the shared polyfills) and every .NET target of the root Directory.Build.props.
    private static IEnumerable<string[]> Builds()
    {
        yield return ["NETSTANDARD", "NETSTANDARD2_0", .. OrGreater("NETSTANDARD", ["1_0", "1_1", "1_2", "1_3", "1_4", "1_5", "1_6", "2_0"])];
        yield return ["NETFRAMEWORK", "NET472", .. OrGreater("NET", ["20", "30", "35", "40", "45", "451", "452", "46", "461", "462", "47", "471", "472"])];
        var targets = Regex.Match(RepositoryFiles.Read("Directory.Build.props"), @"<TargetFrameworks>(?<targets>[^<]+)</TargetFrameworks>").Groups["targets"].Value;
        foreach (var major in Regex.Matches(targets, @"\bnet(?<major>\d+)\.0\b").Cast<Match>().Select(match => int.Parse(match.Groups["major"].Value, System.Globalization.CultureInfo.InvariantCulture)))
        {
            yield return ["NET", "NETCOREAPP", $"NET{major}_0",
                .. OrGreater("NETCOREAPP", ["1_0", "1_1", "2_0", "2_1", "2_2", "3_0", "3_1"]),
                .. OrGreater("NET", Enumerable.Range(5, major - 4).Select(version => $"{version}_0"))];
        }
    }

    private static IEnumerable<string> OrGreater(string prefix, IEnumerable<string> versions)
        => versions.Select(version => $"{prefix}{version}_OR_GREATER");

    private sealed record SourceFile(string Path, string Text, SyntaxTree[] Trees);

    private sealed record LogCall(string Path, InvocationExpressionSyntax Invocation, string? Level, string? Receiver, ExpressionSyntax? Template)
    {
        public string Describe()
            => $"{Path}:{Invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {Invocation.ToString().Split('\n')[0].Trim()}";
    }

    /// <summary>
    /// The const fields declared in src/, by the full name of their type (namespace and containing types), so a template held in
    /// a name can be checked; const locals are found where they're used.
    /// </summary>
    private sealed class ConstantNames
    {
        private readonly HashSet<(string Type, string Name)> _fields = [];
        private readonly HashSet<string> _types = new(StringComparer.Ordinal);

        public static ConstantNames From(IEnumerable<SourceFile> sources)
        {
            var names = new ConstantNames();
            foreach (var type in sources.SelectMany(source => source.Trees).SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>()))
            {
                var fullName = FullName(type);
                names._types.Add(fullName);
                foreach (var field in type.ChildNodes().OfType<FieldDeclarationSyntax>().Where(field => field.Modifiers.Any(SyntaxKind.ConstKeyword)))
                {
                    foreach (var variable in field.Declaration.Variables)
                        names._fields.Add((fullName, variable.Identifier.Text));
                }
            }
            return names;
        }

        // A const local declared earlier in an enclosing block or switch section, or a const field of an enclosing type.
        public bool IsConstant(IdentifierNameSyntax identifier)
        {
            var name = identifier.Identifier.Text;
            var isLocal = identifier.Ancestors().Select(Statements).OfType<SyntaxList<StatementSyntax>>()
                .Any(list => list.TakeWhile(statement => statement.SpanStart < identifier.SpanStart)
                    .OfType<LocalDeclarationStatementSyntax>()
                    .Any(local => local.IsConst && local.Declaration.Variables.Any(variable => variable.Identifier.Text == name)));
            return isLocal || identifier.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any(type => _fields.Contains((FullName(type), name)));
        }

        // Type.Field: the type is resolved like the compiler's simple lookup, from the innermost namespace of the use outwards,
        // then through the file's using directives.
        public bool IsConstantField(MemberAccessExpressionSyntax member)
        {
            var typeName = Regex.Replace(member.Expression.ToString(), @"\s+|^global::", "");
            var type = Candidates(member, typeName).FirstOrDefault(_types.Contains);
            return type is not null && _fields.Contains((type, member.Name.Identifier.Text));
        }

        private static IEnumerable<string> Candidates(SyntaxNode use, string typeName)
        {
            foreach (var type in use.Ancestors().OfType<BaseTypeDeclarationSyntax>())
                yield return $"{FullName(type)}.{typeName}";
            for (var ns = Namespace(use); ns.Length > 0; ns = ns.LastIndexOf('.') is var dot && dot > 0 ? ns.Substring(0, dot) : "")
                yield return $"{ns}.{typeName}";
            yield return typeName;
            foreach (var @using in use.SyntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Where(u => u.Alias is null && u.Name is not null))
                yield return $"{@using.Name}.{typeName}";
        }

        private static string FullName(BaseTypeDeclarationSyntax type)
        {
            var containing = type.Ancestors().OfType<BaseTypeDeclarationSyntax>().Reverse().Select(outer => outer.Identifier.Text);
            var path = string.Join(".", containing.Append(type.Identifier.Text));
            var ns = Namespace(type);
            return ns.Length == 0 ? path : $"{ns}.{path}";
        }

        private static string Namespace(SyntaxNode node)
            => string.Join(".", node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(ns => ns.Name.ToString()));
    }
}
