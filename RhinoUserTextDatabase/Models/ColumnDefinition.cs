using System.Collections.Generic;

namespace RhinoUserTextDatabase.Models
{
    public class ColumnDefinition
    {
        public string Key { get; set; } = "";
        public bool IsDropdown { get; set; }
        public List<string> Options { get; set; } = new List<string>();
    }
}
