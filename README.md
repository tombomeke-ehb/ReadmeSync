# ReadmeSync

**ReadmeSync** is a lightweight, fast, and highly customizable tool with both **interactive TUI** and **command-line** interfaces that automatically scans your source code and generates or updates a `README.md` or `ROADMAP.md` file. It extracts namespaces, classes, interfaces, public methods, XML/Javadoc/docstring summaries, and `// TODO` comments to give you an instant, structured overview of your project.

[![NuGet version](https://badge.fury.io/nu/ReadmeSync.svg)](https://badge.fury.io/nu/ReadmeSync)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Downloads](https://img.shields.io/nuget/dt/ReadmeSync.svg)](https://www.nuget.org/packages/ReadmeSync/)
[![Build Status](https://github.com/tombomeke-ehb/ReadmeSync/actions/workflows/ci.yml/badge.svg)](https://github.com/tombomeke-ehb/ReadmeSync/actions)

---

## Features

- **12 Language Support**: **C#**, **Java**, **Python**, **TypeScript**, **JavaScript**, **PHP**, **Go**, **Rust**, **Ruby**, **Kotlin**, **Swift**, **C++**
- **Interactive TUI Mode**: Beautiful terminal UI with menus, progress bars, and animated scanning (run `readmesync` with no arguments)
- **CLI Mode**: Classic command-line interface for scripting and automation
- **Dry-Run Preview**: Use `--dry-run` to preview generated markdown or JSON without writing files
- **Deep Code Insights**: Extracts `class`, `interface`, `record`, `struct`, `enum`, and type-specific constructs
- **Documentation Extraction**: Automatically pulls documentation comments:
  - C#: `/// <summary>`
  - Java/TypeScript/JavaScript/Kotlin/PHP: `/** ... */` (JSDoc/Javadoc)
  - Python: `"""docstrings"""`
  - Rust/Swift/C++: `///` line comments
- **Inheritance Tracking**: Shows which classes or interfaces your code inherits from
- **Smart Exclusions**: Automatically ignores `bin`, `obj`, `node_modules`, `.git`, `.vs`, `__pycache__`, `dist`, and `build`. Customizable via the `--exclude` flag
- **Safe Updates**: Keeps your manual content intact. It only replaces content *below* the auto-generated marker
- **GitHub Integration**: Optionally generates clickable links to your source files in your repository
- **JSON Export**: Export analysis data for building custom integrations and Live Roadmap APIs
- **Emoji Support**: Optional emoji icons in generated output for better visual organization
- **CI/CD Ready**: Automated testing on multiple platforms ensures reliability

---

## Installation

ReadmeSync is distributed as a .NET Global Tool. You can install it easily via the command line:

```bash
dotnet tool install --global ReadmeSync
```

To update to the latest version:
```bash
dotnet tool update --global ReadmeSync
```

---

## Usage

### Interactive TUI Mode (Recommended)

Simply run without arguments to launch the beautiful interactive TUI:

```bash
readmesync
```

This opens an interactive menu where you can:
- Select language and scan directory
- Configure output file and GitHub URL
- Toggle emojis and telemetry
- Preview dry-run results
- Adjust settings on the fly

### CLI Mode

For scripting and automation, use the command-line interface:

```bash
readmesync [--lang language] [--dry-run] [--use-emojis] [--json] [--exclude folders] [--no-tracking] [scan-root] [output-file] [optional-repo-url]
```

### Examples

**1. Interactive TUI**
```bash
readmesync
```

**2. Basic C# Scan (CLI)**
```bash
readmesync . README.md
```

**3. Scan a Go Project**
```bash
readmesync --lang go ./src README.md
```

**4. Preview Changes Without Writing (Dry-Run)**
```bash
readmesync --dry-run --lang python . README.md
```

**5. Scan with GitHub Links**
```bash
readmesync --lang java ./src ROADMAP.md https://github.com/tombomeke-ehb/MyApp
```

**6. Generate JSON for APIs**
```bash
readmesync --json . roadmap.json
```

**7. Enable Emojis**
```bash
readmesync --use-emojis . README.md
```

**8. Custom Exclusions**
```bash
readmesync --exclude "test,temp,cache" --lang rust . README.md
```

---

## Supported Languages

| Language | Extension | Status | Notes |
|----------|-----------|--------|-------|
| C# | `.cs` | Full Support | v1.0.0+ |
| Java | `.java` | Full Support | v1.2.0+ |
| Python | `.py` | Full Support | v1.3.0+ |
| TypeScript | `.ts` | Full Support | v1.3.0+ |
| JavaScript | `.js` | Full Support | v1.3.0+ |
| PHP | `.php` | Full Support | v1.4.0+ |
| Go | `.go` | Full Support | v2.0.0+ |
| Rust | `.rs` | Full Support | v2.0.0+ |
| Ruby | `.rb` | Full Support | v2.0.0+ |
| Kotlin | `.kt` | Full Support | v2.0.0+ |
| Swift | `.swift` | Full Support | v2.0.0+ |
| C++ | `.cpp` | Full Support | v2.0.0+ |

---

## Command-Line Options

| Flag | Description |
|------|-------------|
| `--lang <language>` | Specify language: `csharp`, `java`, `python`, `typescript`, `javascript`, `php`, `go`, `rust`, `ruby`, `kotlin`, `swift`, `cpp` (default: `csharp`) |
| `--use-emojis` | Enable emoji icons in the generated output |
| `--json` | Export the analysis data to a structured JSON file instead of Markdown |
| `--dry-run` | Preview the output without writing to file |
| `--exclude <folders>` | Comma-separated list of folders to exclude (default: `bin,obj,node_modules,.git,.vs,__pycache__,dist,build`) |
| `--no-tracking` | Disable anonymous telemetry |
| `--tui` | Explicitly launch interactive TUI mode |

---

## Build a Live Roadmap (API)
Using the `--json` flag, you can easily build APIs or web integrations. 

```bash
readmesync --json . roadmap.json
```
This generates a clean, readable JSON file containing all classes, namespaces, and most importantly: your `// TODO` comments! You can host this file on your server to show visitors what you are currently working on.

