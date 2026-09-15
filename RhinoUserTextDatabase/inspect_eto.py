import System
import clr
clr.AddReference("/Applications/Rhino 8.app/Contents/Frameworks/RhCore.framework/Resources/Eto.dll")
import Eto.Forms

grid_type = Eto.Forms.TreeGridView
for member in dir(grid_type):
    if "Edit" in member or "Cell" in member or "Row" in member or "Key" in member:
        print(member)
