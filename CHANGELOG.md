# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.5.0] - 2026-11-11

### Added
- **Kotlin and Go support**, next to C#, Java, Python, TypeScript, JavaScript and PHP.
- **`--lang auto`** detects the languages in the scan directory; `--lang java,kotlin` scans several at once. Aliases such as `cs`, `ts`, `py` and `golang` are accepted.
- **Every type in a file** is now documented, not only the first one, each with its own methods, summary and TODOs. Private nested types are left out.
- **`--check`** writes nothing and exits with code 1 when the output file is out of date, for use in CI.
- **`--dry-run`** prints the result to standard output without writing.
- **`--toc`** adds a table of contents to the generated section.
- **`--no-timestamp`** leaves out the "Last updated" line.
- **Settings file**: options can be kept in `.readmesync.json`; `--init` creates one and `--config` points to another.
- **End marker**: the generated section now ends with `<!-- AUTO-GENERATED END -->`, and content below it is preserved.
- **`FIXME` and `HACK`** comments are collected along with `TODO`; the JSON export gains `todoItems` (tag, text, line), `language` and `line`.
- **Source links for GitLab and Bitbucket**, and a `--branch` option. `--repo-url` and `--output` are available as named options.
- **Interactive mode** (`--tui`, or no arguments in a terminal) with a menu for generating, settings and about.
- `--help`, `--version`, `--quiet` and `--no-default-excludes`.
- Telemetry can also be switched off with the `READMESYNC_NO_TRACKING` or `DO_NOT_TRACK` environment variable.
- The tool runs on newer .NET runtimes when .NET 8 is not installed.

### Changed
- `--exclude` now adds to the default excludes instead of replacing them. Wildcards (`*.g.cs`) and relative paths (`src/legacy`) are supported. More folders are skipped by default (`.idea`, `.vscode`, `out`, `target`, `vendor`, `venv`, `.venv`, `coverage`, `TestResults`).
- Each type is listed under its own name with the path of its file, instead of a made-up file name.
- Python, TypeScript and JavaScript files without a namespace are grouped by folder instead of one `default`/`global` bucket.
- The output file is left untouched when nothing but the timestamp would change.
- `--json` writes to `readmesync.json` by default and refuses to write JSON into a markdown file.
- The repository root is the enclosing git repository when there is one, even if a nested folder holds a README.
- Errors now set a non-zero exit code. Unknown options are reported instead of ignored.
- Python methods starting with an underscore and PHP magic methods are no longer listed as public methods.

### Fixed
- **A README without the marker lost all of its content** on the first run. Existing content is now kept and the overview is appended.
- GitHub source links pointed to a non-existent URL; they now use `/blob/HEAD/`.
- C# methods with nullable or dotted return types (`string? Find()`) were not detected; interface members without `public` are now found too.
- TypeScript methods were not detected because of a typo in the pattern; JavaScript reported every function call as a method.
- Code inside string literals and comments is no longer mistaken for types, methods or TODOs.
- A summary is taken from the comment that belongs to the type, not from the first doc comment in the file. Javadoc tags (`@param`, `@author`) and XML tags are stripped.
- Inheritance no longer includes record parameters, base constructor arguments or text following the word "where" inside a name.
- The reported tool version in telemetry was that of the core library.

## [1.4.0] - 2026-03-13

_Not published to NuGet; its changes ship with 1.5.0._

### Added
- **JSON Export (`--json`)**: Added ability to export the entire code analysis (including `// TODO`s) to a structured JSON file. This is perfect for building Live Roadmaps or APIs.
- **Custom Telemetry Server**: Updated the anonymous telemetry service to point to the official `tombomekestudio.com` .NET backend instead of third-party services.
- **Professional Formatting**: Removed emojis from documentation and output styling for a cleaner, enterprise-ready look.

## [1.3.0] - 2026-02-25

### Added
- **Python support** - Full parsing of Python classes, methods, and docstrings
- **TypeScript support** - Extract namespaces, classes, interfaces, and JSDoc comments
- **JavaScript support** - Parse ES6 classes and JSDoc documentation
- **Refactored architecture** - Code split into dedicated services:
  - `CodeAnalyzer` - Analyzes source code and extracts metadata
  - `MarkdownGenerator` - Generates markdown documentation
  - `RepoRootFinder` - Locates repository root directory
  - `TelemetryService` - Anonymous usage statistics
- **Enhanced CI/CD** - GitHub Actions workflow with:
  - Multi-platform testing (Ubuntu, Windows, macOS)
  - Test gates before publishing
  - Automated quality checks
- **Expanded test coverage** - 21 unit tests covering:
  - All 5 supported languages
  - Edge cases (empty files, no namespace, generics)
  - Integration tests for end-to-end scenarios
- **Improved exclusions** - Now ignores `__pycache__`, `dist`, and `build` folders by default

### Changed
- Package description updated to mention new language support
- Package tags expanded with `python`, `typescript`
- Better detection of project roots (now includes `setup.py`, `pyproject.toml`, `package.json`)

### Fixed
- Language-specific string handling in regex patterns
- Inheritance parsing for TypeScript/JavaScript
- Docstring extraction for Python

## [1.2.2] - 2026-02-18

### Fixed
- Initial stable release
- C# and Java support
- Basic telemetry

## [1.2.0] - 2026-02-16

### Added
- Java language support
- Telemetry tracking
- Custom exclusions via `--exclude` flag

## [1.0.0] - 2025-11-04

### Added
- Initial release
- C# support only
- Basic markdown generation
