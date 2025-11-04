# 🧩 ReadmeSync

> ⚙️ A lightweight C# tool to automatically generate or update **README** / **ROADMAP** files  
> from your project’s real source code (namespaces, classes, public methods, and `// TODO:`s).

---

## 🚀 Overview

**ReadmeSync** scans your project for `.cs` files and builds a structured overview of:
- Namespaces
- Classes
- Public methods
- `// TODO:` comments

It then **merges** that overview into an existing Markdown file, preserving anything **above** a marker and replacing everything **below** it:

    <!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

Originally built for **RPG Manager** (by Tombomeke Studios) and adapted here as a **general-purpose** tool for any C# repository.

---

## 🛠 Installation

Clone and run locally:

    git clone https://github.com/tombomeke-ehb/ReadmeSync.git
    cd ReadmeSync
    dotnet run -- "C:\path\to\your\project" "README.md" "https://github.com/YourUser/YourRepo"

Quick example (generate a roadmap for the current folder):

    dotnet run -- . ROADMAP.md

> Tip: You can omit the third argument if you don’t want clickable repo links.

---

## ⚙️ Usage

    ReadmeSync [project-root] [output-file] [optional-repo-url]

**Arguments**

| Arg                 | Required | Description                                   | Example                                      |
|---------------------|----------|-----------------------------------------------|----------------------------------------------|
| `project-root`      | ✅        | Directory to scan recursively for `.cs` files | `.` or `C:\Repos\MyApp`                      |
| `output-file`       | ✅        | Markdown file to write/update                 | `README.md`, `ROADMAP.md`, `docs/OVERVIEW.md`|
| `optional-repo-url` | ❌        | Base repo URL for clickable file links        | `https://github.com/YourName/YourRepo`       |

---

## 🧭 Manual Section (kept intact)

Place your custom roadmap or notes **above** this marker in the target file:

    <!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

Everything **below** that line will be regenerated on each run.

**Example**

    # My Project Roadmap

    - Phase 1: Core systems
    - Phase 2: UI polish
    - Phase 3: Release

    <!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

---

## 🔍 What the tool generates

- A timestamp  
- Project statistics (namespaces, classes, methods, TODOs)  
- Sections per **namespace**  
- For each class:
  - A heading (optionally linked to your repo)
  - The list of **public methods**
  - Any `// TODO:` lines found in the file

---

## 🧪 Example output

    # 🧮 Code Overview (auto-generated)

    _Last updated: 2025-11-04 15:00_

    📊 **11 Namespaces · 27 Classes · 17 Methods · 0 TODOs**

    ## 🧱 MyApp.Core

    ### Program.cs
    **Public Methods:**
    - `Main()`

---

## 🧰 Typical workflows

**Update README in the repo root**

    dotnet run -- . README.md

**Generate a separate ROADMAP**

    dotnet run -- "C:\Repos\GameEngine" "ROADMAP.md"

**Add clickable links to files (GitHub/GitLab/etc.)**

    dotnet run -- . README.md https://github.com/YourName/YourRepo

---

## 🧩 Notes & limitations

- Designed for **C#** projects; extendable to other languages  
- Uses regex to detect `namespace`, `class`, and `public` methods  
- Skips files without a real `class` declaration  
- Constructors are not counted as methods

---

## 🤝 Contributing

Contributions are welcome!

Ideas to improve:
- Config file support (`ReadmeSync.json`)
- Language adapters (TypeScript, Java, Python)
- CLI flags (e.g., `--no-todo`, `--summary-only`, `--no-links`)
- Output templates / themes

Fork the repo and open a PR 👍

---

## 👤 Author

**Tom Dekoning** — *Tombomeke Studios*  
Originally used (with project-specific tweaks) in: **RPG Manager**.

---

## 🪪 License

MIT License © 2025 Tombomeke Studios  
You may use, modify, and distribute with attribution.
