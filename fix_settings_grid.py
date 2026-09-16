import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

target = """            LoadAllObjects();
            InitializeGrid();"""
replacement = """            LoadAllObjects();
            RefreshSettingsGrid();
            InitializeGrid();"""

content = content.replace(target, replacement)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
