using System.Collections.Generic;
using Rhino.Display;
using RhinoUserTextDatabase.Models;

namespace RhinoUserTextDatabase.UI
{
    public class DatabaseDisplayConduit : DisplayConduit
    {
        public string ActiveColumn { get; set; } = "";
        public List<ObjectRowModel> TrackedObjects { get; set; } = new List<ObjectRowModel>();
        
        private Dictionary<string, System.Drawing.Color> _colorMap = new Dictionary<string, System.Drawing.Color>();
        private Dictionary<System.Drawing.Color, DisplayMaterial> _materials = new Dictionary<System.Drawing.Color, DisplayMaterial>();
        private System.Drawing.Color[] _palette = new[] 
        { 
            System.Drawing.Color.Cyan, 
            System.Drawing.Color.Magenta, 
            System.Drawing.Color.Yellow, 
            System.Drawing.Color.Lime, 
            System.Drawing.Color.Orange, 
            System.Drawing.Color.HotPink 
        };
        
        protected override void PostDrawObjects(DrawEventArgs e)
        {
            if (string.IsNullOrEmpty(ActiveColumn) || TrackedObjects == null) return;
            
            var oldBias = e.Display.ZBiasMode;
            e.Display.ZBiasMode = ZBiasMode.TowardsCamera;
            e.Display.PushDepthTesting(true);

            foreach (var obj in TrackedObjects)
            {
                var val = obj.GetUserString(ActiveColumn);
                if (string.IsNullOrEmpty(val)) continue;
                
                if (!_colorMap.ContainsKey(val))
                {
                    _colorMap[val] = _palette[_colorMap.Count % _palette.Length];
                }
                
                var color = _colorMap[val];
                if (!_materials.ContainsKey(color))
                {
                    // Create a material with slight transparency so you can still read the object form easily
                    _materials[color] = new DisplayMaterial(color, 1.0 - 0.7); // 30% transparency
                }
                var mat = _materials[color];

                var rhObj = Rhino.RhinoDoc.ActiveDoc.Objects.FindId(obj.ObjectId);
                if (rhObj != null && rhObj.Geometry != null)
                {
                    // Draw meshes for surface/brep/extrusion geometry
                    var meshes = rhObj.GetMeshes(Rhino.Geometry.MeshType.Render);
                    if (meshes != null && meshes.Length > 0)
                    {
                        foreach (var mesh in meshes)
                        {
                            e.Display.DrawMeshShaded(mesh, mat);
                        }
                    }
                    else if (rhObj.Geometry is Rhino.Geometry.Curve curve)
                    {
                        e.Display.DrawCurve(curve, color, 3);
                    }
                    else if (rhObj.Geometry is Rhino.Geometry.Point pt)
                    {
                        e.Display.DrawPoint(pt.Location, color);
                    }
                }
            }

            e.Display.PopDepthTesting();
            e.Display.ZBiasMode = oldBias;
        }
    }
}
