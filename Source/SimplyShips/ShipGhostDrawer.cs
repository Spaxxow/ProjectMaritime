using HarmonyLib;
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
	public static class ShipGhostDrawer
	{
		public static void DrawGhostThing_NewTmp(IntVec3 center, Rot4 rot, TerrainDef thingDef, Graphic baseGraphic, Color ghostCol, AltitudeLayer drawAltitude,
			Thing thing = null, bool drawPlaceWorkers = true)
		{
			if (baseGraphic == null)
			{
				baseGraphic = thingDef.graphic;
			}
			Graphic graphic = baseGraphic;
			Vector3 loc = GenThing.TrueCenter(center, rot, thingDef.Size, drawAltitude.AltitudeFor());
			DrawShadow(graphic, loc, rot, thingDef);
		}

		public static void DrawShadow(Graphic graphic, Vector3 loc, Rot4 rot, TerrainDef def)
        {
			Mesh mesh = graphic.MeshAt(rot);
			Quaternion quat = graphic.QuatFromRot(rot);
			//if (graphic.extraRotation != 0f)
			//{
			//	quat *= Quaternion.Euler(Vector3.up * graphic.extraRotation);
			//}
			loc += graphic.DrawOffset(rot);
			Material mat = graphic.MatAt(rot, null);
			graphic.DrawMeshInt(mesh, loc, quat, mat);

			var shadowInfo = Traverse.Create(graphic.ShadowGraphic).Field("shadowInfo").GetValue<ShadowData>();
			var shadowMesh = Traverse.Create(graphic.ShadowGraphic).Field("shadowMesh").GetValue<Mesh>();

			if (shadowMesh != null && shadowInfo != null && (Find.CurrentMap == null || !loc.ToIntVec3().InBounds(Find.CurrentMap) 
				|| !Find.CurrentMap.roofGrid.Roofed(loc.ToIntVec3())) && DebugViewSettings.drawShadows)
			{
				Vector3 position = loc + shadowInfo.offset;
				position.y = AltitudeLayer.Shadows.AltitudeFor();
				Graphics.DrawMesh(shadowMesh, position, rot.AsQuat, MatBases.SunShadowFade, 0);
			}
		}

		public static void DrawGhostThing_NewTmp(IntVec3 center, Rot4 rot, ThingDef thingDef, Graphic baseGraphic, Color ghostCol, AltitudeLayer drawAltitude,
	Thing thing = null, bool drawPlaceWorkers = true)
		{
			if (baseGraphic == null)
			{
				baseGraphic = thingDef.graphic;
			}
			Graphic graphic = baseGraphic;
			Vector3 loc = GenThing.TrueCenter(center, rot, thingDef.Size, drawAltitude.AltitudeFor());
			DrawShadow(graphic, loc, rot, thingDef);
		}

		public static void DrawShadow(Graphic graphic, Vector3 loc, Rot4 rot, ThingDef def)
		{
			Mesh mesh = graphic.MeshAt(rot);
			Quaternion quat = graphic.QuatFromRot(rot);
			//if (graphic.extraRotation != 0f)
			//{
			//	quat *= Quaternion.Euler(Vector3.up * graphic.extraRotation);
			//}
			loc += graphic.DrawOffset(rot);
			Material mat = graphic.MatAt(rot, null);
			graphic.DrawMeshInt(mesh, loc, quat, mat);

			var shadowInfo = Traverse.Create(graphic.ShadowGraphic).Field("shadowInfo").GetValue<ShadowData>();
			var shadowMesh = Traverse.Create(graphic.ShadowGraphic).Field("shadowMesh").GetValue<Mesh>();

			if (shadowMesh != null && shadowInfo != null && (Find.CurrentMap == null || !loc.ToIntVec3().InBounds(Find.CurrentMap)
				|| !Find.CurrentMap.roofGrid.Roofed(loc.ToIntVec3())) && DebugViewSettings.drawShadows)
			{
				Vector3 position = loc + shadowInfo.offset;
				position.y = AltitudeLayer.Shadows.AltitudeFor();
				Graphics.DrawMesh(shadowMesh, position, rot.AsQuat, MatBases.SunShadowFade, 0);
			}
		}
	}
}
