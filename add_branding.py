import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

target = """            settingsLayout.AddRow(_settingsGrid);
            settingsLayout.BeginVertical();
            settingsLayout.AddRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { btnMoveUp, btnMoveDown, btnDeleteCol } });
            settingsLayout.EndVertical();"""

branding_code = """            settingsLayout.AddRow(_settingsGrid);
            settingsLayout.BeginVertical();
            settingsLayout.AddRow(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { btnMoveUp, btnMoveDown, btnDeleteCol } });
            settingsLayout.EndVertical();
            
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var logoImg = Eto.Drawing.Bitmap.FromResource("RhinoUserTextDatabase.Resources.logo.png", asm);
                
                var logoView = new ImageView { Image = logoImg, Size = new Eto.Drawing.Size(150, 48) };
                
                var linkSite = new LinkButton { Text = "www.weareenzyme.com" };
                linkSite.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.weareenzyme.com", UseShellExecute = true });
                
                var linkEmail = new LinkButton { Text = "digital@weareenzyme.com" };
                linkEmail.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "mailto:digital@weareenzyme.com", UseShellExecute = true });
                
                var lblVersion = new Label { Text = "v1.0.0 Beta", TextColor = Eto.Drawing.Colors.Gray };
                
                var brandStack = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 5,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Items = {
                        new StackLayoutItem(logoView, HorizontalAlignment.Center),
                        new StackLayoutItem(linkSite, HorizontalAlignment.Center),
                        new StackLayoutItem(linkEmail, HorizontalAlignment.Center),
                        new StackLayoutItem(lblVersion, HorizontalAlignment.Center)
                    }
                };
                
                settingsLayout.AddRow(null); // padding
                settingsLayout.AddRow(brandStack);
                settingsLayout.AddRow(null); // padding
            }
            catch (System.Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error loading branding: {ex.Message}");
            }"""

content = content.replace(target, branding_code)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
