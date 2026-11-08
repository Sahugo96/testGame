#if !UNITY_5_3_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Game.Tests
{
    /// <summary>
    /// In the .NET harness (sim-tests) JSON is read by System.Text.Json; the game reads it with Unity's JsonUtility, which
    /// differs: no null for strings, arrays, lists and [Serializable] classes ("" / empty / an empty object), and fields of
    /// types it cannot serialize (no [Serializable], dictionaries) stay unset. This brings a System.Text.Json result to what
    /// JsonUtility would give, so tests outside Unity catch code that only works with System.Text.Json.
    /// </summary>
    public static class JsonUtilityLike
    {
        /// <summary>JsonUtility's serialization depth limit for nested classes.</summary>
        private const int MaxDepth = 10;

        public static T Normalize<T>(T value)
        {
            Visit(value, 0);
            return value;
        }

        private static void Visit(object value, int depth)
        {
            if (value == null || depth > MaxDepth)
            {
                return;
            }
            foreach (FieldInfo field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.IsInitOnly || field.IsDefined(typeof(NonSerializedAttribute), false))
                {
                    continue;
                }
                Type type = field.FieldType;
                object current = field.GetValue(value);
                if (type == typeof(string))
                {
                    if (current == null)
                    {
                        field.SetValue(value, string.Empty);
                    }
                }
                else if (type.IsArray || IsList(type))
                {
                    Type element = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                    if (!IsSupported(element))
                    {
                        field.SetValue(value, null);
                        continue;
                    }
                    if (current == null)
                    {
                        current = type.IsArray ? Array.CreateInstance(element, 0) : Activator.CreateInstance(type);
                        field.SetValue(value, current);
                    }
                    foreach (object item in (IEnumerable)current)
                    {
                        Visit(item, depth + 1);
                    }
                }
                else if (type.IsClass)
                {
                    if (!IsSupported(type))
                    {
                        field.SetValue(value, null);
                        continue;
                    }
                    if (current == null && depth + 1 <= MaxDepth)
                    {
                        current = Activator.CreateInstance(type);
                        field.SetValue(value, current);
                    }
                    Visit(current, depth + 1);
                }
                else if (type.IsValueType && !type.IsPrimitive && !type.IsEnum && type.IsDefined(typeof(SerializableAttribute), false))
                {
                    object boxed = current;
                    Visit(boxed, depth + 1);
                    field.SetValue(value, boxed);
                }
            }
        }

        private static bool IsList(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);

        private static bool IsSupported(Type type) =>
            type.IsPrimitive || type.IsEnum || type == typeof(string) || type.IsDefined(typeof(SerializableAttribute), false);
    }
}
#endif
