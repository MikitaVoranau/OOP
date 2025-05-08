using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Lab1_OOP;

namespace ZooApp
{
    public static class TextFileSerializer
    {
        public static void SaveToText(List<livingOrgs> animals, string filePath = "animals.txt")
        {
            var lines = new List<string>();

            foreach (var animal in animals)
            {
                lines.Add($"TypeName: {animal.GetType().Name}");
                foreach (var prop in animal.GetType().GetProperties())
                {
                    if (prop.CanRead)
                    {
                        var value = prop.GetValue(animal);
                        lines.Add($"{prop.Name}: {value}");
                    }
                }
                lines.Add(string.Empty); // Разделитель между объектами
            }

            File.WriteAllLines(filePath, lines);
        }

        public static List<livingOrgs> LoadFromText(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл не найден", filePath);

            var result = new List<livingOrgs>();
            var lines = File.ReadAllLines(filePath);
            var blocks = SplitBlocks(lines);

            foreach (var block in blocks)
            {
                try
                {
                    string typeName = block.FirstOrDefault(line => line.StartsWith("TypeName:"))?
                        .Replace("TypeName: ", "");

                    if (string.IsNullOrEmpty(typeName))
                        continue;

                    var properties = block
                        .Where(line => line.Contains(":"))
                        .Select(line =>
                        {
                            var parts = line.Split(new[] { ':' }, 2);
                            return new { Key = parts[0].Trim(), Value = parts[1].Trim() };
                        })
                        .ToDictionary(x => x.Key, x => (object)x.Value);

                    var type = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.Name == typeName && typeof(livingOrgs).IsAssignableFrom(t));

                    if (type == null) continue;

                    var constructor = type.GetConstructors()
                        .OrderByDescending(c => c.GetParameters().Length)
                        .FirstOrDefault();

                    if (constructor == null) continue;

                    var parameters = constructor.GetParameters()
                        .Select(p =>
                        {
                            if (!properties.TryGetValue(p.Name, out var value)) return null;

                            var targetType = Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType;

                            return targetType == typeof(bool) ? (object)(value.ToString().ToLower() == "true") :
                                   targetType == typeof(int) ? (object)int.Parse(value.ToString()) :
                                   targetType == typeof(double) ? (object)double.Parse(value.ToString()) :
                                   value;
                        })
                        .ToArray();

                    var animal = (livingOrgs)constructor.Invoke(parameters);
                    result.Add(animal);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки из TXT: {ex.Message}");
                }
            }

            return result;
        }

        private static List<List<string>> SplitBlocks(string[] lines)
        {
            var blocks = new List<List<string>>();
            var currentBlock = new List<string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (currentBlock.Count > 0)
                    {
                        blocks.Add(currentBlock);
                        currentBlock = new List<string>();
                    }
                }
                else
                {
                    currentBlock.Add(line);
                }
            }

            if (currentBlock.Count > 0)
                blocks.Add(currentBlock);

            return blocks;
        }
    }
}