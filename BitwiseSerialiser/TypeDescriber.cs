using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BitwiseSerialiser;

/// <summary>
/// Describes objects, using extra attribute data where available
/// </summary>
public static class TypeDescriber
{
    /// <summary>
    /// Create a human-readable description of the types and values of an object
    /// </summary>
    public static string Describe(object? obj, int indent = 0)
    {
        var sb = new StringBuilder();
        DescribeTypeRecursive(obj, sb, indent, "");
        return sb.ToString();
    }

    private static void DescribeTypeRecursive(object? obj, StringBuilder sb, int depth, string name)
    {
        if (depth > 10)
        {
            sb.AppendLine("<reached recursion limit>");
            return;
        }

        if (obj is null)
        {
            sb.AppendLine(Indent(depth) + "<null>");
            return;
        }

        if (obj is string str)
        {
            sb.AppendLine(Indent(depth) + $"\"{str}\"");
            return;
        }

        if (obj is byte[] byteString)
        {
            sb.Append(Indent(depth));
            foreach (var b in byteString)
            {
                sb.Append($"{b:X2}");
            }

            sb.AppendLine();
            return;
        }

        if (obj is IEnumerable list && list.GetType() != typeof(char))
        {
            var idxItr = 0;
            foreach (var item in list)
            {
                sb.AppendLine();
                sb.Append(Indent(depth));
                sb.Append($"{name}[{idxItr++}] ->");
                sb.AppendLine();
                DescribeTypeRecursive(item, sb, depth, name);
            }

            return;
        }

        var publicFields = obj.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .ToList();

        var publicProps = obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToList();

        foreach (var prop in publicProps)
        {
            if (!prop.CanRead) continue;
            if (IsNullList(prop, obj)) continue; // don't list null lists (we use them for optional messages)
            try
            {
                var value = prop.GetValue(obj);
                sb.Append(Indent(depth) + prop.Name + ": ");

                var description = GetAttributeOfType<DescriptionAttribute>(prop);
                if (description?.Description is not null) sb.Append(description.Description);

                DescribeValue(sb, depth, value, prop.Name);
            }
            catch (TargetParameterCountException ex)
            {
                sb.Append(Indent(depth) + $"Error reading '{prop.Name}': {ex.Message}");
            }
        }

        foreach (var field in publicFields)
        {
            var value = field.GetValue(obj);
            sb.Append($"{Indent(depth)}{field.Name}: ");

            var description = GetAttributeOfType<DescriptionAttribute>(field);
            if (description?.Description is not null) sb.Append(description.Description);

            DescribeValue(sb, depth, value, field.Name);
        }
    }

    private static bool IsNullList(PropertyInfo prop, object? src)
    {
        if (src is null) return true;
        if (!typeof(IEnumerable).IsAssignableFrom(prop.PropertyType)) return false;
        var value = prop.GetValue(src);
        return value is null;
    }

    private static void DescribeValue(StringBuilder sb, int depth, object? value, string name)
    {
        if (value is null)
        {
            sb.Append("<null>");
        }
        else if (value is string str)
        {
            sb.Append('"');
            sb.Append(str);
            sb.Append('"');
        }
        else if (value is byte[] byteString)
        {
            sb.Append("0x[");
            foreach (var b in byteString)
            {
                sb.Append($"{b:X2}");
            }

            sb.Append("]");
        }
        else if (value is DateTime dt)
        {
            sb.Append(dt.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        else if (value is Enum en)
        {
            var description = GetAttributeOfType<DescriptionAttribute>(en);
            sb.Append(en);
            TryGetEnumHexValue(en, sb);

            if (description?.Description is not null)
            {
                sb.Append(" - ");
                sb.Append(description.Description);
            }
        }
        else if (value is byte b)
        {
            sb.Append($"0x{b:X2} ({b})");
        }
        else if (value is ushort u16)
        {
            sb.Append($"0x{u16:X4} ({u16})");
        }
        else if (value is uint u32)
        {
            sb.Append($"0x{u32:X8} ({u32})");
        }
        else if (value is ulong u64)
        {
            sb.Append($"0x{u64:X16} ({u64})");
        }
        else if (value.GetType().IsPrimitive)
        {
            sb.Append(value.ToString() ?? "<null>?");
        }
        else
        {
            sb.Append("(" + NameForType(value.GetType()) + ")");
            sb.AppendLine();
            DescribeTypeRecursive(value, sb, depth + 1, name);
        }

        sb.AppendLine();
    }

    private static void TryGetEnumHexValue(Enum en, StringBuilder sb)
    {
        // value__
        var raw = en.GetType().GetField("value__")?.GetValue(en);
        if (raw is null) return;
        sb.Append($" ({raw:X2})");
    }

    /// <summary>
    /// Gets an attribute on a field
    /// </summary>
    /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
    private static T? GetAttributeOfType<T>(ICustomAttributeProvider field) where T : Attribute
    {
        var attributes = field.GetCustomAttributes(typeof(T), false);
        return (attributes.Length > 0) ? (T?)attributes[0] : null;
    }

    /// <summary>
    /// Gets an attribute on an enum field value
    /// </summary>
    /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
    /// <param name="enumVal">The enum value</param>
    /// <returns>The attribute of type T that should exist on the enum value</returns>
    private static T? GetAttributeOfType<T>(Enum enumVal) where T : Attribute
    {
        var type = enumVal.GetType();
        var name = enumVal.ToString();
        if (name is null) return null;
        var memInfo = type.GetMember(name);
        if (memInfo.Length < 1) return null;
        var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
        return (attributes.Length > 0) ? (T?)attributes[0] : null;
    }

    private static string NameForType(Type type)
    {
        if (!type.IsConstructedGenericType) return (type.Name);

        var container = type.Name;
        var contents = (type.GenericTypeArguments?.Select(NameForType)).ToListOrEmpty();
        return container.Substring(0, Max(0,container.Length - 2)) + "<" + string.Join(",", contents) + ">"; // assumes < 10 generic type params
    }

    private static int Max(int a, int b) => a > b ? a : b;

    private static string Indent(int depth) => new(' ', depth * 2);
}