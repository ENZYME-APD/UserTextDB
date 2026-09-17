# UserTextDB

![Version](https://img.shields.io/badge/version-1.0.0_Beta-blue.svg)
![Platform](https://img.shields.io/badge/platform-Rhino_8-black.svg)
![Brand](https://img.shields.io/badge/by-Enzyme-4CAF50.svg)

**UserText DB** is a powerful, lightning-fast spreadsheet interface for managing Rhino User Text attributes. Built by [Enzyme](https://www.weareenzyme.com/), it replaces Rhino's native, one-object-at-a-time attribute panel with an interactive, Excel-style grid that makes handling metadata, BIM information, and model data effortless.

## 📖 Our Story: Light-BIM & Data-Driven Design
At Enzyme, we have spent years experimenting with the concept of **"Light-BIM"**. Influenced by our 20-year experience using Archicad, and inspired by Grasshopper mentor [Ismael Sanz](https://www.linkedin.com/in/ismasanz/)'s obsession with data-driven design workflows (and Elefront), we’ve developed workflows that rely heavily on the key-value pairs of UserText in Rhino. 

Whether it is masterplanning, architectural design, or developing complex geometrical features, we leverage this data to enable computational workflows for geometry automation and data extraction. Our philosophy is simple: **Minimal input, maximum output.**

Because we seamlessly integrate our Rhino models with highly developed BIM models—interchanging data bi-directionally—we needed a tool that could handle massive amounts of metadata. Rhino's native properties panel couldn't keep up. That is why we decided to design our own Rhino plugin: to multiply the usability of Rhino's native features and enable proper spreadsheet/database workflows with absolute ease.

## 🚀 Key Features

* **Excel-Style Data Grid:** View and edit UserText keys across your entire Rhino model in a clean, tabular format. Use arrow keys to navigate and `Enter` to instantly toggle editing.
* **Smart Dropdowns:** Transform any column into a dropdown list. The plugin automatically scans your model and populates the dropdown options with existing unique values—no manual entry required.
* **Batch Editing:** Apply a value (or dropdown selection) to hundreds of selected objects instantly via the Batch Data tab.
* **Two-Way Selection Sync:** Select rows in the grid to select the corresponding geometry in Rhino, and vice versa.
* **Advanced "Smart Search":** A highly intuitive filtering engine that supports comma-separated `AND` logic, `OR` operators, `-` exclusions, targeted `column:search`, and a dedicated Regex `[.*]` mode.
* **CSV Import/Export:** Export your entire model's metadata to Excel, make bulk modifications, and seamlessly import the updated data back onto your Rhino geometry.
* **Full Column Management:** Add, delete, reorder, and hide columns without losing track of your data structure.

## 🛠 Installation

UserText DB is published to McNeel's Yak package manager. 
1. Open Rhino 8.
2. Type `PackageManager` in the command line.
3. Search for **RhinoUserTextDatabase**.
4. Click **Install** and restart Rhino.
5. Run the command `UserTextDB` to open the panel!

## 🔍 Smart Search Syntax Guide

The filter box uses a powerful parser to help you find exactly what you need.

| Feature | Syntax Example | Description |
|---|---|---|
| **AND (Commas)** | `timber, phase 6` | Shows objects that contain both "timber" AND "phase 6". |
| **OR (Pipes/OR)** | `timber OR steel` | Shows objects containing either "timber" or "steel". |
| **Combine AND/OR** | `timber OR steel, phase 6` | Must contain (timber OR steel) AND (phase 6). |
| **Exclude (-)** | `timber, -approved` | Must contain "timber" but must NOT contain "approved". |
| **Targeted Columns** | `mat:timber, status:pending`| Only searches for "timber" in the Material column. |

**Regex Mode:** Click the `.*` toggle next to the search box to switch to pure Regular Expressions mode for advanced pattern matching (e.g., `^(wood|steel).*phase [1-3]$`).

## ✉️ Support & Contact

Built with ❤️ by Enzyme. 
* **Website:** [www.weareenzyme.com](https://www.weareenzyme.com/)
* **Contact:** digital@weareenzyme.com
