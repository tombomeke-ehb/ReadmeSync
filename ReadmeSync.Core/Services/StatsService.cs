using System;
using System.Collections.Generic;
using System.Linq;
using ReadmeSync.Models;

namespace ReadmeSync.Services
{
    public class StatsService
    {
        public CodeStats ComputeStats(IEnumerable<IGrouping<string, CodeFileInfo>> fileGroups, int totalFilesScanned)
        {
            var groupsList = fileGroups.ToList();

            int totalNamespaces = groupsList.Count;
            int totalTypes = groupsList.Sum(g => g.Count());
            int totalMethods = groupsList.Sum(g => g.SelectMany(f => f.Methods).Count());
            int totalTodos = groupsList.Sum(g => g.SelectMany(f => f.Todos).Count());

            var typeBreakdown = new Dictionary<string, int>();
            foreach (var file in groupsList.SelectMany(g => g))
            {
                if (!typeBreakdown.ContainsKey(file.TypeKeyword))
                    typeBreakdown[file.TypeKeyword] = 0;
                typeBreakdown[file.TypeKeyword]++;
            }

            double avgMethodsPerType = totalTypes > 0 ? (double)totalMethods / totalTypes : 0;

            return new CodeStats
            {
                TotalFiles = totalFilesScanned,
                TotalNamespaces = totalNamespaces,
                TotalTypes = totalTypes,
                TotalMethods = totalMethods,
                TotalTodos = totalTodos,
                TypeBreakdown = typeBreakdown,
                AvgMethodsPerType = Math.Round(avgMethodsPerType, 2)
            };
        }
    }
}
