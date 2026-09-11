using System;
 
namespace Member1_DataGeneration
{
    /// <summary>
    /// Wraps System.Random and provides the specific random-variate samplers
    /// this module needs. Seedable so simulation runs are reproducible for testing
    /// (Member 5 will want that for comparing scenarios).
    /// </summary>
    public class RandomGenerator
    {
        private readonly Random _random;
 
        public RandomGenerator(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }
 
        /// <summary>Uniform(0,1), open interval so log(u) is always defined.</summary>
        public double NextUniform()
        {
            double u;
            do { u = _random.NextDouble(); } while (u <= 0.0);
            return u;
        }
 
        /// <summary>
        /// Exponential variate with the given mean, via inverse-CDF sampling.
        /// Used for inter-arrival times (Poisson arrival process) and, optionally,
        /// service times when the historical data looks memoryless.
        /// </summary>
        public double NextExponential(double mean)
        {
            if (mean <= 0) throw new ArgumentOutOfRangeException(nameof(mean), "Mean must be positive.");
            return -mean * Math.Log(NextUniform());
        }
 
        /// <summary>
        /// Normal (Gaussian) variate via Box-Muller, clamped to be non-negative
        /// since service times can't be negative. Useful when the historical
        /// service-time std dev doesn't match an exponential's (std dev == mean).
        /// </summary>
        public double NextNormal(double mean, double stdDev)
        {
            double u1 = NextUniform();
            double u2 = NextUniform();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            double sample = mean + stdDev * z;
            return Math.Max(0.01, sample); // guard against non-positive service times
        }
    }
}
 