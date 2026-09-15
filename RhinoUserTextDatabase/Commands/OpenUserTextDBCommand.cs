using System;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace RhinoUserTextDatabase.Commands
{
    public class OpenUserTextDBCommand : Command
    {
        public OpenUserTextDBCommand()
        {
            Instance = this;
        }

        public static OpenUserTextDBCommand? Instance { get; private set; }

        public override string EnglishName => "UserTextDB";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var panelId = UI.UserTextDatabasePanel.PanelId;
            RhinoApp.WriteLine($"Attempting to open panel with ID: {panelId}");
            
            try
            {
                Panels.OpenPanel(panelId);
                if (!Panels.IsPanelVisible(panelId))
                {
                    Eto.Forms.MessageBox.Show("Panel was requested to open, but is still not visible! Did it fail to register?", "Panel Error");
                }
            }
            catch (Exception ex)
            {
                Eto.Forms.MessageBox.Show($"Exception when opening panel: {ex.Message}");
            }
            
            return Result.Success;
        }
    }
}
