# UserTextDB
Excel-style database management for Rhino User Text.

## Overview
UserText DB provides a dynamic, spreadsheet-like interface directly inside Rhino to view, select, batch-edit, and organize object metadata (UserText).

## Features
- **Dynamic Grouping:** Group objects by any metadata column, up to 3 levels deep.
- **Batch Editing:** Easily apply a specific UserText value to all selected objects simultaneously.
- **Save States:** Save your grid layout, column visibility, and grouping configurations directly into the Rhino document.
- **Advanced Filtering & Selection:** Use powerful search strings and regex to filter your grid and quickly select objects in the 3D model.
- **CSV Export/Import:** Seamlessly move metadata in and out of Excel.

---

## 🔍 Search & Filtering Cheatsheet

### Basic Search
Type any text into the filter box to instantly hide rows that don't contain your search string.
- *Example:* Typing `wood` will show only objects with "wood" in any column.

### Multi-Term Search
Separate words with commas to search for multiple terms at once (OR logic).
- *Example:* `timber, steel` matches objects that contain either "timber" OR "steel".

### Exclusion (NOT)
Prefix a term with a minus `-` to hide rows containing that term.
- *Example:* `-phase 1` will hide any objects belonging to Phase 1.

### Column-Specific Search
Use `columnName:searchTerm` to restrict your search to a specific column.
- *Example:* `mat:wood` searches for "wood" only in the Material column (the column name check is partial, so `mat:` matches `Material`).

### Regular Expressions (Regex Mode)
Enable the `.*` toggle button next to the search bar to use powerful Regular Expressions.
- `^Wall` : Starts with 'Wall'
- `Floor|Roof` : Matches 'Floor' OR 'Roof'
- `\d+` : Matches any number

---
*Created by Enzyme APD*
