using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using BepInEx.Logging;
using UnityEngine;

namespace ValheimEpicLootNoFogDestinations
{
    internal sealed class ExploredCentreIndex
    {
        private sealed class Offer
        {
            internal Vector3 Point;
            internal float Min, Max, Expires;
            internal bool Found;
        }

        private readonly MonoBehaviour host;
        private readonly ManualLogSource log;
        private readonly System.Random random = new System.Random();
        private Dictionary<Heightmap.Biome, List<Vector3>> points = new Dictionary<Heightmap.Biome, List<Vector3>>();
        private Dictionary<Heightmap.Biome, List<Vector3>>? buildingPoints;
        private readonly Dictionary<Heightmap.Biome, Offer> offers = new Dictionary<Heightmap.Biome, Offer>();
        private Coroutine? running;
        private Minimap? map;
        private Player? player;
        private WorldGenerator? world;
        private long revision, scannedRevision = -1;
        private float nextScan;
        private bool complete;

        internal ExploredCentreIndex(MonoBehaviour host, ManualLogSource log) { this.host = host; this.log = log; }
        internal bool Building => running != null;
        internal void Revealed() { revision++; }
        internal void ForgetOffer(Heightmap.Biome biome) { offers.Remove(biome); }

        internal void Reset()
        {
            if (running != null) host.StopCoroutine(running);
            running = null;
            buildingPoints = null;
            points.Clear();
            offers.Clear();
            map = null;
            player = null;
            world = null;
            complete = false;
            revision++;
            scannedRevision = -1;
            nextScan = 0;
        }

        internal void CheckContext()
        {
            if (map != Minimap.instance || player != Player.m_localPlayer || world != WorldGenerator.instance) Reset();
        }

        private bool Ensure()
        {
            CheckContext();
            if (!Minimap.instance || !Player.m_localPlayer || WorldGenerator.instance == null || !ZoneSystem.instance) return false;
            map = Minimap.instance;
            player = Player.m_localPlayer;
            world = WorldGenerator.instance;
            if (running == null && scannedRevision != revision && Time.realtimeSinceStartup >= nextScan)
                running = host.StartCoroutine(Scan(revision));
            return true;
        }

        private IEnumerator Scan(long startedRevision)
        {
            // Always yield once so StartCoroutine's handle is assigned before any completion path.
            yield return null;
            var next = new Dictionary<Heightmap.Biome, List<Vector3>>();
            buildingPoints = next;
            try
            {
                NativeBridge.EnsureNativeIndex();
                float deadline = Time.realtimeSinceStartup + 30f;
                while (!NativeBridge.IndexReady && Time.realtimeSinceStartup < deadline) yield return null;
                if (!NativeBridge.IndexReady || !map) yield break;
                BitArray own = NativeBridge.Explored(map), shared = NativeBridge.Shared(map);
                int size = map.m_textureSize;
                if ((long)size * size != own.Length || own.Length != shared.Length) yield break;
                var budget = Stopwatch.StartNew();
                int found = 0;
                for (int i = 0; i < own.Length; i++)
                {
                    if (CentreRules.Visible(own, shared, i))
                    {
                        CentreRules.PixelCentre(i, size, map.m_pixelSize, out float x, out float z);
                        Heightmap.Biome biome = world!.GetBiome(x, z);
                        if (biome != Heightmap.Biome.None && biome != Heightmap.Biome.All &&
                            NativeBridge.Validate(biome, new Vector3(x, 0, z), 0, float.MaxValue, false, out var point))
                        {
                            if (!next.TryGetValue(biome, out var list)) next[biome] = list = new List<Vector3>();
                            list.Add(point);
                            found++;
                        }
                    }
                    // Bound CPU work per frame, including scans of mostly-unexplored maps.
                    if ((i & 63) == 0 && budget.Elapsed.TotalMilliseconds >= 2)
                    {
                        yield return null;
                        budget.Restart();
                    }
                }
                points = next;
                complete = true;
                scannedRevision = startedRevision;
                offers.Clear();
                log.LogInfo($"Explored-centre scan ready: {found} eligible centres in {points.Count} biomes.");
            }
            finally
            {
                buildingPoints = null;
                running = null;
                nextScan = Time.realtimeSinceStartup + 5f;
            }
        }

        internal bool TryGet(Heightmap.Biome biome, out Vector3 point, bool recheck = false)
        {
            point = Vector3.zero;
            if (!Ensure() || !NativeBridge.IndexReady) return false;
            offers.TryGetValue(biome, out var cached);
            if (!recheck && cached != null && cached.Expires > Time.realtimeSinceStartup)
            {
                if (!cached.Found) return false;
                if (NativeBridge.IsExplored(map!, cached.Point)) { point = cached.Point; return true; }
            }
            NativeBridge.GetRadiusBand(biome, out float min, out float max);
            if (cached != null && cached.Min == min && cached.Max == max && recheck)
            {
                if (cached.Found && NativeBridge.IsExplored(map!, cached.Point) &&
                    (!recheck || NativeBridge.Validate(biome, cached.Point, min, max,
                        CentreRules.InBand(CentreRules.Radius(cached.Point.x, cached.Point.z), min, max), out _)))
                {
                    point = cached.Point;
                    return true;
                }
                if (!cached.Found && !recheck) return false;
            }
            Vector3 selected = Vector3.zero;
            int seenInBand = 0, ties = 0;
            double closest = double.PositiveInfinity;
            void Consider(List<Vector3> list)
            {
                foreach (var candidate in list)
                {
                    if (!NativeBridge.IsExplored(map!, candidate)) continue;
                    double radius = CentreRules.Radius(candidate.x, candidate.z);
                    if (CentreRules.InBand(radius, min, max))
                    {
                        if (random.Next(++seenInBand) == 0) selected = candidate;
                    }
                    else if (seenInBand == 0 && complete)
                    {
                        // Match EpicLoot's fallback: closest radial match when the preferred band
                        // has no eligible point. A fallback must still be explored and valid.
                        double distance = CentreRules.BandDistance(radius, min, max);
                        if (distance < closest) { closest = distance; selected = candidate; ties = 1; }
                        else if (distance == closest && random.Next(++ties) == 0) selected = candidate;
                    }
                }
            }
            if (points.TryGetValue(biome, out var existing)) Consider(existing);
            if (buildingPoints != null && buildingPoints.TryGetValue(biome, out var growing)) Consider(growing);
            bool found = (seenInBand > 0 || !double.IsPositiveInfinity(closest)) &&
                NativeBridge.Validate(biome, selected, min, max, seenInBand > 0, out point) && NativeBridge.IsExplored(map!, point);
            offers[biome] = new Offer { Point = point, Min = min, Max = max, Found = found, Expires = Time.realtimeSinceStartup + 0.5f };
            return found;
        }
    }
}
