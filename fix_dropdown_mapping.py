import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

target = """            _editColumnDropDown.SelectedIndexChanged += (s, e) =>
            {
                if (_editColumnDropDown.SelectedIndex > 0)
                {
                    var colDef = _columns[_editColumnDropDown.SelectedIndex - 1];"""

replacement = """            _editColumnDropDown.SelectedIndexChanged += (s, e) =>
            {
                if (_editColumnDropDown.SelectedIndex > 0)
                {
                    var selectedKey = _editColumnDropDown.Items[_editColumnDropDown.SelectedIndex].Text;
                    var colDef = _columns.FirstOrDefault(c => c.Key == selectedKey);
                    
                    if (colDef == null) return;"""

content = content.replace(target, replacement)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
