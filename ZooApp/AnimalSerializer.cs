using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Lab1_OOP;

namespace ZooApp
{
    public static class AnimalSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        public static void SaveAnimals(List<livingOrgs> animals, string filePath = "animals.json")
        {
            var serializableList = animals.Select(a => new SerializableAnimal
            {
                TypeName = a.GetType().Name,
                Properties = a.GetType()
                    .GetProperties()
                    .ToDictionary(
                        p => p.Name,
                        p => (object)p.GetValue(a) ?? DBNull.Value
                    )
            }).ToList();

            var json = JsonSerializer.Serialize(serializableList, Options);
            File.WriteAllText(filePath, json);
        }

        public static List<livingOrgs> LoadAnimals(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл не найден", filePath);

            var json = File.ReadAllText(filePath);
            var serializableList = JsonSerializer.Deserialize<List<SerializableAnimal>>(json, Options);

            var result = new List<livingOrgs>();

            foreach (var item in serializableList)
            {
                try
                {
                    var type = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.Name == item.TypeName && typeof(livingOrgs).IsAssignableFrom(t));

                    if (type == null) continue;

                    var constructor = type.GetConstructors()
                        .OrderByDescending(c => c.GetParameters().Length)
                        .FirstOrDefault();

                    if (constructor == null) continue;

                    var parameters = constructor.GetParameters()
                        .Select(p =>
                        {
                            var value = GetPropertyValue(item, p.Name);
                            if (value == null || value is DBNull) return null;

                            var targetType = Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType;
                            return Convert.ChangeType(value, targetType);
                        })
                        .ToArray();

                    var animal = (livingOrgs)constructor.Invoke(parameters);
                    result.Add(animal);
                }
                catch { }
            }

            return result;
        }

        private static object GetPropertyValue(SerializableAnimal item, string propertyName)
        {
            foreach (var pair in item.Properties)
            {
                if (pair.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (pair.Value is JsonElement jsonElement)
                    {
                        switch (jsonElement.ValueKind)
                        {
                            case JsonValueKind.String: return jsonElement.GetString();
                            case JsonValueKind.Number:
                                if (jsonElement.TryGetInt32(out int intValue)) return intValue;
                                if (jsonElement.TryGetDouble(out double doubleValue)) return doubleValue;
                                break;
                            case JsonValueKind.True: return true;
                            case JsonValueKind.False: return false;
                            case JsonValueKind.Null: return DBNull.Value;
                        }
                    }
                    return pair.Value;
                }
            }
            return null;
        }
    }

    public class SerializableAnimal
    {
        public string TypeName { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}