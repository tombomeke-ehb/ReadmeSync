# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2025-01-XX

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

## [1.2.2] - 2025-01-XX

### Fixed
- Initial stable release
- C# and Java support
- Basic telemetry

## [1.2.0] - 2025-01-XX

### Added
- Java language support
- Telemetry tracking
- Custom exclusions via `--exclude` flag

## [1.0.0] - 2025-01-XX

### Added
- Initial release
- C# support only
- Basic markdown generation
