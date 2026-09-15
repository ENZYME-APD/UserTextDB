import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

old_logic = """            _settingsGrid.CellEdited += (s, e) => {
                var colDef = (ColumnDefinition)e.Item;
                if (e.Column == 1 && oldKey != null && oldKey != colDef.Key) {
                    foreach (var obj in _rawObjects) {
                        string val = obj.GetUserString(oldKey);
                        if (val != null) {
                            obj.SetUserString(colDef.Key, val);
                            obj.SetUserString(oldKey, null);
                        }
                    }
                }
                UpdateEditDropDown();"""

new_logic = """            _settingsGrid.CellEdited += (s, e) => {
                var colDef = (ColumnDefinition)e.Item;
                
                // Deep Rename logic
                if (e.Column == 1 && oldKey != null && oldKey != colDef.Key) {
                    foreach (var obj in _rawObjects) {
                        string val = obj.GetUserString(oldKey);
                        if (val != null) {
                            obj.SetUserString(colDef.Key, val);
                            obj.SetUserString(oldKey, null);
                        }
                    }
                }
                
                // Auto-populate unique values when toggled to Dropdown
                if (e.Column == 2 && colDef.IsDropdown) {
                    var uniqueValues = new System.Collections.Generic.HashSet<string>();
                    foreach (var obj in _rawObjects) {
                        string val = obj.GetUserString(colDef.Key);
                        if (!string.IsNullOrEmpty(val)) {
                            uniqueValues.Add(val);
                        }
                    }
                    if (uniqueValues.Count > 0) {
                        var existing = colDef.Options ?? new System.Collections.Generic.List<string>();
                        colDef.Options = existing.Union(uniqueValues).Distinct().OrderBy(x => x).ToList();
                    }
                }
                
                UpdateEditDropDown();"""

content = content.replace(old_logic, new_logic)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
