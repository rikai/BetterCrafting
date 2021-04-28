using System.Collections.Generic;

namespace BetterCrafting
{
    public class CategoryData
    {
        public Dictionary<string, string> categoryNames { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, List<string>> categories { get; set; } = new Dictionary<string, List<string>>();
    }
}