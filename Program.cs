using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RegexLabPlatinum
{
    /// <summary>
    /// Аналізатор тексту з підтримкою статистики, I/O та розширеного Unicode.
    /// </summary>
    public class TextAnalyzer
    {
        // Делегат для валідації
        private delegate bool Validator(string candidate);

        // Словник правил. Результат аналізу: Словник (Знайдене слово -> Кількість входжень)
        private readonly Dictionary<string, (Regex Pattern, Validator Validator)> _rules;

        // Окремий скомпільований Regex для римських чисел (для консистентності)
        private readonly Regex _romanRegex;

        // Опції для всіх Regex: Компільовані + Ігнорування пробілів у патерні (для коментарів)
        private const RegexOptions CommonOptions = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace;
        private static readonly TimeSpan CommonTimeout = TimeSpan.FromSeconds(1.0);

        public TextAnalyzer()
        {
            // Ініціалізація Regex для римських чисел
            _romanRegex = new Regex(
                @"^M{0,4}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})$", 
                CommonOptions, CommonTimeout);

            _rules = new Dictionary<string, (Regex, Validator)>
            {
                // 1. АБРЕВІАТУРИ
                // Використовуємо Verbose режим (x) для коментування частин виразу
                { 
                    "Абревіатури", 
                    (new Regex(@"
                        (?<!\w)\.NET\b              # Кейс 1: .NET (якщо перед крапкою немає букви)
                        |                           # АБО
                        \b\p{Lu}{2,}[0-9]*\b        # Кейс 2: Класичні (HTML, HTML5). Мінімум 2 великі Unicode літери.
                        |                           # АБО
                        \b\p{Lu}{1,2}[#+]+          # Кейс 3: Технічні (C#, C++).
                        (?![\p{L}\p{N}])            # Negative Lookahead: далі не має бути літери або цифри (Unicode)
                    ", CommonOptions, CommonTimeout), 
                    IsValidAbbreviation) 
                },

                // 2. IP-АДРЕСИ
                { 
                    "IP-адреси", 
                    (new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", CommonOptions, CommonTimeout), 
                    s => IPAddress.TryParse(s, out _)) 
                },

                // 3. ДАТИ
                { 
                    "Дати", 
                    (new Regex(@"\b\d{1,4}[./-]\d{1,2}[./-]\d{2,4}\b", CommonOptions, CommonTimeout), 
                    IsValidDate) 
                }
            };
        }

        /// <summary>
        /// Аналізує текст і повертає статистику входжень.
        /// </summary>
        public Dictionary<string, Dictionary<string, int>> Analyze(string text)
        {
            var report = new Dictionary<string, Dictionary<string, int>>();

            if (string.IsNullOrWhiteSpace(text)) return report;

            foreach (var rule in _rules)
            {
                report[rule.Key] = ProcessCategory(text, rule.Value.Pattern, rule.Value.Validator);
            }

            return report;
        }

        private Dictionary<string, int> ProcessCategory(string text, Regex regex, Validator validator)
        {
            var stats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Знаходимо ВСІ входження (не Distinct, бо нам треба рахувати)
                var matches = regex.Matches(text);

                foreach (Match match in matches)
                {
                    string value = match.Value;

                    try
                    {
                        // Валідація
                        if (validator(value))
                        {
                            if (!stats.ContainsKey(value))
                                stats[value] = 0;
                            
                            stats[value]++;
                        }
                    }
                    catch (Exception) { /* Ігноруємо окремі помилки валідації */ }
                }
            }
            catch (RegexMatchTimeoutException)
            {
                Console.Error.WriteLine("REGEX TIMEOUT ERROR");
            }

            return stats;
        }

        // --- ВАЛІДАТОРИ ---

        private bool IsValidDate(string dateStr)
        {
            string[] formats = { "d.M.yyyy", "dd.MM.yyyy", "d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };
            return DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        private bool IsValidAbbreviation(string s)
        {
            if (s.Equals(".NET", StringComparison.OrdinalIgnoreCase)) return true;
            
            // Перевірка римських чисел через скомпільований Regex
            if (IsRomanNumeral(s)) return false;

            return true;
        }

        private bool IsRomanNumeral(string s)
        {
            // Швидка перевірка: якщо є цифри/#/+, це не римське число
            if (s.Any(c => char.IsDigit(c) || c == '#' || c == '+')) return false;
            
            return _romanRegex.IsMatch(s);
        }
    }

    // --- UI / Helper Class ---
    static class InputHandler
    {
        public static string GetInputText(string[] args)
        {
            // 1. Якщо передано аргументи командного рядка (шлях до файлу)
            if (args.Length > 0)
            {
                string path = args[0];
                if (File.Exists(path))
                {
                    Console.WriteLine($"Читання з файлу: {path}");
                    return File.ReadAllText(path);
                }
                Console.WriteLine($"Файл не знайдено: {path}");
            }

            // 2. Якщо ні - питаємо користувача
            Console.WriteLine("Оберіть джерело тексту:");
            Console.WriteLine("1. Ввести текст вручну");
            Console.WriteLine("2. Завантажити тестовий приклад");
            Console.Write("Ваш вибір: ");
            
            var key = Console.ReadKey().Key;
            Console.WriteLine();

            if (key == ConsoleKey.D1)
            {
                Console.WriteLine("Введіть текст (Enter двічі для завершення):");
                StringBuilder sb = new StringBuilder();
                string line;
                while (!string.IsNullOrWhiteSpace(line = Console.ReadLine()))
                {
                    sb.AppendLine(line);
                }
                return sb.ToString();
            }

            // Тестовий приклад за замовчуванням
            return @"
                Аналіз технологій: C#, .NET, HTML5, CSS3.
                Ми використовуємо C# для бекенду та HTML5 для фронтенду.
                Також C++ використовується для драйверів.
                Знайди мене: 192.168.1.1, 10.0.0.1.
                Дати релізів: 24.08.1991, 01/01/2000.
                Ігнорувати римські: XXI
            ";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                // Отримуємо текст (з файлу, консолі або тесту)
                string text = InputHandler.GetInputText(args);

                Console.WriteLine("\n--- Початок аналізу ---");
                var analyzer = new TextAnalyzer();
                var report = analyzer.Analyze(text);

                foreach (var category in report)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\nКатегорія: {category.Key}");
                    Console.ResetColor();

                    if (category.Value.Count == 0)
                    {
                        Console.WriteLine("  (Пусто)");
                        continue;
                    }

                    // Виводимо топ значень
                    Console.WriteLine($"  Знайдено унікальних: {category.Value.Count}");
                    Console.WriteLine("  Статистика:");
                    
                    foreach (var item in category.Value.OrderByDescending(x => x.Value))
                    {
                        Console.WriteLine($"   - {item.Key,-15}: {item.Value} раз(ів)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критична помилка: {ex.Message}");
            }

            Console.WriteLine("\nНатисніть Enter для виходу...");
            Console.ReadLine();
        }
    }
}
