using System.Text;

namespace Confeccao.Api.Common.Naming;

public static class KebabCase
{
    /// <summary>
    /// Converts a PascalCase enum name to kebab-case.
    /// "Cutting" -> "cutting"; "AwaitingCutting" -> "awaiting-cutting";
    /// "GG" -> "gg" (consecutive caps treated as one word).
    /// </summary>
    public static string From(string pascal)
    {
        if (string.IsNullOrEmpty(pascal)) return pascal;
        var sb = new StringBuilder(pascal.Length + 4);
        for (var i = 0; i < pascal.Length; i++)
        {
            var c = pascal[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(pascal[i - 1]))
                sb.Append('-');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    public static string From<TEnum>(TEnum value) where TEnum : struct, Enum =>
        From(value.ToString());
}
