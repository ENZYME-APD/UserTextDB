using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Runtime.InteropServices;
using Eto.Forms;
using Eto.Drawing;
using Rhino;
using Rhino.UI;
using Rhino.DocObjects;
using RhinoUserTextDatabase.Models;

namespace RhinoUserTextDatabase.UI
{
    [Guid("A1B2C3D4-E5F6-4789-9A0B-1C2D3E4F5A6B")]
    public class UserTextDatabasePanel : Panel, IPanel
    {
        public static Guid PanelId => typeof(UserTextDatabasePanel).GUID;

        private TreeGridView _grid;
        private Panel _gridContainer;
        private TreeGridItemCollection _dataStore;
        private int _lastEditedColumn = 2;
        private List<ColumnDefinition> _columns;
        private List<ObjectRowModel> _rawObjects;
        private DropDown _groupDropdown1;
        private DropDown _groupDropdown2;
        private DropDown _groupDropdown3;
        private DropDown _viewStatesDropDown;
        private TextBox _newStateNameTextBox;
        private DropDown _sortByDropDown;
        private DropDown _editColumnDropDown;
        private GridView _settingsGrid;
        private System.Collections.ObjectModel.ObservableCollection<ColumnDefinition> _settingsDataStore;
        private TextBox _newKeyTextBox;
        
        private DatabaseDisplayConduit _conduit;
        private DropDown _auditColumnDropDown;

        private TextBox _filterTextBox;
        private ToggleButton _regexToggle;
        private CheckBox _showSelectedOnlyCheckbox;
        private CheckBox _hideTypeCheckbox;
        private DropDown _selectKeyDropDown;
        private DropDown _selectValueDropDown;
        private CheckBox _syncSelectionCheckbox;
        private CheckBox _showNameColumnCheckbox;
        private CheckBox _showTypeColumnCheckbox;
        private CheckBox _hideEmptyObjectsCheckbox;
        private bool _isUpdatingSelection = false;
        public UserTextDatabasePanel()
        {
            this.UseRhinoStyle();

            _rawObjects = new List<ObjectRowModel>();
            _dataStore = new TreeGridItemCollection();
            _columns = new List<ColumnDefinition>
            {
                new ColumnDefinition { Key = "Status", IsDropdown = true, Options = new List<string> { "", "Approved", "Pending", "Rejected" } },
                new ColumnDefinition { Key = "Material", IsDropdown = false },
                new ColumnDefinition { Key = "Phase", IsDropdown = false }
            };
            
            _groupDropdown1 = new DropDown();
            _groupDropdown2 = new DropDown();
            _groupDropdown3 = new DropDown();
            _groupDropdown1.SelectedIndexChanged += (s, e) => RefreshGrid();
            _groupDropdown2.SelectedIndexChanged += (s, e) => RefreshGrid();
            _groupDropdown3.SelectedIndexChanged += (s, e) => RefreshGrid();
            
            _sortByDropDown = new DropDown();
            _sortByDropDown.SelectedIndexChanged += (s, e) => RefreshGrid();
            
            _editColumnDropDown = new DropDown();
            var overrideValueContainer = new Panel();
            var btnApplyOverride = new Button { Text = "Apply to Selected" };
            var newKeyTextBox = new TextBox { PlaceholderText = "Key Name" };
            var optionsTextBox = new TextBox { PlaceholderText = "Options (comma sep)" };
            var addKeyButton = new Button { Text = "Save" };
            var removeKeyButton = new Button { Text = "Remove" };

            _conduit = new DatabaseDisplayConduit();
            _auditColumnDropDown = new DropDown();
            _auditColumnDropDown.SelectedIndexChanged += (s, e) =>
            {
                if (_auditColumnDropDown.SelectedIndex > 0)
                {
                    _conduit.Enabled = true;
                    _conduit.ActiveColumn = _auditColumnDropDown.SelectedKey ?? "";
                }
                else
                {
                    _conduit.Enabled = false;
                }
                RhinoDoc.ActiveDoc?.Views.Redraw();
            };


            Control currentOverrideInput = null;
            
            _editColumnDropDown.SelectedIndexChanged += (s, e) =>
            {
                if (_editColumnDropDown.SelectedIndex > 0)
                {
                    var selectedKey = _editColumnDropDown.Items[_editColumnDropDown.SelectedIndex].Text;
                    var colDef = _columns.FirstOrDefault(c => c.Key == selectedKey);
                    
                    if (colDef == null) return;
                    newKeyTextBox.Text = colDef.Key;
                    optionsTextBox.Text = string.Join(", ", colDef.Options);
                    
                    if (colDef.IsDropdown)
                    {
                        var dd = new DropDown();
                        dd.Items.Add("");
                        foreach (var o in colDef.Options) dd.Items.Add(o);
                        dd.SelectedIndex = 0;
                        currentOverrideInput = dd;
                    }
                    else
                    {
                        currentOverrideInput = new TextBox();
                    }
                    overrideValueContainer.Content = currentOverrideInput;
                }
                else
                {
                    newKeyTextBox.Text = string.Empty;
                    optionsTextBox.Text = string.Empty;
                    currentOverrideInput = new TextBox { Enabled = false };
                    overrideValueContainer.Content = currentOverrideInput;
                }
            };
            btnApplyOverride.Click += (s, e) =>
            {
                if (_editColumnDropDown.SelectedIndex <= 0 || currentOverrideInput == null) return;
                
                var selectedKey = _editColumnDropDown.Items[_editColumnDropDown.SelectedIndex].Text;
                var colDef = _columns.FirstOrDefault(c => c.Key == selectedKey);
                if (colDef == null) return;
                var colKey = colDef.Key;
                string val = "";
                if (currentOverrideInput is DropDown dd) val = dd.SelectedKey ?? "";
                if (currentOverrideInput is TextBox tb) val = tb.Text;
                
                var gridSelectedRows = _grid?.SelectedItems.OfType<DatabaseTreeItem>()
                                           .Where(i => !i.IsGroup && i.RowModel != null)
                                           .Select(i => i.RowModel.ObjectId).ToList() ?? new List<Guid>();
                
                var rhSelected = RhinoDoc.ActiveDoc?.Objects.GetSelectedObjects(false, false)
                                         .Select(o => o.Id).ToList() ?? new List<Guid>();
                
                var targets = rhSelected.Count > 0 ? rhSelected : gridSelectedRows;
                
                if (targets.Count == 0)
                {
                    Eto.Forms.MessageBox.Show("No objects selected in Grid or Rhino!");
                    return;
                }
                
                foreach (var id in targets)
                {
                    var row = _rawObjects.FirstOrDefault(r => r.ObjectId == id);
                    if (row != null) row.SetUserString(colKey, val);
                }
                
                if (_gridContainer.Content is TreeGridView grid) grid.ReloadData();
            };

            
            _newKeyTextBox = new TextBox { PlaceholderText = "New Column Key..." };
            var btnAddColumn = new Button { Text = "Add Column" };
            
            _settingsDataStore = new System.Collections.ObjectModel.ObservableCollection<ColumnDefinition>();
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
                
                SaveSchema();
                UpdateDynamicDropdowns();
                RefreshSettingsGrid();
                InitializeGrid();
            };

            btnAddColumn.Click += (s, e) => {
                var nk = _newKeyTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(nk) && !_columns.Any(c => c.Key.Equals(nk, StringComparison.OrdinalIgnoreCase))) {
                    _columns.Add(new ColumnDefinition { Key = nk });
                    _newKeyTextBox.Text = string.Empty;
                    RefreshSettingsGrid();
                    UpdateDynamicDropdowns();
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
                        if (sel != null) { int row = _settingsDataStore.IndexOf(sel); if (row >= 0) { _settingsGrid.SelectRow(row); } }
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
                        if (sel != null) { int row = _settingsDataStore.IndexOf(sel); if (row >= 0) { _settingsGrid.SelectRow(row); } }
                        InitializeGrid();
                    }
                }
            };
            
            btnDeleteCol.Click += (s, e) => {
                if (_settingsGrid.SelectedItem is ColumnDefinition sel) {
                    var result = Eto.Forms.MessageBox.Show(
                        $"Are you sure you want to delete the column '{sel.Key}'?\n\nThis will permanently remove this UserText key from all objects in the document.",
                        "Confirm Delete",
                        Eto.Forms.MessageBoxButtons.YesNo,
                        Eto.Forms.MessageBoxType.Warning);
                        
                    if (result == Eto.Forms.DialogResult.Yes) {
                        // Wipe data from all tracked objects
                        foreach (var obj in _rawObjects) {
                            obj.SetUserString(sel.Key, null);
                        }
                        
                        _columns.Remove(sel);
                        SaveSchema();
                        RefreshSettingsGrid();
                        UpdateDynamicDropdowns();
                        InitializeGrid();
                    }
                }
            };

            _filterTextBox = new TextBox { PlaceholderText = "Search (e.g. timber OR steel, -phase 1, mat:wood)..." };
            _filterTextBox.TextChanged += (s, e) => RefreshGrid();
            
            _regexToggle = new ToggleButton { Text = ".*", ToolTip = "Enable Regular Expressions (Regex mode)" };
            _regexToggle.CheckedChanged += (s, e) => RefreshGrid();

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
            
            _hideEmptyObjectsCheckbox = new CheckBox { Text = "Hide Objects with No Data", Checked = false };
            _hideEmptyObjectsCheckbox.CheckedChanged += (s, e) => RefreshGrid();

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
            var filterStack = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { new StackLayoutItem(_filterTextBox, true), _regexToggle } };
            viewLayout.AddRow("Filter:", filterStack);
            var filterCheckboxesStack = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 15, Items = { _showSelectedOnlyCheckbox, _hideEmptyObjectsCheckbox } };
            viewLayout.AddRow("", filterCheckboxesStack);
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
            var groupStack = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { new StackLayoutItem(_groupDropdown1, true), new StackLayoutItem(_groupDropdown2, true), new StackLayoutItem(_groupDropdown3, true) } };
            viewLayout.AddRow("Group By:", groupStack);
            viewLayout.AddRow("Sort By:", _sortByDropDown);
            viewLayout.EndVertical();
            viewLayout.BeginVertical();
            viewLayout.AddRow("3D Audit:", _auditColumnDropDown);
            viewLayout.EndVertical();
            viewLayout.AddRow(null);

            var batchLayout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };
            
            var titleFont = new Eto.Drawing.Font(SystemFonts.Default().Family, 11, Eto.Drawing.FontStyle.Bold);
            var subFont = new Eto.Drawing.Font(SystemFonts.Default().Family, 10);
            
            var title1 = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = {
                new Label { Text = "Batch Edit", Font = titleFont, VerticalAlignment = VerticalAlignment.Center },
                new Label { Text = "(Applies to all selected objects)", Font = subFont, VerticalAlignment = VerticalAlignment.Center }
            }};
            batchLayout.AddRow(title1);
            batchLayout.AddRow(new Panel { Height = 5 });
            
            batchLayout.BeginVertical();
            batchLayout.AddRow("Target Col:", _editColumnDropDown);
            batchLayout.AddRow("Value:", overrideValueContainer);
            batchLayout.AddRow("", btnApplyOverride);
            batchLayout.EndVertical();
            
            batchLayout.AddRow(new Panel { Height = 5 });
            batchLayout.AddRow(getSeparator());
            batchLayout.AddRow(new Panel { Height = 5 });
            
            var title2 = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = {
                new Label { Text = "Data Exchange", Font = titleFont, VerticalAlignment = VerticalAlignment.Center },
                new Label { Text = "(Import/Export data to excel)", Font = subFont, VerticalAlignment = VerticalAlignment.Center }
            }};
            batchLayout.AddRow(title2);
            batchLayout.AddRow(new Panel { Height = 5 });
            
            batchLayout.BeginVertical();
            batchLayout.AddRow("CSV Data:", new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { btnExport, btnImport } });
            batchLayout.EndVertical();
            
            batchLayout.AddRow(new Panel { Height = 5 });
            batchLayout.AddRow(getSeparator());
            batchLayout.AddRow(new Panel { Height = 5 });
            
            var title3 = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = {
                new Label { Text = "User Interface", Font = titleFont, VerticalAlignment = VerticalAlignment.Center },
                new Label { Text = "(Optimise controls for vertical/horizontal layout)", Font = subFont, VerticalAlignment = VerticalAlignment.Center }
            }};
            batchLayout.AddRow(title3);
            batchLayout.AddRow(new Panel { Height = 5 });
            
            var btnToggleLayout = new Button { Text = "Switch Vertical/Horizontal UI" };
            batchLayout.BeginVertical();
            batchLayout.AddRow("Panel UI:", btnToggleLayout);
            batchLayout.EndVertical();
            batchLayout.AddRow(null);

            
            _showNameColumnCheckbox = new CheckBox { Text = "Show 'Name' Column", Checked = true };
            _showNameColumnCheckbox.CheckedChanged += (s, e) => { InitializeGrid(); };
            
            _showTypeColumnCheckbox = new CheckBox { Text = "Show 'Type' Column", Checked = true };
            _showTypeColumnCheckbox.CheckedChanged += (s, e) => { InitializeGrid(); };
            

            

            var settingsLayout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };
            
            Control footerControl = new Panel();
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var logoImg = Eto.Drawing.Bitmap.FromResource("RhinoUserTextDatabase.Resources.logo.png", asm);
                
                var logoView = new ImageView { Image = logoImg, Size = new Eto.Drawing.Size(108, 34) };
                
                var linkSite = new LinkButton { Text = "www.weareenzyme.com" };
                linkSite.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.weareenzyme.com", UseShellExecute = true });
                
                var linkEmail = new LinkButton { Text = "digital@weareenzyme.com" };
                linkEmail.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "mailto:digital@weareenzyme.com", UseShellExecute = true });
                
                var lblVersion = new Label { Text = "v1.1.20 Beta", TextColor = Eto.Drawing.Colors.Gray };
                
                var leftStack = new StackLayout { Orientation = Orientation.Vertical, Items = { logoView }, VerticalContentAlignment = VerticalAlignment.Center };
                var rightStack = new StackLayout { 
                    Orientation = Orientation.Vertical, 
                    HorizontalContentAlignment = HorizontalAlignment.Right,
                    Spacing = 2,
                    Items = { linkSite, linkEmail, lblVersion }
                };
                
                var footerDyn = new DynamicLayout();
                footerDyn.AddRow(leftStack, null, rightStack);
                footerControl = footerDyn;
            }
            catch (System.Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error loading branding: {ex.Message}");
            }

            var infoLayout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(20) };
            
            var descFont = new Eto.Drawing.Font(SystemFonts.Default().Family, 11);
            
            var linkDocs = new LinkButton { Text = "📖 Read Documentation on GitHub" };
            linkDocs.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://enzyme-apd.github.io/UserTextDB/", UseShellExecute = true });
            
            // Force strict left-alignment by embedding inside a stretched vertical stack layout.
            // Wrapping horizontal stacks enforce minimum bounding box for the titles, ensuring exact left placement.
            var contentStack = new StackLayout {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Spacing = 5,
                Items = {
                    new StackLayout { Orientation = Orientation.Horizontal, Items = { new Label { Text = "About", Font = titleFont } } },
                    new Panel { Height = 5 },
                    new Label { Text = "UserText DB is an Excel-style database management tool for Rhino User Text. It allows you to quickly view, select, batch-edit, and organize object metadata using a dynamic spreadsheet interface.", Wrap = WrapMode.Word, Font = descFont, TextAlignment = TextAlignment.Left },
                    new Panel { Height = 5 },
                    new Label { Text = "It features a powerful search system (including Regex and Google-style search) as well as advanced database functions.", Wrap = WrapMode.Word, Font = descFont, TextAlignment = TextAlignment.Left },
                    new Panel { Height = 5 },
                    new Label { Text = "For a detailed explanation, examples, and cheat sheets, please check out our documentation.", Wrap = WrapMode.Word, Font = descFont, TextAlignment = TextAlignment.Left },
                    new Panel { Height = 5 },
                    new StackLayout { Orientation = Orientation.Horizontal, Items = { linkDocs } },
                    getSeparator(),
                    footerControl
                }
            };
            
            infoLayout.AddRow(contentStack);
            infoLayout.AddRow(null);

            UpdateDynamicDropdowns();

            _viewStatesDropDown = new DropDown();
            var btnLoadState = new Button { Text = "Load" };
            btnLoadState.Click += (s, e) => ApplyViewState();
            var btnDeleteState = new Button { Text = "Delete" };
            btnDeleteState.Click += (s, e) => DeleteViewState();
            
            _newStateNameTextBox = new TextBox { PlaceholderText = "New State Name..." };
            var btnSaveState = new Button { Text = "Save" };
            btnSaveState.Click += (s, e) => SaveViewState();

            settingsLayout.AddRow(new StackLayout { 
                Orientation = Orientation.Horizontal, Spacing = 5, 
                Items = { new Label { Text = "States:", VerticalAlignment = VerticalAlignment.Center }, new StackLayoutItem(_viewStatesDropDown, true), btnLoadState, btnDeleteState } 
            });
            
            settingsLayout.AddRow(new StackLayout { 
                Orientation = Orientation.Horizontal, Spacing = 5, 
                Items = { new Label { Text = "Save:  ", VerticalAlignment = VerticalAlignment.Center }, new StackLayoutItem(_newStateNameTextBox, true), btnSaveState } 
            });

            settingsLayout.AddRow(getSeparator());

            settingsLayout.AddRow(new StackLayout { 
                Orientation = Orientation.Horizontal, Spacing = 5, 
                Items = { new StackLayoutItem(_newKeyTextBox, true), btnAddColumn } 
            });
            
            settingsLayout.AddRow(new StackLayout { 
                Orientation = Orientation.Horizontal, Spacing = 15, 
                Items = { _showNameColumnCheckbox, _showTypeColumnCheckbox } 
            });

            _settingsGrid.Height = 150;
            settingsLayout.AddRow(_settingsGrid);

            settingsLayout.AddRow(new StackLayout { 
                Orientation = Orientation.Horizontal, Spacing = 5, 
                Items = { btnMoveUp, btnMoveDown, btnDeleteCol } 
            });
            settingsLayout.AddRow(null);

            var tabs = new TabControl();
            tabs.Pages.Add(new TabPage { Text = "Group & Select", Content = viewLayout });
            tabs.Pages.Add(new TabPage { Text = "Batch Data", Content = batchLayout });
            tabs.Pages.Add(new TabPage { Text = "Grid Settings", Content = settingsLayout });
            tabs.Pages.Add(new TabPage { Text = "Info", Content = infoLayout });

            LoadViewStates();



            

            _gridContainer = new Panel();
            
            var splitter = new Splitter
            {
                Orientation = Orientation.Vertical,
                FixedPanel = SplitterFixedPanel.Panel1,
                Panel1 = tabs,
                Panel2 = _gridContainer,
                Panel1MinimumSize = 360,
                Position = 360
            };

            btnToggleLayout.Click += (s, e) =>
            {
                if (splitter.Orientation == Orientation.Vertical)
                {
                    splitter.Orientation = Orientation.Horizontal;
                    splitter.Panel1MinimumSize = 360;
                    splitter.Position = 360;
                }
                else
                {
                    splitter.Orientation = Orientation.Vertical;
                    splitter.Panel1MinimumSize = 360;
                    splitter.Position = 360;
                }
            };
            
            
            bool isUnlocked = RhinoUserTextDatabasePlugIn.Instance?.Settings.GetBool("BetaUnlocked", false) ?? false;
            DateTime expirationDate = new DateTime(2027, 1, 1);
            
            if (!isUnlocked && DateTime.Now > expirationDate)
            {
                var expLayout = new DynamicLayout { DefaultSpacing = new Size(10, 10), Padding = new Padding(20) };
                expLayout.BeginVertical();
                expLayout.AddRow(new Label { Text = "The Beta period for UserText DB has expired." });
                var passInput = new TextBox { PlaceholderText = "Enter unlock key" };
                var btnUnlock = new Button { Text = "Unlock" };
                
                btnUnlock.Click += (s, e) => {
                    var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(passInput.Text));
                    if (encoded == "ZW56eW1lMy4wLTIwMjc=") {
                        RhinoUserTextDatabasePlugIn.Instance?.Settings.SetBool("BetaUnlocked", true);
                        Content = splitter;
                    } else {
                        Eto.Forms.MessageBox.Show("Invalid unlock key.", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                    }
                };
                
                expLayout.AddRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { passInput, btnUnlock } });
                expLayout.EndVertical();
                expLayout.AddRow(null);
                Content = expLayout;
            }
            else
            {
                Content = splitter;
            }


            RhinoDoc.AddRhinoObject += OnAddObject;
            RhinoDoc.DeleteRhinoObject += OnDeleteObject;
            RhinoDoc.SelectObjects += OnSelectionChanged;
            RhinoDoc.DeselectObjects += OnSelectionChanged;
            
            LoadAllObjects();
            RefreshSettingsGrid();
            InitializeGrid();
            
        }

        private void SaveSchema()
        {
            if (RhinoDoc.ActiveDoc == null) return;
            try {
                string json = JsonSerializer.Serialize(_columns);
                RhinoDoc.ActiveDoc.Strings.SetString("UserTextDB", "Schema", json);
            } catch { }
        }

        private void LoadSchema()
        {
            if (RhinoDoc.ActiveDoc == null) return;
            string json = RhinoDoc.ActiveDoc.Strings.GetValue("UserTextDB", "Schema");
            if (!string.IsNullOrEmpty(json))
            {
                try {
                    var savedCols = JsonSerializer.Deserialize<System.Collections.Generic.List<ColumnDefinition>>(json);
                    if (savedCols != null && savedCols.Count > 0)
                    {
                        var existingKeys = new System.Collections.Generic.HashSet<string>(_columns.Select(c => c.Key), StringComparer.OrdinalIgnoreCase);
                        foreach (var sc in savedCols)
                        {
                            if (!existingKeys.Contains(sc.Key))
                            {
                                _columns.Add(sc);
                                existingKeys.Add(sc.Key);
                            }
                            else
                            {
                                var match = _columns.First(c => c.Key.Equals(sc.Key, StringComparison.OrdinalIgnoreCase));
                                match.IsDropdown = sc.IsDropdown;
                                match.Options = sc.Options;
                            }
                        }
                    }
                } catch { }
            }
        }

        private void LoadAllObjects()
        {
            _rawObjects.Clear();
            if (RhinoDoc.ActiveDoc == null) return;
            
            LoadSchema();
            
            var settings = new Rhino.DocObjects.ObjectEnumeratorSettings
            {
                NormalObjects = true,
                LockedObjects = true,
                HiddenObjects = true,
                ActiveObjects = true,
                DeletedObjects = false
            };
            
            var existingKeys = new HashSet<string>(_columns.Select(c => c.Key), StringComparer.OrdinalIgnoreCase);
            bool newColumnsAdded = false;
            
            foreach (var rhObj in RhinoDoc.ActiveDoc.Objects.GetObjectList(settings))
            {
                _rawObjects.Add(new ObjectRowModel(rhObj));
                var userStrings = rhObj.Attributes.GetUserStrings();
                foreach (string key in userStrings.AllKeys)
                {
                    if (!existingKeys.Contains(key))
                    {
                        existingKeys.Add(key);
                        _columns.Add(new ColumnDefinition { Key = key, IsDropdown = false, Options = new List<string>() });
                        newColumnsAdded = true;
                    }
                }
            }
            
            if (newColumnsAdded)
            {
                SaveSchema();
                SaveSchema();
                UpdateDynamicDropdowns();
                RefreshSettingsGrid();
                InitializeGrid();
            }
            else
            {
                RefreshGrid();
            }
        }


        private void LoadViewStates()
        {
            if (_viewStatesDropDown == null) return;
            _viewStatesDropDown.Items.Clear();
            if (RhinoDoc.ActiveDoc == null) return;
            string json = RhinoDoc.ActiveDoc.Strings.GetValue("UserTextDB", "ViewStates");
            if (!string.IsNullOrEmpty(json))
            {
                try {
                    var states = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>>(json);
                    if (states != null)
                    {
                        foreach (var key in states.Keys) _viewStatesDropDown.Items.Add(key);
                    }
                } catch { }
            }
            if (_viewStatesDropDown.Items.Count > 0) _viewStatesDropDown.SelectedIndex = 0;
        }

        private void SaveViewState()
        {
            var name = _newStateNameTextBox?.Text?.Trim();
            if (string.IsNullOrEmpty(name) || RhinoDoc.ActiveDoc == null) return;
            
            var states = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();
            string json = RhinoDoc.ActiveDoc.Strings.GetValue("UserTextDB", "ViewStates");
            if (!string.IsNullOrEmpty(json))
            {
                try { 
                    var deserialized = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>>(json);
                    if (deserialized != null) states = deserialized;
                } catch { }
            }
            
            var visibleKeys = _columns.Where(c => c.IsVisible).Select(c => c.Key).ToList();
            states[name] = visibleKeys;
            
            RhinoDoc.ActiveDoc.Strings.SetString("UserTextDB", "ViewStates", JsonSerializer.Serialize(states));
            if (_newStateNameTextBox != null) _newStateNameTextBox.Text = "";
            LoadViewStates();
            _viewStatesDropDown.SelectedKey = name;
        }
        
        private void ApplyViewState()
        {
            if (_viewStatesDropDown == null || _viewStatesDropDown.SelectedIndex < 0 || RhinoDoc.ActiveDoc == null) return;
            var name = _viewStatesDropDown.SelectedKey;
            
            string json = RhinoDoc.ActiveDoc.Strings.GetValue("UserTextDB", "ViewStates");
            if (string.IsNullOrEmpty(json)) return;
            try {
                var states = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>>(json);
                if (states != null && states.ContainsKey(name))
                {
                    var keys = states[name];
                    foreach (var col in _columns) col.IsVisible = false;
                    
                    var newCols = new System.Collections.Generic.List<ColumnDefinition>();
                    foreach (var k in keys)
                    {
                        var match = _columns.FirstOrDefault(c => c.Key == k);
                        if (match != null)
                        {
                            match.IsVisible = true;
                            newCols.Add(match);
                        }
                    }
                    foreach (var col in _columns)
                    {
                        if (!newCols.Contains(col)) newCols.Add(col);
                    }
                    _columns = newCols;
                    
                    RefreshSettingsGrid();
                    UpdateDynamicDropdowns();
                    InitializeGrid();
                }
            } catch { }
        }
        
        private void DeleteViewState()
        {
            if (_viewStatesDropDown == null || _viewStatesDropDown.SelectedIndex < 0 || RhinoDoc.ActiveDoc == null) return;
            var name = _viewStatesDropDown.SelectedKey;
            
            string json = RhinoDoc.ActiveDoc.Strings.GetValue("UserTextDB", "ViewStates");
            if (string.IsNullOrEmpty(json)) return;
            try {
                var states = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>>(json);
                if (states != null && states.ContainsKey(name))
                {
                    states.Remove(name);
                    RhinoDoc.ActiveDoc.Strings.SetString("UserTextDB", "ViewStates", JsonSerializer.Serialize(states));
                    LoadViewStates();
                }
            } catch { }
        }

        private System.Collections.Generic.IEnumerable<DatabaseTreeItem> GroupItems(System.Collections.Generic.IEnumerable<ObjectRowModel> items, System.Collections.Generic.List<string> keys, int keyIndex)
        {
            if (keyIndex >= keys.Count)
            {
                return items.Select(o => new DatabaseTreeItem(o));
            }
            
            string currentKey = keys[keyIndex];
            
            System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, ObjectRowModel>> groups;
            if (currentKey == "Type")
            {
                groups = items.GroupBy(o => o.ObjectType).OrderBy(g => g.Key);
            }
            else
            {
                groups = items.GroupBy(o => string.IsNullOrEmpty(o.GetUserString(currentKey)) ? "Unassigned" : o.GetUserString(currentKey)).OrderBy(g => g.Key);
            }
            
            var result = new System.Collections.Generic.List<DatabaseTreeItem>();
            foreach (var g in groups)
            {
                string title = currentKey == "Type" ? $"Type: {g.Key}" : $"{currentKey}: {g.Key}";
                var groupNode = new DatabaseTreeItem(title);
                var children = GroupItems(g, keys, keyIndex + 1);
                foreach (var c in children) groupNode.Children.Add(c);
                result.Add(groupNode);
            }
            return result;
        }

        private void OnAddObject(object? sender, RhinoObjectEventArgs e)
        {
            if (e.TheObject != null && !_rawObjects.Any(r => r.ObjectId == e.TheObject.Id))
            {
                _rawObjects.Add(new ObjectRowModel(e.TheObject));
                
                bool newColumnsAdded = false;
                var existingKeys = new HashSet<string>(_columns.Select(c => c.Key), StringComparer.OrdinalIgnoreCase);
                var userStrings = e.TheObject.Attributes.GetUserStrings();
                
                foreach (string key in userStrings.AllKeys)
                {
                    if (!existingKeys.Contains(key))
                    {
                        existingKeys.Add(key);
                        _columns.Add(new ColumnDefinition { Key = key, IsDropdown = false, Options = new List<string>() });
                        newColumnsAdded = true;
                    }
                }
                
                if (newColumnsAdded)
                {
                    UpdateDynamicDropdowns();
                    InitializeGrid();
                }
                else
                {
                    RefreshGrid();
                }
            }
        }

        private void OnDeleteObject(object? sender, RhinoObjectEventArgs e)
        {
            if (e.TheObject != null)
            {
                int removed = _rawObjects.RemoveAll(r => r.ObjectId == e.TheObject.Id);
                if (removed > 0) RefreshGrid();
            }
        }

        private void OnSelectionChanged(object? sender, RhinoObjectSelectionEventArgs e)
        {
            if (!_isUpdatingSelection && _showSelectedOnlyCheckbox != null && _showSelectedOnlyCheckbox.Checked == true)
            {
                // Defer refresh to let Rhino finish its internal selection state updates
                Application.Instance.AsyncInvoke(() => RefreshGrid());
            }
        }

        private void InitializeGrid()
        {
            if (_gridContainer.Content is TreeGridView oldGrid)
            {
                oldGrid.DataStore = null;
                oldGrid.KeyDown -= OnGridKeyDown;
            }
            
            _gridContainer.Content = null;

            _grid = new TreeGridView
            {
                ShowHeader = true,
                GridLines = GridLines.Both,
                AllowMultipleSelection = true,
                DataStore = _dataStore
            };

            _grid.KeyDown += OnGridKeyDown;
            _grid.SelectionChanged += OnGridSelectionChanged;
            _grid.CellEdited += OnCellEdited;

            if (_showNameColumnCheckbox.Checked == true)
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
            }

            foreach (var colDef in _columns.Where(c => c.IsVisible))
            {
                var columnKey = colDef.Key;
                
                if (colDef.IsDropdown)
                {
                    var statusOptions = new List<string>(colDef.Options);
                    if (!statusOptions.Contains("")) statusOptions.Insert(0, "");

                    _grid.Columns.Add(new GridColumn
                    {
                        HeaderText = columnKey,
                        DataCell = new ComboBoxCell
                        {
                            DataStore = statusOptions,
                            Binding = Binding.Delegate<DatabaseTreeItem, object>(
                                r => r.GetValue(columnKey),
                                (r, val) => { if (val != null) r.SetValue(columnKey, val.ToString()); }
                            )
                        },
                        Editable = true
                    });
                }
                else
                {
                    _grid.Columns.Add(new GridColumn
                    {
                        HeaderText = columnKey,
                        DataCell = new TextBoxCell
                        {
                            Binding = Binding.Delegate<DatabaseTreeItem, string>(
                                r => r.GetValue(columnKey),
                                (r, val) => r.SetValue(columnKey, val)
                            )
                        },
                        Editable = true
                    });
                }
            }

            _gridContainer.Content = _grid;
            RefreshGrid();
        }

        private void OnCellEdited(object? sender, GridViewCellEventArgs e)
        {
            _lastEditedColumn = e.Column;
            var item = e.Item as DatabaseTreeItem;
            if (item != null)
            {
                string header = _grid.Columns[e.Column].HeaderText;
                if (header == "Type") return;
                
                bool isNameCol = header == "Name";
                var colDef = isNameCol ? null : _columns.FirstOrDefault(c => c.Key == header);
                
                if (!isNameCol && colDef == null) return;
                
                var newValue = isNameCol ? item.Name : item.GetValue(colDef.Key);

                var selectedItems = _grid.SelectedItems.OfType<DatabaseTreeItem>().ToList();
                
                // If they disabled Sync, we can use the actual Rhino selection for batch editing!
                var rhSelected = RhinoDoc.ActiveDoc?.Objects.GetSelectedObjects(false, false).Select(o => o.Id).ToList() ?? new List<Guid>();
                
                if (_syncSelectionCheckbox != null && _syncSelectionCheckbox.Checked == false && item.RowModel != null && rhSelected.Contains(item.RowModel.ObjectId) && rhSelected.Count > 1)
                {
                    foreach (var rhId in rhSelected)
                    {
                        var row = _rawObjects.FirstOrDefault(r => r.ObjectId == rhId);
                        if (row != null && row.ObjectId != item.RowModel.ObjectId) 
                        {
                            if (isNameCol) row.ObjectName = newValue;
                            else row.SetUserString(colDef.Key, newValue);
                        }
                    }
                    _grid.ReloadData();
                }
                else if (selectedItems.Contains(item) && selectedItems.Count > 1)
                {
                    foreach (var selected in selectedItems)
                    {
                        if (selected != item) 
                        {
                            if (isNameCol) selected.Name = newValue;
                            else selected.SetValue(colDef.Key, newValue);
                        }
                    }
                    _grid.ReloadData();
                }
            }
        }

        
        private bool MatchesFilter(Models.ObjectRowModel o, string filterText)
        {
            if (string.IsNullOrWhiteSpace(filterText)) return true;

            if (_regexToggle != null && _regexToggle.Checked)
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
private void RefreshGrid()
        {
            if (_grid != null) _grid.DataStore = null;
            _dataStore.Clear();
            
            var filterText = _filterTextBox?.Text?.ToLowerInvariant() ?? "";
            
            // Filter raw objects
            bool hideEmpty = _hideEmptyObjectsCheckbox?.Checked ?? false;
            var filtered = _rawObjects.Where(o => 
                MatchesFilter(o, _filterTextBox?.Text) &&
                (!(_showSelectedOnlyCheckbox?.Checked ?? false) || (RhinoDoc.ActiveDoc?.Objects.FindId(o.ObjectId)?.IsSelected(false) > 0)) &&
                (!hideEmpty || _columns.Any(c => !string.IsNullOrEmpty(o.GetUserString(c.Key))))
            ).ToList();
            
            if (_conduit != null)
            {
                _conduit.TrackedObjects = filtered;
            }

            if (_sortByDropDown != null && _sortByDropDown.SelectedIndex > 0)
            {
                var sortKey = _sortByDropDown.SelectedKey;
                filtered = filtered.OrderBy(o => o.GetUserString(sortKey) ?? "").ToList();
            }

            var activeGroupKeys = new System.Collections.Generic.List<string>();
            if (_groupDropdown1 != null && _groupDropdown1.SelectedIndex > 0) activeGroupKeys.Add(_groupDropdown1.SelectedKey);
            if (_groupDropdown2 != null && _groupDropdown2.SelectedIndex > 0) activeGroupKeys.Add(_groupDropdown2.SelectedKey);
            if (_groupDropdown3 != null && _groupDropdown3.SelectedIndex > 0) activeGroupKeys.Add(_groupDropdown3.SelectedKey);
            
            activeGroupKeys = activeGroupKeys.Distinct().ToList();

            if (activeGroupKeys.Count == 0)
            {
                foreach (var o in filtered) _dataStore.Add(new DatabaseTreeItem(o));
            }
            else
            {
                var groupedItems = GroupItems(filtered, activeGroupKeys, 0);
                foreach (var g in groupedItems) _dataStore.Add(g);
            }
            
            if (_grid != null) _grid.DataStore = _dataStore;
            if (_conduit != null && _conduit.Enabled) RhinoDoc.ActiveDoc?.Views.Redraw();
        }

        private void OnGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter && _grid.SelectedRow >= 0)
            {
                e.Handled = true;
                
                int colToEdit = _lastEditedColumn;
                
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
                
                _grid.BeginEdit(_grid.SelectedRow, colToEdit);
            }
        }
        
        private void OnGridSelectionChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingSelection || RhinoDoc.ActiveDoc == null || (_syncSelectionCheckbox != null && _syncSelectionCheckbox.Checked == false)) return;
            _isUpdatingSelection = true;
            
            RhinoDoc.ActiveDoc.Objects.UnselectAll();
            
            foreach (var item in _grid.SelectedItems)
            {
                if (item is DatabaseTreeItem dti && !dti.IsGroup && dti.RowModel != null)
                {
                    RhinoDoc.ActiveDoc.Objects.Select(dti.RowModel.ObjectId, true, true);
                }
                else if (item is DatabaseTreeItem group)
                {
                    SelectGroupChildren(group);
                }
            }
            
            RhinoDoc.ActiveDoc.Views.Redraw();
            _isUpdatingSelection = false;
        }

        private void SelectGroupChildren(DatabaseTreeItem group)
        {
            if (RhinoDoc.ActiveDoc == null) return;
            foreach (var child in group.Children)
            {
                if (child is DatabaseTreeItem cdti && !cdti.IsGroup && cdti.RowModel != null)
                    RhinoDoc.ActiveDoc.Objects.Select(cdti.RowModel.ObjectId, true, true);
                else if (child is DatabaseTreeItem cGroup)
                    SelectGroupChildren(cGroup);
            }
        }

        private void ExportToCsv()
        {
            var dialog = new Eto.Forms.SaveFileDialog { Filters = { new FileFilter("CSV Files", ".csv") } };
            if (dialog.ShowDialog(this) == DialogResult.Ok)
            {
                using (var writer = new System.IO.StreamWriter(dialog.FileName))
                {
                    var headers = new List<string> { "ObjectId", "Name", "Type" };
                    headers.AddRange(_columns.Where(c => c.IsVisible).Select(c => c.Key));
                    writer.WriteLine(string.Join(",", headers.Select(EscapeCsv)));
                    
                    foreach (var obj in _rawObjects)
                    {
                        var row = new List<string> { obj.ObjectId.ToString(), obj.ObjectName, obj.ObjectType };
                        foreach (var col in _columns.Where(c => c.IsVisible))
                        {
                            row.Add(EscapeCsv(obj.GetUserString(col.Key)));
                        }
                        writer.WriteLine(string.Join(",", row));
                    }
                }
            }
        }

        private void ImportFromCsv()
        {
            var dialog = new Eto.Forms.OpenFileDialog { Filters = { new FileFilter("CSV Files", ".csv") } };
            if (dialog.ShowDialog(this) == DialogResult.Ok)
            {
                using (var reader = new System.IO.StreamReader(dialog.FileName))
                {
                    var headerLine = reader.ReadLine();
                    if (headerLine == null) return;
                    var headers = ParseCsvLine(headerLine);
                    
                    int idIndex = headers.IndexOf("ObjectId");
                    if (idIndex < 0) { MessageBox.Show("Invalid CSV format: Missing ObjectId column", "Import Error", MessageBoxButtons.OK); return; }
                    
                    var userTextHeaders = headers.Skip(3).ToList();
                    bool columnsAdded = false;
                    
                    foreach(var key in userTextHeaders) {
                        if (!_columns.Any(c => c.Key == key)) {
                            _columns.Add(new ColumnDefinition { Key = key, IsDropdown = false, Options = new List<string>() });
                            columnsAdded = true;
                        }
                    }
                    
                    if (columnsAdded)
                    {
                        InitializeGrid();
                    }
                    
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrEmpty(line)) continue;
                        
                        var values = ParseCsvLine(line);
                        if (values.Count <= idIndex) continue;
                        
                        if (Guid.TryParse(values[idIndex], out Guid id))
                        {
                            var rhObj = RhinoDoc.ActiveDoc?.Objects.FindId(id);
                            if (rhObj != null)
                            {
                                for (int i = 3; i < headers.Count && i < values.Count; i++)
                                {
                                    var key = headers[i];
                                    var val = values[i];
                                    rhObj.Attributes.SetUserString(key, val);
                                }
                                rhObj.CommitChanges();
                            }
                        }
                    }
                    
                    LoadAllObjects(); // Reload to pick up fresh user strings
                }
            }
        }

        private string EscapeCsv(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            return field;
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var currentField = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        currentField.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            result.Add(currentField.ToString());
            return result;
        }

        
        private void UpdateDynamicDropdowns()
        {
            var visibleCols = _columns.Where(c => c.IsVisible).ToList();
            
            var prevEdit = _editColumnDropDown?.SelectedKey;
            if (_editColumnDropDown != null) {
                _editColumnDropDown.Items.Clear();
                _editColumnDropDown.Items.Add("--- Select Column ---");
                foreach (var c in visibleCols) _editColumnDropDown.Items.Add(c.Key);
                var existingEdit = _editColumnDropDown.Items.FirstOrDefault(i => i.Text == prevEdit);
                if (existingEdit != null) _editColumnDropDown.SelectedKey = existingEdit.Key;
                else _editColumnDropDown.SelectedIndex = 0;
            }


            
            void populateGroup(DropDown dd) {
                var prev = dd?.SelectedKey;
                if (dd != null) {
                    dd.Items.Clear();
                    dd.Items.Add("None");
                    foreach (var c in visibleCols) dd.Items.Add(c.Key);
                    var existing = dd.Items.FirstOrDefault(i => i.Text == prev);
                    if (existing != null) dd.SelectedKey = existing.Key;
                    else if (dd.Items.Count > 0) dd.SelectedIndex = 0;
                }
            }
            populateGroup(_groupDropdown1);
            populateGroup(_groupDropdown2);
            populateGroup(_groupDropdown3);

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

        private void RefreshSettingsGrid() {
            _settingsDataStore.Clear();
            foreach (var c in _columns) _settingsDataStore.Add(c);
            if (_settingsGrid != null) { _settingsGrid.DataStore = null; _settingsGrid.DataStore = _settingsDataStore; }
        }
        
        public void PanelShown(uint documentSerialNumber, ShowPanelReason reason) { }
        public void PanelHidden(uint documentSerialNumber, ShowPanelReason reason) { }
        public void PanelClosing(uint documentSerialNumber, bool documentIsClosing)
        {
            RhinoDoc.AddRhinoObject -= OnAddObject;
            RhinoDoc.DeleteRhinoObject -= OnDeleteObject;
            _conduit.Enabled = false;
        }
    }
}
