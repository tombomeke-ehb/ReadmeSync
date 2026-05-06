using Xunit;
using ReadmeSync.Services;

namespace ReadmeSync.Tests
{
    public class ValidatorServiceTests
    {
        private readonly ValidatorService _validator = new();

        [Fact]
        public void Validate_ValidMarkdown_ReturnsValid()
        {
            // Arrange
            string markdown = @"# Manual Content

<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

# Code Overview (auto-generated)

## MyApp.Core

### MyClass

Some content here.
";

            // Act
            var result = _validator.Validate(markdown, "https://github.com/user/repo");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public void Validate_MissingMarker_ReturnsError()
        {
            // Arrange
            string markdown = @"# Manual Content

# Code Overview

## MyApp.Core
";

            // Act
            var result = _validator.Validate(markdown, "https://github.com/user/repo");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Issues);
            Assert.Contains(result.Issues, i => i.Message.Contains("AUTO-GENERATED"));
        }

        [Fact]
        public void Validate_EmptyNamespaceSection_ReturnsWarning()
        {
            // Arrange
            string markdown = @"<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

# Code Overview

## MyApp.Core

## MyApp.UI

Some content
";

            // Act
            var result = _validator.Validate(markdown, "https://github.com/user/repo");

            // Assert
            Assert.NotNull(result);
            // Empty sections should be flagged as warnings
            var emptyIssues = result.Issues.FindAll(i => i.Message.Contains("empty") || i.Message.Contains("Empty"));
            Assert.NotEmpty(emptyIssues);
        }

        [Fact]
        public void Validate_MissingSummary_ReturnsInfo()
        {
            // Arrange
            string markdown = @"<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

# Code Overview

## MyApp.Core

### MyClass

**Public Methods:**
- `Method1()`
";

            // Act
            var result = _validator.Validate(markdown, "https://github.com/user/repo");

            // Assert
            Assert.NotNull(result);
            // Missing summary should be flagged as info
            var summaryIssues = result.Issues.FindAll(i => i.Message.Contains("summary") || i.Message.Contains("Summary"));
            Assert.NotEmpty(summaryIssues);
        }

        [Fact]
        public void Validate_ValidLinkFormat_ReturnsValid()
        {
            // Arrange
            string markdown = @"<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

# Code Overview

## MyApp.Core

### [MyClass.cs](https://github.com/user/repo/MyClass.cs) *(class)*

> Summary here

**Public Methods:**
- `Method1()`
";

            // Act
            var result = _validator.Validate(markdown, "https://github.com/user/repo");

            // Assert
            Assert.NotNull(result);
            // Link should be valid
            var linkIssues = result.Issues.FindAll(i => i.Message.Contains("link") || i.Message.Contains("Link"));
            Assert.Empty(linkIssues);
        }

        [Fact]
        public void Validate_MultipleIssues_CollectsAll()
        {
            // Arrange
            string markdown = @"# Content without marker

## MyApp.Core

### MyClass

Bad content
";

            // Act
            var result = _validator.Validate(markdown, "https://github.com/user/repo");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Issues);
        }

        [Fact]
        public void Validate_CompleteSection_ReturnsValid()
        {
            // Arrange
            string markdown = @"<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

# Code Overview (auto-generated)

## MyApp.Core

### [Player.cs](https://github.com/user/repo/Player.cs) *(class)*

**Inherits:** `Entity`

> A player in the game.

**Public Methods:**
- `Attack()`
- `Defend()`
";

            // Act
            var result = _validator.Validate(markdown, "https://github.com/user/repo");

            // Assert
            Assert.NotNull(result);
            // Should be valid or have only minor info issues
            var errorCount = result.Issues.FindAll(i => i.Severity == "Error").Count;
            Assert.Equal(0, errorCount);
        }

        [Fact]
        public void Validate_NoPublicMethods_ReturnsValid()
        {
            // Arrange
            string markdown = @"<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

# Code Overview

## MyApp.Core

### [MyClass.cs](https://github.com/user/repo/MyClass.cs) *(class)*

> A utility class.

_No public methods found._
";

            // Act
            var result = _validator.Validate(markdown, "https://github.com/user/repo");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithTODOs_ReturnsValid()
        {
            // Arrange
            string markdown = @"<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

# Code Overview

## MyApp.Core

### [MyClass.cs](https://github.com/user/repo/MyClass.cs) *(class)*

> Summary.

**Public Methods:**
- `Method1()`

**TODOs:**
- [ ] TODO 1
- [ ] TODO 2
";

            // Act
            var result = _validator.Validate(markdown, "https://github.com/user/repo");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_IsValid_Property_ReflectsErrorCount()
        {
            // Arrange
            string validMarkdown = @"<!-- AUTO-GENERATED BELOW – DO NOT EDIT -->

# Code Overview

Valid content.
";

            string invalidMarkdown = @"Invalid content without marker";

            // Act
            var validResult = _validator.Validate(validMarkdown, "https://github.com/user/repo");
            var invalidResult = _validator.Validate(invalidMarkdown, "https://github.com/user/repo");

            // Assert
            Assert.True(validResult.IsValid);
            Assert.False(invalidResult.IsValid);
        }
    }
}
