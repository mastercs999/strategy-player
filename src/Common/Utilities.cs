using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public static class Utilities
    {
        public static string CsvSeparator => Thread.CurrentThread.CurrentCulture.TextInfo.ListSeparator;

        public static object ParseValue(Type targetType, string rawValue)
        {
            if (targetType == typeof(string))
                return rawValue;
            else if (targetType == typeof(int))
                return int.Parse(rawValue, CultureInfo.InvariantCulture);
            else if (targetType == typeof(double))
                return double.Parse(rawValue, CultureInfo.InvariantCulture);
            else if (targetType == typeof(decimal))
                return decimal.Parse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture);
            else if (targetType == typeof(bool))
                return bool.Parse(rawValue);
            else
                throw new ArgumentException($"Can't parse unknown target type {targetType.Name} from raw value '{rawValue}'");
        }

        public static T LoadOrDo<T>(string path, Func<T> getFunction)
        {
            if (!File.Exists(path))
            {
                T instance = getFunction();
                instance.Serialize(path);

                return instance;
            }
            else
                return Serializer.Deserialize<T>(path);
        }
        public static T[] LoadOrDo<T>(string path, int arrayLength, Func<int, T> getFunction)
        {
            if (!File.Exists(path))
            {
                T[] instances = new T[arrayLength];

                for (int i = 0; i < instances.Length; ++i)
                    instances[i] = getFunction(i);

                instances.Serialize(path);

                return instances;
            }
            else
                return Serializer.Deserialize<T[]>(path);
        }

        public static double Distance(double[] x, double[] y)
        {
            double sum = 0;

            for (int i = 0; i < x.Length; ++i)
                sum += Math.Pow(x[i] - y[i], 2);

            return Math.Sqrt(sum);
        }
        public static double Distance(int[] x, int[] y)
        {
            double sum = 0;

            for (int i = 0; i < x.Length; ++i)
                sum += Math.Pow(x[i] - y[i], 2);

            return Math.Sqrt(sum);
        }
        public static double Correlation(double[] xs, double[] ys)
        {
            double sumX = 0;
            double sumX2 = 0;
            double sumY = 0;
            double sumY2 = 0;
            double sumXY = 0;

            int n = xs.Length;

            for (int i = 0; i < n; ++i)
            {
                double x = xs[i];
                double y = ys[i];

                sumX += x;
                sumX2 += x * x;
                sumY += y;
                sumY2 += y * y;
                sumXY += x * y;
            }

            double stdX = Math.Sqrt(sumX2 / n - sumX * sumX / n / n);
            double stdY = Math.Sqrt(sumY2 / n - sumY * sumY / n / n);
            double covariance = (sumXY / n - sumX * sumY / n / n);

            double result = covariance / stdX / stdY;

            return double.IsNaN(result) ? -1 : result;
        }
        public static decimal Gauss(decimal x, decimal a, decimal b, decimal c)
        {
            return (decimal)((double)a * Math.Pow(Math.E, (-Math.Pow((double)x - (double)b, 2) / (2 * Math.Pow((double)c, 2)))));
        }

        public static string DownloadString(string url)
        {
            using (WebClient wc = new WebClient())
                return wc.DownloadString(new Uri(url));
        }
        public static void DownloadFile(string url, string file)
        {
            using (WebClient wc = new WebClient())
                wc.DownloadFile(new Uri(url), file);
        }
        public static void DownloadFileAkaBrowser(string url, string file)
        {
            using (WebClient wc = Utilities.PrepareBrowserWebClient())
                wc.DownloadFile(new Uri(url), file);
        }
        public static WebClient PrepareBrowserWebClient()
        {
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:52.0) Gecko/20100101 Firefox/52.0");
                wc.Headers.Add("Accept", "text/html");

                return wc;
            }
        }
        
        public static T Max<T>(params T[] values)
        {
            return values.Max();
        }
        public static T Min<T>(params T[] values)
        {
            return values.Min();
        }
        public static decimal Return(decimal startValue, decimal endValue)
        {
            return (endValue - startValue) / startValue;
        }
        public static decimal LogReturn(decimal startValue, decimal endValue)
        {
            return (decimal)Math.Log(1 + (double)Return(startValue, endValue));
        }

        public static void CreateDirectories(object config)
        {
            foreach (PropertyInfo pi in config.GetType().GetProperties())
                if (pi.PropertyType == typeof(string))
                {
                    string val = (string)pi.GetValue(config);
                    if (val.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
                        Directory.CreateDirectory(Path.GetDirectoryName(val));
                }
        }
    }
}
