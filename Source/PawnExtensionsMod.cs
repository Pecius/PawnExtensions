using HarmonyLib;
using Verse;

namespace PawnExtensions
{
    public class PawnExtensionsMod : Mod
    {
        public PawnExtensionsMod(ModContentPack content) : base(content)
        {
            Harmony harmonyInstance = new Harmony("Pecius.PawnExtensions");
            harmonyInstance.PatchAll();

            DefBackCompatibilityConverter.InstallConverter();
        }
    }
}