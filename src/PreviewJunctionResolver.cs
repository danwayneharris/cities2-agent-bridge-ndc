using System;
using System.Collections.Generic;
namespace CitiesIIAgentBridge {
    // Pure identity topology; no positions, Unity state, or snapshot-local lane IDs.
    public static class PreviewJunctionResolver {
        public sealed class Result {
            public string Status;
            public string[] Candidates;
            public Result(string status, string[] candidates) { Status = status; Candidates = candidates; }
        }
        // Each row is original edge identity, preview start identity, preview end identity.
        public static Result Resolve(string[] expectedEdges, string[][] rows, bool complete) {
            if (!complete) return new Result("incomplete", new string[0]);
            var expected = new HashSet<string>(expectedEdges);
            if (expected.Count < 2 || expected.Count != expectedEdges.Length)
                return new Result("unsupported", new string[0]);
            HashSet<string> common = null;
            foreach (var original in expected) {
                var matches = new List<string[]>();
                foreach (var row in rows) {
                    if (row == null || row.Length != 3) return new Result("incomplete", new string[0]);
                    if (row[0] == original) matches.Add(row);
                }
                if (matches.Count == 0) return new Result("missing", new string[0]);
                if (matches.Count != 1) return new Result("ambiguous", new string[0]);
                var edge = matches[0];
                if (String.IsNullOrEmpty(edge[1]) || String.IsNullOrEmpty(edge[2]) || edge[1] == "0:0" || edge[2] == "0:0")
                    return new Result("incomplete", new string[0]);
                if (edge[1] == edge[2]) return new Result("unsupported", new string[0]);
                var endpoints = new HashSet<string> { edge[1], edge[2] };
                if (common == null) common = endpoints; else common.IntersectWith(endpoints);
            }
            var candidates = new List<string>(common); candidates.Sort(StringComparer.Ordinal);
            return new Result(candidates.Count == 1 ? "resolved" : candidates.Count == 0 ? "missing" : "ambiguous", candidates.ToArray());
        }
    }
}
