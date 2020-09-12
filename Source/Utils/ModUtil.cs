using System.Text.RegularExpressions;
using Verse;

namespace PawnExtensions.Utils
{
    public static class ModUtil
    {
        private static Regex reModIDPostfix = new Regex($@"(.+?)(?:{ModMetaData.SteamModPostfix}|_copy)?$");

        public static bool IsActive(string modID)
        {
            return ModsConfig.IsActive(modID) || ModsConfig.IsActive(modID + ModMetaData.SteamModPostfix) || ModsConfig.IsActive(modID + "_copy");
        }

        public static string StripModID(this string modID)
        {
            Match result = reModIDPostfix.Match(modID);

            if (!result.Success)
            {
                Log.Error($"Could not strip mod id '{modID}' from postfixes!");
                return modID;
            }

            return result.Groups[1].Value;
        }
    }
}