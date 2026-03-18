using GoldSavings.App.Model;
using GoldSavings.App.Client;
using GoldSavings.App.Services;

namespace GoldSavings.App;

class Program
{

    static void SavePricesToXml(List<GoldPrice> prices, string path)
    {
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<GoldPrice>));
        using (var writer = new StreamWriter(path))
        {
            serializer.Serialize(writer, prices);
        }
    }

    static List<GoldPrice> LoadPricesFromXml(string path)
    {
        return (List<GoldPrice>)new System.Xml.Serialization.XmlSerializer(typeof(List<GoldPrice>)).Deserialize(new StreamReader(path));
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Hello, Gold Investor!");

        // Step 1: Get gold prices (92‑day chunks)
        GoldDataService dataService = new GoldDataService();
        DateTime startDate = new DateTime(2019, 01, 01);
        DateTime endDate = DateTime.Now;

        const int maxDays = 92;
        List<GoldPrice> goldPrices = new List<GoldPrice>();

        DateTime chunkStart = startDate;

        while (chunkStart <= endDate)
        {
            DateTime chunkEnd = chunkStart.AddDays(maxDays);
            if (chunkEnd > endDate)
                chunkEnd = endDate;

            Console.WriteLine($"Fetching: {chunkStart:yyyy-MM-dd} → {chunkEnd:yyyy-MM-dd}");

            var chunkData = dataService
                .GetGoldPrices(chunkStart, chunkEnd)
                .GetAwaiter()
                .GetResult();

            goldPrices.AddRange(chunkData);

            chunkStart = chunkEnd.AddDays(1);
        }

        if (goldPrices.Count == 0)
        {
            Console.WriteLine("No data found. Exiting.");
            return;
        }

        Console.WriteLine($"Retrieved {goldPrices.Count} records. Ready for analysis.");

        // Step 2: Perform analysis
        GoldAnalysisService analysisService = new GoldAnalysisService(goldPrices);
        var avgPrice = analysisService.GetAveragePrice();

        
        // TASK a
        DateTime oneYearAgo = DateTime.Now.AddYears(-1);
        var lastYear = goldPrices
            .Where(p => p.Date >= oneYearAgo)
            .ToList();

        var top3 = lastYear
            .OrderByDescending(p => p.Price)
            .Take(3)
            .ToList();

        var bottom3 = lastYear
            .OrderBy(p => p.Price)
            .Take(3)
            .ToList();

        var top3Q =
            (from g in lastYear
            orderby g.Price descending
            select g).Take(3).ToList();

        var bottom3Q =
            (from g in lastYear
            orderby g.Price ascending
            select g).Take(3).ToList();

        GoldResultPrinter.PrintPrices(top3, "TOP 3 (Method)");
        GoldResultPrinter.PrintPrices(bottom3, "BOTTOM 3 (Method)");

        GoldResultPrinter.PrintPrices(top3Q, "TOP 3(Query)");
        GoldResultPrinter.PrintPrices(bottom3Q, "BOTTOM 3 (Query)");
        // END a

        // START b

        var jan2020Price = goldPrices
            .Where(p => p.Date.Year == 2020 && p.Date.Month == 1)
            .OrderBy(p => p.Date)
            .FirstOrDefault();

        double buyPrice = jan2020Price.Price;

        var gainDays = goldPrices
            .Where(p => p.Date > jan2020Price.Date &&
                        (p.Price - buyPrice) / buyPrice > 0.05)
            .OrderBy(p => p.Date)
            .ToList();

        if (gainDays.Count == 0)
        {
            Console.WriteLine("\nNo days with more than 5% profit since January 2020.");
        }
        else
        {
            var first10 = gainDays.Take(10).ToList();
            GoldResultPrinter.PrintPrices(
                first10,
                "Days with >5% profit after buying in January 2020"
            );
        }
        
        // END b
        
        // START c
                
        var prices2019to2022 = goldPrices
            .Where(p => p.Date.Year >= 2019 && p.Date.Year <= 2022)
            .OrderByDescending(p => p.Price)
            .ToList();

        var secondTenOpeners = prices2019to2022
            .Skip(10)
            .Take(3)
            .ToList();

        GoldResultPrinter.PrintPrices(
            secondTenOpeners,
            "11,12,13 places"
        );

        // END c

        // START d
        
        var averagesByYear =
            from g in goldPrices
            where g.Date.Year == 2020 
            || g.Date.Year == 2023 
            || g.Date.Year == 2024
            group g by g.Date.Year into year
            orderby year.Key
            select new
            {
                Year = year.Key,
                Average = year.Average(x => x.Price)
            };

        // Print results
        Console.WriteLine("\nAvg. Gold Prices ");
        foreach (var result in averagesByYear)
        {
            Console.WriteLine($"Year {result.Year}: {Math.Round(result.Average, 2)} PLN");
        }

        // END d

        // START e

        var data = goldPrices
            .Where(p => p.Date.Year >= 2020 && p.Date.Year <= 2024)
            .OrderBy(p => p.Date)
            .ToList();

        double minPrice = data[0].Price;
        DateTime minDate = data[0].Date;

        double bestProfit = 0;
        DateTime buy = minDate;
        DateTime sell = minDate;

        foreach (var p in data)
        {
            if (p.Price < minPrice)
            {
                minPrice = p.Price;
                minDate = p.Date;
            }

            double profit = p.Price - minPrice;
            if (profit > bestProfit)
            {
                bestProfit = profit;
                buy = minDate;
                sell = p.Date;
            }
        }

        double roi = bestProfit / minPrice;

        Console.WriteLine("\nBest Buy/Sell 2020–2024");
        Console.WriteLine($"BUY  on {buy:yyyy-MM-dd} at {minPrice} PLN");
        Console.WriteLine($"SELL on {sell:yyyy-MM-dd} at {data.First(p => p.Date == sell).Price} PLN");
        Console.WriteLine($"ROI: {Math.Round(roi * 100, 2)}%");
        



        // Step 3: Print results
        GoldResultPrinter.PrintSingleValue(Math.Round(avgPrice, 2), "Average Gold Price Last Half Year");

        // TASK 3
        SavePricesToXml(goldPrices, "goldPrices.xml");

        // TASK 4
        var loaded = LoadPricesFromXml("goldPrices.xml");
        Console.WriteLine($"Loaded {loaded.Count} prices from XML.");


        Console.WriteLine("\nGold Analyis Queries with LINQ Completed.");
    }
}