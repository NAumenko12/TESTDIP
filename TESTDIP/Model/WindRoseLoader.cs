using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTDIP.Model
{
    public class WindRoseLoader
    {
        // Загрузка данных розы ветров из CSV файла
        // Формат: Направление,Вероятность
        // Пример: С,0.15
        //         СВ,0.10
        //         и т.д.
        public static WindRoseData LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Файл розы ветров не найден: {filePath}");
                    return new WindRoseData(); 
                }

                var lines = File.ReadAllLines(filePath);
                var probabilities = new List<double>();

                foreach (var line in lines.Skip(1)) 
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 2 && double.TryParse(parts[1], out double prob))
                    {
                        probabilities.Add(prob);
                    }
                }
                if (probabilities.Count != 8 && probabilities.Count != 16)
                {
                    Console.WriteLine($"Неверное количество направлений в розе ветров: {probabilities.Count}. Ожидается 8 или 16.");
                    return new WindRoseData();
                }
                double sum = probabilities.Sum();
                if (Math.Abs(sum - 1.0) > 0.01) 
                {
                    for (int i = 0; i < probabilities.Count; i++)
                    {
                        probabilities[i] /= sum;
                    }
                }

                return new WindRoseData { DirectionProbabilities = probabilities };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки розы ветров: {ex.Message}");
                return new WindRoseData();
            }
        }
        public static void SaveToFile(WindRoseData windRose, string filePath)
        {
            try
            {
                var lines = new List<string> { "Направление,Вероятность" };
                string[] directions8 = { "С", "СВ", "В", "ЮВ", "Ю", "ЮЗ", "З", "СЗ" };
                string[] directions16 = {
                    "С", "ССВ", "СВ", "ВСВ", "В", "ВЮВ", "ЮВ", "ЮЮВ",
                    "Ю", "ЮЮЗ", "ЮЗ", "ЗЮЗ", "З", "ЗСЗ", "СЗ", "ССЗ"
                };

                string[] directions = windRose.DirectionProbabilities.Count == 16 ? directions16 : directions8;

                for (int i = 0; i < windRose.DirectionProbabilities.Count; i++)
                {
                    lines.Add($"{directions[i]},{windRose.DirectionProbabilities[i]:F4}");
                }

                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения розы ветров: {ex.Message}");
            }
        }
    }
}
