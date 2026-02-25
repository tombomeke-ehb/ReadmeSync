using System.Linq;
using Xunit;
using ReadmeSync;

namespace ReadmeSync.Tests
{
    public class AnalyzerTests
    {
        [Fact]
        public void AnalyzeCode_CSharp_ExtractsCorrectly()
        {
            // Arrange
            string code = @"
using System;

namespace MyApp.Core
{
    /// <summary>
    /// Represents a player in the game.
    /// </summary>
    public class Player : Entity, IPlayable
    {
        public void Attack() { }
        public int GetHealth() { return 100; }

        // TODO: Implement movement
    }
}";
            var patterns = LanguagePatterns.For("csharp");

            // Act
            var result = Program.AnalyzeCode(code, patterns, "csharp");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MyApp.Core", result.Namespace);
            Assert.Equal("class", result.TypeKeyword);
            Assert.Equal("Player", result.Class);
            Assert.Equal("Entity, IPlayable", result.Inheritance);
            Assert.Equal("Represents a player in the game.", result.Summary);
            Assert.Contains("Attack", result.Methods);
            Assert.Contains("GetHealth", result.Methods);
            Assert.Contains("Implement movement", result.Todos);
        }

        [Fact]
        public void AnalyzeCode_Java_ExtractsCorrectly()
        {
            // Arrange
            string code = @"
package com.example.app;

/**
 * Represents a player in the game.
 */
public class Player extends Entity implements IPlayable {
    public void attack() { }
    public int getHealth() { return 100; }

    // TODO: Implement movement
}";
            var patterns = LanguagePatterns.For("java");

            // Act
            var result = Program.AnalyzeCode(code, patterns, "java");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("com.example.app", result.Namespace);
            Assert.Equal("class", result.TypeKeyword);
            Assert.Equal("Player", result.Class);
            Assert.Equal("extends Entity implements IPlayable", result.Inheritance);
            Assert.Equal("Represents a player in the game.", result.Summary);
            Assert.Contains("attack", result.Methods);
            Assert.Contains("getHealth", result.Methods);
            Assert.Contains("Implement movement", result.Todos);
        }

        [Fact]
        public void AnalyzeCode_Java_Interface_ExtractsCorrectly()
        {
            // Arrange
            string code = @"
package com.example.core;

/**
 * Defines playable actions.
 */
public interface IPlayable {
    public void attack();
    public void defend();
}";
            var patterns = LanguagePatterns.For("java");

            // Act
            var result = Program.AnalyzeCode(code, patterns, "java");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("com.example.core", result.Namespace);
            Assert.Equal("interface", result.TypeKeyword);
            Assert.Equal("IPlayable", result.Class);
            Assert.Equal("Defines playable actions.", result.Summary);
            Assert.Contains("attack", result.Methods);
            Assert.Contains("defend", result.Methods);
        }

        [Fact]
        public void AnalyzeCode_CSharp_Record_ExtractsCorrectly()
        {
            // Arrange
            string code = @"
namespace MyApp.Models
{
    public record UserDto(int Id, string Name);
}";
            var patterns = LanguagePatterns.For("csharp");

            // Act
            var result = Program.AnalyzeCode(code, patterns, "csharp");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MyApp.Models", result.Namespace);
            Assert.Equal("record", result.TypeKeyword);
            Assert.Equal("UserDto", result.Class);
        }
    }
}
