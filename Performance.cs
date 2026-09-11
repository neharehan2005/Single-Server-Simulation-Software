using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Member1_DataGeneration
{
    /// <summary>
    /// Member 3's deliverable: takes the List&lt;SimulationResult&gt; produced by
    /// Member 2's QueueServerSimulation and computes the summary performance
    /// measures for the single-server queue.
    /// </summary>
    public class PerformanceMetrics
    {
        public int TotalCustomers { get; set; }
        public double TotalSimulationTime { get; set; }

        public double AverageWaitingTime { get; set; }
        public double MaxWaitingTime { get; set; }

        public double AverageTimeInSystem { get; set; }
        public double MaxTimeInSystem { get; set; }

        public double AverageQueueLength { get; set; }
        public int MaxQueueLength { get; set; }

        public double TotalBusyTime { get; set; }
        public double TotalIdleTime { get; set; }
        public double ServerUtilization { get; set; }     // rho, 0..1

        public int NumberWhoWaited { get; set; }
        public double ProbabilityOfWaiting { get; set; }   // fraction of customers who had to wait

        // ----- Little's Law (L = λW, Lq = λWq) -----
        // These are the theoretically correct time-averaged occupancy figures,
        // distinct from AverageQueueLength above (which is only an average of
        // the per-customer "how many were waiting behind me" snapshot, not a
        // true time-weighted average).
        public double ArrivalRate { get; set; }             // λ = TotalCustomers / TotalSimulationTime
        public double AverageNumberInSystem { get; set; }   // L  = λ * AverageTimeInSystem
        public double AverageNumberInQueue { get; set; }    // Lq = λ * AverageWaitingTime
    }

    public class PerformanceAnalyzer
    {
        /// <summary>
        /// Computes all summary statistics from Member 2's per-customer results.
        /// </summary>
        public PerformanceMetrics Analyze(List<SimulationResult> results)
        {
            if (results == null || results.Count == 0)
                throw new ArgumentException("Results list cannot be empty.");

            // Always work through customers in the order they were served/arrived.
            var ordered = results.OrderBy(r => r.CustomerId).ToList();

            int n = ordered.Count;

            // ----- Waiting time -----
            double avgWait = ordered.Average(r => r.WaitingTime);
            double maxWait = ordered.Max(r => r.WaitingTime);

            // ----- Time in system (wait + service) -----
            double avgSystem = ordered.Average(r => r.TimeInSystem);
            double maxSystem = ordered.Max(r => r.TimeInSystem);

            // ----- Queue length -----
            // Uses the QueueLength Member 2 recorded for each customer
            // (how many people were still waiting behind them at that moment).
            double avgQueueLength = ordered.Average(r => (double)r.QueueLength);
            int maxQueueLength = ordered.Max(r => r.QueueLength);

            // ----- Busy / idle time & utilization -----
            // The server can only work on one customer at a time, so total time
            // it was actually busy = sum of every customer's service time.
            double totalBusyTime = ordered.Sum(r => r.ServiceTime);

            // Simulation clock runs from 0 to the last customer's service end.
            double totalSimTime = ordered.Max(r => r.ServiceEndTime);

            double totalIdleTime = totalSimTime - totalBusyTime;
            double utilization = totalSimTime > 0 ? totalBusyTime / totalSimTime : 0.0;

            // ----- How many customers actually had to wait -----
            int numberWhoWaited = ordered.Count(r => r.WaitingTime > 0.0001);
            double probWaiting = (double)numberWhoWaited / n;

            // ----- Little's Law: L = λW, Lq = λWq -----
            // λ (arrival rate) here is measured over the simulation horizon actually
            // observed (n customers over totalSimTime), which makes L and Lq exact
            // time-averaged occupancy figures for this run (verified against direct
            // time-integration of the number-in-system/number-in-queue curves).
            double arrivalRate = totalSimTime > 0 ? n / totalSimTime : 0.0;
            double averageNumberInSystem = arrivalRate * avgSystem;
            double averageNumberInQueue = arrivalRate * avgWait;

            return new PerformanceMetrics
            {
                TotalCustomers = n,
                TotalSimulationTime = Math.Round(totalSimTime, 4),

                AverageWaitingTime = Math.Round(avgWait, 4),
                MaxWaitingTime = Math.Round(maxWait, 4),

                AverageTimeInSystem = Math.Round(avgSystem, 4),
                MaxTimeInSystem = Math.Round(maxSystem, 4),

                AverageQueueLength = Math.Round(avgQueueLength, 4),
                MaxQueueLength = maxQueueLength,

                TotalBusyTime = Math.Round(totalBusyTime, 4),
                TotalIdleTime = Math.Round(totalIdleTime, 4),
                ServerUtilization = Math.Round(utilization, 4),

                NumberWhoWaited = numberWhoWaited,
                ProbabilityOfWaiting = Math.Round(probWaiting, 4),

                ArrivalRate = Math.Round(arrivalRate, 4),
                AverageNumberInSystem = Math.Round(averageNumberInSystem, 4),
                AverageNumberInQueue = Math.Round(averageNumberInQueue, 4)
            };
        }

        /// <summary>Prints a clean report to the console.</summary>
        public void PrintReport(PerformanceMetrics m)
        {
            Console.WriteLine();
            Console.WriteLine("==========================================================================");
            Console.WriteLine("                    PERFORMANCE ANALYSIS  (Member 3)");
            Console.WriteLine("==========================================================================");
            Console.WriteLine($"Total customers served        : {m.TotalCustomers}");
            Console.WriteLine($"Total simulation time (min)   : {m.TotalSimulationTime:F2}");
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine($"Average waiting time (min)    : {m.AverageWaitingTime:F2}");
            Console.WriteLine($"Maximum waiting time (min)    : {m.MaxWaitingTime:F2}");
            Console.WriteLine($"Average time in system (min)  : {m.AverageTimeInSystem:F2}");
            Console.WriteLine($"Maximum time in system (min)  : {m.MaxTimeInSystem:F2}");
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine($"Average queue length          : {m.AverageQueueLength:F2}");
            Console.WriteLine($"Maximum queue length          : {m.MaxQueueLength}");
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine($"Total server busy time (min)  : {m.TotalBusyTime:F2}");
            Console.WriteLine($"Total server idle time (min)  : {m.TotalIdleTime:F2}");
            Console.WriteLine($"Server utilization            : {m.ServerUtilization:P2}");
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine($"Customers who had to wait     : {m.NumberWhoWaited} / {m.TotalCustomers}");
            Console.WriteLine($"Probability of waiting        : {m.ProbabilityOfWaiting:P2}");
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine($"Arrival rate (λ, per min)      : {m.ArrivalRate:F4}");
            Console.WriteLine($"Avg number in system (L)      : {m.AverageNumberInSystem:F2}");
            Console.WriteLine($"Avg number in queue (Lq)      : {m.AverageNumberInQueue:F2}");
            Console.WriteLine("==========================================================================");
        }

        /// <summary>
        /// Exports the summary metrics to CSV so Member 5 (graphs/analysis) and
        /// Member 4 (UI) can consume them without needing this class directly.
        /// </summary>
        public static void ExportToCsv(PerformanceMetrics m, string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Metric,Value");
            sb.AppendLine($"TotalCustomers,{m.TotalCustomers}");
            sb.AppendLine($"TotalSimulationTime,{m.TotalSimulationTime:F4}");
            sb.AppendLine($"AverageWaitingTime,{m.AverageWaitingTime:F4}");
            sb.AppendLine($"MaxWaitingTime,{m.MaxWaitingTime:F4}");
            sb.AppendLine($"AverageTimeInSystem,{m.AverageTimeInSystem:F4}");
            sb.AppendLine($"MaxTimeInSystem,{m.MaxTimeInSystem:F4}");
            sb.AppendLine($"AverageQueueLength,{m.AverageQueueLength:F4}");
            sb.AppendLine($"MaxQueueLength,{m.MaxQueueLength}");
            sb.AppendLine($"TotalBusyTime,{m.TotalBusyTime:F4}");
            sb.AppendLine($"TotalIdleTime,{m.TotalIdleTime:F4}");
            sb.AppendLine($"ServerUtilization,{m.ServerUtilization:F4}");
            sb.AppendLine($"NumberWhoWaited,{m.NumberWhoWaited}");
            sb.AppendLine($"ProbabilityOfWaiting,{m.ProbabilityOfWaiting:F4}");
            sb.AppendLine($"ArrivalRate,{m.ArrivalRate:F4}");
            sb.AppendLine($"AverageNumberInSystem_L,{m.AverageNumberInSystem:F4}");
            sb.AppendLine($"AverageNumberInQueue_Lq,{m.AverageNumberInQueue:F4}");

            File.WriteAllText(path, sb.ToString());
        }
    }
}
