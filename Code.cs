using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

enum Category { Food, Transport, Fun, School, Other }

record Income(DateTime Date, string Source, decimal Amount)
{
    public override string ToString() => $"{Date:yyyy-MM-dd} | {Source} | {Amount:N2} €";
}

record Expense(DateTime Date, Category Category, decimal Amount, string Note)
{
    public override string ToString() => $"{Date:yyyy-MM-dd} | {Category} | {Amount:N2} € | {Note}";
}

record Subscription(string Name, decimal MonthlyPrice, DateTime StartDate, bool IsActive)
{
    public override string ToString() => $"{Name} | {MonthlyPrice:N2} € | {StartDate:yyyy-MM-dd} | {(IsActive ? "Active" : "Inactive")}";
}

class Program
{
    static List<Income> incomes = new();
    static List<Expense> expenses = new();
    static List<Subscription> subs = new();
    static JsonSerializerOptions jsonOpts = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    static void Main()
    {
        while (true)
        {
            Console.WriteLine("\n1) Income 2) Expenses 3) Subs 4) Lists 5) Filters 6) Report 7) JSON 0) Exit");
            Console.Write("Choice: ");
            switch (Console.ReadLine())
            {
                case "1": IncomeMenu(); break;
                case "2": ExpenseMenu(); break;
                case "3": SubMenu(); break;
                case "4": ShowAll(); break;
                case "5": FilterMenu(); break;
                case "6": Report(); break;
                case "7": JsonMenu(); break;
                case "0": return;
            }
        }
    }

    // --- Income ---
    static void IncomeMenu()
    {
        Console.WriteLine("1) Add 2) View 3) Delete 0) Back");
        var c = Console.ReadLine();
        if (c == "1") incomes.Add(new Income(ReadDate("Date"), ReadStr("Source: "), ReadDec("Amount: ")));
        else if (c == "2") incomes.ForEach(Console.WriteLine);
        else if (c == "3") Delete(incomes);
    }

    // --- Expenses ---
    static void ExpenseMenu()
    {
        Console.WriteLine("1) Add 2) View 3) Delete 0) Back");
        var c = Console.ReadLine();
        if (c == "1")
        {
            Console.WriteLine("Categories: " + string.Join(", ", Enum.GetNames(typeof(Category))));
            var cat = Enum.Parse<Category>(ReadStr("Category: "), true);
            expenses.Add(new Expense(ReadDate("Date"), cat, ReadDec("Amount: "), ReadStr("Note: ")));
        }
        else if (c == "2") expenses.ForEach(Console.WriteLine);
        else if (c == "3") Delete(expenses);
    }

    // --- Subscriptions ---
    static void SubMenu()
    {
        Console.WriteLine("1) Add 2) View 3) Toggle 4) Delete 0) Back");
        var c = Console.ReadLine();
        if (c == "1") subs.Add(new Subscription(ReadStr("Name: "), ReadDec("Price: "), ReadDate("Start date"), true));
        else if (c == "2") subs.ForEach(Console.WriteLine);
        else if (c == "3") { var s = Pick(subs); if (s != null) subs[subs.IndexOf(s)] = s with { IsActive = !s.IsActive }; }
        else if (c == "4") Delete(subs);
    }

    // --- Lists ---
    static void ShowAll()
    {
        incomes.ForEach(i => Console.WriteLine("I: " + i));
        expenses.ForEach(e => Console.WriteLine("E: " + e));
        subs.ForEach(s => Console.WriteLine("S: " + s));
    }

    // --- Filters ---
    static void FilterMenu()
    {
        Console.WriteLine("1) By Date 2) By Category");
        var c = Console.ReadLine();
        if (c == "1")
        {
            var f = ReadDate("From date");
            var t = ReadDate("To date");
            Console.WriteLine("Incomes: " + incomes.Where(i => i.Date >= f && i.Date <= t).Sum(i => i.Amount));
            Console.WriteLine("Expenses: " + expenses.Where(e => e.Date >= f && e.Date <= t).Sum(e => e.Amount));
        }
        else if (c == "2")
        {
            var cat = Enum.Parse<Category>(ReadStr("Category: "), true);
            var list = expenses.Where(e => e.Category == cat).ToList();
            list.ForEach(Console.WriteLine);
            Console.WriteLine("Total: " + list.Sum(e => e.Amount));
        }
    }

    // --- Report ---
    static void Report()
    {
        Console.Write("Month (YYYY-MM): ");
        var input = Console.ReadLine() ?? "";
        if (!DateTime.TryParseExact(input + "-01", "yyyy-MM-dd", null, DateTimeStyles.None, out var start))
        {
            Console.WriteLine("Invalid month format. Use YYYY-MM (e.g., 2025-09).");
            return;
        }
        var end = start.AddMonths(1).AddDays(-1);
        var inc = incomes.Where(i => i.Date >= start && i.Date <= end).Sum(i => i.Amount);
        var exp = expenses.Where(e => e.Date >= start && e.Date <= end).Sum(e => e.Amount);
        var sub = subs.Where(s => s.IsActive && s.StartDate <= end).Sum(s => s.MonthlyPrice);
        Console.WriteLine($"Income {inc:N2}€, Expenses {exp:N2}€, Subs {sub:N2}€, Net {(inc - exp - sub):N2}€");
    }

    // --- JSON ---
    static void JsonMenu()
    {
        Console.WriteLine("1) Export 2) Import");
        var c = Console.ReadLine();
        if (c == "1")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { incomes, expenses, subs }, jsonOpts));
        }
        else if (c == "2")
        {
            Console.WriteLine("Paste JSON then END:");
            var text = string.Join("\n", ReadUntil("END"));
            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(text, jsonOpts);
                Console.WriteLine("Imported (not restoring for simplicity).");
            }
            catch { Console.WriteLine("Invalid JSON."); }
        }
    }

    // --- Helpers ---
    static string ReadStr(string msg) { Console.Write(msg); return Console.ReadLine() ?? ""; }

    static decimal ReadDec(string msg)
    {
        Console.Write(msg);
        while (true)
        {
            if (decimal.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) && v > 0)
                return v;
            Console.Write("Invalid number, try again: ");
        }
    }

    static DateTime ReadDate(string msg)
    {
        Console.Write($"{msg} (YYYY-MM-DD): ");
        while (true)
        {
            var input = Console.ReadLine();
            if (DateTime.TryParseExact(input, "yyyy-MM-dd", null, DateTimeStyles.None, out var d))
                return d;
            Console.Write("Invalid date, use YYYY-MM-DD: ");
        }
    }

    static T? Pick<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++) Console.WriteLine($"{i + 1}) {list[i]}");
        if (int.TryParse(Console.ReadLine(), out var n) && n > 0 && n <= list.Count) return list[n - 1];
        return default;
    }

    static void Delete<T>(List<T> list)
    {
        var item = Pick(list);
        if (item != null) list.Remove(item);
    }

    static IEnumerable<string> ReadUntil(string stop)
    {
        string? l;
        while ((l = Console.ReadLine()) != null && l != stop) yield return l;
    }
}
