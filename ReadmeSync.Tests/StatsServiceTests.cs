using System.Collections.Generic;
using System.Linq;
using Xunit;
using ReadmeSync.Models;
using ReadmeSync.Services;

namespace ReadmeSync.Tests
{
    public class StatsServiceTests
    {
        private readonly StatsService _statsService = new();

        [Fact]
        public void ComputeStats_EmptyFileGroups_ReturnsZeros()
        {
            // Arrange
            var emptyGroups = Enumerable.Empty<IGrouping<string, CodeFileInfo>>();

            // Act
            var stats = _statsService.ComputeStats(emptyGroups, 0);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(0, stats.TotalNamespaces);
            Assert.Equal(0, stats.TotalTypes);
            Assert.Equal(0, stats.TotalMethods);
            Assert.Equal(0, stats.TotalTodos);
            Assert.Equal(0, stats.TotalFiles);
        }

        [Fact]
        public void ComputeStats_SingleNamespaceSingleClass_ComputesCorrectly()
        {
            // Arrange
            var fileInfo = new CodeFileInfo
            {
                Namespace = "MyApp.Core",
                TypeKeyword = "class",
                Class = "Player",
                Methods = new List<string> { "Attack", "Defend", "Move" },
                Todos = new List<string> { "TODO 1" }
            };

            var groups = new[] { fileInfo }
                .GroupBy(f => f.Namespace)
                .AsEnumerable();

            // Act
            var stats = _statsService.ComputeStats(groups, 1);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(1, stats.TotalNamespaces);
            Assert.Equal(1, stats.TotalTypes);
            Assert.Equal(3, stats.TotalMethods);
            Assert.Equal(1, stats.TotalTodos);
            Assert.Equal(1, stats.TotalFiles);
            Assert.Equal(3.0, stats.AvgMethodsPerType);
        }

        [Fact]
        public void ComputeStats_MultipleNamespacesMultipleClasses_ComputesCorrectly()
        {
            // Arrange
            var files = new List<CodeFileInfo>
            {
                new() {
                    Namespace = "MyApp.Core",
                    TypeKeyword = "class",
                    Class = "Player",
                    Methods = new List<string> { "Attack", "Defend" },
                    Todos = new List<string> { "TODO 1", "TODO 2" }
                },
                new() {
                    Namespace = "MyApp.Core",
                    TypeKeyword = "class",
                    Class = "Enemy",
                    Methods = new List<string> { "Chase", "Attack", "Retreat" },
                    Todos = new List<string>()
                },
                new() {
                    Namespace = "MyApp.UI",
                    TypeKeyword = "class",
                    Class = "GameWindow",
                    Methods = new List<string> { "Render", "Update", "HandleInput", "Close" },
                    Todos = new List<string> { "TODO 3" }
                }
            };

            var groups = files.GroupBy(f => f.Namespace).AsEnumerable();

            // Act
            var stats = _statsService.ComputeStats(groups, 3);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(2, stats.TotalNamespaces);
            Assert.Equal(3, stats.TotalTypes);
            Assert.Equal(9, stats.TotalMethods);
            Assert.Equal(3, stats.TotalTodos);
            Assert.Equal(3, stats.TotalFiles);
            Assert.Equal(3.0, stats.AvgMethodsPerType);
        }

        [Fact]
        public void ComputeStats_TypeBreakdownCounting_IsCorrect()
        {
            // Arrange
            var files = new List<CodeFileInfo>
            {
                new() {
                    Namespace = "MyApp",
                    TypeKeyword = "class",
                    Class = "Class1",
                    Methods = new List<string> { "Method1" },
                    Todos = new List<string>()
                },
                new() {
                    Namespace = "MyApp",
                    TypeKeyword = "interface",
                    Class = "Interface1",
                    Methods = new List<string>(),
                    Todos = new List<string>()
                },
                new() {
                    Namespace = "MyApp",
                    TypeKeyword = "class",
                    Class = "Class2",
                    Methods = new List<string> { "Method1", "Method2" },
                    Todos = new List<string>()
                },
                new() {
                    Namespace = "MyApp",
                    TypeKeyword = "record",
                    Class = "Record1",
                    Methods = new List<string>(),
                    Todos = new List<string>()
                }
            };

            var groups = files.GroupBy(f => f.Namespace).AsEnumerable();

            // Act
            var stats = _statsService.ComputeStats(groups, 4);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(3, stats.TypeBreakdown["class"]);
            Assert.Equal(1, stats.TypeBreakdown["interface"]);
            Assert.Equal(1, stats.TypeBreakdown["record"]);
        }

        [Fact]
        public void ComputeStats_AvgMethodsPerTypeCalculation_IsAccurate()
        {
            // Arrange
            var files = new List<CodeFileInfo>
            {
                new() {
                    Namespace = "MyApp",
                    TypeKeyword = "class",
                    Class = "Class1",
                    Methods = new List<string> { "M1", "M2", "M3", "M4", "M5" },
                    Todos = new List<string>()
                },
                new() {
                    Namespace = "MyApp",
                    TypeKeyword = "class",
                    Class = "Class2",
                    Methods = new List<string> { "M1", "M2" },
                    Todos = new List<string>()
                },
                new() {
                    Namespace = "MyApp",
                    TypeKeyword = "class",
                    Class = "Class3",
                    Methods = new List<string> { "M1", "M2", "M3" },
                    Todos = new List<string>()
                }
            };

            var groups = files.GroupBy(f => f.Namespace).AsEnumerable();

            // Act
            var stats = _statsService.ComputeStats(groups, 3);

            // Assert
            Assert.NotNull(stats);
            // (5 + 2 + 3) / 3 = 10 / 3 = 3.33...
            Assert.True(stats.AvgMethodsPerType > 3.3 && stats.AvgMethodsPerType < 3.4);
        }

        [Fact]
        public void ComputeStats_WithPrivateMethods_IncludesThemInCount()
        {
            // Arrange
            var fileInfo = new CodeFileInfo
            {
                Namespace = "MyApp",
                TypeKeyword = "class",
                Class = "Player",
                Methods = new List<string> { "PublicMethod", "[private] PrivateMethod", "[private] AnotherPrivate" },
                Todos = new List<string>()
            };

            var groups = new[] { fileInfo }
                .GroupBy(f => f.Namespace)
                .AsEnumerable();

            // Act
            var stats = _statsService.ComputeStats(groups, 1);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(3, stats.TotalMethods);
            Assert.Equal(3.0, stats.AvgMethodsPerType);
        }

        [Fact]
        public void ComputeStats_MultipleFiles_CountsFilesCorrectly()
        {
            // Arrange
            var files = new List<CodeFileInfo>
            {
                new() {
                    Namespace = "MyApp",
                    TypeKeyword = "class",
                    Class = "Class1",
                    Methods = new List<string>(),
                    Todos = new List<string>()
                },
                new() {
                    Namespace = "MyApp",
                    TypeKeyword = "class",
                    Class = "Class2",
                    Methods = new List<string>(),
                    Todos = new List<string>()
                },
                new() {
                    Namespace = "MyApp",
                    TypeKeyword = "class",
                    Class = "Class3",
                    Methods = new List<string>(),
                    Todos = new List<string>()
                }
            };

            var groups = files.GroupBy(f => f.Namespace).AsEnumerable();

            // Act
            var stats = _statsService.ComputeStats(groups, 3);

            // Assert
            Assert.Equal(3, stats.TotalFiles);
        }
    }
}
