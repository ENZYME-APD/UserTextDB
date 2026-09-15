import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

# 1. Add fields for settings grid
field_insert = """
        private DropDown _editColumnDropDown;
        private GridView _settingsGrid;
        private GridItemCollection _settingsDataStore;
"""
content = re.sub(r'private DropDown _editColumnDropDown;', field_insert, content)

# 2. Let's find the UI construction part.
# We'll replace the block from `var viewLayout = new DynamicLayout` down to `var tabs = new TabControl();`
# But wait, `addKeyButton` and `removeKeyButton` are defined way above.

