using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;


namespace GravityTurn
{
    public class GT : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Gravity Turn"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Gravity Turn"; } }
        public override string DisplaySection { get { return "Gravity Turn"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }


        [GameParameters.CustomParameterUI("#autoLOC_GT_UseCompactSkin")] // Use compact skin
        public bool useCompact = true;

        [GameParameters.CustomParameterUI("#autoLOC_GT_UseRegularSkin")] // Use regular skin
        public bool useNormal = false;

        [GameParameters.CustomParameterUI("#autoLOC_GT_UseStockSkin")] // Use stock skin
        public bool useStock = false;

        [GameParameters.CustomParameterUI("#autoLOC_GT_UseMechjeb")]
        public bool useMechjebIfAvailable = true;

        bool lastUseCompact;
        bool lastUseNormal;
        bool lastUseStock;
        bool initted = false;

        public override void SetDifficultyPreset(GameParameters.Preset preset) { }

        public override bool Enabled(MemberInfo member, GameParameters parameters) 
        {
            if (!initted)
            {
                initted = true;
                lastUseCompact = useCompact;
                lastUseNormal = useNormal;
                lastUseStock = useStock;
            }
            if (useCompact != lastUseCompact)
            {
                useNormal = false;
                useStock = false;
            }
            else
                if (useNormal != lastUseNormal)
            {
                useCompact = false;
                useStock = false;
            } else if (useStock != lastUseStock)
            {
                useCompact = false;
                useNormal = false;
            }
            lastUseCompact = useCompact;
            lastUseNormal = useNormal;
            lastUseStock = useStock;
            return true; 
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters) { return true; }

        public override IList ValidValues(MemberInfo member) { return null; }
    }

}
