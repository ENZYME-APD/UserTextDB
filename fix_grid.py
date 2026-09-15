import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

content = content.replace("if (_settingsGrid != null) _settingsGrid.ReloadData();", "if (_settingsGrid != null) { _settingsGrid.DataStore = null; _settingsGrid.DataStore = _settingsDataStore; }")

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
