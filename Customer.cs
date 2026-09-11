using System;

namespace Member1_DataGeneration
{
    /// <summary>
    /// Represents a single simulated customer's raw input data.
    /// This is the object Member 1's module hands off to Member 2 (queue/server simulation).
    /// </summary>
    public class Customer
    {
        public int Id { get; set; }

        /// <summary>Time between this customer's arrival and the previous one, in minutes.</summary>
        public double InterArrivalTime { get; set; }

        /// <summary>Absolute arrival time in minutes, measured from simulation start (t = 0).</summary>
        public double ArrivalTime { get; set; }

        /// <summary>
        /// Same ArrivalTime, but rendered as a wall-clock "HH:mm" string (e.g. simulation
        /// start + ArrivalTime minutes). Purely for readability/reporting — the queue logic
        /// downstream should use the numeric ArrivalTime, not this string.
        /// </summary>
        public string ArrivalClockTime { get; set; } = "";

        /// <summary>How long this customer will need at the server, in minutes.</summary>
        public double ServiceTime { get; set; }

        public override string ToString()
        {
            return $"Customer {Id,-4} | InterArrival: {InterArrivalTime,6:F2} min | " +
                   $"Arrival: {ArrivalTime,7:F2} min ({ArrivalClockTime}) | Service: {ServiceTime,6:F2} min";
        }

        /// <summary>CSV row for handing this record to Member 2's / Member 4's code.</summary>
        public string ToCsvRow()
        {
            return $"{Id},{InterArrivalTime:F4},{ArrivalTime:F4},{ArrivalClockTime},{ServiceTime:F4}";
        }
    }
}