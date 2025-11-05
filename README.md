# 🧩 ReadmeSync
> ⚙️ A lightweight CLI that automatically generates or updates **README** / **ROADMAP** files  
> based on your project’s actual source code — namespaces/packages, classes, public methods, and // TODO: comments.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](#)
[![License](https://img.shields.io/badge/license-Tombomeke%20Studios%20(MIT%20Modified)-orange)](#license)
[![Build](https://img.shields.io/badge/GitHub%20Actions-Publish%20on%20Tag-green)](#-automated-publishing)
[![Release](https://img.shields.io/github/v/release/tombomeke-ehb/ReadmeSync?color=blue&label=latest)](https://github.com/tombomeke-ehb/ReadmeSync/releases)
[![Issues](https://img.shields.io/github/issues/tombomeke-ehb/ReadmeSync)](https://github.com/tombomeke-ehb/ReadmeSync/issues)
[![Stars](https://img.shields.io/github/stars/tombomeke-ehb/ReadmeSync?style=social)](https://github.com/tombomeke-ehb/ReadmeSync)

---

## ✨ Features
- 🧠 Automatically documents your project structure  
- 🧩 Merges updates directly into existing README / ROADMAP files  
- 🔗 Optionally adds clickable GitHub file links  
- ⚙️ Detects and lists:
  - Namespaces / Packages  
  - Classes  
  - Public methods  
  - // TODO: comments  
- 🌍 Multi-language support: C# and Java  
- 🚀 CLI-based — no dependencies, no setup required  

---

## 🧰 Requirements
- .NET 8.0 SDK or higher  
- Windows, macOS, or Linux  
- A project containing .cs or .java files  

---

## 🚀 Overview
ReadmeSync scans your source code and builds a clean, auto-generated overview of:
- Namespaces / Packages  
- Classes  
- Public methods  
- TODO comments  

It then merges that overview into an existing Markdown file, preserving everything above the marker and regenerating everything below it.

<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

Originally created for **RPG Manager** by **Tombomeke Studios**, now expanded into a general-purpose documentation automation tool.

---

## 🛠 Installation

### 🧩 Option 1 — via NuGet (recommended)
Install the CLI globally:

```
dotnet tool install -g ReadmeSync
```
Once installed, you can run it from anywhere:

```
readmesync . README.md
```
To generate a roadmap with clickable GitHub links:

```
readmesync . ROADMAP.md https://github.com/YourName/YourRepo
```
Update anytime with:

```
dotnet tool update -g ReadmeSync
```
---

### 🧰 Option 2 — Build from source
If you’d like to run it locally or explore how it works:

```
git clone https://github.com/tombomeke-ehb/ReadmeSync.git  
cd ReadmeSync  
dotnet run --project ReadmeSync -- . README.md
```

💡 ReadmeSync can also be integrated into CI/CD (e.g. GitHub Actions)
to keep your documentation automatically in sync with your source code.

---

## ⚙️ Usage
readmesync [options] [project-root] [output-file] [optional-repo-url]

Argument / Option | Required | Description | Example
----------------- | -------- | ------------ | --------
project-root | ✅ | Directory to scan recursively | . or C:\Repos\MyApp
output-file | ✅ | Markdown file to write/update | README.md, ROADMAP.md
optional-repo-url | ❌ | Base repo URL for clickable links | https://github.com/YourName/YourRepo
--lang [csharp|java] | ❌ | Choose which language to parse | --lang java

Behavior details:
- Detects repository root automatically (.git, .sln, README.md, .github/, etc.)
- Keeps everything above <!-- AUTO-GENERATED BELOW – DO NOT EDIT --> intact
- Regenerates everything below that marker
- Adds clickable file links if a repository URL is provided

---

## 🧭 Manual Section (kept intact)
Your own content (notes, roadmap, etc.) always stays safe.  
Only the section below the marker will ever be updated.

# My Project Roadmap
- Phase 1: Core systems  
- Phase 2: UI polish  
- Phase 3: Release  

<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

---

## 🔍 What the tool generates
- Timestamp (Last updated: YYYY-MM-DD HH:mm)
- Project statistics (namespaces/packages, classes, methods, TODOs)
- Sections per namespace/package
- For each class:
  - A heading (linked to file)
  - Public methods
  - Found TODOs

---

## 🧪 Example output
# 🧮 Code Overview (auto-generated)
Language: C#  
Last updated: 2025-11-04 15:00

11 Namespaces · 27 Classes · 17 Methods · 0 TODOs

MyApp.Core  
Program.cs  
Public Methods:
- Main()

---

## 🧾 Command reference
readmesync --help

Usage examples:
readmesync . README.md  
readmesync --lang java ./src ROADMAP.md https://github.com/YourName/YourRepo

---

## 🧩 Notes & limitations
- Designed for C# and Java projects  
- Detects public methods, TODOs, and class declarations  
- Skips files without a class definition  
- Constructors are ignored as methods  
- The section below the marker is fully regenerated each run  
- Additional language support will be added by Tombomeke Studios in future releases  

---

## 🧭 Future roadmap
- [ ] Configuration file (readmesync.json)  
- [ ] Additional language support (Python, TypeScript, etc.)  
- [ ] Optional Git commit hook for auto-sync  
- [ ] Template themes for README generation  
- [ ] Markdown statistics and formatting improvements  

---

## 🧾 Changelog
v1.1.0 – Multi-language update  
- Added Java support via --lang java  
- Improved regex detection and documentation output  
- Refined CLI argument parsing  
- Better repository root detection  

v1.0.0 – Initial release  
- Basic C# scanning (namespaces, classes, methods, TODOs)  
- README/ROADMAP auto-generation  
- Repository link support  

---

## 🤝 Contributing
ReadmeSync is open for public usage and feedback.  
You’re welcome to:
- Open issues for bugs or feature requests.  
- Submit pull requests with improvements.  

Code contributions are reviewed manually to ensure they align with Tombomeke Studios’ standards and project direction.

---

## 🌍 Related projects
- RPG Manager — the original inspiration  
- Tombomeke Studios — more tools and experiments  

---

## 👤 Author
Tom Dekoning  
🎯 Tombomeke Studios  
Creator of RPG Manager and developer of ReadmeSync.

---

## 🪪 License
Tombomeke Studios License (MIT-Modified)  

© 2025 Tom Dekoning — Tombomeke Studios. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy  
of this software and associated documentation files (the “Software”), to use,  
study, and modify the Software for personal or internal educational purposes only,  
subject to the following conditions:

- Redistribution of the Software, in original or modified form, is not permitted without prior written consent from Tombomeke Studios.  
- The Software may not be sold, sublicensed, or used in any commercial product or service.  
- Credit must be given where used: "ReadmeSync by Tombomeke Studios (Tom Dekoning)".

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,  
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE  
AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES  
OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF,  
OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
