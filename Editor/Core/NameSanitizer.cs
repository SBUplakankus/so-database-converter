using System.Linq;
using System.Text.RegularExpressions;
using DataToScriptableObject.Editor.Enums;

namespace DataToScriptableObject.Editor
{
    public static class NameSanitizer
    {
        private static readonly string[] CSharpKeywords =
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
        
        private static readonly Regex InvalidCharsRegex = 
            new Regex(@"[^a-zA-Z0-9_]", RegexOptions.Compiled);
        
        private static readonly Regex InvalidFileCharsRegex = 
            new Regex(@"[<>:""/\\|?*]", RegexOptions.Compiled);

        public static string SanitizeFieldName(string raw) 
            => SanitizeIdentifier(raw, "field", NamingCase.CamelCase);

        public static string SanitizeClassName(string raw) 
            => SanitizeIdentifier(raw, "GeneratedClass", NamingCase.PascalCase);

        public static string SanitizeFileName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "unnamed";

            var result = raw.Trim();
            result = result.Replace(' ', '_');
            result = InvalidFileCharsRegex.Replace(result, "");

            if (result.Length > 64)
                result = result.Substring(0, 64);

            return string.IsNullOrEmpty(result) ? "unnamed" : result;
        }

        private static string SanitizeIdentifier(string raw, string defaultValue, NamingCase namingCase)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;

            var result = raw.Trim();
            result = ConvertToCase(result, namingCase);
            result = InvalidCharsRegex.Replace(result, "");

            if (string.IsNullOrEmpty(result))
                return defaultValue;

            if (char.IsDigit(result[0]))
                result = "_" + result;

            // For camelCase, ensure first char is lowercase after case conversion
            if (namingCase == NamingCase.CamelCase)
                result = char.ToLowerInvariant(result[0]) + (result.Length > 1 ? result[1..] : "");

            if (CSharpKeywords.Contains(result.ToLowerInvariant()))
                result += "_";

            return result;
        }

        private static string ConvertToCase(string input, NamingCase namingCase)
        {
            input = input.Replace('-', ' ');
            input = input.Replace('_', ' ');
            input = input.Replace('\t', ' ');
            
            var words = input.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            if (words.Length == 0)
                return input;

            for (var i = 0; i < words.Length; i++)
            {
                if (words[i].Length <= 0) continue;
                
                // Check if word is all uppercase (like "ID", "URL", etc.)
                bool isAllCaps = words[i].Length > 1 && words[i].All(char.IsUpper);
                
                if (i == 0 && namingCase == NamingCase.CamelCase)
                {
                    // For camelCase, convert all-caps words to lowercase
                    if (isAllCaps)
                        words[i] = words[i].ToLowerInvariant();
                    else
                        words[i] = char.ToLowerInvariant(words[i][0]) + words[i].Substring(1);
                }
                else
                {
                    // For PascalCase or non-first words, keep first char upper, rest as-is unless all-caps
                    if (isAllCaps)
                        words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1).ToLowerInvariant();
                    else
                        words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Concat(words);
        }
    }
}