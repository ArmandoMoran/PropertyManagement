using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;

class Program
{
    static async Task Main()
    {
        string connStr = "Server=localhost;Database=PropertyManagement;Trusted_Connection=True;TrustServerCertificate=True;";
        var downloadsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var templatePath = Path.Combine(downloadsDir, "Investment Portfolio Questionnaire.xlsx");
        var outputPath = @"C:\repos\Investment_Portfolio_Questionnaire_Filled_v5.xlsx";
        
        using var db = new SqlConnection(connStr);
        await db.OpenAsync();

        Console.WriteLine("Generating Master Property Sheets with Summary...");

        var wb = new XLWorkbook();
        if (File.Exists(templatePath)) {
            wb = new XLWorkbook(templatePath);
        } else {
            wb.Worksheets.Add("Sheet1");
        }
        var ws = wb.Worksheet("Sheet1");
        ws.Name = "Questionnaire";

        var allProps = (await db.QueryAsync(@"
            SELECT p.PropertyId, p.Street, p.FullAddress, p.Owner, p.Units, p.PurchaseDate,
                   i.Carrier AS InsuranceCarrier, i.RenewalDate AS InsuranceRenewal,
                   l.LenderName, l.MortgageNumber
            FROM Properties p
            LEFT JOIN Insurance i ON p.PropertyId = i.PropertyId
            LEFT JOIN (SELECT PropertyId, LenderName, MortgageNumber, 
                       ROW_NUMBER() OVER (PARTITION BY PropertyId ORDER BY EffectiveDate DESC) AS rn
                       FROM Lenders) l ON p.PropertyId = l.PropertyId AND l.rn = 1
            ORDER BY p.Street")).ToList();

        var roofYears = (await db.QueryAsync<(int PropertyId, int Year)>(
            @"SELECT PropertyId, MAX(YEAR(TransactionDate)) AS Year FROM Transactions 
              WHERE (LOWER(Notes) LIKE '%replace%roof%' OR LOWER(Notes) LIKE '%new%roof%' OR LOWER(Notes) LIKE '%install%roof%')
                AND Amount < -3000 AND PropertyId IS NOT NULL
              GROUP BY PropertyId")).ToDictionary(x => x.PropertyId, x => x.Year);

        var hvacYears = (await db.QueryAsync<(int PropertyId, int Year)>(
            @"SELECT PropertyId, MAX(YEAR(TransactionDate)) AS Year FROM Transactions 
              WHERE (LOWER(Notes) LIKE '%new ac system%' 
                  OR LOWER(Notes) LIKE '%complete gas system%' 
                  OR LOWER(Notes) LIKE '%complete ac system%' 
                  OR LOWER(Notes) LIKE '%new system%' 
                  OR LOWER(Notes) LIKE '%new%unit%'
                  OR LOWER(Notes) LIKE '%replace complete system%')
                AND Amount < -3000 AND PropertyId IS NOT NULL
              GROUP BY PropertyId")).ToDictionary(x => x.PropertyId, x => x.Year);

        var plumbingYears = (await db.QueryAsync<(int PropertyId, int Year)>(
            @"SELECT PropertyId, MAX(YEAR(TransactionDate)) AS Year FROM Transactions 
              WHERE (LOWER(Notes) LIKE '%repipe%' OR LOWER(Notes) LIKE '%whole house repipe%')
                AND Amount < -2000 AND PropertyId IS NOT NULL
              GROUP BY PropertyId")).ToDictionary(x => x.PropertyId, x => x.Year);

        var waterHeaterYears = (await db.QueryAsync<(int PropertyId, int Year)>(
            @"SELECT PropertyId, MAX(YEAR(TransactionDate)) AS Year FROM Transactions 
              WHERE LOWER(Notes) NOT LIKE '%water heater door%' AND LOWER(Notes) NOT LIKE '%clean%' AND (
                (LOWER(Notes) LIKE '%new%water heater%' OR LOWER(Notes) LIKE '%replace%water heater%')
                OR ((LOWER(Notes) LIKE '%water heater%') AND (LOWER(Notes) LIKE '%gallon%'))
              ) AND Amount < -400 AND PropertyId IS NOT NULL
              GROUP BY PropertyId")).ToDictionary(x => x.PropertyId, x => x.Year);

        var tax1098Interest = (await db.QueryAsync<(int PropertyId, decimal InterestPaid)>(
            @"SELECT PropertyId, MortgageInterest FROM Tax1098 WHERE TaxYear = 2025")).ToDictionary(x => x.PropertyId, x => x.InterestPaid);

        var occupiedProps = (await db.QueryAsync<int>(
            @"SELECT DISTINCT PropertyId FROM Transactions 
              WHERE Category = 'Income' AND TransactionDate >= DATEADD(MONTH, -3, GETDATE()) AND PropertyId IS NOT NULL")).ToHashSet();

        var propertiesWithDogs = (await db.QueryAsync<int>(
            @"SELECT DISTINCT PropertyId FROM Transactions 
              WHERE (LOWER(SubCategory) LIKE '%pet%' OR LOWER(Notes) LIKE '%pet%' OR LOWER(Details) LIKE '%pet%')
                AND TransactionDate >= DATEADD(MONTH, -6, GETDATE()) AND PropertyId IS NOT NULL")).ToHashSet();

        var ownerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["am"] = "Blue Eagle Family Trust", ["sw"] = "Blue Eagle Family Trust", ["jt"] = "Blue Eagle Family Trust",
        };

        int row = 2;
        foreach (var prop in allProps)
        {
            int pid = (int)prop.PropertyId;
            ws.Cell(row, 1).Value = (string)prop.Street;
            ws.Cell(row, 2).Value = ownerMap.GetValueOrDefault((string)(prop.Owner ?? ""), "Blue Eagle Family Trust");
            ws.Cell(row, 3).Value = (string)(prop.InsuranceCarrier ?? "");
            
            if (prop.PurchaseDate != null)
                ws.Cell(row, 4).Value = ((DateTime)prop.PurchaseDate).ToString("MM/dd/yyyy");
            
            if (prop.InsuranceRenewal != null)
                ws.Cell(row, 5).Value = ((DateTime)prop.InsuranceRenewal).ToString("MM/dd/yyyy");
            
            ws.Cell(row, 6).Value = occupiedProps.Contains(pid) ? "Occupied" : "Vacant";
            ws.Cell(row, 7).Value = (int?)prop.Units ?? 1;
            
            ws.Cell(row, 8).Value = ""; ws.Cell(row, 9).Value = ""; ws.Cell(row, 10).Value = ""; ws.Cell(row, 11).Value = "";
            if (roofYears.TryGetValue(pid, out var ry)) ws.Cell(row, 8).Value = ry;
            if (hvacYears.TryGetValue(pid, out var hy)) ws.Cell(row, 9).Value = hy;
            if (plumbingYears.TryGetValue(pid, out var py)) ws.Cell(row, 10).Value = py;
            if (waterHeaterYears.TryGetValue(pid, out var why)) ws.Cell(row, 11).Value = why;
            
            ws.Cell(row, 13).Value = propertiesWithDogs.Contains(pid) ? "Yes" : "No";
            ws.Cell(row, 15).Value = (string)(prop.LenderName ?? "");
            ws.Cell(row, 16).Value = (string)(prop.MortgageNumber ?? "");
            
            if (tax1098Interest.TryGetValue(pid, out var interest))
            {
                ws.Cell(row, 17).Value = interest;
                // ws.Cell(row, 17).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
                ws.Cell(row, 18).Value = "1098";
            }
            row++;
        }
        ws.Columns().AdjustToContents();

        // ---- ADD SUMMARY SHEET ----
        var sumWs = wb.Worksheets.Add("SUMMARY");
        sumWs.Position = 1; // Put it first
        
        sumWs.Cell(1,1).Value = "YTD Category Summary";
        sumWs.Cell(1,1).Style.Font.Bold = true;

        int col = 2;
        foreach(var p in allProps) {
            sumWs.Cell(1, col).Value = (string)p.Street;
            sumWs.Cell(1, col).Style.Font.Bold = true;
            col++;
        }
        sumWs.Cell(1, col).Value = "TOTAL";
        sumWs.Cell(1, col).Style.Font.Bold = true;

        // Categories to tally
        var categories = new[] { 
            "Income",
            "Repairs & Maintenance",
            "Management Fees",
            "Taxes",
            "Insurance",
            "Admin & Other",
            "Utilities",
            "Legal & Professional",
            "Capital Expenses",
            "Mortgages & Loans"
        };

        var txs = (await db.QueryAsync<dynamic>(@"
            SELECT PropertyId, Category, SUM(Amount) as TotalAmount
            FROM Transactions
            WHERE PropertyId IS NOT NULL
            GROUP BY PropertyId, Category
        ")).ToList();

        int sumRow = 3;
        foreach (var cat in categories) {
            sumWs.Cell(sumRow, 1).Value = cat;
            sumWs.Cell(sumRow, 1).Style.Font.Bold = true;
            decimal totalAll = 0;
            int ccat = 2;
            foreach(var p in allProps) {
                int pid = (int)p.PropertyId;
                var propTx = txs.FirstOrDefault(t => t.PropertyId == pid && t.Category == cat);
                decimal amt = propTx != null ? (decimal)propTx.TotalAmount : 0;
                sumWs.Cell(sumRow, ccat).Value = amt;
                sumWs.Cell(sumRow, ccat).Style.NumberFormat.Format = "$#,##0.00";
                totalAll += amt;
                ccat++;
            }
            sumWs.Cell(sumRow, ccat).Value = totalAll;
            sumWs.Cell(sumRow, ccat).Style.NumberFormat.Format = "$#,##0.00";
            sumWs.Cell(sumRow, ccat).Style.Font.Bold = true;
            sumRow++;
        }

        sumRow++;
        sumWs.Cell(sumRow, 1).Value = "NET OPERATING INCOME";
        sumWs.Cell(sumRow, 1).Style.Font.Bold = true;
        int noicc = 2;
        decimal noiTotal = 0;
        foreach(var p in allProps) {
            int pid = (int)p.PropertyId;
            decimal pIncome = txs.Where(t => t.PropertyId == pid && t.Category == "Income").Sum(t => (decimal)t.TotalAmount);
            decimal pExpenses = txs.Where(t => t.PropertyId == pid && t.Category != "Income" && t.Category != "Mortgages & Loans" && t.Category != "Transfers").Sum(t => (decimal)t.TotalAmount);
            decimal pNoi = pIncome + pExpenses;
            sumWs.Cell(sumRow, noicc).Value = pNoi;
            sumWs.Cell(sumRow, noicc).Style.NumberFormat.Format = "$#,##0.00";
            sumWs.Cell(sumRow, noicc).Style.Font.Bold = true;
            noiTotal += pNoi;
            noicc++;
        }
        sumWs.Cell(sumRow, noicc).Value = noiTotal;
        sumWs.Cell(sumRow, noicc).Style.NumberFormat.Format = "$#,##0.00";
        sumWs.Cell(sumRow, noicc).Style.Font.Bold = true;

        sumRow += 2;
        sumWs.Cell(sumRow, 1).Value = "CASH FLOW";
        sumWs.Cell(sumRow, 1).Style.Font.Bold = true;
        int cfcc = 2;
        decimal cfTotal = 0;
        foreach(var p in allProps) {
            int pid = (int)p.PropertyId;
            decimal pIncome = txs.Where(t => t.PropertyId == pid && t.Category == "Income").Sum(t => (decimal)t.TotalAmount);
            decimal pAllExp = txs.Where(t => t.PropertyId == pid && t.Category != "Income" && t.Category != "Transfers").Sum(t => (decimal)t.TotalAmount);
            decimal pCf = pIncome + pAllExp;
            sumWs.Cell(sumRow, cfcc).Value = pCf;
            sumWs.Cell(sumRow, cfcc).Style.NumberFormat.Format = "$#,##0.00";
            sumWs.Cell(sumRow, cfcc).Style.Font.Bold = true;
            cfTotal += pCf;
            cfcc++;
        }
        sumWs.Cell(sumRow, cfcc).Value = cfTotal;
        sumWs.Cell(sumRow, cfcc).Style.NumberFormat.Format = "$#,##0.00";
        sumWs.Cell(sumRow, cfcc).Style.Font.Bold = true;

        sumWs.Columns().AdjustToContents();
        wb.SaveAs(outputPath);
        
        Console.WriteLine($"File properly generated with Questionnaire AND Summary at: {outputPath}");
    }
}
