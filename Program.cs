using System;
using System.Collections.Generic;
using System.Linq; // Потрібно для Distinct()
using System.Text.RegularExpressions;

namespace RegexLabImproved
{
    /// <summary>
    /// Статичний клас для аналізу тексту.
    /// Містить попередньо скомпільовані регулярні вирази для продуктивності.
    /// </summary>
    public static class TextAnalyzer
    {
        // 1. ВИПРАВЛЕНИЙ REGEX ДЛЯ АБРЕВІАТУР
        // Логіка:
        // Частина 1: \b[A-Z]{2,}\b 
        //    -> Знаходить слова з 2+ великих літер (HTML, JSON). \b гарантує, що це окреме слово.
        //    -> Це відсіює "Hello" (бо там є малі) і "I" (бо одна літера).
        //
        // Частина 2: \b[A-Z][A-Za-z0-9]*[#+]+
        //    -> Знаходить терміни, що починаються з літери, але обов'язково закінчуються на # або + (C#, C++).
        //    -> Тут ми не ставимо \b в кінці, бо #/+ не є "word char", і \b там не спрацює як очікується.
        //
        private const string AbbreviationPattern = @"\b[A-Z]{2,}\b|\b[A-Z][A-Za-z0-9]*[#+]+";
        
        // Додаткові патерни для бонусного завдання
        private const string IpPattern = @"\b(?:\d{1,3}\.){3}\d{1,3}\b";
        private const string DatePattern = @"\b\d{1,2}[./-]\d{1,2}[./-]\d{2,4}\b";

        // Створюємо Regex один раз із опцією Compiled для швидкодії
        private static readonly Regex _abbreviationRegex = new Regex(AbbreviationPattern, RegexOptions.Compiled);
        private static readonly Regex _ipRegex = new Regex(IpPattern, RegexOptions.Compiled);
        private static readonly Regex _dateRegex = new Regex(DatePattern, RegexOptions.Compiled);

        /// <summary>
        /// Знаходить унікальні абревіатури у тексті.
        /// </summary>
        public static IEnumerable<string> GetUniqueAbbreviations(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Enumerable.Empty<string>();

            // Отримуємо Matches, перетворюємо на String, фільтруємо унікальні
            return _abbreviationRegex.Matches(text)
                                     .Cast<Match>()
                                     .Select(m => m.Value)
                                     .Distinct(); 
        }

        /// <summary>
        /// Комплексний аналіз тексту (Бонусне завдання).
        /// </summary>
        public static void PrintAnalysisReport(string text)
        {
            Console.WriteLine("\n--- Звіт аналізу тексту ---");

            var abbrs = _abbreviationRegex.Matches(text);
            var ips = _ipRegex.Matches(text);
            var dates = _dateRegex.Matches(text);

            Console.WriteLine($"Знайдено сутностей:");
            Console.WriteLine($" - Абревіатур: {abbrs.Count}");
            Console.WriteLine($" - IP-адрес:   {ips.Count}");
            Console.WriteLine($" - Дат:        {dates.Count}");

            Console.WriteLine("\nДеталі (Абревіатури):");
            foreach (var item in abbrs.Cast<Match>().Select(m => m.Value).Distinct())
            {
                Console.Write($"[{item}] ");
            }
            Console.WriteLine("\n---------------------------");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Текст, що містить пастки для старого коду (C#, C++, Hello, I, дати)
            string testText = 
                "Сучасні мови: C#, C++ та Java. Web використовує HTML5, JSON та CSS. " +
                "Зверніть увагу на сервер 192.168.0.1, запущений 12.11.2025. " +
                "Слова Hello, World, та займенник Я (I) не повинні потрапити у вибірку. " +
                "Ще раз повторюю: HTML, JSON (дублікати мають зникнути).";

            Console.WriteLine("Вхідний текст:\n" + testText);

            // 1. Основне завдання: Пошук та вивід унікальних абревіатур
            try
            {
                var results = TextAnalyzer.GetUniqueAbbreviations(testText);

                Console.WriteLine("\n[Результат основного завдання]");
                if (results.Any())
                {
                    Console.WriteLine("Знайдені унікальні скорочення:");
                    foreach (var abbr in results)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"> {abbr}");
                    }
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("Нічого не знайдено.");
                }

                // 2. Бонусне завдання (Статистика)
                TextAnalyzer.PrintAnalysisReport(testText);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Сталася помилка при обробці: {ex.Message}");
            }

            Console.WriteLine("\nНатисніть Enter для завершення...");
            Console.ReadLine();
        }
    }
}
