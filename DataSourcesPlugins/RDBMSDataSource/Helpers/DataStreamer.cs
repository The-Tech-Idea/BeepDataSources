using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.DataBase.Helpers
{
    internal static class DataStreamer
    {
        public static IEnumerable<Dictionary<string, object>> Stream(IDataReader reader)
        {
            if (reader == null) yield break;
            using (reader)
            {
                int fieldCount = reader.FieldCount;
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>(fieldCount, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < fieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    yield return row;
                }
            }
        }

        // Cache property mappings per type for performance
        private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _propertyCache =
            new ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>();

        /// <summary>
        /// Streams data from an IDataReader into objects of the specified type.
        /// </summary>
        /// <param name="reader">The data reader to stream from.</param>
        /// <param name="type">The runtime type to instantiate for each row.</param>
        /// <returns>Enumerable sequence of objects of the specified type.</returns>
        public static IEnumerable<object> Stream(IDataReader reader, Type type)
        {
            if (reader == null || type == null) yield break;

            // Ensure type has a parameterless constructor
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new ArgumentException($"Type '{type.FullName}' must have a public parameterless constructor.", nameof(type));
            }

            using (reader)
            {
                // Get or build property mapping cache for this type
                var propertyMap = _propertyCache.GetOrAdd(type, t =>
                    t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanWrite)
                     .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase));

                while (reader.Read())
                {
                    object instance = Activator.CreateInstance(type);
                    MapRowToInstance(reader, instance, propertyMap);
                    yield return instance;
                }
            }
        }

        private static void MapRowToInstance(IDataReader reader, object instance, Dictionary<string, PropertyInfo> propertyMap)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                if (propertyMap.TryGetValue(columnName, out PropertyInfo prop))
                {
                    object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    SetPropertyValue(instance, prop, value);
                }
            }
        }

        private static void SetPropertyValue(object instance, PropertyInfo prop, object value)
        {
            // Handle null assignment
            if (value == null)
            {
                if (prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) == null)
                    return; // Skip non-nullable value types
                prop.SetValue(instance, null);
                return;
            }

            // Direct assignment if types are compatible
            if (prop.PropertyType.IsAssignableFrom(value.GetType()))
            {
                prop.SetValue(instance, value);
                return;
            }

            // Attempt type conversion
            try
            {
                object converted = Convert.ChangeType(value, prop.PropertyType);
                prop.SetValue(instance, converted);
            }
            catch
            {
                // Skip on conversion failure (could log in production scenarios)
            }
        }
    }
}