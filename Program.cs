using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace RegexLabUltimate
{
    /// <summary>
    /// Аналізатор тексту.
    /// Реалізує пошук за шаблонами з суровою пост-валідацією.
    /// </summary>
    public class TextAnalyzer
    {
        // Делегат для валідації: приймає рядок-кандидат, повертає true, якщо він валідний.
        private delegate bool Validator(string candidate);

        // Словник правил: Назва -> (Regex, Валідатор)
        private readonly Dictionary<string, (Regex Pattern, Validator Validator)> _rules;

        // Формати дат для суворої перевірки
        private readonly string[] _dateFormats = { 
            "d.M.yyyy", "dd.MM.yyyy", 
            "d/M/yyyy", "dd/MM/yyyy", 
            "yyyy-MM-dd" 
        };

        public TextAnalyzer()
        {
            // ExplicitCapture: покращує продуктивність, не зберігаючи безіменні групи.
            // Compiled: пришвидшує роботу при багаторазовому використанні.
            var opts = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
            var timeout = TimeSpan.FromSeconds(1.0);

            _rules = new Dictionary<string, (Regex, Validator)>
            {
                // 1. АБРЕВІАТУРИ
                // \p{Lu} - будь-яка велика літера Unicode (підтримка кирилиці: ЗСУ, НАТО).
                // Логіка: 
                //   1. .NET (окремий кейс, lookbehind перевіряє, що перед крапкою немає букви)
                //   2. Класичні абревіатури: мінімум 2 великі літери, можуть бути цифри в кінці (HTML5).
                //   3. Технічні: 1-2 великі літери + спецсимволи # або + (C#, C++).
                { 
                    "Абревіатури", 
                    (new Regex(@"(?<!\w)\.NET\b|\b\p{Lu}{2,}[0-9]*\b|\b\p{Lu}{1,2}[#+]+(?![a-zA-Z0-9])", opts, timeout), 
                    IsValidAbbreviation) 
                },

                // 2. IP-АДРЕСИ (IPv4)
                // Попередній відбір через Regex, точна перевірка через IPAddress.Parse
                { 
                    "IP-адреси", 
                    (new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", opts, timeout), 
                    s => IPAddress.TryParse(s, out _)) 
                },

                // 3. ДАТИ
                // Попередній відбір, точна перевірка через TryParseExact
                { 
                    "Дати", 
                    (new Regex(@"\b\d{1,4}[./-]\d{1,2}[./-]\d{2,4}\b", opts, timeout), 
                    IsValidDate) 
                }
            };
        }

        /// <summary>
        /// Головний метод аналізу.
        /// </summary>
        public Dictionary<string, List<string>> Analyze(string text)
        {
            var results = new Dictionary<string, List<string>>();

            if (string.IsNullOrWhiteSpace(text)) return results;

            foreach (var rule in _rules)
            {
                results[rule.Key] = ProcessCategory(text, rule.Value.Pattern, rule.Value.Validator);
            }

            return results;
        }

        /// <summary>
        /// Приватний метод для обробки однієї категорії (Refactoring: зменшення дублювання).
        /// </summary>
        private List<string> ProcessCategory(string text, Regex regex, Validator validator)
        {
            var validMatches = new List<string>();

            try
            {
                // Знаходимо унікальні кандидати
                var candidates = regex.Matches(text)
                                      .Cast<Match>()
                                      .Select(m => m.Value)
                                      .Distinct();

                foreach (var candidate in candidates)
                {
                    try
                    {
                        // Захищений виклик валідатора
                        if (validator(candidate))
                        {
                            validMatches.Add(candidate);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Логування помилки валідації конкретного значення (для дебагу)
                        System.Diagnostics.Debug.WriteLine($"Помилка валідації '{candidate}': {ex.Message}");
                    }
                }
            }
            catch (RegexMatchTimeoutException)
            {
                validMatches.Add("ERROR: Regex Timeout");
            }

            return validMatches;
        }

        // --- ХЕЛПЕРИ ДЛЯ ВАЛІДАЦІЇ ---

        private bool IsValidDate(string dateStr)
        {
            return DateTime.TryParseExact(
                dateStr, 
                _dateFormats, 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.None, 
                out _
            );
        }

        private bool IsValidAbbreviation(string s)
        {
            // 1. Перевірка на ".NET" (це валідна абревіатура для нас)
            if (s.Equals(".NET", StringComparison.OrdinalIgnoreCase)) return true;
            
            // 2. Фільтрація Римських цифр (I, II, IV, XIX...)
            // Якщо слово складається ТІЛЬКИ з символів римських чисел, перевіряємо його структуру.
            // Це простий евристичний фільтр.
            if (IsRomanNumeral(s)) return false;

            return true;
        }

        /// <summary>
        /// Перевіряє, чи є рядок римським числом.
        /// </summary>
        private bool IsRomanNumeral(string s)
        {
            // Якщо є цифри або #/+, це точно не римське число (C++, HTML5)
            if (s.Any(c => char.IsDigit(c) || c == '#' || c == '+')) return false;

            // Регулярний вираз для перевірки коректності римських чисел
            string romanPattern = @"^M{0,4}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})$";
            return Regex.IsMatch(s, romanPattern);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Тестовий текст:
            // - Кирилиця (ЗСУ)
            // - Римські цифри (XXI століття) - мають ігноруватися
            // - C#, .NET, HTML5 - мають знайтися
            // - Некоректні дати
            string text = @"
                Вітаємо! Ми використовуємо стек: C#, .NET Core, HTML5 та CSS3.
                Наші сервери: 192.168.0.1 (local) та 8.8.8.8 (DNS).
                Історія: у XXI столітті (а також у XX) технології змінилися.
                Король Людовик XIV не знав про JSON.
                Важливі дати: 24.08.1991 (День Незалежності), 
                32.01.2023 (помилка), 2023-12-31 (новий рік).
                Українські скорочення: ЗСУ, НБУ, АЕС.
            ";

            Console.WriteLine("--- Вхідний текст ---");
            Console.WriteLine(text.Trim());
            Console.WriteLine(new string('-', 40));

            try
            {
                var analyzer = new TextAnalyzer();
                var report = analyzer.Analyze(text);

                Console.WriteLine("\n--- Результати аналізу (Ultimate) ---");
                foreach (var category in report)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"{category.Key}: ");
                    Console.ResetColor();
                    Console.WriteLine($"{category.Value.Count} шт.");

                    if (category.Value.Any())
                    {
                        Console.WriteLine("   " + string.Join(", ", category.Value));
                    }
                    else
                    {
                        Console.WriteLine("   (Пусто)");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критична помилка: {ex.Message}");
            }

            Console.WriteLine("Натисніть Enter...");
            Console.ReadLine();
        }
    }
}
