import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

# 1. Add RefreshSettingsGrid
if "private void RefreshSettingsGrid()" not in content:
    content = content.replace(
        "public void PanelShown(",
        """private void RefreshSettingsGrid() {
            _settingsDataStore.Clear();
            foreach (var c in _columns) _settingsDataStore.Add(c);
            if (_settingsGrid != null) _settingsGrid.ReloadData(Eto.Forms.EtoRange<int>.FromLength(0, _settingsDataStore.Count));
        }
        
        public void PanelShown("""
    )

# 2. Fix Grid.SelectedItem assignments
content = content.replace("_settingsGrid.SelectedItem = sel;", "if (sel != null) { int row = _settingsDataStore.IndexOf(sel); if (row >= 0) { _settingsGrid.SelectRow(row); } }")

# 3. manageLayout issue - let's find `manageLayout` being used
pattern = re.compile(r'tabs\.Pages\.Add\(new TabPage \{ Text = "Manage Data", Content = manageLayout \}\);')
content = pattern.sub('', content)

# Remove the old manageLayout leftovers
pattern2 = re.compile(r'var manageLayout = new DynamicLayout.*?tabs = new TabControl\(\);', re.DOTALL)
# Actually, wait. I replaced it all up to `var tabs = new TabControl();`. If it still exists, my replacement missed something.
with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
