using System;
using System.Reflection;
using Rhino.UI;

class Program {
    static void Main() {
        foreach (var m in typeof(Panels).GetMethods()) {
            if (m.Name == "RegisterPanel") {
                Console.WriteLine(m.ToString());
            }
        }
    }
}
