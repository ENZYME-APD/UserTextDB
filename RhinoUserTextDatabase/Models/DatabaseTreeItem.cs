using System;
using System.Collections.Generic;
using Eto.Forms;
using Rhino.DocObjects;

namespace RhinoUserTextDatabase.Models
{
    public class DatabaseTreeItem : TreeGridItem
    {
        public bool IsGroup { get; set; }
        public string GroupName { get; set; }
        public ObjectRowModel? RowModel { get; set; }

        public DatabaseTreeItem(string groupName)
        {
            IsGroup = true;
            GroupName = groupName;
        }

        public DatabaseTreeItem(ObjectRowModel rowModel)
        {
            IsGroup = false;
            GroupName = string.Empty;
            RowModel = rowModel;
        }

        public string Name
        {
            get => IsGroup ? GroupName : RowModel?.ObjectName ?? "";
            set
            {
                if (!IsGroup && RowModel != null)
                {
                    RowModel.ObjectName = value;
                }
            }
        }
        public string Type => IsGroup ? $"({Children.Count} items)" : RowModel?.ObjectType ?? "";

        public string GetValue(string key)
        {
            if (IsGroup)
            {
                if (Children.Count == 0) return "";
                string firstVal = ((DatabaseTreeItem)Children[0]).GetValue(key);
                for (int i = 1; i < Children.Count; i++)
                {
                    if (((DatabaseTreeItem)Children[i]).GetValue(key) != firstVal)
                        return "*VARIES*";
                }
                return firstVal;
            }
            return RowModel?.GetUserString(key) ?? "";
        }

        public void SetValue(string key, string value)
        {
            if (IsGroup)
            {
                foreach (var child in Children)
                {
                    if (child is DatabaseTreeItem dti && !dti.IsGroup)
                    {
                        dti.SetValue(key, value);
                    }
                }
            }
            else
            {
                RowModel?.SetUserString(key, value);
            }
        }
    }
}
