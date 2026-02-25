# ReadmeSync

**ReadmeSync** is a lightweight, fast, and highly customizable command-line tool that automatically scans your source code and generates or updates a `README.md` or `ROADMAP.md` file. It extracts namespaces, classes, interfaces, public methods, XML/Javadoc/docstring summaries, and `// TODO` comments to give you an instant, structured overview of your project.

[![NuGet version](https://badge.fury.io/nu/ReadmeSync.svg)](https://badge.fury.io/nu/ReadmeSync)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Downloads](https://img.shields.io/nuget/dt/ReadmeSync.svg)](https://www.nuget.org/packages/ReadmeSync/)
[![Build Status](https://github.com/tombomeke-ehb/ReadmeSync/actions/workflows/ci.yml/badge.svg)](https://github.com/tombomeke-ehb/ReadmeSync/actions)

---

## ✨ Features

- **Multi-Language Support**: **C#**, **Java**, **Python**, **TypeScript**, and **JavaScript** 🚀
- **Deep Code Insights**: Extracts `class`, `interface`, `record`, `struct`, and `enum` types.
- **Documentation Extraction**: Automatically pulls documentation comments:
  - C#: `/// <summary>`
  - Java/TypeScript/JavaScript: `/** ... */` (JSDoc/Javadoc)
  - Python: `"""docstrings"""`
- **Inheritance Tracking**: Shows which classes or interfaces your code inherits from.
- **Smart Exclusions**: Automatically ignores `bin`, `obj`, `node_modules`, `.git`, `.vs`, `__pycache__`, `dist`, and `build`. Customizable via the `--exclude` flag.
- **Safe Updates**: Keeps your manual content intact. It only replaces content *below* the auto-generated marker.
- **GitHub Integration**: Optionally generates clickable links to your source files in your repository.
- **CI/CD Ready**: Automated testing on multiple platforms ensures reliability.

---

## 📦 Installation

ReadmeSync is distributed as a .NET Global Tool. You can install it easily via the command line:

```bash
dotnet tool install --global ReadmeSync
```

To update to the latest version:
```bash
dotnet tool update --global ReadmeSync
```

---

## 🚀 Usage

Navigate to your project directory and run the tool:

```bash
readmesync [--lang language] [scan-root] [output-file] [optional-repo-url]
```

### Examples

**1. Basic C# Scan (Current Directory)**
```bash
readmesync . README.md
```

**2. Scan a Java Project**
```bash
readmesync --lang java ./src ROADMAP.md
```

**3. Scan a Python Project**
```bash
readmesync --lang python ./app README.md https://github.com/user/repo
```

**4. Scan a TypeScript Project**
```bash
readmesync --lang typescript ./src DOCS.md
```

**5. Generate Clickable GitHub Links**
```bash
readmesync . README.md https://github.com/YOUR_USERNAME/YOUR_REPO
```

**6. Use Emojis for Visual Appeal**
```bash
readmesync --use-emojis . README.md
```

**7. Custom Exclusions**
```bash
readmesync --exclude "test,temp,cache" . README.md
```

---

## 🌐 Supported Languages

| Language | Extension | Status |
|----------|-----------|--------|
| C# | `.cs` | ✅ Full Support |
| Java | `.java` | ✅ Full Support |
| Python | `.py` | ✅ Full Support (v1.3.0+) |
| TypeScript | `.ts` | ✅ Full Support (v1.3.0+) |
| JavaScript | `.js` | ✅ Full Support (v1.3.0+) |

---

## 🔧 Command-Line Options

| Flag | Description |
|------|-------------|
| `--lang <language>` | Specify language: `csharp`, `java`, `python`, `typescript`, `javascript` (default: `csharp`) |
| `--use-emojis` | Enable emoji icons in the generated output |
| `--exclude <folders>` | Comma-separated list of folders to exclude |
| `--no-tracking` | Disable anonymous telemetry |

