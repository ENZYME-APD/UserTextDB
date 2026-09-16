import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

# 1. Add `_regexToggle` to fields
content = content.replace("private TextBox _filterTextBox;", "private TextBox _filterTextBox;\n        private ToggleButton _regexToggle;")

# 2. Add `_regexToggle` initialization
ui_init_target = """            _filterTextBox = new TextBox { PlaceholderText = "Search/Filter Objects..." };
            _filterTextBox.TextChanged += (s, e) => RefreshGrid();"""
ui_init_replacement = """            _filterTextBox = new TextBox { PlaceholderText = "Search (e.g. timber OR steel, -phase 1, mat:wood)..." };
            _filterTextBox.TextChanged += (s, e) => RefreshGrid();
            
            _regexToggle = new ToggleButton { Text = ".*", ToolTip = "Enable Regular Expressions (Regex mode)" };
            _regexToggle.CheckedChanged += (s, e) => RefreshGrid();"""
content = content.replace(ui_init_target, ui_init_replacement)

# 3. Add to viewLayout
layout_target = """            viewLayout.AddRow("Filter:", _filterTextBox);"""
layout_replacement = """            var filterStack = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { new StackLayoutItem(_filterTextBox, true), _regexToggle } };
            viewLayout.AddRow("Filter:", filterStack);"""
content = content.replace(layout_target, layout_replacement)

# 4. Add MatchesFilter function and modify RefreshGrid
refresh_target = """            // Filter raw objects
            var filtered = _rawObjects.Where(o => 
                (string.IsNullOrEmpty(filterText) ||
                o.ObjectName.ToLowerInvariant().Contains(filterText) ||
                o.ObjectType.ToLowerInvariant().Contains(filterText) ||
                _columns.Any(c => (o.GetUserString(c.Key) ?? "").ToLowerInvariant().Contains(filterText))) &&
                (!(_showSelectedOnlyCheckbox?.Checked ?? false) || (RhinoDoc.ActiveDoc?.Objects.FindId(o.ObjectId)?.IsSelected(false) > 0))
            ).ToList();"""
            
refresh_replacement = """            // Filter raw objects
            var filtered = _rawObjects.Where(o => 
                MatchesFilter(o, _filterTextBox?.Text) &&
                (!(_showSelectedOnlyCheckbox?.Checked ?? false) || (RhinoDoc.ActiveDoc?.Objects.FindId(o.ObjectId)?.IsSelected(false) > 0))
            ).ToList();"""
content = content.replace(refresh_target, refresh_replacement)

# 5. Insert MatchesFilter function
func_code = """
        private bool MatchesFilter(Models.ObjectRowModel o, string filterText)
        {
            if (string.IsNullOrWhiteSpace(filterText)) return true;

            if (_regexToggle != null && (_regexToggle.Checked ?? false))
            {
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(filterText, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (regex.IsMatch(o.ObjectName) || regex.IsMatch(o.ObjectType)) return true;
                    foreach (var c in _columns)
                    {
                        var val = o.GetUserString(c.Key) ?? "";
                        if (regex.IsMatch(val)) return true;
                    }
                    return false;
                }
                catch
                {
                    // Invalid regex, fail safely
                    return false;
                }
            }

            var andBlocks = filterText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in andBlocks)
            {
                string b = block.Trim();
                if (string.IsNullOrEmpty(b)) continue;

                var orTerms = b.Split(new[] { " OR ", " | " }, StringSplitOptions.RemoveEmptyEntries);
                bool blockMatched = false;
                
                foreach (var t in orTerms)
                {
                    string term = t.Trim();
                    if (string.IsNullOrEmpty(term)) continue;

                    bool exclude = false;
                    if (term.StartsWith("-"))
                    {
                        exclude = true;
                        term = term.Substring(1).Trim();
                    }

                    string targetColumn = null;
                    int colonIdx = term.IndexOf(':');
                    if (colonIdx > 0)
                    {
                        targetColumn = term.Substring(0, colonIdx).Trim().ToLowerInvariant();
                        term = term.Substring(colonIdx + 1).Trim();
                    }
                    
                    term = term.ToLowerInvariant();
                    bool termMatched = false;
                    
                    if (string.IsNullOrEmpty(targetColumn))
                    {
                        if (o.ObjectName.ToLowerInvariant().Contains(term) ||
                            o.ObjectType.ToLowerInvariant().Contains(term) ||
                            _columns.Any(c => (o.GetUserString(c.Key) ?? "").ToLowerInvariant().Contains(term)))
                        {
                            termMatched = true;
                        }
                    }
                    else
                    {
                        if (targetColumn == "name" && o.ObjectName.ToLowerInvariant().Contains(term)) termMatched = true;
                        else if (targetColumn == "type" && o.ObjectType.ToLowerInvariant().Contains(term)) termMatched = true;
                        else
                        {
                            var targetCols = _columns.Where(c => c.Key.ToLowerInvariant().Contains(targetColumn)).ToList();
                            if (targetCols.Any(c => (o.GetUserString(c.Key) ?? "").ToLowerInvariant().Contains(term)))
                            {
                                termMatched = true;
                            }
                        }
                    }

                    if (exclude) termMatched = !termMatched;

                    if (termMatched)
                    {
                        blockMatched = true;
                        break; 
                    }
                }

                if (!blockMatched) return false;
            }

            return true;
        }
"""

# Insert right after RefreshGrid
insert_idx = content.find("private void RefreshGrid()")
if insert_idx == -1:
    print("Could not find RefreshGrid")
    exit(1)

content = content[:insert_idx] + func_code + content[insert_idx:]

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)

