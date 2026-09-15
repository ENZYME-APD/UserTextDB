import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

# I need to find the existing time bomb block and replace it
pattern = re.compile(r'bool isUnlocked = RhinoUserTextDatabasePlugIn.*?Content = splitter;\n            }', re.DOTALL)

fixed_code = """bool isUnlocked = RhinoUserTextDatabasePlugIn.Instance?.Settings.GetBool("BetaUnlocked", false) ?? false;
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
            }"""

content = pattern.sub(fixed_code, content)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
