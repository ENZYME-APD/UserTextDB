using System;
using System.Runtime.InteropServices;
using Rhino;
using Rhino.PlugIns;
using Rhino.UI;

[assembly: Guid("5E4B2F8A-7C1D-4E9B-9A8C-3D2F1B0A9C8D")]

namespace RhinoUserTextDatabase
{
    public class RhinoUserTextDatabasePlugIn : PlugIn
    {
        public RhinoUserTextDatabasePlugIn()
        {
            Instance = this;
        }

        public static RhinoUserTextDatabasePlugIn? Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            try
            {
                System.Drawing.Icon? icon = null;
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("RhinoUserTextDatabase.icon.ico"))
                {
                    if (stream != null)
                    {
                        icon = new System.Drawing.Icon(stream);
                    }
                }

                Panels.RegisterPanel(this, typeof(UI.UserTextDatabasePanel), "UserText DB (Beta)", icon);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Eto.Forms.MessageBox.Show($"Error registering panel: {ex.Message}");
                return LoadReturnCode.ErrorNoDialog;
            }
            return LoadReturnCode.Success;
        }
    }
}
