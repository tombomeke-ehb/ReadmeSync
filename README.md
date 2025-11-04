# 🧩 ReadmeSync
> ⚙️ A lightweight C# CLI that automatically generates or updates **README** / **ROADMAP** files  
> based on your project’s actual source code — namespaces, classes, public methods, and // TODO:s.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](#)
[![License](https://img.shields.io/badge/license-Tombomeke%20Studios%20(MIT%20Modified)-orange)](#license)
[![Build](https://img.shields.io/badge/GitHub%20Actions-Publish%20on%20Tag-green)](#-automated-publishing)
[![Release](https://img.shields.io/github/v/release/tombomeke-ehb/ReadmeSync?color=blue&label=latest)](https://github.com/tombomeke-ehb/ReadmeSync/releases)
[![Issues](https://img.shields.io/github/issues/tombomeke-ehb/ReadmeSync)](https://github.com/tombomeke-ehb/ReadmeSync/issues)
[![Stars](https://img.shields.io/github/stars/tombomeke-ehb/ReadmeSync?style=social)](https://github.com/tombomeke-ehb/ReadmeSync)

---

## ✨ Features
- 🧠 Automatically documents your C# project structure  
- 🧩 Merges updates directly into existing README / ROADMAP files  
- 🔗 Optionally adds clickable GitHub file links  
- ⚙️ Supports namespaces, classes, public methods, and // TODO: parsing  
- 🚀 CLI-based — no dependencies, no setup required  

---

## 🧰 Requirements
- .NET 8.0 SDK or higher  
- Windows, macOS, or Linux  
- A C# project containing `.cs` files  

---

## 🚀 Overview
ReadmeSync scans your C# project for `.cs` files and builds a structured overview of:
- Namespaces  
- Classes  
- Public methods  
- // TODO: comments  

It then merges this overview into an existing Markdown file, keeping your manual section above the marker and replacing everything below it.

<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

Originally created for **RPG Manager** by **Tombomeke Studios**, later expanded into a general-purpose automation tool.

---

## 🛠 Installation

### 🧩 Option 1 — via [NuGet](https://www.nuget.org/packages/ReadmeSync) (recommended)

Install the tool globally on your system:
```
dotnet tool install -g ReadmeSync
```
Once installed, you can run it from anywhere:
```
readmesync . README.md
```
Or, to generate a roadmap with clickable GitHub links:
```
readmesync . ROADMAP.md https://github.com/YourName/YourRepo
```
Update the tool anytime with:
```
dotnet tool update -g ReadmeSync
```
---

### 🧰 Option 2 — Build from source
If you want to modify or debug ReadmeSync locally:
```
git clone https://github.com/tombomeke-ehb/ReadmeSync.git  
cd ReadmeSync  
dotnet run --project ReadmeSync -- . README.md
```

💡 Tip: You can use ReadmeSync as part of an automated workflow (e.g., GitHub Actions or commit hooks)
to keep your documentation always in sync with your source code.

---

## ⚙️ Usage
readmesync [project-root] [output-file] [optional-repo-url]

Argument | Required | Description | Example
--------- | -------- | ------------ | --------
project-root | ✅ | Directory to scan recursively for .cs files | . or C:\Repos\MyApp
output-file | ✅ | Markdown file to write/update | README.md, ROADMAP.md, docs/OVERVIEW.md
optional-repo-url | ❌ | Base repo URL for clickable file links | https://github.com/YourName/YourRepo

Behavior details:
- Automatically detects the repository root (.git, .sln, README.md, or .github/)
- Keeps everything above the marker and regenerates the section below it
- Links files relative to your repository URL (if provided)

---

## 🧭 Manual Section (kept intact)
You can write your own notes or roadmap above this marker.  
Everything below it will be auto-generated and replaced on each run.

# My Project Roadmap
- Phase 1: Core systems  
- Phase 2: UI polish  
- Phase 3: Release  

<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

---

## 🔍 What the tool generates
- Timestamp  
- Project statistics (namespaces, classes, methods, TODOs)  
- Sections per namespace  
- For each class:  
  - A heading (linked to the repo, if provided)  
  - List of public methods  
  - Detected // TODO: items  

---

## 🧪 Example output
<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

# 🧮 Code Overview (auto-generated)
_Last updated: 2025-11-04 15:00_

📊 11 Namespaces · 27 Classes · 17 Methods · 0 TODOs

## 🧱 MyApp.Core
### Program.cs
Public Methods:
- Main()

---

## 🧰 Typical workflows
Update README in the repo root:
readmesync . README.md

Generate a separate ROADMAP:
readmesync "C:\Repos\GameEngine" "ROADMAP.md"

Add clickable GitHub links:
readmesync . README.md https://github.com/YourName/YourRepo

---

## 🧾 Command reference
readmesync --help

Usage:  
readmesync [project-root] [output-file] [optional-repo-url]

Examples:  
readmesync . README.md  
readmesync . ROADMAP.md https://github.com/YourName/YourRepo  

---

## 🖼 Demo
Example of ReadmeSync generating a roadmap (coming soon...)

---

## 🧩 Notes & limitations
- Designed for C# projects (regex-based parsing)  
- Extendable to other languages  
- Skips files without a class definition  
- Constructors are ignored as methods  
- The tool regenerates below the marker each time  

---

## 🧭 Future roadmap
- [ ] Config file (`readmesync.json`)  
- [ ] Support for TypeScript & Java parsing  
- [ ] Optional Git commit hook for auto-sync  
- [ ] Template themes for README generation  

---

## 🧾 Changelog
**v1.0.0 – Initial release**  
- Basic scanning for namespaces, classes, and methods  
- README/ROADMAP auto-generation  
- Repository link support  

---

## 🤝 Contributing
Suggestions and pull requests are welcome!  
Potential future improvements include:
- Config file support (ReadmeSync.json)
- Multi-language parsing (TypeScript, Java, Python)
- Custom CLI flags (--no-todo, --summary-only, etc.)
- Theming and templating support

---

## 🌍 Related projects
- [RPG Manager](https://github.com/tombomeke-ehb/RPGManager) — the original project that inspired ReadmeSync  
- [Tombomeke Studios](https://github.com/tombomeke-ehb) — more personal tools and experiments  

---

## 👤 Author
**Tom Dekoning**  
🎯 **Tombomeke Studios**  
Creator of **RPG Manager** and developer of **ReadmeSync**.

---

## 🪪 License
**Tombomeke Studios License (MIT-Modified)**  

© 2025 Tom Dekoning — Tombomeke Studios. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy  
of this software and associated documentation files (the “Software”), to use,  
study, and modify the Software for **personal or internal educational purposes only**,  
subject to the following conditions:

- The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.  
- Redistribution of the Software, in original or modified form, is **not permitted** without prior written consent from Tombomeke Studios.  
- The Software may **not** be sold, sublicensed, or used in any commercial product or service.  
- Credit must be given where used:  
  “ReadmeSync by Tombomeke Studios (Tom Dekoning)”.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,  
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE  
AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES  
OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF,  
OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
