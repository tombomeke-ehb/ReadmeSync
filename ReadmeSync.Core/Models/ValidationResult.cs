using System.Collections.Generic;

namespace ReadmeSync.Models
{
    public enum Severity { Error, Warning, Info }

    public class ValidationIssue
    {
        public Severity Severity { get; set; }
        public string Message { get; set; } = "";
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new();
    }
}
