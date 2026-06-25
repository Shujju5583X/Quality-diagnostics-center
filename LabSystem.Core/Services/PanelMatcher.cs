using System.Collections.Generic;
using System.Linq;
using LabSystem.Core.Models;

namespace LabSystem.Core.Services
{
    public static class PanelMatcher
    {
        public static List<PanelMatch> MatchPanels(IEnumerable<TestType> orderedTestTypes, IEnumerable<TestPanel> panels)
        {
            var matches = new List<PanelMatch>();
            var orderedIds = new HashSet<int>(orderedTestTypes.Select(t => t.TypeId));
            var appliedIds = new HashSet<int>();

            foreach (var panel in panels.OrderByDescending(p => p.TestTypes.Count))
            {
                var panelTypeIds = panel.TestTypes.Select(t => t.TypeId).ToList();
                if (panelTypeIds.Count > 0 && panelTypeIds.All(id => orderedIds.Contains(id) && !appliedIds.Contains(id)))
                {
                    matches.Add(new PanelMatch
                    {
                        Panel = panel,
                        MatchedTypeIds = panelTypeIds,
                        Price = panel.Price
                    });
                    foreach (var id in panelTypeIds)
                        appliedIds.Add(id);
                }
            }

            return matches;
        }
    }

    public class PanelMatch
    {
        public TestPanel Panel { get; set; }
        public List<int> MatchedTypeIds { get; set; }
        public decimal Price { get; set; }
    }
}
