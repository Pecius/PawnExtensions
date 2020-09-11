using System.Collections.Generic;
using Verse;

namespace PawnExtensions
{
    public class HediffImmunity : Editable
    {
        public List<HediffDef> hediffs;
        public bool restrict;

        public override IEnumerable<string> ConfigErrors()
        {
            if (hediffs == null)
                yield return "hediffs is null";
        }
    }
}