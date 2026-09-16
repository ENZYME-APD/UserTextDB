import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

target = """            btnApplyOverride.Click += (s, e) =>
            {
                if (_editColumnDropDown.SelectedIndex <= 0 || currentOverrideInput == null) return;
                
                var colKey = _columns[_editColumnDropDown.SelectedIndex - 1].Key;"""

replacement = """            btnApplyOverride.Click += (s, e) =>
            {
                if (_editColumnDropDown.SelectedIndex <= 0 || currentOverrideInput == null) return;
                
                var selectedKey = _editColumnDropDown.Items[_editColumnDropDown.SelectedIndex].Text;
                var colDef = _columns.FirstOrDefault(c => c.Key == selectedKey);
                if (colDef == null) return;
                var colKey = colDef.Key;"""

content = content.replace(target, replacement)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
