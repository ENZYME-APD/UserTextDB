import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

# In InitializeGrid, we add Name and Type (fixed columns), then we loop over _columns
# The loop looks like `for (int i = 0; i < _columns.Count; i++)`
init_grid_pattern = re.compile(r'for \(int i = 0; i < _columns\.Count; i\+\)\s*\{.*?\}', re.DOTALL)
replacement = """
            var visibleCols = _columns.Where(c => c.IsVisible).ToList();
            for (int i = 0; i < visibleCols.Count; i++)
            {
                int colIndex = i;
                var colDef = visibleCols[colIndex];
                var gridCol = new GridColumn
                {
                    HeaderText = colDef.Key,
                    DataCell = colDef.IsDropdown
                        ? new ComboBoxCell { Binding = new DelegateBinding<DatabaseTreeItem, string>(m => m.GetValue(colDef.Key), (m, v) => m.SetValue(colDef.Key, v)) }
                        : new TextBoxCell { Binding = new DelegateBinding<DatabaseTreeItem, string>(m => m.GetValue(colDef.Key), (m, v) => m.SetValue(colDef.Key, v)) },
                    Editable = true,
                    Sortable = true
                };

                if (colDef.IsDropdown && gridCol.DataCell is ComboBoxCell combo)
                {
                    var comboData = new Eto.Forms.ListItemCollection();
                    foreach (var opt in colDef.Options) comboData.Add(new Eto.Forms.ListItem { Text = opt, Key = opt });
                    combo.DataStore = comboData;
                }
                
                _grid.Columns.Add(gridCol);
            }
"""
content = init_grid_pattern.sub(replacement, content, count=1)

# In ExportToCsv()
# `var headers = new List<string> { "Guid", "Name", "Type" };`
# `headers.AddRange(_columns.Select(c => c.Key));`
csv_pattern = re.compile(r'headers\.AddRange\(_columns\.Select\(c => c\.Key\)\);')
csv_replacement = 'headers.AddRange(_columns.Where(c => c.IsVisible).Select(c => c.Key));'
content = csv_pattern.sub(csv_replacement, content)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
