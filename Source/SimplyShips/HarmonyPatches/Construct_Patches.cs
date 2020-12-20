using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SimplyShips
{
    [StaticConstructorOnStartup]
    internal static class HarmonyInit
    {
        static HarmonyInit()
        {
            new Harmony("Spaxxow.SimplyShips").PatchAll();
        }
    }

    [HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.CanBuildOnTerrain))]
    public static class GenConstruct_Patch
    {
        public static bool Prefix(ref bool __result, BuildableDef entDef, IntVec3 c, Map map, Rot4 rot, Thing thingToIgnore = null, ThingDef stuffDef = null)
        {
            if (entDef is TerrainDef terrainDef && terrainDef == SS_DefOf.SS_Deck)
            {
                __result = CanBuildOnTerrain(entDef, c, map, rot, thingToIgnore, stuffDef);
                return false;
            }
            return true;
        }

        public static bool CanBuildOnTerrain(BuildableDef entDef, IntVec3 c, Map map, Rot4 rot, Thing thingToIgnore = null, ThingDef stuffDef = null)
        {
            TerrainAffordanceDef terrainAffordanceNeed = entDef.GetTerrainAffordanceNeed(stuffDef);
            if (terrainAffordanceNeed != null)
            {
                CellRect cellRect = GenAdj.OccupiedRect(c, rot, entDef.Size);
                cellRect.ClipInsideMap(map);
                foreach (IntVec3 item in cellRect)
                {
                    if (!map.terrainGrid.TerrainAt(item).affordances.Contains(terrainAffordanceNeed))
                    {
                        return false;
                    }
                    List<Thing> thingList = item.GetThingList(map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        if (thingList[i] != thingToIgnore)
                        {
                            TerrainDef terrainDef = thingList[i].def.entityDefToBuild as TerrainDef;
                            if (terrainDef != null && !terrainDef.affordances.Contains(terrainAffordanceNeed))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
    }

    [StaticConstructorOnStartup]
    public static class DefPatches
    {
        static DefPatches()
        {
            foreach (TerrainDef t in DefDatabase<TerrainDef>.AllDefs.Where(x => x.IsWater))
            {
                t.affordances.Add(SS_DefOf.SS_Water);
            }
        }
    }
}
