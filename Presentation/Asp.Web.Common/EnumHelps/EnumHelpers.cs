using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace src.Web.Common.EnumHelps
{
    public static class EnumHelpers<T>
    {
        public static IDictionary<string, T> GetValues(bool ignoreCase)
        {
            var enumValues = new Dictionary<string, T>();

            foreach (FieldInfo fi in typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                string key = fi.Name;

                var display = fi.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];
                if (display != null)
                    key = (display.Length > 0) ? display[0].Name : fi.Name;

                if (ignoreCase)
                    key = key.ToLower();

                if (!enumValues.ContainsKey(key))
                    enumValues[key] = (T)fi.GetRawConstantValue();
            }

            return enumValues;
        }

        public static T Parse(string value)
        {
            T result;

            try
            {
                result = (T)Enum.Parse(typeof(T), value, true);
            }
            catch (Exception)
            {
                result = ParseDisplayValues(value, true);
            }


            return result;
        }

        private static T ParseDisplayValues(string value, bool ignoreCase)
        {
            IDictionary<string, T> values = GetValues(ignoreCase);

            string key = null;
            if (ignoreCase)
                key = value.ToLower();
            else
                key = value;

            if (values.ContainsKey(key))
                return values[key];

            throw new ArgumentException(value);
        }
        public static T GetValueFromName(string name)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DisplayAttribute)) as DisplayAttribute;
                if (attribute != null)
                {
                    if (attribute.Name == name)
                    {
                        return (T)field.GetValue(null);
                    }
                }
                else
                {
                    if (field.Name == name)
                        return (T)field.GetValue(null);
                }
            }

            throw new ArgumentOutOfRangeException("name");
        }
    }
}
