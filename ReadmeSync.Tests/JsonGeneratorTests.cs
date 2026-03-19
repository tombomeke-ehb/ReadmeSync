using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using ReadmeSync.Models;
using ReadmeSync.Services;

namespace ReadmeSync.Tests
{
    public class JsonGeneratorTests
    {
        [Fact]
        public void GenerateJson_ValidInput_CreatesValidJsonFile()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            var testData = new List<CodeFileInfo>
            {
                new CodeFileInfo
                {
                    Namespace = "Api.Controllers",
                    TypeKeyword = "class",
                    Class = "UserController",
                    Summary = "Handles user requests",
                    Methods = new List<string> { "GetUsers", "CreateUser" },
                    Todos = new List<string> { "Add authentication" }
                }
            };
            
            var groupedData = testData.GroupBy(x => x.Namespace);
            var generator = new JsonGenerator();

            try
            {
                // Act
                generator.GenerateJson(tempFile, groupedData);

                // Assert
                Assert.True(File.Exists(tempFile));
                string content = File.ReadAllText(tempFile);
                
                Assert.Contains("\"namespace\": \"Api.Controllers\"", content);
                Assert.Contains("\"class\": \"UserController\"", content);
                Assert.Contains("\"Add authentication\"", content);
                Assert.StartsWith("[", content.Trim()); // Verify it's a JSON array
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}
