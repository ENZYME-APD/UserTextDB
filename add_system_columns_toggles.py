import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

# 1. Add fields
fields = """        private CheckBox _showNameColumnCheckbox;
        private CheckBox _showTypeColumnCheckbox;"""
content = content.replace("private CheckBox _syncSelectionCheckbox;", "private CheckBox _syncSelectionCheckbox;\n" + fields)

# 2. Instantiate and wire up in constructor (around line 100)
# Let's find a good place. After _syncSelectionCheckbox initialization:
# _syncSelectionCheckbox = new CheckBox { Text = "Sync Rhino Selection", Checked = true };
# _syncSelectionCheckbox.CheckedChanged += (s, e) => { OnGridSelectionChanged(null, null); };

sync_init = """            _syncSelectionCheckbox = new CheckBox { Text = "Sync Rhino Selection", Checked = true };
            _syncSelectionCheckbox.CheckedChanged += (s, e) => { OnGridSelectionChanged(null, null); };"""
sync_init_new = sync_init + """
            
            _showNameColumnCheckbox = new CheckBox { Text = "Show 'Name' Column", Checked = true };
            _showNameColumnCheckbox.CheckedChanged += (s, e) => InitializeGrid();
            
            _showTypeColumnCheckbox = new CheckBox { Text = "Show 'Type' Column", Checked = true };
            _showTypeColumnCheckbox.CheckedChanged += (s, e) => InitializeGrid();"""
content = content.replace(sync_init, sync_init_new)

# 3. Add to settingsLayout
settings_layout = """            var settingsLayout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };
            settingsLayout.BeginVertical();
            settingsLayout.AddRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { _newKeyTextBox, btnAddColumn } });
            settingsLayout.EndVertical();"""
settings_layout_new = """            var settingsLayout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };
            settingsLayout.BeginVertical();
            settingsLayout.AddRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { _newKeyTextBox, btnAddColumn } });
            settingsLayout.AddRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 15, Items = { _showNameColumnCheckbox, _showTypeColumnCheckbox } });
            settingsLayout.EndVertical();"""
content = content.replace(settings_layout, settings_layout_new)

# 4. Modify InitializeGrid
init_grid = """            _grid.Columns.Add(new GridColumn
            {
                HeaderText = "Name",
                DataCell = new TextBoxCell { Binding = Binding.Property<DatabaseTreeItem, string>(r => r.Name) },
                Editable = true
            });

            _grid.Columns.Add(new GridColumn
            {
                HeaderText = "Type",
                DataCell = new TextBoxCell { Binding = Binding.Property<DatabaseTreeItem, string>(r => r.Type) },
                Editable = false
            });"""
init_grid_new = """            if (_showNameColumnCheckbox.Checked == true)
            {
                _grid.Columns.Add(new GridColumn
                {
                    HeaderText = "Name",
                    DataCell = new TextBoxCell { Binding = Binding.Property<DatabaseTreeItem, string>(r => r.Name) },
                    Editable = true
                });
            }

            if (_showTypeColumnCheckbox.Checked == true)
            {
                _grid.Columns.Add(new GridColumn
                {
                    HeaderText = "Type",
                    DataCell = new TextBoxCell { Binding = Binding.Property<DatabaseTreeItem, string>(r => r.Type) },
                    Editable = false
                });
            }"""
content = content.replace(init_grid, init_grid_new)

# 5. Modify OnCellEdited
on_cell_edited = """            if (item != null && e.Column != 1)
            {
                bool isNameCol = e.Column == 0;
                var colDef = isNameCol ? null : _columns[e.Column - 2];
                var newValue = isNameCol ? item.Name : item.GetValue(colDef.Key);"""
on_cell_edited_new = """            if (item != null)
            {
                string header = _grid.Columns[e.Column].HeaderText;
                if (header == "Type") return;
                
                bool isNameCol = header == "Name";
                var colDef = isNameCol ? null : _columns.FirstOrDefault(c => c.Key == header);
                
                if (!isNameCol && colDef == null) return;
                
                var newValue = isNameCol ? item.Name : item.GetValue(colDef.Key);"""
content = content.replace(on_cell_edited, on_cell_edited_new)

# 6. Modify OnGridKeyDown
grid_key_down = """                // If they haven't edited anything yet, default to column 2 (the first custom column)
                // If they've edited something, use the last column they edited.
                int colToEdit = _lastEditedColumn >= 2 ? _lastEditedColumn : 2;
                
                _grid.BeginEdit(_grid.SelectedRow, colToEdit);"""
grid_key_down_new = """                int colToEdit = _lastEditedColumn;
                
                // Fallback to the first editable column if the last edited column is invalid/hidden
                if (colToEdit < 0 || colToEdit >= _grid.Columns.Count || !_grid.Columns[colToEdit].Editable)
                {
                    colToEdit = 0;
                    for (int i = 0; i < _grid.Columns.Count; i++)
                    {
                        if (_grid.Columns[i].Editable)
                        {
                            colToEdit = i;
                            break;
                        }
                    }
                }
                
                _grid.BeginEdit(_grid.SelectedRow, colToEdit);"""
content = content.replace(grid_key_down, grid_key_down_new)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
