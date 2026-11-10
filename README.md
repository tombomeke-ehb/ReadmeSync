# ReadmeSync

**ReadmeSync** is a lightweight, fast command-line tool that scans your source code and generates or updates a `README.md` or `ROADMAP.md` file. It extracts namespaces, classes, interfaces, public methods, documentation comments and `// TODO` notes to give you an instant, structured overview of your project.

[![NuGet version](https://badge.fury.io/nu/ReadmeSync.svg)](https://badge.fury.io/nu/ReadmeSync)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Downloads](https://img.shields.io/nuget/dt/ReadmeSync.svg)](https://www.nuget.org/packages/ReadmeSync/)
[![Build Status](https://github.com/tombomeke-ehb/ReadmeSync/actions/workflows/ci.yml/badge.svg)](https://github.com/tombomeke-ehb/ReadmeSync/actions)

---

## Features

- **Eight languages**: C#, Java, Kotlin, Python, TypeScript, JavaScript, PHP and Go. Scan one, several, or let `--lang auto` detect them.
- **Every type in a file**: classes, interfaces, records, structs, enums, traits and objects, each with its own public methods. Private nested types are left out.
- **Documentation extraction**: the comment that belongs to each type is used as its summary:
  - C#: `/// <summary>`
  - Java, Kotlin, TypeScript, JavaScript, PHP: `/** ... */`
  - Python: `"""docstrings"""`
  - Go: `// comment` lines above the type
- **TODO tracking**: collects `TODO`, `FIXME` and `HACK` comments, with line numbers in the JSON export.
- **Safe updates**: only the generated section is replaced. Everything you wrote above it (and below its end marker) stays exactly as it is. A file that has no generated section yet gets one appended.
- **No noisy diffs**: when your code did not change, the file is not touched.
- **CI friendly**: `--check` fails the build when the README is out of date.
- **Source links**: pass your repository URL and every type links to its file on GitHub, GitLab or Bitbucket.
- **JSON export**: `--json` for live roadmaps, dashboards and other integrations.
- **Project settings**: keep your options in a `.readmesync.json` file.
- **Interactive mode**: run `readmesync` without arguments for a guided menu.

---

## Installation

ReadmeSync is distributed as a .NET Global Tool:

```bash
dotnet tool install --global ReadmeSync
```

To update to the latest version:

```bash
dotnet tool update --global ReadmeSync
```

Requires the .NET 8 runtime or newer.

---

## Usage

```bash
readmesync [options] [scan-root] [output-file] [repo-url]
```

| Argument | Description |
|----------|-------------|
| `scan-root` | Directory to scan. Default: the current directory. |
| `output-file` | File to update, relative to the repository root. Default: `README.md`. |
| `repo-url` | Repository URL, used to link every type to its source file. |

### Examples

**Scan a C# project and update the README**
```bash
readmesync . README.md
```

**Detect the languages automatically and add a table of contents**
```bash
readmesync --lang auto --toc . README.md
```

**Scan several languages into a roadmap**
```bash
readmesync --lang java,kotlin ./src ROADMAP.md
```

**Link every type to its source file**
```bash
readmesync . README.md https://github.com/YOUR_USERNAME/YOUR_REPO
```

**Skip extra folders** (added to the default excludes; wildcards and relative paths work too)
```bash
readmesync --exclude "tests,*.g.cs,src/legacy" . README.md
```

**Preview without writing anything**
```bash
readmesync --dry-run .
```

**Fail a CI build when the README is out of date**
```bash
readmesync --check
```

---

## Supported Languages

| Language | `--lang` | Files | Namespace taken from |
|----------|----------|-------|----------------------|
| C# | `csharp`, `cs` | `.cs` | `namespace` |
| Java | `java` | `.java` | `package` |
| Kotlin | `kotlin`, `kt` | `.kt`, `.kts` | `package` |
| Python | `python`, `py` | `.py` | `# Package: name` comment, otherwise the folder |
| TypeScript | `typescript`, `ts` | `.ts`, `.tsx` | `namespace`, otherwise the folder |
| JavaScript | `javascript`, `js` | `.js`, `.jsx`, `.mjs`, `.cjs` | the folder |
| PHP | `php` | `.php` | `namespace` |
| Go | `go`, `golang` | `.go` | `package` |

C#, Java, Kotlin, PHP and Go files without a namespace or package are left out of the overview.

---

## Command-Line Options

| Option | Description |
|--------|-------------|
| `-l`, `--lang <list>` | Language, comma-separated list, or `auto`. Default: `csharp`. |
| `-o`, `--output <file>` | Same as the `output-file` argument. |
| `--repo-url <url>` | Same as the `repo-url` argument. |
| `--branch <name>` | Branch, tag or commit used in source links. Default: `HEAD`. |
| `-e`, `--exclude <list>` | Extra folders or files to skip. Wildcards (`*.g.cs`) and relative paths (`src/legacy`) are allowed. |
| `--no-default-excludes` | Do not skip the default folders. |
| `--json` | Write the analysis as JSON instead of markdown. Default output: `readmesync.json`. |
| `--toc` | Add a table of contents to the generated section. |
| `--use-emojis` | Use emoji icons in the generated section. |
| `--no-timestamp` | Leave out the "Last updated" line. |
| `--check` | Write nothing; exit with code 1 when the output file is out of date. |
| `--dry-run` | Write nothing; print the result to standard output. |
| `-c`, `--config <file>` | Settings file to use. |
| `--init` | Create a `.readmesync.json` with the default settings. |
| `--tui` | Start the interactive mode. |
| `-q`, `--quiet` | Only print errors. |
| `--no-tracking` | Disable anonymous usage statistics. |
| `-v`, `--version` | Show the version. |
| `-h`, `--help` | Show the help. |

Default excludes: `bin`, `obj`, `node_modules`, `.git`, `.vs`, `.idea`, `.vscode`, `__pycache__`, `dist`, `build`, `out`, `target`, `vendor`, `venv`, `.venv`, `coverage`, `TestResults`.

Exit codes: `0` success, `1` `--check` found the output out of date, `2` error.

---

## How Updates Work

ReadmeSync writes its overview between two markers:

```markdown
# My Project

Everything up here is yours and is never changed.

<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->
...generated overview...
<!-- AUTO-GENERATED END -->

Content down here is kept as well.
```

- A file without markers keeps all of its content; the overview is appended.
- A file from an older ReadmeSync version (start marker only) has everything below the marker replaced, as before.
- The "Last updated" line only changes when the overview itself changes.

---

## Settings File

Run `readmesync --init` to create a `.readmesync.json` in the current directory:

```json
{
  "language": "auto",
  "scanDirectory": ".",
  "outputFile": "README.md",
  "repositoryUrl": "",
  "branch": "HEAD",
  "exclude": [],
  "useEmojis": false,
  "toc": false,
  "timestamp": true,
  "outputJson": false,
  "noTracking": false
}
```

The file is picked up from the scan root or the current directory. Every value is optional, comments are allowed, and command-line options take precedence.

---

## Use in CI

Keep the README honest by checking it on every pull request:

```yaml
- name: Check that the README is up to date
  run: |
    dotnet tool install --global ReadmeSync
    readmesync --check --no-tracking
```

---

## Build a Live Roadmap (API)

With `--json` you get the whole analysis as data:

```bash
readmesync --json . roadmap.json
```

```json
[
  {
    "namespace": "Shop.Orders",
    "typeKeyword": "class",
    "class": "Order",
    "inheritance": "IEntity",
    "summary": "An order placed by a customer.",
    "methods": ["Total"],
    "todos": ["Add discounts"],
    "todoItems": [{ "tag": "TODO", "text": "Add discounts", "line": 12 }],
    "path": "src/Orders/Order.cs",
    "link": "https://github.com/user/repo/blob/HEAD/src/Orders/Order.cs",
    "language": "csharp",
    "line": 8
  }
]
```

Host this file on your server to show visitors what you are working on.

---

## Privacy

ReadmeSync sends anonymous usage statistics: the tool version, the scanned language and the operating system description. No code, file names or paths are sent. To switch it off, pass `--no-tracking`, set `"noTracking": true` in the settings file, or set the `READMESYNC_NO_TRACKING=1` or `DO_NOT_TRACK=1` environment variable.

---

## License

MIT. See [LICENSE](https://github.com/tombomeke-ehb/ReadmeSync/blob/main/LICENSE). Changes per version are listed in the [changelog](https://github.com/tombomeke-ehb/ReadmeSync/blob/main/CHANGELOG.md).
