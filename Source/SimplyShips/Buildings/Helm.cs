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
    public class Helm : Building
    {
        private ShipManager shipManager;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.shipManager = Find.World.GetComponent<ShipManager>();
            if (!shipManager.TryFindShipParent(this, out Ship ship))
            {
                MakeShip();
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            yield return new Command_SetLocalRoute
            {
                defaultLabel = "SS.CommandSetLocalRoute".Translate(),
                defaultDesc = "SS.CommandSetLocalRouteDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc12,
                map = this.Map,
                action = StartRoute
            };
        }

        private void StartRoute(IntVec3 cell)
        {
            if (!shipManager.TryFindShipParent(this, out Ship ship))
            {
                ship = MakeShip();
            }
            ship.TeleportTo(cell);
        }

        private Ship MakeShip()
        {
            var ship = new Ship(this);
            ship.Init();
            shipManager.RegisterShip(ship);
            return ship;
        }
    }
}
