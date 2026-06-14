using LabSystem.Core;

namespace LabSystem.Tests
{
    public static class TestHelper
    {
        public static string FindFileUpwards(params string[] pathParts)
        {
            return FileUtilities.FindFileUpwards(pathParts);
        }
    }
}
