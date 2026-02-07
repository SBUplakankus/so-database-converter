using System.Linq;
using System.Text.RegularExpressions;

namespace DataToScriptableObject.Editor
{
    public static class NameSanitizer
    {
        private static readonly string[] CSharpKeywords = new[]
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        public static string SanitizeFieldName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "field";

            string result = raw.Trim();

            result = ConvertToCamelCase(result);

            result = Regex.Replace(result, @"[^a-zA-Z0-9_]", "");

            if (string.IsNullOrEmpty(result))
                return "field";

            if (char.IsDigit(result[0]))
                result = "_" + result;

            result = char.ToLowerInvariant(result[0]) + result.Substring(1);

            if (CSharpKeywords.Contains(result))
                result += "_";

            return result;
        }

        public static string SanitizeClassName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "GeneratedClass";

            string result = raw.Trim();

            result = ConvertToPascalCase(result);

            result = Regex.Replace(result, @"[^a-zA-Z0-9_]", "");

            if (string.IsNullOrEmpty(result))
                return "GeneratedClass";

            if (char.IsDigit(result[0]))
                result = "_" + result;

            if (CSharpKeywords.Contains(result))
                result += "_";

            return result;
        }

        public static string SanitizeFileName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "unnamed";

            string result = raw.Trim();

            result = result.Replace(' ', '_');

            result = Regex.Replace(result, @"[<>:""/\\|?*]", "");

            if (result.Length > 64)
                result = result.Substring(0, 64);

            if (string.IsNullOrEmpty(result))
                return "unnamed";

            return result;
        }

        private static string ConvertToCamelCase(string input)
        {
            input = input.Replace('-', ' ');
            
            input = Regex.Replace(input, @"_([a-z])", m => m.Groups[1].Value.ToUpperInvariant());
            
            string[] words = input.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            if (words.Length == 0)
                return input;

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    if (i == 0)
                        words[i] = char.ToLowerInvariant(words[i][0]) + words[i].Substring(1);
                    else
                        words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Concat(words);
        }

        private static string ConvertToPascalCase(string input)
        {
            input = input.Replace('-', ' ');
            input = input.Replace('_', ' ');
            
            string[] words = input.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            if (words.Length == 0)
                return input;

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Concat(words);
        }
    }
}
