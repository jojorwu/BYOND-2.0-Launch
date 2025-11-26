using System.IO;

namespace Core
{
    public static class ValidationUtils
    {
        public static bool IsValidAssetName(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return false;
            }

            if (assetName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            if (assetName.Contains(".."))
            {
                return false;
            }

            return true;
        }
    }
}
