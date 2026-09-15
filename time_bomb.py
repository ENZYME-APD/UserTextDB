import re

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'r') as f:
    content = f.read()

time_bomb_code = """
            bool isUnlocked = RhinoUserTextDatabasePlugIn.Instance?.Settings.GetBool("BetaUnlocked", false) ?? false;
            DateTime expirationDate = new DateTime(2027, 1, 1);
            
            if (!isUnlocked && DateTime.Now > expirationDate)
            {
                var expLayout = new DynamicLayout { DefaultSpacing = new Size(10, 10), Padding = new Padding(20) };
                expLayout.AddCentered(new Label { Text = "The Beta period for UserText DB has expired.", Font = new Font(SystemFont.Bold, 12) });
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
                
                var inputStack = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 5, Items = { passInput, btnUnlock } };
                expLayout.AddCentered(inputStack);
                expLayout.AddRow(null);
                Content = expLayout;
            }
            else
            {
                Content = splitter;
            }
"""

content = content.replace("Content = splitter;", time_bomb_code)

with open('RhinoUserTextDatabase/UI/UserTextDatabasePanel.cs', 'w') as f:
    f.write(content)
