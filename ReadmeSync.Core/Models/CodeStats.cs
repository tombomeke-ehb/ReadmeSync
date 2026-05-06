using System.Collections.Generic;

namespace ReadmeSync.Models
{
    public class CodeStats
    {
        public int TotalFiles { get; set; }
        public int TotalNamespaces { get; set; }
        public int TotalTypes { get; set; }
        public int TotalMethods { get; set; }
        public int TotalTodos { get; set; }
        public Dictionary<string, int> TypeBreakdown { get; set; } = new();
        public double AvgMethodsPerType { get; set; }
    }
}
