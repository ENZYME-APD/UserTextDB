import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    lines = f.readlines()

start_idx = -1
end_idx = -1

for i, line in enumerate(lines):
    if "var settingsLayout = new DynamicLayout" in line:
        start_idx = i
    if "UpdateEditDropDown();" in line and start_idx != -1:
        end_idx = i
        break

if start_idx == -1 or end_idx == -1:
    print("Could not find bounds")
    exit(1)

new_code = """
            var settingsLayout = new TableLayout { Spacing = new Size(5, 5), Padding = new Padding(10) };
            
            Control footerControl = new Panel();
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var logoImg = Eto.Drawing.Bitmap.FromResource("RhinoUserTextDatabase.Resources.logo.png", asm);
                
                var logoView = new ImageView { Image = logoImg, Size = new Eto.Drawing.Size(120, 38) };
                
                var linkSite = new LinkButton { Text = "www.weareenzyme.com" };
                linkSite.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.weareenzyme.com", UseShellExecute = true });
                
                var linkEmail = new LinkButton { Text = "digital@weareenzyme.com" };
                linkEmail.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "mailto:digital@weareenzyme.com", UseShellExecute = true });
                
                var lblVersion = new Label { Text = "v1.0.0 Beta", TextColor = Eto.Drawing.Colors.Gray };
                
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

            settingsLayout.Rows.Add(new TableRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { _newKeyTextBox, btnAddColumn } }));
            settingsLayout.Rows.Add(new TableRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 15, Items = { _showNameColumnCheckbox, _showTypeColumnCheckbox } }));
            settingsLayout.Rows.Add(new TableRow { Cells = { _settingsGrid }, ScaleHeight = true });
            settingsLayout.Rows.Add(new TableRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { btnMoveUp, btnMoveDown, btnDeleteCol } }));
            settingsLayout.Rows.Add(new TableRow(new Panel { Height = 10 })); // padding
            settingsLayout.Rows.Add(new TableRow(footerControl));

"""

lines = lines[:start_idx] + [new_code] + lines[end_idx:]

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.writelines(lines)
