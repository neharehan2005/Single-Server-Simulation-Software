using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Member1_DataGeneration
{
    /// <summary>
    /// Reads the real observed Sapphire data (SapphireData.csv, exported from the
    /// original spreadsheet) and fits simple distribution parameters from it:
    ///   - mean inter-arrival time (used for an exponential arrival process)
    ///   - mean and standard deviation of service time
    /// These fitted parameters are what DataGenerator uses to produce new,
    /// randomly generated (but realistic) customers for the simulation.
    /// </summary>
    public class HistoricalDataAnalyzer
    {
        public double MeanInterArrivalMinutes { get; private set; }
        public double MeanServiceMinutes { get; private set; }
        public double StdDevServiceMinutes { get; private set; }
        public int ArrivalsObserved { get; private set; }
        public int ServiceSamplesObserved { get; private set; }

        /// <summary>
        /// Loads SapphireData.csv and fits parameters.
        /// Columns expected: Customer,ArrivalTime,ServiceIn,ServiceOut,Departure,Payment,Server
        /// Times are HH:mm. Blank ServiceIn/ServiceOut = customer left without being served
        /// (balked/reneged) and is excluded from the service-time sample.
        /// The Server column is ignored on purpose — this project models a single server.
        /// </summary>
        public void LoadAndFit(string csvPath)
        {
            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"Could not find historical data file at '{csvPath}'.");

            var arrivalMinutes = new List<double>();
            var serviceDurations = new List<double>();

            var lines = File.ReadAllLines(csvPath);
            for (int i = 1; i < lines.Length; i++) // skip header row
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;

                var cols = line.Split(',');
                if (cols.Length < 4) continue;

                double? arrival = ParseClockTime(cols[1]);
                double? serviceIn = ParseClockTime(cols[2]);
                double? serviceOut = ParseClockTime(cols[3]);

                if (arrival.HasValue)
                    arrivalMinutes.Add(arrival.Value);

                if (serviceIn.HasValue && serviceOut.HasValue)
                {
                    double duration = serviceOut.Value - serviceIn.Value;
                    if (duration < 0) duration += 24 * 60; // guard against midnight rollover
                    if (duration > 0 && duration < 60)      // discard obvious data-entry errors
                        serviceDurations.Add(duration);
                }
            }

            if (arrivalMinutes.Count < 2)
                throw new InvalidOperationException("Not enough arrival records in historical data to fit inter-arrival times.");
            if (serviceDurations.Count < 2)
                throw new InvalidOperationException("Not enough complete service records in historical data to fit service times.");

            arrivalMinutes.Sort();
            var interArrivals = new List<double>();
            for (int i = 1; i < arrivalMinutes.Count; i++)
            {
                double gap = arrivalMinutes[i] - arrivalMinutes[i - 1];
                if (gap >= 0) interArrivals.Add(gap);
            }

            ArrivalsObserved = arrivalMinutes.Count;
            ServiceSamplesObserved = serviceDurations.Count;
            MeanInterArrivalMinutes = interArrivals.Average();
            MeanServiceMinutes = serviceDurations.Average();
            StdDevServiceMinutes = StdDev(serviceDurations, MeanServiceMinutes);
        }

        private static double StdDev(List<double> values, double mean)
        {
            double sumSq = values.Sum(v => (v - mean) * (v - mean));
            return Math.Sqrt(sumSq / values.Count);
        }

        /// <summary>Parses "HH:mm" into minutes-since-midnight. Returns null for blank cells.</summary>
        private static double? ParseClockTime(string cell)
        {
            if (string.IsNullOrWhiteSpace(cell)) return null;
            var parts = cell.Split(':');
            if (parts.Length != 2) return null;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int h)) return null;
            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int m)) return null;
            return h * 60 + m;
        }

        public void PrintSummary()
        {
            Console.WriteLine("=== Fitted parameters from SapphireData.csv ===");
            Console.WriteLine($"Arrivals observed:        {ArrivalsObserved}");
            Console.WriteLine($"Mean inter-arrival time:  {MeanInterArrivalMinutes:F3} min  (=> arrival rate lambda = {1.0 / MeanInterArrivalMinutes:F3} customers/min)");
            Console.WriteLine($"Service records observed: {ServiceSamplesObserved}");
            Console.WriteLine($"Mean service time:        {MeanServiceMinutes:F3} min  (=> service rate mu = {1.0 / MeanServiceMinutes:F3} customers/min)");
            Console.WriteLine($"Std dev of service time:  {StdDevServiceMinutes:F3} min");
            Console.WriteLine();
        }
    }
}
