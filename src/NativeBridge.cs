using System;
using System.Collections;
using System.Reflection;
using EpicLoot.Adventure;
using EpicLoot.Adventure.Feature;
using HarmonyLib;
using UnityEngine;

namespace ValheimEpicLootNoFogDestinations
{
    internal static class NativeBridge
    {
        internal delegate void RadiusBand(Heightmap.Biome biome, out float min, out float max);
        private delegate bool CandidateCheck(Heightmap.Biome biome, Vector2 candidate, float min, float max,
            bool requireBand, bool requireZoneAgreement, ref int tried, out Vector3 point);

        private static Type RequiredType(string name) => AccessTools.TypeByName(name) ?? throw new MissingMemberException(name);
        internal static readonly Type LocationType = RequiredType("EpicLoot.Adventure.Feature.BountyLocationEarlyCache");
        private static readonly Type IndexType = RequiredType("EpicLoot.Adventure.WorldBiomeIndex");
        internal static readonly MethodInfo LocateMethod = AccessTools.Method(LocationType, "TryGetBiomePoint") ?? throw new MissingMethodException("TryGetBiomePoint");
        internal static readonly RadiusBand GetRadiusBand = AccessTools.MethodDelegate<RadiusBand>(AccessTools.Method(LocationType, "GetRadiusBand"));
        internal static readonly Action EnsureNativeIndex = AccessTools.MethodDelegate<Action>(AccessTools.Method(IndexType, "EnsureBuilt"));
        private static readonly MethodInfo IndexState = AccessTools.PropertyGetter(IndexType, "State") ?? throw new MissingMethodException("WorldBiomeIndex.State");
        private static readonly CandidateCheck CheckCandidate = AccessTools.MethodDelegate<CandidateCheck>(AccessTools.Method(IndexType, "TryCandidate"));
        internal static readonly Func<Minimap, Vector3, bool> IsExplored = AccessTools.MethodDelegate<Func<Minimap, Vector3, bool>>(AccessTools.Method(typeof(Minimap), "IsExplored"));
        internal static readonly AccessTools.FieldRef<Minimap, BitArray> Explored = AccessTools.FieldRefAccess<Minimap, BitArray>("m_explored");
        internal static readonly AccessTools.FieldRef<Minimap, BitArray> Shared = AccessTools.FieldRefAccess<Minimap, BitArray>("m_exploredOthers");
        internal static readonly Func<MerchantListPanel<TreasureMapListElement>, TreasureMapListElement> SelectedTreasure =
            AccessTools.MethodDelegate<Func<MerchantListPanel<TreasureMapListElement>, TreasureMapListElement>>(AccessTools.Method(typeof(MerchantListPanel<TreasureMapListElement>), "GetSelectedItem"));
        internal static readonly Func<MerchantListPanel<BountyListElement>, BountyListElement> SelectedBounty =
            AccessTools.MethodDelegate<Func<MerchantListPanel<BountyListElement>, BountyListElement>>(AccessTools.Method(typeof(MerchantListPanel<BountyListElement>), "GetSelectedItem"));

        internal static bool IndexReady => IndexState.Invoke(null, null)?.ToString() == "Ready";

        internal static bool Validate(Heightmap.Biome biome, Vector3 candidate, float min, float max, bool requireBand, out Vector3 point)
        {
            int tried = 0;
            var p = new Vector2(candidate.x, candidate.z);
            // Preserve EpicLoot's own biome, water, lava, edge and zone-agreement checks,
            // including its relaxed zone-agreement pass. Never relax map visibility.
            return CheckCandidate(biome, p, min, max, requireBand, true, ref tried, out point) ||
                   CheckCandidate(biome, p, min, max, requireBand, false, ref tried, out point);
        }
    }
}
