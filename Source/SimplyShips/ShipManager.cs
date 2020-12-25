using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SimplyShips
{
    public class ShipManager : WorldComponent
    {
        public ShipManager(World world) : base(world)
        {
            ships = new List<Ship>();
        }

        public bool TryFindShipParent(Helm helm, out Ship foundShip)
        {
            foreach (var ship in ships)
            {
                if (ship.helm == helm)
                {
                    foundShip = ship;
                    return true;
                }
            }

            foreach (var ship in ships)
            {
                if (ship.terrains.Keys.Contains(helm.Position))
                {
                    foundShip = ship;
                    return true;
                }
            }
            foundShip = null;
            return false;
        }
        public void RegisterShip(Ship ship)
        {
            ships.Add(ship);
        }

        public void DeregisterShip(Ship ship)
        {
            ships.Remove(ship);
        }

        bool update;
        public override void FinalizeInit()
        {
            base.FinalizeInit();

        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (!update)
            {
                foreach (var ship in ships)
                {
                    ship.SpawnSetup();
                }
                update = true;
            }
        }

        private List<Ship> ships;
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref ships, "ships", LookMode.Deep);
        }
    }
}
