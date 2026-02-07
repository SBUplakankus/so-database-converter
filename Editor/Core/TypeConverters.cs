using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DataToScriptableObject.Editor
{
    public static class TypeConverters
    {
        private const float ColorByteMax = 255f;
        
#if UNITY_EDITOR
        private static readonly string[] AssetExtensions = { ".png", ".jpg", ".prefab", ".asset", ".mat", ".fbx" };
#endif

        public static int ToInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;

            Debug.LogWarning($"Failed to convert '{value}' to int. Using default: 0");
            return 0;
        }

        public static float ToFloat(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0f;

            if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out float result))
                return result;

            Debug.LogWarning($"Failed to convert '{value}' to float. Using default: 0f");
            return 0f;
        }

        public static double ToDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0.0;

            if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double result))
                return result;

            Debug.LogWarning($"Failed to convert '{value}' to double. Using default: 0.0");
            return 0.0;
        }

        public static long ToLong(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0L;

            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result))
                return result;

            Debug.LogWarning($"Failed to convert '{value}' to long. Using default: 0L");
            return 0L;
        }

        public static bool ToBool(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string lower = value.ToLowerInvariant();
            if (lower == "true" || lower == "yes" || lower == "1")
                return true;
            if (lower == "false" || lower == "no" || lower == "0")
                return false;

            Debug.LogWarning($"Failed to convert '{value}' to bool. Using default: false");
            return false;
        }

        public static string ToString(string value, string nullToken)
        {
            if (value == null)
                return null;
            
            // Case-insensitive null token comparison
            if (nullToken != null && string.Equals(value, nullToken, StringComparison.OrdinalIgnoreCase))
                return null;
            
            return value;
        }

        public static Vector2 ToVector2(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Vector2.zero;

            value = value.Trim().Trim('(', ')');
            var parts = value.Split(',');
            
            if (parts.Length >= 2)
            {
                var x = ToFloat(parts[0].Trim());
                var y = ToFloat(parts[1].Trim());
                return new Vector2(x, y);
            }

            Debug.LogWarning($"Failed to convert '{value}' to Vector2. Using default: Vector2.zero");
            return Vector2.zero;
        }

        public static Vector3 ToVector3(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Vector3.zero;

            value = value.Trim().Trim('(', ')');
            var parts = value.Split(',');
            
            if (parts.Length >= 3)
            {
                var x = ToFloat(parts[0].Trim());
                var y = ToFloat(parts[1].Trim());
                var z = ToFloat(parts[2].Trim());
                return new Vector3(x, y, z);
            }

            Debug.LogWarning($"Failed to convert '{value}' to Vector3. Using default: Vector3.zero");
            return Vector3.zero;
        }

        public static Color ToColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Color.white;

            value = value.Trim();

            // Try hex color first
            if (value.StartsWith("#") && ColorUtility.TryParseHtmlString(value, out Color hexColor))
                return hexColor;

            // Try RGB/RGBA
            var parts = value.Split(',');
            if (parts.Length >= 3)
            {
                var r = NormalizeColorComponent(ToFloat(parts[0].Trim()));
                var g = NormalizeColorComponent(ToFloat(parts[1].Trim()));
                var b = NormalizeColorComponent(ToFloat(parts[2].Trim()));
                var a = 1f;

                if (parts.Length >= 4)
                {
                    a = NormalizeColorComponent(ToFloat(parts[3].Trim()));
                }

                return new Color(r, g, b, a);
            }

            Debug.LogWarning($"Failed to convert '{value}' to Color. Using default: Color.white");
            return Color.white;
        }

        private static float NormalizeColorComponent(float value)
        {
            return value > 1f ? value / ColorByteMax : value;
        }

        public static object ToEnum(string value, Type enumType)
        {
            if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse(enumType, value, true, out object result))
                return result;

            // Fallback to first enum value
            var values = Enum.GetValues(enumType);
            if (values.Length > 0)
                return values.GetValue(0);
            
            Debug.LogWarning($"Enum type {enumType.Name} has no values");
            return null;
        }

        public static T[] ToArray<T>(string value, string delimiter, Func<string, T> elementConverter)
        {
            // Empty or whitespace-only strings should produce an array with one element
            // This matches CSV behavior where an empty cell still represents one value
            if (value == null)
                return Array.Empty<T>();
            
            var parts = value.Split(new[] { delimiter }, StringSplitOptions.None);
            var result = new T[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                result[i] = elementConverter(parts[i].Trim());
            }

            return result;
        }

        public static List<T> ToList<T>(string value, string delimiter, Func<string, T> elementConverter)
        {
            return new List<T>(ToArray(value, delimiter, elementConverter));
        }

#if UNITY_EDITOR
        public static UnityEngine.Object ToAssetReference(string value, Type assetType)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath(value, assetType);
            
            if (asset == null)
            {
                foreach (var ext in AssetExtensions)
                {
                    asset = UnityEditor.AssetDatabase.LoadAssetAtPath(value + ext, assetType);
                    if (asset != null)
                        break;
                }
            }

            if (asset == null)
                Debug.LogWarning($"Could not load asset at path: {value}");

            return asset;
        }
#endif
    }
}