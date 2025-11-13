using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace RegexLabPro
{
    /// <summary>
    /// Клас-аналізатор, що поєднує потужність Regex та логічну валідацію C#.
    /// </summary>
    public class TextAnalyzer
    {
        // Структура правила: Назва -> (Регулярний вираз, Функція валідації)
        // Валідатор приймає рядок і повертає true, якщо це дійсно коректні дані.
        private readonly Dictionary<string, (Regex Pattern, Func<string, bool> Validator)> _rules;

        public TextAnalyzer()
        {
            var opts = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
            var timeout = TimeSpan.FromSeconds(1.0);

            _rules = new Dictionary<string, (Regex, Func<string, bool>)>
            {
                // 1. АБРЕВІАТУРИ (Складна логіка)
                // Пояснення Regex:
                // (?<![\w])\.NET\b      -> Ловить ".NET" (якщо перед крапкою немає букви)
                // |                     -> АБО
                // \b[A-Z]{2,}[0-9]*\b   -> Класичні (HTML, HTML5). Мінімум 2 великі літери. Цифри в кінці ок.
                // |                     -> АБО
                // \b[A-Z0-9]{1,2}[#+]+(?![a-zA-Z0-9]) -> C#, C++, J# (Спецсимволи, після яких немає букв)
                { 
                    "Абревіатури", 
                    (new Regex(@"(?<![\w])\.NET\b|\b[A-Z]{2,}[0-9]*\b|\b[A-Z0-9]{1,2}[#+]+(?![a-zA-Z0-9])", opts, timeout), 
                    s => s != "II" && s != "III") // Приклад валідації: ігнорувати римські цифри, якщо треба
                },

                // 2. IP-АДРЕСИ (IPv4)
                // Використовуємо суворий Regex, який не пускає числа > 299, 
                // але точну перевірку до 255 зробимо через IPAddress.TryParse
                { 
                    "IP-адреси", 
                    (new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", opts, timeout), 
                    s => IPAddress.TryParse(s, out _)) // C# перевірить, чи октети <= 255
                },

                // 3. ДАТИ
                // Формат: dd.mm.yyyy або dd/mm/yyyy
                { 
                    "Дати", 
                    (new Regex(@"\b\d{1,2}[./-]\d{1,2}[./-]\d{4}\b", opts, timeout), 
                    s => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) // Перевіряє 30 лютого і т.д.
                }
            };
        }

        /// <summary>
        /// Знаходить, фільтрує та валідує збіги.
        /// </summary>
        public Dictionary<string, List<string>> Analyze(string text)
        {
            var results = new Dictionary<string, List<string>>();

            if (string.IsNullOrWhiteSpace(text)) return results;

            foreach (var rule in _rules)
            {
                string category = rule.Key;
                Regex regex = rule.Value.Pattern;
                Func<string, bool> isValid = rule.Value.Validator;

                try
                {
                    // 1. Знаходимо всі кандидати через Regex
                    var matches = regex.Matches(text)
                                       .Cast<Match>()
                                       .Select(m => m.Value)
                                       .Distinct(); // Прибираємо дублікати

                    // 2. Пропускаємо через C# валідатор (точність)
                    var validMatches = new List<string>();
                    foreach (var match in matches)
                    {
                        if (isValid(match))
                        {
                            validMatches.Add(match);
                        }
                    }

                    results[category] = validMatches;
                }
                catch (RegexMatchTimeoutException)
                {
                    results[category] = new List<string> { "ERROR: Timeout" };
                }
            }

            return results;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // ТЕСТОВІ ДАНІ (Складні випадки)
            string text = @"
                Програмування: Використовуємо C#, C++, .NET 8 та HTML5.
                Іноді пишемо на Node.js (не абревіатура) або JSON.
                Хибні цілі: Hello (просто слово), I (одна буква), 999.999.999.999 (фейк IP).
                Реальний IP: 192.168.0.1 та 10.0.0.255.
                Дати: 
                - 24.08.1991 (День Незалежності) - ОК
                - 30.02.2023 (Неіснуюча дата) - має бути відфільтровано
                - 01/01/2000 - ОК
            ";

            Console.WriteLine("--- Вхідний текст ---");
            Console.WriteLine(text.Trim());
            Console.WriteLine(new string('-', 40));

            var analyzer = new TextAnalyzer();
            var report = analyzer.Analyze(text);

            Console.WriteLine("\n--- Результати аналізу ---");
            foreach (var category in report)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{category.Key}: ");
                Console.ResetColor();
                Console.WriteLine($"{category.Value.Count} знайдено");

                if (category.Value.Count > 0)
                {
                    Console.WriteLine("   " + string.Join(", ", category.Value));
                }
                else
                {
                    Console.WriteLine("   (Пусто)");
                }
                Console.WriteLine();
            }

            Console.WriteLine("Натисніть Enter...");
            Console.ReadLine();
        }
    }
}
