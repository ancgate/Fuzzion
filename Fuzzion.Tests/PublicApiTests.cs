using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Fuzzion.Tests;

/// <summary>
/// Freezes the public API (Phase 9). Every public and protected type and member of the
/// Fuzzion assembly is listed, one per line, and compared with
/// <c>Fuzzion.Tests/PublicApi/Fuzzion.api.txt</c>. An unintended change fails this test.
/// To accept an intended change, run the tests once with the environment variable
/// <c>FUZZION_UPDATE_PUBLIC_API=1</c>, review the file's diff, and record the change in
/// CHANGELOG.md.
/// </summary>
public class PublicApiTests
{
    private const string BaselineName = "Fuzzion.api.txt";

    [Fact]
    public void PublicApiMatchesBaseline()
    {
        var actual = Describe(typeof(Fuzz).Assembly);
        var path = BaselinePath();

        if (Environment.GetEnvironmentVariable("FUZZION_UPDATE_PUBLIC_API") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllLines(path, actual);
            return;
        }

        Assert.True(File.Exists(path), $"Missing {path}. Run the tests with FUZZION_UPDATE_PUBLIC_API=1 to create it.");
        var expected = File.ReadAllLines(path).Where(l => l.Length > 0).ToArray();

        var added = actual.Except(expected).ToArray();
        var removed = expected.Except(actual).ToArray();
        Assert.True(added.Length == 0 && removed.Length == 0,
            "The public API changed. If intended, run the tests with FUZZION_UPDATE_PUBLIC_API=1 and update CHANGELOG.md." + Environment.NewLine +
            string.Join(Environment.NewLine, added.Select(l => "+ " + l).Concat(removed.Select(l => "- " + l))));
    }

    private static string BaselinePath([CallerFilePath] string thisFile = "") =>
        Path.Combine(Path.GetDirectoryName(thisFile)!, "PublicApi", BaselineName);

    /// <summary>One line per public/protected type and member, sorted, in a runtime-independent format.</summary>
    internal static string[] Describe(Assembly assembly)
    {
        var lines = new List<string>();
        foreach (var type in assembly.GetExportedTypes())
        {
            var typeName = Name(type);
            lines.Add($"{Kind(type)} {typeName}{Bases(type)}");

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                       BindingFlags.Static | BindingFlags.DeclaredOnly;

            if (type.IsEnum)
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    lines.Add($"{typeName}.{field.Name} = {Convert.ToInt64(field.GetRawConstantValue())}");
                }

                continue;
            }

            foreach (var member in type.GetMembers(flags))
            {
                var line = Member(member);
                if (line != null)
                {
                    lines.Add($"{typeName}::{line}");
                }
            }
        }

        return lines.Distinct().OrderBy(l => l, StringComparer.Ordinal).ToArray();
    }

    private static string Member(MemberInfo member)
    {
        if (member.Name.StartsWith("<", StringComparison.Ordinal) || member.IsDefined(typeof(CompilerGeneratedAttribute), false))
        {
            return null;
        }

        switch (member)
        {
            case ConstructorInfo c when Visible(c):
                return $"{(c.IsStatic ? "static " : "")}.ctor({Parameters(c)})";
            case MethodInfo m when Visible(m) && !m.IsSpecialName:
                return $"{Modifiers(m)}{Name(m.ReturnType)} {m.Name}{GenericArguments(m)}({Parameters(m)})";
            case MethodInfo m when Visible(m) && m.IsSpecialName && m.Name.StartsWith("op_", StringComparison.Ordinal):
                return $"static {Name(m.ReturnType)} {m.Name}({Parameters(m)})";
            case PropertyInfo p:
                var accessors = new[] { p.GetMethod, p.SetMethod }.Where(a => a != null && Visible(a)).ToArray();
                if (accessors.Length == 0) return null;
                var getterOrSetter = accessors[0];
                var index = p.GetIndexParameters();
                var indexText = index.Length == 0 ? "" : $"[{string.Join(", ", index.Select(i => Name(i.ParameterType)))}]";
                return $"{Modifiers(getterOrSetter)}{Name(p.PropertyType)} {p.Name}{indexText} {{ {string.Join(" ", accessors.Select(a => a == p.GetMethod ? "get;" : "set;"))} }}";
            case FieldInfo f when f.IsPublic || f.IsFamily || f.IsFamilyOrAssembly:
                return $"{(f.IsStatic ? "static " : "")}{(f.IsInitOnly ? "readonly " : "")}{(f.IsLiteral ? "const " : "")}{Name(f.FieldType)} {f.Name}";
            case EventInfo e:
                return $"event {Name(e.EventHandlerType!)} {e.Name}";
            case Type nested when nested.IsNestedPublic || nested.IsNestedFamily:
                return null; // listed as its own type
            default:
                return null;
        }
    }

    private static bool Visible(MethodBase m) => m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly;

    private static string Modifiers(MethodInfo m) =>
        (m.IsFamily || m.IsFamilyOrAssembly ? "protected " : "") +
        (m.IsStatic ? "static " : "") +
        (m.IsAbstract ? "abstract " : m.IsVirtual && !m.IsFinal && m.GetBaseDefinition() == m ? "virtual " : "") +
        (m.IsVirtual && m.GetBaseDefinition() != m && !m.DeclaringType!.IsInterface ? "override " : "");

    private static string Kind(Type t) =>
        t.IsInterface ? "interface" :
        t.IsEnum ? "enum" :
        t.IsValueType ? (t.GetCustomAttributes(false).Any(a => a.GetType().Name == "IsReadOnlyAttribute") ? "readonly struct" : "struct") :
        typeof(Delegate).IsAssignableFrom(t) ? "delegate" :
        t.IsAbstract && t.IsSealed ? "static class" :
        t.IsAbstract ? "abstract class" :
        t.IsSealed ? "sealed class" : "class";

    private static string Bases(Type t)
    {
        var bases = new List<string>();
        if (t.BaseType != null && t.BaseType != typeof(object) && t.BaseType != typeof(ValueType) &&
            t.BaseType != typeof(Enum) && t.BaseType != typeof(MulticastDelegate))
        {
            bases.Add(Name(t.BaseType));
        }

        // Only interfaces the type adds itself: those inherited from a base type defined
        // elsewhere differ by runtime (for example System.Enum implements ISpanFormattable
        // on .NET 8+ but not on .NET Framework).
        var inherited = new HashSet<Type>(t.BaseType?.GetInterfaces() ?? Type.EmptyTypes);
        bases.AddRange(t.GetInterfaces()
            .Where(i => (i.IsPublic || i.IsNestedPublic) && !inherited.Contains(i))
            .Select(Name)
            .OrderBy(n => n, StringComparer.Ordinal));
        return bases.Count == 0 ? "" : " : " + string.Join(", ", bases);
    }

    private static string GenericArguments(MethodInfo m) =>
        m.IsGenericMethodDefinition ? "<" + string.Join(", ", m.GetGenericArguments().Select(a => a.Name)) + ">" : "";

    private static string Parameters(MethodBase m) =>
        string.Join(", ", m.GetParameters().Select(p =>
            (p.IsDefined(typeof(ParamArrayAttribute), false) ? "params " : "") +
            (p.IsOut ? "out " : p.ParameterType.IsByRef ? "ref " : "") +
            Name(p.ParameterType) + " " + p.Name +
            (p.HasDefaultValue ? " = " + (p.DefaultValue == null ? "null" : Convert.ToString(p.DefaultValue, System.Globalization.CultureInfo.InvariantCulture)) : "")));

    private static string Name(Type t)
    {
        if (t.IsByRef) return Name(t.GetElementType()!);
        if (t.IsArray) return Name(t.GetElementType()!) + "[" + new string(',', t.GetArrayRank() - 1) + "]";
        if (t.IsGenericParameter) return t.Name;

        var name = t.IsNested ? Name(t.DeclaringType!) + "." + t.Name : (t.Namespace == null ? t.Name : t.Namespace + "." + t.Name);
        var tick = name.IndexOf('`');
        if (tick >= 0) name = name.Substring(0, tick);
        if (!t.IsGenericType) return name;

        // Nested types repeat their parent's generic arguments; show only their own.
        var own = t.GetGenericArguments().Skip(t.IsNested ? t.DeclaringType!.GetGenericArguments().Length : 0).ToArray();
        return own.Length == 0 ? name : name + "<" + string.Join(", ", own.Select(Name)) + ">";
    }
}
