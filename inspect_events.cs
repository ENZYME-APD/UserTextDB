using System;
using System.Linq;
using System.Reflection;
using Eto.Forms;

class Program
{
    static void Main()
    {
        var type = typeof(TreeGridView);
        var events = type.GetEvents(BindingFlags.Public | BindingFlags.Instance);
        foreach (var e in events)
        {
            if (e.Name.Contains("Cell") || e.Name.Contains("Edit"))
                Console.WriteLine(e.Name);
        }
    }
}
