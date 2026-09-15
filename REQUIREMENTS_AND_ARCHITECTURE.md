# Rhino User Text Database Plugin: Requirements & Architecture Summary

## 1. Project Overview
The objective is to develop a cross-platform (Windows & macOS) C# plugin for Rhino 8 that replaces the standard Attribute User Text interface with an Excel-style, database-like management suite.
The plugin provides advanced features for architectural, BIM, and computational workflows—such as batch key editing, schema enforcement, grouping, viewport display overlays, and two-way external data synchronization.

## 2. Requirements Matrix & Proposed Solutions

### UI & User Experience (UX)
| Requirement | Proposed Technical Solution |
| :--- | :--- |
| Cross-Platform Compatibility | Build UI using Eto.Forms (Rhino.UI.IPanels) compiled in .NET 7 / .NET Core for native execution on both Windows and macOS. |
| Rhino Theme Integration | Call `this.UseRhinoStyle()` on all Eto containers to inherit Rhino's light/dark system palettes automatically. |
| Excel-Style Grid Navigation | Implement an editable TreeGridView with key-listener overrides (Keys.Enter, Keys.Tab) to advance focus vertically/horizontally across cells. |
| Predefined Key Dropdowns | Use Eto.Forms.ComboBoxCell bound to strict schema definitions to enforce standard value inputs (e.g., Status, Material, Phase). |
| Dockable Panel Integration | Register panel GUIDs inside the PlugIn class during initialization via `Panels.RegisterPanel()`. |

### Database View & Hierarchy Management
| Requirement | Proposed Technical Solution |
| :--- | :--- |
| Flat Mode vs. Grouped Mode | Implement a flexible view model using TreeGridView capable of rendering flat object rows or hierarchical tree structures. |
| Single-Key Grouping | Group objects under expandable parent nodes based on shared values of a single target key (e.g., Group by Material). |
| Match-All-Keys Grouping | Concatenate all predefined key values into a hash key to aggregate objects sharing identical metadata across all fields. |
| Batch Parent Edits | Apply updates at parent node cells down to all child RhinoObject references simultaneously, committing updates in a single loop. |
| Conflict Handling (*VARIES*) | Evaluate child values during render; if values differ among group members, display *VARIES* until overwritten. |

### Viewport Interactivity & Intelligence
| Requirement | Proposed Technical Solution |
| :--- | :--- |
| Non-Destructive Colorization | Implement a custom Rhino.Display.DisplayPipeline conduit (DisplayConduit) to draw semi-transparent shaded mesh/Brep overlays in real time based on metadata values (e.g., Status colors). |
| Metadata Selection & Isolation | Add contextual commands utilizing `RhinoDoc.Objects.FindByUserString()` to query and select or hide/isolate objects matching UI focus. |
| Real-time Event Synchronization | Subscribe to RhinoDoc.SelectObjects, DeselectObjects, and DeselectAllObjects events to keep the grid synced with viewport actions. |

### Data Interoperability & IO
| Requirement | Proposed Technical Solution |
| :--- | :--- |
| Native Excel (.xlsx) Support | Integrate the lightweight ClosedXML NuGet package for headless reading and writing of openxml spreadsheets. |
| CSV & JSON Exchange | Build an asymmetric parsing service (DataExchangeManager) to export/import metadata tables mapped by Rhino Object GUID (ObjectId). |
| Two-Way Synchronization | Read external schemas, lookup geometry by Guid, and invoke `rhinoObject.Attributes.SetUserString()` followed by `rhinoObject.CommitChanges()`. |

## 3. Technology Stack & Project Structure
- **Language & Framework:** C# / .NET 7 / .NET Core
- **Core APIs:** RhinoCommon (Rhino.DocObjects, Rhino.Display, Rhino.Commands)
- **UI Toolkit:** Eto.Forms & Rhino.UI
- **Third-Party Packages:** ClosedXML (Excel generation), System.Text.Json
- **Deployment/Distribution:** .yak Package Manager file format

### Proposed Solution Architecture
```
RhinoUserTextDatabase/
├── PlugIn.cs                   # Plugin entry point & panel registration
├── Data/
│   ├── SchemaDefinition.cs     # Predefined key/value JSON models
│   └── DataExchangeManager.cs  # Excel, CSV, and JSON IO engines
├── Display/
│   └── UserTextColorConduit.cs # Viewport DisplayConduit for visual audits
├── Models/
│   ├── ObjectRowModel.cs       # Flat data model for standard grid rows
│   └── DatabaseTreeItem.cs     # Hierarchical model for tree/group views
└── UI/
    ├── UserTextDatabasePanel.cs# Main Eto panel & event subscriptions
    └── Controls/
        └── CustomGridCells.cs  # Specialized ComboBoxCell and navigation rules
```

## 4. Implementation Roadmap
- **Phase 1: Foundation**
  - Setup C# project with RhinoCommon & Eto.Forms templates
  - Create basic dockable panel and register selection event hooks
- **Phase 2: Data Model & Grid**
  - Build ObjectRowModel and bind to Eto.Forms GridView
  - Implement Excel key navigation listeners (Enter / Tab behavior)
- **Phase 3: Hierarchical Grouping & Dropdowns**
  - Upgrade GridView to TreeGridView
  - Add single-key and match-all-keys grouping logic
  - Implement ComboBoxCell for predefined value lists
- **Phase 4: Viewport Interactivity**
  - Develop DisplayConduit for real-time color overlays
  - Implement "Select by Key" and "Isolate by Key" viewport commands
- **Phase 5: IO & Package Deployment**
  - Integrate ClosedXML for .xlsx export/import
  - Build CSV and JSON DataExchangeManager engines
  - Package build artifacts into a ready-to-distribute .yak package
```
