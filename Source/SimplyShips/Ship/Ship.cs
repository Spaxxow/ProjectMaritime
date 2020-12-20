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
    public class Ship : IExposable
    {
        public HashSet<IntVec3> cells = new HashSet<IntVec3>();
        private Dictionary<IntVec3, TerrainDef> terrains = new Dictionary<IntVec3, TerrainDef>();
        private HashSet<Thing> things = new HashSet<Thing>();
        private Map map;
        public Helm helm;
        public Ship()
        {

        }
        public Ship(Helm helm)
        {
            this.map = helm.Map;
            this.helm = helm;
        }
        public void Init()
        {
            map.floodFiller.FloodFill(helm.Position, (IntVec3 x) => x.GetTerrain(map) == SS_DefOf.SS_Deck, delegate (IntVec3 x)
            {
                cells.Add(x);
            });
            foreach (var cell in cells)
            {
                foreach (var t in cell.GetThingList(map))
                {
                    things.Add(t);
                }
                terrains[cell] = cell.GetTerrain(map);
            }
        }

        public void TeleportTo(IntVec3 dest)
        {
            var rootPos = cells.First();
            cells.Clear();
            foreach (var terrain in terrains)
            {
                map.terrainGrid.RemoveTopLayer(terrain.Key, false);
                var newPos = terrain.Key - rootPos + dest;
                map.terrainGrid.SetTerrain(newPos, terrain.Value);
                MoteMaker.MakeStaticMote(newPos, map, ThingDefOf.Mote_AirPuff, 5f);
                cells.Add(newPos);
            }

            foreach (var thing in things)
            {
                var newPos = thing.Position - rootPos + dest;
                if (!thing.DestroyedOrNull())
                {
                    thing.DeSpawn();
                    GenSpawn.Spawn(thing, newPos, map);
                }
            }

            terrains.Clear();
            foreach (var cell in cells)
            {
                foreach (var t in cell.GetThingList(map))
                {
                    things.Add(t);
                }
                terrains[cell] = cell.GetTerrain(map);
            }
        }
        public void ExposeData()
        {
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref helm, "helm");
            Scribe_Collections.Look(ref things, "things", LookMode.Reference);
            Scribe_Collections.Look(ref terrains, "terrains", LookMode.Value, LookMode.Def, ref intVecKeys, ref terrainDefValues);
            Scribe_Collections.Look(ref cells, "cells", LookMode.Value);
        }

        private List<IntVec3> intVecKeys;
        private List<TerrainDef> terrainDefValues;
    }
}
