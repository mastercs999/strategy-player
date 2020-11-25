using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Extensions
{
    public static class CustomExtensions
    {
        public static string Text<T>(this T source)
        {
            FieldInfo fi = source.GetType().GetField(source.ToString());
            DescriptionAttribute desciption = fi.GetCustomAttribute<DescriptionAttribute>();

            return desciption != null ? desciption.Description : source.ToString();
        }
        public static T ToEnum<T>(this string text)
        {
            foreach (T value in EnumUtilities.GetArray<T>())
                if (value.ToString() == text || value.Text() == text)
                    return value;

            throw new FormatException($"Couldn't parse '{text}' into enum of type '{typeof(T).Name}'");
        }
        public static void Rethrow(this Exception ex)
        {
            ExceptionDispatchInfo.Capture(ex).Throw();
        }

        public static string AddVersion(this string path, int version)
        {
            string extension = Path.GetExtension(path);

            return path.Substring(0, path.Length - extension.Length) + "_" + version + extension;
        }
        public static string AddVersion(this string path, string version)
        {
            string extension = Path.GetExtension(path);

            return path.Substring(0, path.Length - extension.Length) + "_" + version + extension;
        }

        public static void ParallelLoop<T>(this IEnumerable<T> list, Action<int> action)
        {
            list.ParallelLoop((i, x) => action(i));
        }
        public static void ParallelLoop<T>(this IEnumerable<T> list, Action<int, T> action)
        {
            list.ParallelLoop(action, 8);
        }
        public static void ParallelLoop<T>(this IEnumerable<T> list, Action<int, T> action, int threadsCount)
        {
            List<T> items = list.ToList();
            List<Thread> threads = new List<Thread>();

            for (int p = 0; p < threadsCount; ++p)
            {
                int pid = p;
                Thread thread = new Thread((mainThread) =>
                {
                    try
                    {
                        for (int i = pid; i < items.Count; i += threadsCount)
                            action(i, items[i]);
                    }
                    catch (Exception ex)
                    {
                        ThreadMessage.ThrownException = ex;
                        (mainThread as Thread).Interrupt();
                    }
                })
                {
                    IsBackground = true
                };
                thread.Start(Thread.CurrentThread);

                threads.Add(thread);
            }

            foreach (Thread t in threads)
                t.Join();
        }
        public static void SerialLoop<T>(this IEnumerable<T> list, Action<int> action)
        {
            int index = 0;
            foreach (T item in list)
                action(index++);
        }
        public static void SerialLoop<T>(this IEnumerable<T> list, Action<int, T> action)
        {
            int index = 0;
            foreach (T item in list)
                action(index++, item);
        }

        public static IEnumerable<List<T>> Split<T>(this IEnumerable<T> source, int length)
        {
            List<T> items = new List<T>(length);

            foreach (T item in source)
            {
                items.Add(item);

                if (items.Count == length)
                {
                    yield return items;

                    items = new List<T>(length);
                }
            }

            if (items.Count > 0)
                yield return items;
        }
        public static IEnumerable<(int index, T value)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((x, i) => (i, x));
        }
        public static bool Contains(this string str, string text, StringComparison stringComparison)
        {
            return str.IndexOf(text, stringComparison) >= 0;
        }
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }
        public static ulong Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, ulong> selector)
        {
            ulong sum = 0;

            foreach (TSource item in source)
                sum += selector(item);

            return sum;
        }
        public static ConcurrentDictionary<TKey, TElement> ToConcurrentDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return new ConcurrentDictionary<TKey, TElement>(source.ToDictionary(keySelector, elementSelector));
        }
        public static IEnumerable<T> ExceptOne<T>(this IEnumerable<T> source, T item)
        {
            return source.Except(new T[] { item });
        }
        public static IEnumerable<T> ConcatItem<T>(this IEnumerable<T> source, T item)
        {
            return source.Concat(new T[] { item });
        }
        public static IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs(this NameValueCollection source)
        {
            return source.Cast<string>().Select(x => new KeyValuePair<string, string>(x, source[x]));
        }
 
        public static void AppendToJsonFile<T>(this IEnumerable<T> source, string filePath)
        {
            // Nothing to add
            if (!source.Any())
                return;

            // Convert to json
            string jsonString = JsonConvert.SerializeObject(source, Formatting.Indented);

            // Append to the file
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                // Seek 3 characters back: \r\n]
                if (fs.Length >= 3)
                {
                    fs.Seek(-3, SeekOrigin.End);
                    sw.Write("," + jsonString.Substring(1));
                }
                else
                    sw.Write(jsonString);
            }
        }

        public static T Clone<T>(this T obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);

                ms.Seek(0, SeekOrigin.Begin);
                formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(ms);
            }
        }

        public static string GetRealTypeName(this Type type)
        {
            // We handle only generic types
            if (!type.IsGenericType)
                return type.Name;

            // First part is sanatized name
            StringBuilder sb = new StringBuilder();
            sb.Append(type.Name.Contains('`') ? type.Name.Substring(0, type.Name.IndexOf('`')) : type.Name);

            // Now append type arguments
            sb.Append('<');
            sb.Append(String.Join(",", type.GetGenericArguments().Select(x => x.GetRealTypeName())));
            sb.Append('>');

            return sb.ToString();
        }
    }
}
