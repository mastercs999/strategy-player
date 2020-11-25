using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.TreeStructure
{
    public static class TreeBuilder
    {
        public static List<Node<TValue>> Create<TSource, TValue>(List<TSource> source, params Func<TSource, (string text, TValue value)>[] levels)
        {
            List<Node<TValue>> firstLevel = new List<Node<TValue>>();

            foreach (TSource item in source)
            {
                List<Node<TValue>> currentLevel = firstLevel;
                foreach (Func<TSource, (string text, TValue value)> func in levels)
                {
                    Node<TValue> node = NextLevel(currentLevel, func(item).text, func(item).value);
                    currentLevel = node.Children;
                }
            }

            return firstLevel;
        }

        private static Node<T> NextLevel<T>(List<Node<T>> nodes, string text, T value)
        {
            Node<T> result = nodes.SingleOrDefault(x => x.Value.Equals(value));
            if (result == null)
            {
                result = new Node<T>(text, value);
                nodes.Add(result);
            }

            return result;
        }
    }
}
