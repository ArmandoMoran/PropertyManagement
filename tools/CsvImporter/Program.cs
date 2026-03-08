using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Dapper;

class Program
{
    static async Task Main()
    {
        string connStr = "Server=localhost;Database=PropertyManagement;Trusted_Connection=True;TrustServerCertificate=True;";
        var csvPath = @"C:\Users\arman\Downloads\Transactions (1).csv";
        
        using var db = new SqlConnection(connStr);
        await db.OpenAsync();

        Console.WriteLine($"Loading properties from DB...");
        var propertiesMap = (await db.QueryAsync<(int PropertyId, string Street)>(
            @"SELECT PropertyId, Street FROM Properties"))
            .ToDictionary(p => p.Street, p => p.PropertyId, StringComparer.OrdinalIgnoreCase);
        
        var nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "5212 Meadow Field", "5212 Meadow Fld" },
            { "8815 Adams Hill Dr", "8815 Adam Hill" },
            { "1508-1510 Raleigh Dr", "1508 Raleigh Dr" },
            { "6528 Oklahoma St SE", "6528 Oklahoma St" },
            { "838 Saddlebrook Dr", "838 Saddlebrook" },
            { "8322 Sageline St", "8322 Sageline" }
        };

        string NormalizeProperty(string csvProp)
        {
            csvProp = csvProp.Trim();
            return nameMap.TryGetValue(csvProp, out var mapped) ? mapped : csvProp;
        }

        var lines = File.ReadAllLines(csvPath);
        Console.WriteLine($"CSV lines: {lines.Length}");
        
        var records = ParseCsv(lines);
        Console.WriteLine($"Parsed records: {records.Count}");
        
        int imported = 0, skipped = 0;
        
        foreach (var rec in records)
        {
            var dateStr = rec.GetValueOrDefault("Date", "");
            if (!DateTime.TryParseExact(dateStr, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var txDate))
            {
                skipped++;
                continue;
            }
            
            var csvProp = rec.GetValueOrDefault("Property", "").Trim();
            int? propertyId = null;
            if (!string.IsNullOrEmpty(csvProp))
            {
                var dbStreet = NormalizeProperty(csvProp);
                if (propertiesMap.TryGetValue(dbStreet, out var pid))
                {
                    propertyId = pid;
                }
            }
            
            var name = rec.GetValueOrDefault("Name", "");
            var notes = rec.GetValueOrDefault("Notes", "");
            var details = rec.GetValueOrDefault("Details", "");
            var category = rec.GetValueOrDefault("Category", "");
            var subCategory = rec.GetValueOrDefault("Sub-Category", "");
            if (!decimal.TryParse(rec.GetValueOrDefault("Amount", "0"), out var amount)) amount = 0;
            var portfolio = rec.GetValueOrDefault("Portfolio", "");
            var unit = rec.GetValueOrDefault("Unit", "");
            var dataSource = rec.GetValueOrDefault("Data Source", "");
            var account = rec.GetValueOrDefault("Account", "");
            var owner = rec.GetValueOrDefault("Owner", "");
            
            var existing = await db.QueryFirstOrDefaultAsync<int?>(
                @"SELECT TOP 1 TransactionId FROM Transactions 
                  WHERE TransactionDate = @TxDate AND Amount = @Amount AND Name = @Name 
                  AND (PropertyId = @PropertyId OR (PropertyId IS NULL AND @PropertyId IS NULL))",
                new { TxDate = txDate, PropertyId = propertyId, Amount = amount, Name = name });
            
            if (existing.HasValue)
            {
                skipped++;
                continue;
            }
            
            await db.ExecuteAsync(
                @"INSERT INTO Transactions (TransactionDate, Name, Notes, Details, Category, SubCategory, Amount, Portfolio, PropertyId, PropertyRaw, Unit, DataSource, Account, Owner)
                  VALUES (@TxDate, @Name, @Notes, @Details, @Category, @SubCategory, @Amount, @Portfolio, @PropertyId, @PropertyRaw, @Unit, @DataSource, @Account, @Owner)",
                new
                {
                    TxDate = txDate, Name = name, Notes = notes, Details = details,
                    Category = string.IsNullOrWhiteSpace(category) ? (string?)null : category, 
                    SubCategory = string.IsNullOrWhiteSpace(subCategory) ? (string?)null : subCategory,
                    Amount = amount, Portfolio = portfolio,
                    PropertyId = propertyId, PropertyRaw = string.IsNullOrWhiteSpace(csvProp) ? (string?)null : csvProp,
                    Unit = string.IsNullOrWhiteSpace(unit) ? (string?)null : unit,
                    DataSource = string.IsNullOrWhiteSpace(dataSource) ? "Transaction Import" : dataSource,
                    Account = string.IsNullOrWhiteSpace(account) ? (string?)null : account,
                    Owner = string.IsNullOrWhiteSpace(owner) ? (string?)null : owner
                });
            imported++;
        }
        
        Console.WriteLine($"Imported: {imported}, Skipped (dups/invalid date): {skipped}");
    }

    static List<Dictionary<string, string>> ParseCsv(string[] lines)
    {
        var result = new List<Dictionary<string, string>>();
        if (lines.Length == 0) return result;
        
        var headers = ParseCsvLine(lines[0]);
        
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            while (CountQuotes(line) % 2 != 0 && i + 1 < lines.Length)
            {
                i++;
                line += "\n" + lines[i];
            }
            
            var fields = ParseCsvLine(line);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int j = 0; j < headers.Count && j < fields.Count; j++)
            {
                dict[headers[j]] = fields[j];
            }
            result.Add(dict);
        }
        return result;
    }

    static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        fields.Add(current.ToString().Trim());
        return fields;
    }

    static int CountQuotes(string s) => s.Count(c => c == '"');
}
