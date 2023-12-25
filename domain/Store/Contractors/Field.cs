using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Contractors
{
    public abstract class Field
    {
        public string Label { get; }
        public string Name { get; }
        public string Value {  get; }
        public Field(string label, string name, string value)
        {
            Label = label;
            Name = name;
            Value = value;
        }
    }
    
    public class SelectionField : Field
    {
        public IReadOnlyDictionary<string, string> Items { get; }

        public SelectionField(string label, string name, string value, IReadOnlyDictionary<string, string> items) 
            : base(label, name, value)
        {
            Items = items;

        }
    }
}
