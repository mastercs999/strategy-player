using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.TreeStructure
{
    public class Node<TValue>
    {
        public string Name { get; set; }
        public TValue Value { get; set; }

        public List<Node<TValue>> Children { get; set; }




        public Node(string name, TValue value)
        {
            Name = name;
            Value = value;
            Children = new List<Node<TValue>>();
        }
    }
}
