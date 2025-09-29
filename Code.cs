using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PersonalFinancePlanner
{
    public enum Category {Food, Transport, Fun, School, Other}

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    public static class Tools
    {
        public static decimal SafeParseDecimal(string input)
        {
            if (!decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
                throw new ValidationException("Nederīga skaitliskā vērtība.");
            if (v <= 0)
                throw new ValidationException("Vērtībai jābūt lielākai par 0.");
            return v;
        }

        public static decimal SafeDivide(decimal a, decimal b)
        {
            if (b == 0) return 0;
            return a / b;
        }

        public static string Percent(decimal part, decimal total)
        {
            if (total == 0) return "0%";
            var p = SafeDivide(part * 100, total);
            return Math.Round(p, 1) + "%";
        }

        public static string ReadNonEmptyString(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(s)) return s;
                Console.WriteLine("Ievade nedrīkst būt tukša. Mēģini vēlreiz.");
            }
        }

        public static DateTime ReadDate(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine()?.Trim();
                if (DateTime.TryParse(s, out var d)) return d.Date;
                Console.WriteLine("Nederīgs datums. Lietot pieņemtus datuma formātus (piem., 2025-09-29). Mēģini vēlreiz.");
            }
        }
    }

    public class Income
    {
        public DateTime Date { get; set; }
        public string Source { get; set; }
        public decimal Amount { get; set; }

        public Income() { }

        public Income(DateTime date, string source, decimal amount)
        {
            Date = date;
            Source = source ?? throw new ValidationException("Avots nevar būt tukšs.");
            if (amount <= 0) throw new ValidationException("Ienākumam jābūt > 0.");
            Amount = amount;
        }

        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd} | {Source,-15} | {Amount,10:N2} €";
        }
    }

    public class Expense
    {
        public DateTime Date { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Category Category { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; }

        public Expense() { }

        public Expense(DateTime date, Category category, decimal amount, string note)
        {
            Date = date;
            Category = category;
            if (amount <= 0) throw new ValidationException("Izdevumam jābūt > 0.");
            Amount = amount;
            Note = note ?? string.Empty;
        }

        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd} | {Category,-9} | {Amount,10:N2} € | {Note}";
        }
    }

    public class Subscription
    {
        public string Name { get; set; }
        public decimal MonthlyPrice { get; set; }
        public DateTime StartDate { get; set; }
        public bool IsActive { get; set; }

        public Subscription() { }

        public Subscription(string name, decimal monthlyPrice, DateTime startDate, bool isActive = true)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ValidationException("Abonementa nosaukums nevar būt tukšs.");
            if (monthlyPrice <= 0) throw new ValidationException("Mēneša maksa jābūt > 0.");
            MonthlyPrice = monthlyPrice;
            StartDate = startDate;
            IsActive = isActive;
        }

        public override string ToString()
        {
            return $"{Name,-20} | {MonthlyPrice,8:N2} € | {StartDate:yyyy-MM-dd} | {(IsActive ? "Aktīvs" : "Neaktīvs")}";
        }
    }

    class Program
    {
        static List<Income> incomes = new List<Income>();
        static List<Expense> expenses = new List<Expense>();
        static List<Subscription> subscriptions = new List<Subscription>();

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            ShowHelp();
            while (true)
            {
                try
                {
                    Console.WriteLine();
                    Console.WriteLine("=== PERSONĪGAIS FINANŠU PLĀNOTĀJS ===");
                    Console.WriteLine("1) Ienākumi  2) Izdevumi  3) Abonementi  4) Saraksti  5) Filtri  6) Mēneša pārskats  7) Import/Export JSON  0) Iziet");
                    Console.Write("Izvēlies opciju: ");
                    var c = Console.ReadLine();
                    switch (c)
                    {
                        case "1": IncomesMenu(); break;
                        case "2": ExpensesMenu(); break;
                        case "3": SubscriptionsMenu(); break;
                        case "4": ListsMenu(); break;
                        case "5": FiltersMenu(); break;
                        case "6": MonthlyReport(); break;
                        case "7": JsonMenu(); break;
                        case "0": return;
                        case "help": ShowHelp(); break;
                        default: Console.WriteLine("Nederīga izvēle."); break;
                    }
                }
                catch (ValidationException vex)
                {
                    Console.WriteLine($"Kļūda: {vex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Nezināma kļūda: {ex.Message}");
                }
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Palīdzība: ievadi komandas numerus. Pie datuma ievades vari izmantot formātu YYYY-MM-DD. JSON importam/iizekspor-tam izmanto opciju 7.");
        }
        static void IncomesMenu()
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("--- IENĀKUMI ---");
                Console.WriteLine("1) Pievienot  2) Parādīt  3) Dzēst  0) Atpakaļ");
                var c = Console.ReadLine();
                if (c == "0") return;
                try
                {
                    if (c == "1") AddIncome();
                    else if (c == "2") ShowIncomes();
                    else if (c == "3") DeleteIncome();
                    else Console.WriteLine("Nederīga izvēle.");
                }
                catch (ValidationException) { throw; }
                catch (Exception ex) { Console.WriteLine($"Kļūda: {ex.Message}"); }
            }
        }

        static void AddIncome()
        {
            var date = Tools.ReadDate("Datums (YYYY-MM-DD): ");
            var source = Tools.ReadNonEmptyString("Avots: ");
            Console.Write("Summa: ");
            var amountStr = Console.ReadLine();
            var amount = Tools.SafeParseDecimal(amountStr);
            incomes.Add(new Income(date, source, amount));
            Console.WriteLine("Ienākums pievienots.");
        }

        static void ShowIncomes()
        {
            if (!incomes.Any()) { Console.WriteLine("Nav ienākumu."); return; }
            var ordered = incomes.OrderByDescending(i => i.Date).ToList();
            Console.WriteLine("Datums       | Avots           |     Summa");
            Console.WriteLine(new string('-', 45));
            foreach (var i in ordered) Console.WriteLine(i.ToString());
        }

        static void DeleteIncome()
        {
            ShowIncomes();
            Console.Write("Ievadi dzēšamā ieraksta datumu (YYYY-MM-DD) vai pilnu avotu nosaukumu: ");
            var key = Console.ReadLine()?.Trim();
            var found = incomes.Where(x => x.Date.ToString("yyyy-MM-dd") == key || x.Source.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!found.Any()) { Console.WriteLine("Nekas netika atrasts."); return; }
            foreach (var f in found) Console.WriteLine(f.ToString());
            Console.Write("Apstiprini dzēšanu (j/n): ");
            if (Console.ReadLine()?.ToLower() == "j")
            {
                foreach (var f in found) incomes.Remove(f);
                Console.WriteLine("Dzēsts.");
            }
        }
		
        static void ExpensesMenu()
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("--- IZDEVUMI ---");
                Console.WriteLine("1) Pievienot  2) Parādīt  3) Dzēst  4) Filtrēt  0) Atpakaļ");
                var c = Console.ReadLine();
                if (c == "0") return;
                try
                {
                    if (c == "1") AddExpense();
                    else if (c == "2") ShowExpenses();
                    else if (c == "3") DeleteExpense();
                    else if (c == "4") FilterExpensesInteractive();
                    else Console.WriteLine("Nederīga izvēle.");
                }
                catch (ValidationException) { throw; }
                catch (Exception ex) { Console.WriteLine($"Kļūda: {ex.Message}"); }
            }
        }

        static void AddExpense()
        {
            var date = Tools.ReadDate("Datums (YYYY-MM-DD): ");
            Console.WriteLine("Kategorijas: " + string.Join(", ", Enum.GetNames(typeof(Category))));
            var catStr = Tools.ReadNonEmptyString("Kategorija: ");
            if (!Enum.TryParse<Category>(catStr, true, out var category)) throw new ValidationException("Nederīga kategorija.");
            Console.Write("Summa: ");
            var amount = Tools.SafeParseDecimal(Console.ReadLine());
            var note = Tools.ReadNonEmptyString("Piezīme: ");
            expenses.Add(new Expense(date, category, amount, note));
            Console.WriteLine("Izdevums pievienots.");
        }

        static void ShowExpenses()
        {
            if (!expenses.Any()) { Console.WriteLine("Nav izdevumu."); return; }
            var ordered = expenses.OrderByDescending(e => e.Date).ToList();
            Console.WriteLine("Datums       | Kategorija |     Summa € | Piezīme");
            Console.WriteLine(new string('-', 70));
            foreach (var e in ordered) Console.WriteLine(e.ToString());
        }

        static void DeleteExpense()
        {
            ShowExpenses();
            Console.Write("Ievadi dzēšamā ieraksta datumu (YYYY-MM-DD) vai piezīmes fragmentu: ");
            var key = Console.ReadLine()?.Trim();
            var found = expenses.Where(x => x.Date.ToString("yyyy-MM-dd") == key || x.Note.IndexOf(key ?? "", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if (!found.Any()) { Console.WriteLine("Nekas netika atrasts."); return; }
            foreach (var f in found) Console.WriteLine(f.ToString());
            Console.Write("Apstiprini dzēšanu (j/n): ");
            if (Console.ReadLine()?.ToLower() == "j")
            {
                foreach (var f in found) expenses.Remove(f);
                Console.WriteLine("Dzēsts.");
            }
        }

        static void FilterExpensesInteractive()
        {
            Console.WriteLine("Filtrēt pēc: 1) Datuma diapazons  2) Kategorija  0) Atpakaļ");
            var c = Console.ReadLine();
            if (c == "0") return;
            if (c == "1")
            {
                var from = Tools.ReadDate("No datuma (YYYY-MM-DD): ");
                var to = Tools.ReadDate("Līdz datuma (YYYY-MM-DD): ");
                var res = expenses.Where(e => e.Date >= from && e.Date <= to).OrderByDescending(e => e.Date).ToList();
                PrintExpensesWithSum(res);
            }
            else if (c == "2")
            {
                Console.WriteLine("Kategorijas: " + string.Join(", ", Enum.GetNames(typeof(Category))));
                var catStr = Tools.ReadNonEmptyString("Kategorija: ");
                if (!Enum.TryParse<Category>(catStr, true, out var category)) { Console.WriteLine("Nederīga kategorija."); return; }
                var res = expenses.Where(e => e.Category == category).OrderByDescending(e => e.Date).ToList();
                PrintExpensesWithSum(res);
            }
            else Console.WriteLine("Nederīga izvēle.");
        }

        static void PrintExpensesWithSum(List<Expense> list)
        {
            if (!list.Any()) { Console.WriteLine("Nav ierakstu."); return; }
            foreach (var e in list) Console.WriteLine(e.ToString());
            Console.WriteLine(new string('-', 40));
            Console.WriteLine($"Kopā: {list.Sum(x => x.Amount):N2} €");
        }
        static void SubscriptionsMenu()
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("--- ABONEMENTI ---");
                Console.WriteLine("1) Pievienot  2) Parādīt  3) Aktivizēt/Deaktivizēt  4) Dzēst  0) Atpakaļ");
                var c = Console.ReadLine();
                if (c == "0") return;
                try
                {
                    if (c == "1") AddSubscription();
                    else if (c == "2") ShowSubscriptions();
                    else if (c == "3") ToggleSubscription();
                    else if (c == "4") DeleteSubscription();
                    else Console.WriteLine("Nederīga izvēle.");
                }
                catch (ValidationException) { throw; }
                catch (Exception ex) { Console.WriteLine($"Kļūda: {ex.Message}"); }
            }
        }

        static void AddSubscription()
        {
            var name = Tools.ReadNonEmptyString("Nosaukums: ");
            Console.Write("Mēneša maksa: ");
            var price = Tools.SafeParseDecimal(Console.ReadLine());
            var start = Tools.ReadDate("Sākuma datums (YYYY-MM-DD): ");
            subscriptions.Add(new Subscription(name, price, start, true));
            Console.WriteLine("Abonements pievienots.");
        }

        static void ShowSubscriptions()
        {
            if (!subscriptions.Any()) { Console.WriteLine("Nav abonementu."); return; }
            Console.WriteLine("Nosaukums              |   Cena € | Sākums     | Stāvoklis");
            Console.WriteLine(new string('-', 60));
            foreach (var s in subscriptions.OrderByDescending(s => s.StartDate)) Console.WriteLine(s.ToString());
        }

        static void ToggleSubscription()
        {
            ShowSubscriptions();
            Console.Write("Ievadi abonementa nosaukumu: ");
            var name = Console.ReadLine()?.Trim();
            var s = subscriptions.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (s == null) { Console.WriteLine("Nav atrasts."); return; }
            s.IsActive = !s.IsActive;
            Console.WriteLine($"Abonements '{s.Name}' tagad: {(s.IsActive ? "Aktīvs" : "Neaktīvs")}.");
        }

        static void DeleteSubscription()
        {
            ShowSubscriptions();
            Console.Write("Ievadi dzēšamā abonementa nosaukumu: ");
            var name = Console.ReadLine()?.Trim();
            var s = subscriptions.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (s == null) { Console.WriteLine("Nav atrasts."); return; }
            Console.Write("Apstiprini dzēšanu (j/n): ");
            if (Console.ReadLine()?.ToLower() == "j") { subscriptions.Remove(s); Console.WriteLine("Dzēsts."); }
        }
        static void ListsMenu()
        {
            Console.WriteLine("--- SARAKSTI (visi ieraksti sakārtoti pēc datuma dilstoši) ---");
            var all = new List<(DateTime date, string text)>();
            all.AddRange(incomes.Select(i => (i.Date, "I: " + i.ToString())));
            all.AddRange(expenses.Select(e => (e.Date, "E: " + e.ToString())));
            all.AddRange(subscriptions.Select(s => (s.StartDate, "S: " + s.ToString())));
            foreach (var item in all.OrderByDescending(x => x.date)) Console.WriteLine(item.text);
        }
        static void FiltersMenu()
        {
            Console.WriteLine("--- FILTRI ---");
            Console.WriteLine("1) Pēc datuma diapazona  2) Pēc kategorijas (izdevumi)  0) Atpakaļ");
            var c = Console.ReadLine();
            if (c == "0") return;
            if (c == "1")
            {
                var from = Tools.ReadDate("No datuma: ");
                var to = Tools.ReadDate("Līdz datuma: ");
                var inc = incomes.Where(i => i.Date >= from && i.Date <= to).ToList();
                var exp = expenses.Where(e => e.Date >= from && e.Date <= to).ToList();
                Console.WriteLine($"Ienākumu summa: {inc.Sum(x => x.Amount):N2} €");
                Console.WriteLine($"Izdevumu summa: {exp.Sum(x => x.Amount):N2} €");
            }
            else if (c == "2")
            {
                Console.WriteLine("Kategorijas: " + string.Join(", ", Enum.GetNames(typeof(Category))));
                var catStr = Tools.ReadNonEmptyString("Kategorija: ");
                if (!Enum.TryParse<Category>(catStr, true, out var category)) { Console.WriteLine("Nederīga kategorija."); return; }
                var res = expenses.Where(e => e.Category == category).ToList();
                PrintExpensesWithSum(res);
            }
            else Console.WriteLine("Nederīga izvēle.");
        }
		
        static void MonthlyReport()
        {
            Console.WriteLine("Ievadi mēnesi formātā YYYY-MM (piem., 2025-09): ");
            var s = Console.ReadLine()?.Trim();
            if (!DateTime.TryParseExact(s + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var firstOfMonth))
            {
                Console.WriteLine("Nederīgs formāts.");
                return;
            }
            var lastOfMonth = new DateTime(firstOfMonth.Year, firstOfMonth.Month, DateTime.DaysInMonth(firstOfMonth.Year, firstOfMonth.Month));
            var inc = incomes.Where(i => i.Date >= firstOfMonth && i.Date <= lastOfMonth).ToList();
            var exp = expenses.Where(e => e.Date >= firstOfMonth && e.Date <= lastOfMonth).ToList();
            var activeSubs = subscriptions.Where(su => su.IsActive && su.StartDate <= lastOfMonth).ToList();

            decimal incSum = inc.Sum(x => x.Amount);
            decimal expSum = exp.Sum(x => x.Amount);
            decimal subsSum = activeSubs.Sum(x => x.MonthlyPrice);
            decimal net = incSum - expSum - subsSum;

            Console.WriteLine();
            Console.WriteLine($"Mēneša pārskats {firstOfMonth:yyyy-MM}");
            Console.WriteLine(new string('-', 40));
            Console.WriteLine($"Ienākumi: {incSum:N2} €");
            Console.WriteLine($"Izdevumi: {expSum:N2} €");
            Console.WriteLine($"Aktīvie abonementi (kopā mēnesī): {subsSum:N2} € ({activeSubs.Count} gab.)");
            Console.WriteLine($"Neto: {net:N2} €");
            Console.WriteLine();
            Console.WriteLine("Kategoriju sadalījums (izdevumi):");
            var byCat = exp.GroupBy(e => e.Category).Select(g => new { Cat = g.Key, Sum = g.Sum(x => x.Amount) }).ToList();
            foreach (var bc in byCat)
            {
                Console.WriteLine($"{bc.Cat,-9} — {bc.Sum:N2} € ({Tools.Percent(bc.Sum, expSum)})");
            }
            if (exp.Any())
            {
                var max = exp.OrderByDescending(e => e.Amount).First();
                Console.WriteLine($"\nLielākais izdevums: {max.Amount:N2} € — {max.Category} ({max.Date:yyyy-MM-dd}) — {max.Note}");
            }
            var days = (lastOfMonth - firstOfMonth).Days + 1;
            var avgDaily = Tools.SafeDivide(expSum, days);
            Console.WriteLine($"Vidējais dienas tēriņš: {Math.Round(avgDaily, 2):N2} €");
        }
		
        static void JsonMenu()
        {
            Console.WriteLine("1) Eksportēt JSON  2) Importēt JSON  0) Atpakaļ");
            var c = Console.ReadLine();
            if (c == "0") return;
            if (c == "1") ExportJson();
            else if (c == "2") ImportJson();
            else Console.WriteLine("Nederīga izvēle.");
        }

        static void ExportJson()
        {
            var wrapper = new
            {
                Incomes = incomes,
                Expenses = expenses,
                Subscriptions = subscriptions
            };
            var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
            var json = JsonSerializer.Serialize(wrapper, options);
            Console.WriteLine("--- JSON output ---");
            Console.WriteLine(json);
            Console.WriteLine("--- END ---");
            Console.WriteLine("(Kopē starp --- JSON output --- un --- END ---) ");
        }

        static void ImportJson()
        {
            Console.WriteLine("Ielīmē JSON tekstu. Pabeidz ar rindu: ---END---");
            var lines = new List<string>();
            while (true)
            {
                var line = Console.ReadLine();
                if (line == "---END---") break;
                lines.Add(line);
            }
            var json = string.Join("\n", lines);
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
                var doc = JsonDocument.Parse(json);
                var tempIn = new List<Income>();
                var tempEx = new List<Expense>();
                var tempSub = new List<Subscription>();

                if (doc.RootElement.TryGetProperty("Incomes", out var jin))
                {
                    foreach (var el in jin.EnumerateArray())
                    {
                        var date = el.GetProperty("Date").GetDateTime();
                        var source = el.GetProperty("Source").GetString();
                        var amount = el.GetProperty("Amount").GetDecimal();
                        tempIn.Add(new Income(date, source, amount));
                    }
                }

                if (doc.RootElement.TryGetProperty("Expenses", out var jex))
                {
                    foreach (var el in jex.EnumerateArray())
                    {
                        var date = el.GetProperty("Date").GetDateTime();
                        var catStr = el.GetProperty("Category").GetString();
                        if (!Enum.TryParse<Category>(catStr, true, out var cat)) throw new ValidationException("Nederīga kategorija importā.");
                        var amount = el.GetProperty("Amount").GetDecimal();
                        var note = el.GetProperty("Note").GetString();
                        tempEx.Add(new Expense(date, cat, amount, note));
                    }
                }

                if (doc.RootElement.TryGetProperty("Subscriptions", out var jsub))
                {
                    foreach (var el in jsub.EnumerateArray())
                    {
                        var name = el.GetProperty("Name").GetString();
                        var price = el.GetProperty("MonthlyPrice").GetDecimal();
                        var start = el.GetProperty("StartDate").GetDateTime();
                        var active = el.GetProperty("IsActive").GetBoolean();
                        tempSub.Add(new Subscription(name, price, start, active));
                    }
                }
                incomes = tempIn;
                expenses = tempEx;
                subscriptions = tempSub;
                Console.WriteLine("Importēts veiksmīgi.");
            }
            catch (JsonException jex)
            {
                Console.WriteLine($"JSON kļūda: {jex.Message}. Importēšana atcelta.");
            }
            catch (ValidationException vex)
            {
                Console.WriteLine($"Validācijas kļūda: {vex.Message}. Importēšana atcelta.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kļūda importā: {ex.Message}. Importēšana atcelta.");
            }
        }
    }
}
