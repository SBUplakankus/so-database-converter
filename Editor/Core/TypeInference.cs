using DataToScriptableObject;
using System.Globalization;
using System.Linq;

namespace DataToScriptableObject.Editor
{
    public static class TypeInference
    {
        public static ResolvedType Infer(string[] values)
        {
            if (values == null || values.Length == 0)
                return ResolvedType.String;

            var nonEmptyValues = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();

            if (nonEmptyValues.Length == 0)
                return ResolvedType.String;

            if (TryInferInt(nonEmptyValues))
                return ResolvedType.Int;

            if (TryInferFloat(nonEmptyValues))
                return ResolvedType.Float;

            if (TryInferBool(nonEmptyValues))
                return ResolvedType.Bool;

            return ResolvedType.String;
        }

        private static bool TryInferInt(string[] values)
        {
            foreach (var value in values)
            {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    return false;
            }
            return true;
        }

        private static bool TryInferFloat(string[] values)
        {
            foreach (var value in values)
            {
                if (!float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
                    return false;
            }
            return true;
        }

        private static bool TryInferBool(string[] values)
        {
            foreach (var value in values)
            {
                string lower = value.ToLowerInvariant();
                if (lower != "true" && lower != "false" && 
                    lower != "yes" && lower != "no" && 
                    lower != "1" && lower != "0")
                    return false;
            }
            return true;
        }
    }
}
