﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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

        private bool heightDirty;
        private int maxHeightCached;
        private int MaxHeight
        {
            get
            {
                if (heightDirty)
                {
                    maxHeightCached = cells.GroupBy(item => item.x).Select(group => group.ToList()).ToList().OrderByDescending(x => x.Count).FirstOrDefault().Count();
                    heightDirty = false;
                }
                return maxHeightCached;
            }
        }

        private bool widhtDirty;
        private int maxWidhtCached;
        private int MaxWidth
        {
            get
            {
                if (widhtDirty)
                {
                    maxWidhtCached = cells.GroupBy(item => item.z).Select(group => group.ToList()).ToList().OrderByDescending(x => x.Count).FirstOrDefault().Count();
                    widhtDirty = false;
                }
                return maxWidhtCached;
            }
        }

        HashSet<List<IntVec3>> goodVerticalWaterCells = new HashSet<List<IntVec3>>();
        HashSet<List<IntVec3>> goodHorizontalWaterCells = new HashSet<List<IntVec3>>();
        public void SpawnSetup()
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
            widhtDirty = true;
            heightDirty = true;

            var waterCells = this.map.AllCells.Where(x => IsWaterOrDeck(x));

            var verticalWater = waterCells.GroupBy(item => item.x).Select(group => group.ToList());
            var horizontalWater = waterCells.GroupBy(item => item.z).Select(group => group.ToList());

            foreach (var waterList in verticalWater)
            {
                var newList = new List<IntVec3>();
                var zPos = -1;
                foreach (var cell in waterList.OrderBy(x => x.z))
                {
                    if (cell.z != zPos + 1)
                    {
                        if (newList.Count > this.MaxHeight)
                        {
                            goodVerticalWaterCells.Add(newList.ListFullCopy());
                        }
                        newList = new List<IntVec3>();
                    }
                    zPos = cell.z;
                    newList.Add(cell);
                }
                if (newList.Count > this.MaxHeight)
                {
                    goodVerticalWaterCells.Add(newList.ListFullCopy());
                }
            }

            foreach (var waterList in horizontalWater)
            {
                var newList = new List<IntVec3>();
                var xPos = -1;
                foreach (var cell in waterList.OrderBy(x => x.x))
                {
                    if (cell.x != xPos + 1)
                    {
                        if (newList.Count > this.MaxWidth)
                        {
                            goodHorizontalWaterCells.Add(newList.ListFullCopy());
                        }
                        newList = new List<IntVec3>();
                    }
                    xPos = cell.x;
                    newList.Add(cell);
                }
                if (newList.Count > this.MaxWidth)
                {
                    goodHorizontalWaterCells.Add(newList.ListFullCopy());
                }
            }

            foreach (var goodList in goodVerticalWaterCells)
            {
                var lastInd = goodList.Count - 1;
                var prevInd = lastInd - (this.MaxHeight / 2);
                var count = goodList.Count - prevInd;
                goodList.RemoveRange(prevInd, count);
                var firstInd = 0;
                var secondInd = firstInd + (this.MaxHeight / 2);
                goodList.RemoveRange(firstInd, secondInd);
            }

            foreach (var goodList in goodHorizontalWaterCells)
            {
                var lastInd = goodList.Count - 1;
                var prevInd = lastInd - (this.MaxWidth / 2);
                var count = goodList.Count - prevInd;
                goodList.RemoveRange(prevInd, count);
                var firstInd = 0;
                var secondInd = firstInd + (this.MaxWidth / 2);
                goodList.RemoveRange(firstInd, secondInd);
            }
        }


        public static readonly Color CanPlaceColor = new Color(0.5f, 1f, 0.6f, 0.4f);

        public static readonly Color CannotPlaceColor = new Color(1f, 0f, 0f, 0.4f);

        private Dictionary<IntVec3, HashSet<IntVec3>> cellsToCheck = new Dictionary<IntVec3, HashSet<IntVec3>>();
        public bool CanTeleportTo(IntVec3 dest)
        {
            if (CanPassBetween(dest))
            {
                if (!cellsToCheck.TryGetValue(dest, out HashSet<IntVec3> cells2))
                {
                    bool process = true;
                    var cells3 = new HashSet<IntVec3>();
                    map.floodFiller.FloodFill(helm.Position, (IntVec3 x) => process && (x.GetEdifice(map)?.def == SS_DefOf.SS_Helm || CanPassBetween(x)), delegate (IntVec3 x)
                    {
                        cells3.Add(x);
                        if (x.x == dest.x && x.z == dest.z)
                        {
                            process = false;
                            return;
                        }
                    });
                    cellsToCheck[dest] = cells3;
                    cells2 = cells3;
                }
                if (cells2.Where(x => x.z == dest.z && x.x == dest.x).Any())
                {
                    var rootPos = cells.First();
                    foreach (var terrain in terrains)
                    {
                        var newPos = terrain.Key - rootPos + dest;
                        ShipGhostDrawer.DrawGhostThing_NewTmp(newPos, Rot4.North, terrain.Value, terrain.Value.graphic, Designator_Place.CanPlaceColor, AltitudeLayer.Blueprint);
                    }
                    return true;
                }
            }
            var rootPos2 = cells.First();
            foreach (var terrain in terrains)
            {
                var newPos = terrain.Key - rootPos2 + dest;
                ShipGhostDrawer.DrawGhostThing_NewTmp(newPos, Rot4.North, terrain.Value, terrain.Value.graphic, Designator_Place.CannotPlaceColor, AltitudeLayer.Blueprint);
            }
            return false;
        }

        private bool CanPassBetween(IntVec3 cell)
        {
            if (!FindCell(goodVerticalWaterCells, cell))
            {
                return false;
            }
            if (!FindCell(goodHorizontalWaterCells, cell))
            {
                return false;
            }
            return true;
        }

        private bool FindCell(HashSet<List<IntVec3>> lists, IntVec3 cell)
        {
            foreach (var list in lists)
            {
                foreach (var c in list)
                {
                    if (c.z == cell.z && c.x == cell.x)
                    {
                        return true;
                    }
                } 
            }
            return false;
        }

        private bool IsWaterOrDeck(IntVec3 cell)
        {
            var def = cell.GetTerrain(map);
            if (def.IsWater || def == SS_DefOf.SS_Deck)
            {
                if (cell.GetEdifice(map) == null)
                {
                    return true;
                }
            }
            return false;
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
            Scribe_Values.Look(ref heightDirty, "heightDirty", true);
            Scribe_Values.Look(ref widhtDirty, "widhtDirty", true);
        }

        private List<IntVec3> intVecKeys;
        private List<TerrainDef> terrainDefValues;
    }
}
