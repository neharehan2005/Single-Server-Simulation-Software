using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Member1_DataGeneration
{
    public enum ServiceTimeDistribution { Exponential, Normal }

    /// <summary>
    /// Member 1's deliverable: produces the list of simulated Customer records
    /// that feed the rest of the pipeline (Member 2's queue/server logic).
    ///
    /// Distribution choice:
    ///   - Inter-arrival times are always drawn from an Exponential distribution
    ///     (standard assumption for a Poisson arrival process), using the mean
    ///     inter-arrival time fitted from SapphireData.csv.
    ///   - Service times default to Exponential too, but auto-switch to Normal
    ///     if the historical std dev is far from the historical mean (a real
    ///     exponential distribution has std dev == mean, so a big mismatch means
    ///     Exponential is a poor fit and Normal is used instead).
    /// </summary>
    public class DataGenerator
    {
        private readonly HistoricalDataAnalyzer _fittedParams;
        private readonly RandomGenerator _rng;
        public ServiceTimeDistribution ServiceDistribution { get; }

        public DataGenerator(HistoricalDataAnalyzer fittedParams, int? seed = null)
        {
            _fittedParams = fittedParams ?? throw new ArgumentNullException(nameof(fittedParams));
            _rng = new RandomGenerator(seed);

            // Heuristic: exponential requires std dev ≈ mean. If the real data's
            // std dev deviates by more than 30%, fall back to Normal.
            double ratio = fittedParams.StdDevServiceMinutes / fittedParams.MeanServiceMinutes;
            ServiceDistribution = (ratio >= 0.7 && ratio <= 1.3)
                ? ServiceTimeDistribution.Exponential
                : ServiceTimeDistribution.Normal;
        }

        /// <summary>
        /// Generates <paramref name="customerCount"/> simulated customers.
        /// </summary>
        /// <param name="customerCount">How many customers to generate.</param>
        /// <param name="simulationStartTime">
        /// Wall-clock time that t = 0 represents, used only to render the readable
        /// ArrivalClockTime column (e.g. 15:00, matching when the real Sapphire
        /// observation window began). Doesn't affect any of the underlying numbers.
        /// </param>
        public List<Customer> Generate(int customerCount, TimeSpan? simulationStartTime = null)
        {
            if (customerCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(customerCount), "Must generate at least one customer.");

            TimeSpan startTime = simulationStartTime ?? new TimeSpan(15, 0, 0); // default 15:00

            var customers = new List<Customer>(customerCount);
            double clock = 0.0;

            for (int i = 1; i <= customerCount; i++)
            {
                double interArrival = _rng.NextExponential(_fittedParams.MeanInterArrivalMinutes);
                clock += interArrival;

                double serviceTime = ServiceDistribution == ServiceTimeDistribution.Exponential
                    ? _rng.NextExponential(_fittedParams.MeanServiceMinutes)
                    : _rng.NextNormal(_fittedParams.MeanServiceMinutes, _fittedParams.StdDevServiceMinutes);

                TimeSpan clockTime = startTime + TimeSpan.FromMinutes(clock);
                // Wrap past midnight if a long run ever pushes past 24:00
                clockTime = TimeSpan.FromMinutes(clockTime.TotalMinutes % (24 * 60));

                customers.Add(new Customer
                {
                    Id = i,
                    InterArrivalTime = Math.Round(interArrival, 4),
                    ArrivalTime = Math.Round(clock, 4),
                    ArrivalClockTime = clockTime.ToString(@"hh\:mm"),
                    ServiceTime = Math.Round(serviceTime, 4)
                });
            }

            return customers;
        }

        /// <summary>Writes generated customers to CSV for Member 2/3/4 to consume.</summary>
        public static void ExportToCsv(List<Customer> customers, string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("CustomerId,InterArrivalTime,ArrivalTime,ArrivalClockTime,ServiceTime");
            foreach (var c in customers)
                sb.AppendLine(c.ToCsvRow());

            File.WriteAllText(path, sb.ToString());
        }
    }
}