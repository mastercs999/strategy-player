using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Extensions
{
    public static class StatsExtensions
    {
        public static double Normalize(this double value, double newMean, double newStd, double oldMean, double oldStd)
        {
            return (value - oldMean) * (newStd / oldStd) + newMean;
        }
        public static IEnumerable<double> Normalize(this IEnumerable<double> source)
        {
            // Get values
            double[] values = source.ToArray();

            // Find stats   
            double mean = values.Average();
            double stdDev = values.Sum(d => Math.Pow(d - mean, 2));
            stdDev = Math.Sqrt(stdDev / (values.Count() - 1));

            // Normalize
            foreach (double value in values)
                yield return value.Normalize(0, 1, mean, stdDev);
        }

        public static IEnumerable<decimal> CumulativeSum(this IEnumerable<decimal> list)
        {
            foreach (decimal d in list.CumulativeSum(0))
                yield return d;
        }
        public static IEnumerable<decimal> CumulativeSum(this IEnumerable<decimal> list, decimal initial)
        {
            yield return initial;

            decimal sum = initial;
            foreach (decimal d in list)
                yield return sum += d;
        }
        public static decimal ProfitFactor(this IEnumerable<decimal> source)
        {
            // To list
            List<decimal> list = source.ToList();

            // Nothing
            if (!list.Any())
                return 1;

            List<decimal> above = list.Where(x => x > 0).ToList();
            List<decimal> below = list.Where(x => x <= 0).ToList();

            if (above.Count == 0)
                return 0;
            if (below.Count == 0)
                return 100 * list.Count;

            return above.Sum() / -below.Sum();
        }
        public static List<decimal> CumulativeAverage(this IEnumerable<decimal> list)
        {
            List<decimal> result = new List<decimal>(list.Count());

            decimal sum = 0;
            int count = 0;

            foreach (decimal d in list)
            {
                ++count;
                sum += d;

                result.Add(sum / count);
            }

            return result;
        }
        public static decimal Median(this IEnumerable<decimal> source)
        {
            List<decimal> sorted = source.OrderBy(x => x).ToList();

            if (sorted.Count % 2 == 1)
                return sorted[sorted.Count / 2];
            else
                return (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2;
        }
        public static decimal GrossProfit(this IEnumerable<decimal> source)
        {
            return source.Where(x => x > 0).Sum();
        }
        public static decimal GrossLoss(this IEnumerable<decimal> source)
        {
            return source.Where(x => x < 0).Sum();
        }
        public static decimal DrawdownPercentage(this IEnumerable<decimal> source, decimal capital)
        {
            decimal peak = capital;
            decimal maxDistancePercentage = 0;
            foreach (decimal d in source.CumulativeSum(capital))
            {
                if (d > peak)
                    peak = d;
                else
                {
                    decimal distancePercentage = (peak - d) / peak;
                    if (distancePercentage > maxDistancePercentage)
                        maxDistancePercentage = distancePercentage;
                }
            }

            return maxDistancePercentage * 100;
        }
        public static decimal DrawdownAbsolute(this IEnumerable<decimal> source, decimal capital)
        {
            decimal peak = capital;
            decimal maxDistance = 0;
            foreach (decimal d in source.CumulativeSum(capital))
            {
                if (d > peak)
                    peak = d;
                else
                {
                    decimal distance = (peak - d);
                    if (distance > maxDistance)
                        maxDistance = distance;
                }
            }

            return maxDistance;
        }
    }
}
