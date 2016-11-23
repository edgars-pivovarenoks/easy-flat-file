using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace EasyFlatFile
{
    public static class Extensions
    {
        public static int ToInt(this object value)
        {
            return
                Convert.ToInt32(value);
        }
        public static bool HasValue(this string value)
        {
            return
                !string.IsNullOrWhiteSpace(value);
        }
        public static string ToSeparatedString<T>(this IEnumerable<T> array, string separator = ",")
        {
            return string.Join(separator, array);
        }
        public static string ToSeparatedString(this string[] array, string separator = ",")
        {
            return string.Join(separator, array);
        }
        public static string ToNonNullString(this string str)
        {
            if (str == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(str))
                return string.Empty;

            return
                str;

        }
        public static string ToStringOrDefault(this object obj, string defaultValue)
        {
            var value = obj.ToStringSafe();

            if (value.HasValue())
                return value;
            else
                return
                    defaultValue;
        }
        public static string ToStringSafe(this object obj)
        {
            if (obj == null)
                return null;

            string result = obj.ToString();

            if (string.IsNullOrWhiteSpace(result))
                return null;

            return
                result.Trim();
        }
        public static string ToStringSafe(this object obj, Type objType)
        {
            if (obj == null)
                return null;

            string result;
            if (objType.GetTypeInfo().IsAssignableFrom(typeof(decimal)))
                result = ((decimal)obj).ToString(CultureInfo.InvariantCulture);
            else
                result = obj.ToString();

            if (string.IsNullOrWhiteSpace(result))
                return null;

            return
                result.Trim();
        }
    }
}
