using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SimplyShips
{
	public class Command_SetLocalRoute : Command
	{
		private TargetingParameters ForShipDestination()
		{
			TargetingParameters targetingParameters = new TargetingParameters();
			targetingParameters.canTargetLocations = true;
			targetingParameters.canTargetSelf = false;
			targetingParameters.canTargetPawns = false;
			targetingParameters.canTargetFires = false;
			targetingParameters.canTargetBuildings = false;
			targetingParameters.canTargetItems = false;
			targetingParameters.validator = (TargetInfo x) => ship.CanTeleportTo(x.Cell);
			return targetingParameters;
		}
		public override void ProcessInput(Event ev)
		{
			base.ProcessInput(ev);
			SoundStarter.PlayOneShotOnCamera(SoundDefOf.Tick_Tiny, null);
			Texture2D texture2D = ContentFinder<Texture2D>.Get("UI/Commands/PodEject", true);
			Find.Targeter.BeginTargeting(ForShipDestination(), delegate (LocalTargetInfo target)
			{
				this.action(target.Cell);
			}, null, null, texture2D);
		}

		public Action<IntVec3> action;
		public Ship ship;

		public Map map;
	}
}
