import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

target = "var settingsLayout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };"

new_code = """
            _showNameColumnCheckbox = new CheckBox { Text = "Show 'Name' Column", Checked = true };
            _showNameColumnCheckbox.CheckedChanged += (s, e) => { InitializeGrid(); };
            
            _showTypeColumnCheckbox = new CheckBox { Text = "Show 'Type' Column", Checked = true };
            _showTypeColumnCheckbox.CheckedChanged += (s, e) => { InitializeGrid(); };
            
            var settingsLayout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };"""

content = content.replace(target, new_code)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
