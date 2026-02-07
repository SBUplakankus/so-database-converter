using System.Globalization;
using System.Linq;

namespace DataToScriptableObject.Editor
{
    public static class TypeInference
    {
        private static readonly string[] BooleanValues = { "true", "false", "yes", "no", "1", "0" };

        public static ResolvedType Infer(string[] values)
        {
            if (values == null || values.Length == 0)
                return ResolvedType.String;

            var nonEmptyValues = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();

            if (nonEmptyValues.Length == 0)
                return ResolvedType.String;

            if (TryInferBool(nonEmptyValues))
                return ResolvedType.Bool;

            if (TryInferInt(nonEmptyValues))
                return ResolvedType.Int;

            if (TryInferFloat(nonEmptyValues))
                return ResolvedType.Float;

            return ResolvedType.String;
        }

        private static bool TryInferInt(string[] values)
        {
            return values.All(v => int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out _));
        }

        private static bool TryInferFloat(string[] values)
        {
            return values.All(v => float.TryParse(v, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _));
        }

        private static bool TryInferBool(string[] values)
        {
            return values.All(v => BooleanValues.Contains(v.ToLowerInvariant()));
        }
        
        private static bool TryInferVector2(string[] values)
        {
            return values.All(v =>
            {
                var trimmed = v.Trim().Trim('(', ')');
                var parts = trimmed.Split(',');
                return parts.Length == 2 && parts.All(p => float.TryParse(p.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _));
            });
        }

        private static bool TryInferColor(string[] values)
        {
            return values.All(v => 
                v.Trim().StartsWith("#") || 
                v.Split(',').Length >= 3);
        }
    }
}