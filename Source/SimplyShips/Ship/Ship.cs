using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SimplyShips
{
    public class Ship : IExposable
    {
        public Dictionary<IntVec3, TerrainDef> terrains = new Dictionary<IntVec3, TerrainDef>();
        private Dictionary<IntVec3, List<Thing>> cellsWithThings = new Dictionary<IntVec3, List<Thing>>();
        private Dictionary<IntVec3, TerrainDef> rotatedTerrains = new Dictionary<IntVec3, TerrainDef>();
        private Dictionary<IntVec3, List<Thing>> rotatedCellsWithThings = new Dictionary<IntVec3, List<Thing>>();

        private Map map;
        public Helm helm;
        private Rot4 curRotation;
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
                    maxHeightCached = terrains.Keys.GroupBy(item => item.x).Select(group => group.ToList()).ToList().OrderByDescending(x => x.Count).FirstOrDefault().Count();
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
                    maxWidhtCached = terrains.Keys.GroupBy(item => item.z).Select(group => group.ToList()).ToList().OrderByDescending(x => x.Count).FirstOrDefault().Count();
                    widhtDirty = false;
                }
                return maxWidhtCached;
            }
        }

        HashSet<IntVec3> goodWaterCells = new HashSet<IntVec3>();

        public void ReInit()
        {
            terrains.Clear();
            if (cellsWithThings != null)
            {
                cellsWithThings.Clear();
            }
            else
            {
                cellsWithThings = new Dictionary<IntVec3, List<Thing>>();
            }

            map.floodFiller.FloodFill(helm.Position, (IntVec3 x) => x.GetTerrain(map) == SS_DefOf.SS_Deck, delegate (IntVec3 x)
            {
                terrains[x] = x.GetTerrain(map);
            });

            foreach (var cell in terrains.Keys)
            {
                foreach (var t in cell.GetThingList(map))
                {
                    if (cellsWithThings.ContainsKey(cell))
                    {
                        cellsWithThings[cell].Add(t);
                    }
                    else
                    {
                        cellsWithThings[cell] = new List<Thing>() { t };
                    }
                }
            }
            widhtDirty = true;
            heightDirty = true;
            rotatedTerrains.Clear();
            foreach (var terrain in terrains)
            {
                rotatedTerrains[terrain.Key] = terrain.Value;
            }
            rotatedCellsWithThings.Clear();
            foreach (var thing in cellsWithThings)
            {
                rotatedCellsWithThings[thing.Key] = thing.Value;
            }
        }

        public void SpawnSetup()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            ReInit();
            HashSet<List<IntVec3>> goodVerticalWaterCells = new HashSet<List<IntVec3>>();
            HashSet<List<IntVec3>> goodHorizontalWaterCells = new HashSet<List<IntVec3>>();
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

            foreach (var goodList in goodVerticalWaterCells)
            {
                foreach (var goodList2 in goodHorizontalWaterCells)
                {
                    foreach (var cell1 in goodList)
                    {
                        foreach (var cell2 in goodList2)
                        {
                            if (cell1.x == cell2.x && cell1.z == cell2.z)
                            {
                                goodWaterCells.Add(cell1);
                            }
                        }
                    }
                } 
            }
            stopWatch.Stop();
            Log.Message("Total time: " + stopWatch.Elapsed + " - good water cells" + goodWaterCells.Count);
        }

        public void RotateShip(RotationDirection rotationDirection)
        {
            this.curRotation = curRotation.Rotated(rotationDirection);
            var rootPos = terrains.Keys.First();
            rotatedTerrains.Clear();
            foreach (var terrain in terrains)
            {
                rotatedTerrains[(terrain.Key.RotatedBy(curRotation) - (rootPos.RotatedBy(curRotation)) + rootPos)] = terrain.Value;
            }
            rotatedCellsWithThings.Clear();
            foreach (var thing in cellsWithThings)
            {
                rotatedCellsWithThings[(thing.Key.RotatedBy(curRotation) - (rootPos.RotatedBy(curRotation)) + rootPos)] = thing.Value;
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
                    var rootPos = terrains.Keys.First();
                    foreach (var terrain in rotatedTerrains)
                    {
                        var newPos = terrain.Key - rootPos + dest;
                        ShipGhostDrawer.DrawGhostThing_NewTmp(newPos, Rot4.North, terrain.Value, terrain.Value.graphic, Designator_Place.CanPlaceColor, AltitudeLayer.Blueprint);
                    }
                    foreach (var list in rotatedCellsWithThings)
                    {
                        var newPos = list.Key - rootPos + dest;
                        foreach (var thing in list.Value)
                        {
                            thing.def.graphic.Draw(newPos.ToVector3Shifted(), Rot4.North, thing, 0);
                        } 
                    }
                    return true;
                }
            }
            return false;
        }

        private bool CanPassBetween(IntVec3 cell)
        {
            if (goodWaterCells.Contains(cell))
            {
                return true;
            }
            return false;
        }
        private bool IsWaterOrDeck(IntVec3 cell)
        {
            if (terrains.Keys.Contains(cell))
            {
                return true;
            }
            var def = cell.GetTerrain(map);
            if (def.IsWater)
            {
                var edifice = cell.GetEdifice(map);
                if (edifice == null)
                {
                    return true;
                }
            }
            return false;
        }
        public void TeleportTo(IntVec3 dest)
        {
            var rootPos = terrains.Keys.First();
            foreach (var terrain in terrains)
            {
                map.terrainGrid.RemoveTopLayer(terrain.Key, false);
            }
            foreach (var terrain in rotatedTerrains)
            {
                var newPos = terrain.Key - rootPos + dest;
                map.terrainGrid.SetTerrain(newPos, terrain.Value);
                MoteMaker.MakeStaticMote(newPos, map, ThingDefOf.Mote_AirPuff, 5f);
            }
            foreach (var list in cellsWithThings.Values)
            {
                foreach (var thing in list)
                {
                    var newPos = thing.Position - rootPos + dest;
                    if (!thing.DestroyedOrNull())
                    {
                        thing.DeSpawn();
                        GenSpawn.Spawn(thing, newPos, map);
                    }
                }
            }

            this.ReInit();
        }
        public void ExposeData()
        {
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref helm, "helm");
            Scribe_Collections.Look(ref terrains, "terrains", LookMode.Value, LookMode.Def, ref intVecKeys, ref terrainDefValues);
            Scribe_Collections.Look(ref cellsWithThings, "cellsWithThings", LookMode.Value, LookMode.Reference, ref intVecKeys2, ref thingValues);
            Scribe_Values.Look(ref heightDirty, "heightDirty", true);
            Scribe_Values.Look(ref widhtDirty, "widhtDirty", true);
            Scribe_Values.Look(ref curRotation, "curRotation", Rot4.South);
        }

        private List<IntVec3> intVecKeys;
        private List<TerrainDef> terrainDefValues;


        private List<IntVec3> intVecKeys2;
        private List<List<Thing>> thingValues;
    }
}
