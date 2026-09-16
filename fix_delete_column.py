import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

target = """            btnDeleteCol.Click += (s, e) => {
                if (_settingsGrid.SelectedItem is ColumnDefinition sel) {
                    _columns.Remove(sel);
                    RefreshSettingsGrid();
                    UpdateEditDropDown();
                    InitializeGrid();
                }
            };"""

replacement = """            btnDeleteCol.Click += (s, e) => {
                if (_settingsGrid.SelectedItem is ColumnDefinition sel) {
                    var result = Eto.Forms.MessageBox.Show(
                        $"Are you sure you want to delete the column '{sel.Key}'?\\n\\nThis will permanently remove this UserText key from all objects in the document.",
                        "Confirm Delete",
                        Eto.Forms.MessageBoxButtons.YesNo,
                        Eto.Forms.MessageBoxType.Warning);
                        
                    if (result == Eto.Forms.DialogResult.Yes) {
                        // Wipe data from all tracked objects
                        foreach (var obj in _rawObjects) {
                            obj.SetUserString(sel.Key, null);
                        }
                        
                        _columns.Remove(sel);
                        RefreshSettingsGrid();
                        UpdateEditDropDown();
                        InitializeGrid();
                    }
                }
            };"""

content = content.replace(target, replacement)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
