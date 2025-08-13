using System.Collections.Generic;
using TSMapEditor.CCEngine;

namespace TSMapEditor.Models
{
    public class StringTable(string csfFileName,int capacity = 0)
    {
        /// <summary>
        /// Map of all CSF label/string pairs that have been parsed.
        /// </summary>
        public readonly Dictionary<string, CsfString> map = new Dictionary<string, CsfString>(capacity);
        public string CSFFileName = csfFileName;
        public IEnumerable<CsfString> GetStringEnumerator()
        {
            return map.Values;
        }

        public string LookUpValue(string label)
        {
            return map.TryGetValue(label.ToUpper(), out var result) ? result.Value : null;
        }

        public CsfString LookUpString(string label)
        {
            return map.TryGetValue(label.ToUpper(), out var result) ? result : null;
        }
    }
}
