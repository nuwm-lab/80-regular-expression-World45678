using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RegexLab
{
    /// <summary>
    /// Клас, що відповідає за логіку пошуку абревіатур.
    /// </summary>
    public static class AbbreviationSearcher
    {
        // Пояснення регулярного виразу:
        // \b           - Границя слова (початок)
        // [A-Z0-9#+]{2,} - Шукаємо символи (великі літери, цифри, # або +)
        //                у кількості 2 або більше разів (щоб знайти C#, але пропустити "I")
        // \b           - Границя слова (кінець)
        private const string Pattern = @"\b[A-Z0-9#+]{2,}\b";

        /// <summary>
        /// Знаходить всі абревіатури у тексті.
        /// </summary>
        /// <param name="text">Вхідний текст.</param>
        /// <returns>Колекція унікальних знайдених абревіатур.</returns>
        public static IEnumerable<string> FindAbbreviations(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            // Використовуємо Regex.Matches для пошуку всіх входжень
            MatchCollection matches = Regex.Matches(text, Pattern);

            foreach (Match match in matches)
            {
                // Повертаємо значення знайденого збігу
                yield return match.Value;
            }
        }

        /// <summary>
        /// Метод для підсвічування знайдених абревіатур у консолі.
        /// (Додаткова функціональність для наочності)
        /// </summary>
        public static void PrintTextWithHighlights(string text)
        {
            var matches = Regex.Matches(text, Pattern);
            int lastIndex = 0;

            Console.WriteLine("\n--- Візуалізація у тексті ---");
            
            foreach (Match match in matches)
            {
                // Друкуємо текст ДО абревіатури звичайним кольором
                Console.Write(text.Substring(lastIndex, match.Index - lastIndex));

                // Друкуємо абревіатуру іншим кольором
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(match.Value);
                Console.ResetColor();

                lastIndex = match.Index + match.Length;
            }

            // Друкуємо хвіст тексту
            if (lastIndex < text.Length)
            {
                Console.Write(text.Substring(lastIndex));
            }
            Console.WriteLine("\n-----------------------------");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 1. Тестові дані (змішаний текст із різними варіантами)
            string sampleText = 
                "Сучасні веб-технології включають HTML, CSS та JavaScript. " +
                "Для бекенду часто використовують C# (.NET Core) або Java. " +
                "Обмін даними відбувається у форматі JSON або XML. " +
                "Також існують старі формати, як MP3. " +
                "А ось звичайне слово Hello не має бути знайдено, як і займенник Я.";

            Console.WriteLine("Лабораторна робота: Регулярні вирази (Regex)\n");
            Console.WriteLine("Вхідний текст:");
            Console.WriteLine(sampleText);

            // 2. Виконання пошуку
            var abbreviations = AbbreviationSearcher.FindAbbreviations(sampleText);

            // 3. Виведення результатів
            Console.WriteLine("\nЗнайдені абревіатури:");
            int count = 0;
            foreach (var abbr in abbreviations)
            {
                count++;
                Console.WriteLine($"{count}. {abbr}");
            }

            if (count == 0)
            {
                Console.WriteLine("Абревіатур не знайдено.");
            }

            // 4. Бонус: Візуалізація
            AbbreviationSearcher.PrintTextWithHighlights(sampleText);

            Console.WriteLine("\nНатисніть Enter для виходу...");
            Console.ReadLine();
        }
    }
}
