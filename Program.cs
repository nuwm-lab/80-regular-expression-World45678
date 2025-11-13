using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace RegexLabFinal
{
    /// <summary>
    /// Сервіс для аналізу тексту за допомогою регулярних виразів.
    /// Реалізує патерн "Стратегія" через словник правил.
    /// </summary>
    public class TextAnalyzer
    {
        // Словник: Назва категорії -> Регулярний вираз
        private readonly Dictionary<string, Regex> _patterns;

        public TextAnalyzer()
        {
            // Налаштування Regex:
            // 1. Compiled - для швидкодії при багаторазовому використанні.
            // 2. CultureInvariant - щоб поведінка не залежала від мови ОС.
            // 3. Timeout - захист від ReDoS атак (зависання).
            var options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
            var timeout = TimeSpan.FromSeconds(2.0);

            _patterns = new Dictionary<string, Regex>
            {
                // 1. Абревіатури та тех. терміни
                // Група 1: \b[A-Z][A-Z0-9]+\b -> Починається з букви, далі букви або цифри (HTML, HTML5, MP3). Мінімум 2 символи.
                // Група 2: \b[A-Z]{1,2}[#+]+(?![A-Za-z0-9]) -> C#, C++, J#. 
                //          (?![A-Za-z0-9]) - це Negative Lookahead. Означає "зупинись, якщо далі не буква і не цифра".
                { "Абревіатури", new Regex(@"\b[A-Z][A-Z0-9]+\b|\b[A-Z]{1,2}[#+]+(?![A-Za-z0-9])", options, timeout) },

                // 2. IP-адреси (IPv4)
                { "IP-адреси",   new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", options, timeout) },

                // 3. Дати (формати dd.mm.yyyy, dd/mm/yyyy тощо)
                { "Дати",        new Regex(@"\b\d{1,2}[./-]\d{1,2}[./-]\d{2,4}\b", options, timeout) }
            };
        }

        /// <summary>
        /// Універсальний метод пошуку.
        /// </summary>
        /// <param name="text">Вхідний текст.</param>
        /// <param name="category">Ключ категорії (наприклад, "Абревіатури").</param>
        /// <returns>Список унікальних значень.</returns>
        public IEnumerable<string> GetUniqueMatches(string text, string category)
        {
            if (string.IsNullOrWhiteSpace(text) || !_patterns.ContainsKey(category))
                return Enumerable.Empty<string>();

            try
            {
                return _patterns[category]
                    .Matches(text)
                    .Select(m => m.Value)
                    .Distinct(); // Забезпечуємо унікальність
            }
            catch (RegexMatchTimeoutException)
            {
                Console.WriteLine($"[Увага] Час очікування Regex для категорії '{category}' вичерпано.");
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Генерує повний звіт по всіх категоріях.
        /// </summary>
        public void PrintFullReport(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine("Текст порожній.");
                return;
            }

            Console.WriteLine("\n=== ЗВЕДЕНИЙ ЗВІТ АНАЛІЗУ ===");
            Console.WriteLine($"{"Категорія",-15} | {"К-сть",-5} | {"Знайдені унікальні значення"}");
            Console.WriteLine(new string('-', 60));

            foreach (var category in _patterns.Keys)
            {
                var matches = GetUniqueMatches(text, category).ToList();
                string valuesString = matches.Any() ? string.Join(", ", matches) : "-";
                
                // Форматований вивід таблицею
                Console.WriteLine($"{category,-15} | {matches.Count,-5} | {valuesString}");
            }
            Console.WriteLine(new string('=', 60));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Текст для тестування всіх випадків:
            // - HTML5 (цифра в кінці)
            // - MP3 (цифра всередині або в кінці)
            // - C#, C++ (спецсимволи)
            // - .NET (починається з крапки - наш поточний патерн це не ловить, якщо не додати, але C# ловить)
            // - Дати та IP
            string text = @"
                Сучасний стек технологій включає: HTML5, CSS3, та JavaScript (JS).
                Для бекенду використовують C#, C++ або Java.
                Ми перейшли з MP3 на нові формати.
                Сервер конфігурації знаходиться за адресою 192.168.1.10 або 10.0.0.5.
                Дата релізу: 14.11.2025 (оновлено 15/11/2025).
                Ігнорувати: просто слова Hello, World та I (одинарні).
                Повтори для перевірки унікальності: C#, HTML5, 192.168.1.10.
            ";

            Console.WriteLine("Вхідний текст:");
            Console.WriteLine(text.Trim());

            try
            {
                var analyzer = new TextAnalyzer();
                
                // Виконання основного та бонусного завдання (Звіт)
                analyzer.PrintFullReport(text);
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
