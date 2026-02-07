using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DataToScriptableObject.Editor
{
    public static class TypeConverters
    {
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
            if (value == nullToken)
                return null;

            return value ?? "";
        }

        public static Vector2 ToVector2(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Vector2.zero;

            try
            {
                value = value.Trim().Trim('(', ')');
                var parts = value.Split(',');
                
                if (parts.Length >= 2)
                {
                    float x = ToFloat(parts[0].Trim());
                    float y = ToFloat(parts[1].Trim());
                    return new Vector2(x, y);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to convert '{value}' to Vector2: {ex.Message}. Using default: Vector2.zero");
            }

            return Vector2.zero;
        }

        public static Vector3 ToVector3(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Vector3.zero;

            try
            {
                value = value.Trim().Trim('(', ')');
                var parts = value.Split(',');
                
                if (parts.Length >= 3)
                {
                    float x = ToFloat(parts[0].Trim());
                    float y = ToFloat(parts[1].Trim());
                    float z = ToFloat(parts[2].Trim());
                    return new Vector3(x, y, z);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to convert '{value}' to Vector3: {ex.Message}. Using default: Vector3.zero");
            }

            return Vector3.zero;
        }

        public static Color ToColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Color.white;

            try
            {
                value = value.Trim();

                if (value.StartsWith("#"))
                {
                    if (ColorUtility.TryParseHtmlString(value, out Color color))
                        return color;
                }

                var parts = value.Split(',');
                if (parts.Length >= 3)
                {
                    float r, g, b, a = 1f;

                    float r_val = ToFloat(parts[0].Trim());
                    float g_val = ToFloat(parts[1].Trim());
                    float b_val = ToFloat(parts[2].Trim());

                    if (r_val > 1f || g_val > 1f || b_val > 1f)
                    {
                        r = r_val / 255f;
                        g = g_val / 255f;
                        b = b_val / 255f;
                    }
                    else
                    {
                        r = r_val;
                        g = g_val;
                        b = b_val;
                    }

                    if (parts.Length >= 4)
                    {
                        float a_val = ToFloat(parts[3].Trim());
                        a = a_val > 1f ? a_val / 255f : a_val;
                    }

                    return new Color(r, g, b, a);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to convert '{value}' to Color: {ex.Message}. Using default: Color.white");
            }

            return Color.white;
        }

        public static object ToEnum(string value, Type enumType)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                var values = Enum.GetValues(enumType);
                return values.Length > 0 ? values.GetValue(0) : null;
            }

            try
            {
                if (Enum.TryParse(enumType, value, true, out object result))
                    return result;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to convert '{value}' to enum {enumType.Name}: {ex.Message}");
            }

            var fallbackValues = Enum.GetValues(enumType);
            return fallbackValues.Length > 0 ? fallbackValues.GetValue(0) : null;
        }

        public static T[] ToArray<T>(string value, string delimiter, Func<string, T> elementConverter)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new T[0];

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
                string[] extensions = { ".png", ".jpg", ".prefab", ".asset", ".mat", ".fbx" };
                foreach (var ext in extensions)
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
