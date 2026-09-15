import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

# 1. Add _lastEditedColumn to fields
content = content.replace("private TreeGridItemCollection _dataStore;", "private TreeGridItemCollection _dataStore;\n        private int _lastEditedColumn = 2;")

# 2. Update OnCellEdited to track column
cell_edited_sig = "private void OnCellEdited(object? sender, GridViewCellEventArgs e)\n        {"
cell_edited_new = cell_edited_sig + "\n            _lastEditedColumn = e.Column;"
content = content.replace(cell_edited_sig, cell_edited_new)

# 3. Update OnGridKeyDown
keydown_old = """        private void OnGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter && _grid.SelectedRow >= 0)
            {
                e.Handled = true;
                _grid.BeginEdit(_grid.SelectedRow, 2); // default to first editable column
            }
        }"""
keydown_new = """        private void OnGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter && _grid.SelectedRow >= 0)
            {
                e.Handled = true;
                
                // If they haven't edited anything yet, default to column 2 (the first custom column)
                // If they've edited something, use the last column they edited.
                int colToEdit = _lastEditedColumn >= 2 ? _lastEditedColumn : 2;
                
                _grid.BeginEdit(_grid.SelectedRow, colToEdit);
            }
        }"""
content = content.replace(keydown_old, keydown_new)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
