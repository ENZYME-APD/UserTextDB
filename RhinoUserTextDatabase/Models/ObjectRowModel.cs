using System;
using Rhino.DocObjects;

namespace RhinoUserTextDatabase.Models
{
    public class ObjectRowModel
    {
        private readonly RhinoObject _rhinoObject;

        public ObjectRowModel(RhinoObject rhinoObject)
        {
            _rhinoObject = rhinoObject;
        }

        public RhinoObject RhinoObject => _rhinoObject;

        public Guid ObjectId => _rhinoObject.Id;
        
        public string ObjectName
        {
            get => _rhinoObject.Name ?? "Unnamed";
            set
            {
                _rhinoObject.Attributes.Name = value;
                _rhinoObject.CommitChanges();
            }
        }
        
        public string ObjectType => _rhinoObject.ObjectType.ToString();

        public string GetUserString(string key)
        {
            return _rhinoObject.Attributes.GetUserString(key) ?? string.Empty;
        }

        public void SetUserString(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                _rhinoObject.Attributes.DeleteUserString(key);
            }
            else
            {
                _rhinoObject.Attributes.SetUserString(key, value);
            }
            _rhinoObject.CommitChanges();
        }
    }
}
