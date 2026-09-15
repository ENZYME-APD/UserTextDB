import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

# We need to construct the new UI logic.
# 1. Add fields
content = content.replace(
    'private DropDown _editColumnDropDown;',
    'private DropDown _editColumnDropDown;\n        private GridView _settingsGrid;\n        private GridItemCollection _settingsDataStore;\n        private TextBox _newKeyTextBox;'
)

# Replace the block from `addKeyButton.Click +=` down to `var tabs = new TabControl();`
pattern = re.compile(r'addKeyButton\.Click \+= \(s, e\) =>.*?var tabs = new TabControl\(\);', re.DOTALL)

new_ui = """
            _newKeyTextBox = new TextBox { PlaceholderText = "New Column Key..." };
            var btnAddColumn = new Button { Text = "Add Column" };
            
            _settingsDataStore = new GridItemCollection();
            _settingsGrid = new GridView { ShowHeader = true, GridLines = GridLines.Both, DataStore = _settingsDataStore };
            
            _settingsGrid.Columns.Add(new GridColumn { HeaderText = "Visible", Editable = true, DataCell = new CheckBoxCell { Binding = Binding.Property<ColumnDefinition, bool?>(c => c.IsVisible) } });
            
            _settingsGrid.Columns.Add(new GridColumn { HeaderText = "Key Name", Editable = true, DataCell = new TextBoxCell { Binding = Binding.Property<ColumnDefinition, string>(c => c.Key) } });
            
            _settingsGrid.Columns.Add(new GridColumn { HeaderText = "Dropdown?", Editable = true, DataCell = new CheckBoxCell { Binding = Binding.Property<ColumnDefinition, bool?>(c => c.IsDropdown) } });
            
            var optionsBinding = Binding.Delegate<ColumnDefinition, string>(
                c => string.Join(", ", c.Options),
                (c, val) => {
                    c.Options = val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToList();
                    c.IsDropdown = c.Options.Count > 0;
                }
            );
            _settingsGrid.Columns.Add(new GridColumn { HeaderText = "Options (comma sep)", Editable = true, DataCell = new TextBoxCell { Binding = optionsBinding } });

            string oldKey = null;
            _settingsGrid.CellEditing += (s, e) => {
                if (e.Column == 1) { oldKey = ((ColumnDefinition)e.Item).Key; }
            };
            
            _settingsGrid.CellEdited += (s, e) => {
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
                UpdateEditDropDown();
                InitializeGrid();
            };

            btnAddColumn.Click += (s, e) => {
                var nk = _newKeyTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(nk) && !_columns.Any(c => c.Key.Equals(nk, StringComparison.OrdinalIgnoreCase))) {
                    _columns.Add(new ColumnDefinition { Key = nk });
                    _newKeyTextBox.Text = string.Empty;
                    RefreshSettingsGrid();
                    UpdateEditDropDown();
                    InitializeGrid();
                }
            };
            
            var btnMoveUp = new Button { Text = "Move Up" };
            var btnMoveDown = new Button { Text = "Move Down" };
            var btnDeleteCol = new Button { Text = "Delete Column" };
            
            btnMoveUp.Click += (s, e) => {
                if (_settingsGrid.SelectedItem is ColumnDefinition sel) {
                    int idx = _columns.IndexOf(sel);
                    if (idx > 0) {
                        _columns.RemoveAt(idx);
                        _columns.Insert(idx - 1, sel);
                        RefreshSettingsGrid();
                        _settingsGrid.SelectedItem = sel;
                        InitializeGrid();
                    }
                }
            };
            
            btnMoveDown.Click += (s, e) => {
                if (_settingsGrid.SelectedItem is ColumnDefinition sel) {
                    int idx = _columns.IndexOf(sel);
                    if (idx >= 0 && idx < _columns.Count - 1) {
                        _columns.RemoveAt(idx);
                        _columns.Insert(idx + 1, sel);
                        RefreshSettingsGrid();
                        _settingsGrid.SelectedItem = sel;
                        InitializeGrid();
                    }
                }
            };
            
            btnDeleteCol.Click += (s, e) => {
                if (_settingsGrid.SelectedItem is ColumnDefinition sel) {
                    _columns.Remove(sel);
                    RefreshSettingsGrid();
                    UpdateEditDropDown();
                    InitializeGrid();
                }
            };

            _filterTextBox = new TextBox { PlaceholderText = "Search/Filter Objects..." };
            _filterTextBox.TextChanged += (s, e) => RefreshGrid();

            var btnRefresh = new Button { Text = "Reload Document" };
            btnRefresh.Click += (s, e) => LoadAllObjects();
            
            _hideTypeCheckbox = new CheckBox { Text = "Hide 'Type' Column", Checked = false };
            _hideTypeCheckbox.CheckedChanged += (s, e) => {
                if (_grid != null && _grid.Columns.Count > 1) 
                    _grid.Columns[1].Visible = !(_hideTypeCheckbox.Checked ?? false);
            };
            var btnExport = new Button { Text = "Export CSV" };
            btnExport.Click += (s, e) => ExportToCsv();
            
            var btnImport = new Button { Text = "Import CSV" };
            btnImport.Click += (s, e) => ImportFromCsv();

            _showSelectedOnlyCheckbox = new CheckBox { Text = "Show Only Selected in Grid", Checked = false };
            _showSelectedOnlyCheckbox.CheckedChanged += (s, e) => RefreshGrid();

            _selectKeyDropDown = new DropDown();
            _selectValueDropDown = new DropDown();
            _syncSelectionCheckbox = new CheckBox { Text = "Select from Grid", Checked = true };
            
            var btnSelectData = new Button { Text = "Select Matches" };

            _selectKeyDropDown.SelectedIndexChanged += (s, e) => 
            {
                _selectValueDropDown.Items.Clear();
                var key = _selectKeyDropDown.SelectedKey;
                if (!string.IsNullOrEmpty(key)) 
                {
                    var vals = _rawObjects.Select(o => o.GetUserString(key)).Where(v => !string.IsNullOrEmpty(v)).Distinct().OrderBy(v => v);
                    foreach (var v in vals) _selectValueDropDown.Items.Add(v);
                    if (_selectValueDropDown.Items.Count > 0) _selectValueDropDown.SelectedIndex = 0;
                }
            };
            
            btnSelectData.Click += (s, e) => 
            {
                var key = _selectKeyDropDown.SelectedKey;
                var val = _selectValueDropDown.SelectedKey;
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val)) 
                {
                    _isUpdatingSelection = true;
                    bool add = Eto.Forms.Keyboard.Modifiers.HasFlag(Eto.Forms.Keys.Shift);
                    bool remove = Eto.Forms.Keyboard.Modifiers.HasFlag(Eto.Forms.Keys.Control) || Eto.Forms.Keyboard.Modifiers.HasFlag(Eto.Forms.Keys.Application);
                    bool intersect = Eto.Forms.Keyboard.Modifiers.HasFlag(Eto.Forms.Keys.Alt);
                    
                    if (!add && !remove && !intersect) RhinoDoc.ActiveDoc?.Objects.UnselectAll();
                        
                    foreach (var obj in _rawObjects) 
                    {
                        bool match = obj.GetUserString(key) == val;
                        var rhObj = RhinoDoc.ActiveDoc?.Objects.FindId(obj.ObjectId);
                        if (rhObj == null) continue;
                        
                        if (intersect) { if (!match) rhObj.Select(false); }
                        else if (remove) { if (match) rhObj.Select(false); }
                        else { if (match) rhObj.Select(true, true); }
                    }
                    RhinoDoc.ActiveDoc?.Views.Redraw();
                    _isUpdatingSelection = false;
                    if (_showSelectedOnlyCheckbox.Checked == true) RefreshGrid();
                }
            };

            Control getSeparator() 
            {
                var p = new Panel { Height = 1, BackgroundColor = Colors.Gray };
                var stack = new StackLayout { Orientation = Orientation.Vertical, Spacing = 0, Padding = new Padding(0, 8) };
                stack.Items.Add(new StackLayoutItem(p, true));
                return stack;
            }

            var viewLayout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };
            viewLayout.BeginVertical();
            viewLayout.AddRow(btnRefresh);
            viewLayout.EndVertical();
            viewLayout.AddRow(getSeparator());
            viewLayout.BeginVertical();
            viewLayout.AddRow("Filter:", _filterTextBox);
            viewLayout.AddRow("", _showSelectedOnlyCheckbox);
            viewLayout.EndVertical();
            viewLayout.AddRow(getSeparator());
            viewLayout.BeginVertical();
            viewLayout.AddRow("Select By:", _selectKeyDropDown);
            viewLayout.AddRow("Value:", _selectValueDropDown);
            viewLayout.AddRow("", btnSelectData);
            viewLayout.AddRow("", _syncSelectionCheckbox);
            viewLayout.EndVertical();
            viewLayout.AddRow(getSeparator());
            viewLayout.BeginVertical();
            viewLayout.AddRow("Group By:", _groupByDropDown);
            viewLayout.AddRow("Column:", _groupColumnDropDown);
            viewLayout.AddRow("Sort By:", _sortByDropDown);
            viewLayout.EndVertical();
            viewLayout.BeginVertical();
            viewLayout.AddRow("3D Audit:", _auditColumnDropDown);
            viewLayout.EndVertical();
            viewLayout.AddRow(null);

            var batchLayout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };
            batchLayout.BeginVertical();
            batchLayout.AddRow("Batch Edit", new Label { Text = "(applies to all selected objects)" });
            batchLayout.AddRow("Target Col:", _editColumnDropDown);
            batchLayout.AddRow("Value:", overrideValueContainer);
            batchLayout.AddRow("", btnApplyOverride);
            batchLayout.EndVertical();
            batchLayout.AddRow(getSeparator());
            batchLayout.BeginVertical();
            batchLayout.AddRow("CSV Data:", new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { btnExport, btnImport } });
            batchLayout.EndVertical();
            batchLayout.AddRow(getSeparator());
            var btnToggleLayout = new Button { Text = "Switch Vertical/Horizontal UI" };
            batchLayout.BeginVertical();
            batchLayout.AddRow("Panel UI:", btnToggleLayout);
            batchLayout.EndVertical();
            batchLayout.AddRow(null);

            var settingsLayout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };
            settingsLayout.BeginVertical();
            settingsLayout.AddRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { _newKeyTextBox, btnAddColumn } });
            settingsLayout.EndVertical();
            settingsLayout.AddRow(_settingsGrid);
            settingsLayout.BeginVertical();
            settingsLayout.AddRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { btnMoveUp, btnMoveDown, btnDeleteCol } });
            settingsLayout.EndVertical();

            UpdateEditDropDown();
            _groupColumnDropDown.Enabled = _groupByDropDown.SelectedIndex == 2;

            var tabs = new TabControl();
            tabs.Pages.Add(new TabPage { Text = "View & Select", Content = viewLayout });
            tabs.Pages.Add(new TabPage { Text = "Batch Data", Content = batchLayout });
            tabs.Pages.Add(new TabPage { Text = "Settings", Content = settingsLayout });
"""

content = pattern.sub(new_ui, content)

# 3. Add RefreshSettingsGrid method to the bottom
content = content.replace(
    'public void PanelShown(',
    """private void RefreshSettingsGrid() {
            _settingsDataStore.Clear();
            foreach (var c in _columns) _settingsDataStore.Add(c);
            if (_settingsGrid != null) _settingsGrid.ReloadData(Eto.Forms.EtoRange<int>.FromLength(0, _settingsDataStore.Count));
        }
        
        public void PanelShown("""
)

# 4. Modify LoadAllObjects to call RefreshSettingsGrid
content = content.replace(
    'UpdateEditDropDown();\n                InitializeGrid();',
    'UpdateEditDropDown();\n                RefreshSettingsGrid();\n                InitializeGrid();'
)

# 5. Make sure UpdateEditDropDown filters by IsVisible
update_edit_body = """
        private void UpdateEditDropDown()
        {
            var visibleCols = _columns.Where(c => c.IsVisible).ToList();
            
            var prevEdit = _editColumnDropDown?.SelectedKey;
            if (_editColumnDropDown != null) {
                _editColumnDropDown.Items.Clear();
                _editColumnDropDown.Items.Add("--- New Column ---");
                foreach (var c in visibleCols) _editColumnDropDown.Items.Add(c.Key);
                var existingEdit = _editColumnDropDown.Items.FirstOrDefault(i => i.Text == prevEdit);
                if (existingEdit != null) _editColumnDropDown.SelectedKey = existingEdit.Key;
                else _editColumnDropDown.SelectedIndex = 0;
            }

            var prevGroup = _groupColumnDropDown?.SelectedKey;
            if (_groupColumnDropDown != null) {
                _groupColumnDropDown.Items.Clear();
                foreach (var c in visibleCols) _groupColumnDropDown.Items.Add(c.Key);
                var existingGroup = _groupColumnDropDown.Items.FirstOrDefault(i => i.Text == prevGroup);
                if (existingGroup != null) _groupColumnDropDown.SelectedKey = existingGroup.Key;
                else if (_groupColumnDropDown.Items.Count > 0) _groupColumnDropDown.SelectedIndex = 0;
            }

            var prevAudit = _auditColumnDropDown?.SelectedKey;
            if (_auditColumnDropDown != null) {
                _auditColumnDropDown.Items.Clear();
                _auditColumnDropDown.Items.Add("None");
                foreach (var c in visibleCols) _auditColumnDropDown.Items.Add(c.Key);
                var existingAudit = _auditColumnDropDown.Items.FirstOrDefault(i => i.Text == prevAudit);
                if (existingAudit != null) _auditColumnDropDown.SelectedKey = existingAudit.Key;
                else if (_auditColumnDropDown.Items.Count > 0) _auditColumnDropDown.SelectedIndex = 0;
            }

            var prevSort = _sortByDropDown?.SelectedKey;
            if (_sortByDropDown != null) {
                _sortByDropDown.Items.Clear();
                _sortByDropDown.Items.Add("None");
                foreach (var c in visibleCols) _sortByDropDown.Items.Add(c.Key);
                var existingSort = _sortByDropDown.Items.FirstOrDefault(i => i.Text == prevSort);
                if (existingSort != null) _sortByDropDown.SelectedKey = existingSort.Key;
                else if (_sortByDropDown.Items.Count > 0) _sortByDropDown.SelectedIndex = 0;
            }

            var prevSelKey = _selectKeyDropDown?.SelectedKey;
            if (_selectKeyDropDown != null)
            {
                _selectKeyDropDown.Items.Clear();
                foreach (var c in visibleCols) _selectKeyDropDown.Items.Add(c.Key);
                var existingSelKey = _selectKeyDropDown.Items.FirstOrDefault(i => i.Text == prevSelKey);
                if (existingSelKey != null) _selectKeyDropDown.SelectedKey = existingSelKey.Key;
                else if (_selectKeyDropDown.Items.Count > 0) _selectKeyDropDown.SelectedIndex = 0;
            }
        }
"""
# We'll use regex to replace UpdateEditDropDown
pattern2 = re.compile(r'private void UpdateEditDropDown\(\).*?public void PanelShown', re.DOTALL)
content = pattern2.sub(update_edit_body + '\n        public void PanelShown', content)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
