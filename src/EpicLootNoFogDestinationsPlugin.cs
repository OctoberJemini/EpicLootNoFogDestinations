using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using EpicLoot.Adventure;
using EpicLoot.Adventure.Feature;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimEpicLootNoFogDestinations
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(EpicGuid)]
    public sealed class EpicLootNoFogDestinationsPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "kenjorissen.mods.epiclootnofogdestinations";
        public const string PluginName = "EpicLoot: No Fog Destinations";
        public const string PluginVersion = "0.3.0";
        public const string EpicGuid = "randyknapp.mods.epicloot";
        private static EpicLootNoFogDestinationsPlugin? Instance;
        private ConfigEntry<bool> enabledSetting = null!;
        private Harmony? harmony;
        private ExploredCentreIndex? index;
        private bool reportedError;
        private bool reportedWarningError;
        private bool previewQueued;
        private readonly CompatibilityWarning compatibilityWarning = new CompatibilityWarning();
        private bool Active => enabledSetting.Value && index != null;
        private const string MissingCentre = "No suitable explored centre found. Explore more of this biome.";
        private const string Scanning = "Checking explored locations...";

        private void Awake()
        {
            Instance = this;
            enabledSetting = Config.Bind("General", "Enabled", true, "Restrict NEW EpicLoot bounties and treasures to explored centres. Disabling restores EpicLoot's original placement.");
            try { RegisterWarningPreview(); }
            catch (Exception error) { Logger.LogWarning("Compatibility warning preview command unavailable: " + error); }
            var installed = Chainloader.PluginInfos.TryGetValue(EpicGuid, out var epic) ? epic.Metadata.Version : null;
            compatibilityWarning.Reason = CompatibilityWarning.VersionReason(installed);
            if (compatibilityWarning.Reason == null)
            {
                try
                {
                    var failures = Compatibility.Check(epic.Instance.GetType().Assembly, ApiContract.Entries);
                    if (failures.Count > 0) compatibilityWarning.Reason = "EpicLoot " + installed + ": " + string.Join("; ", failures.ToArray());
                    else Logger.LogInfo("EpicLoot " + installed + " passed API checks; installing explored-destination hooks.");
                }
                catch (Exception error) { compatibilityWarning.Reason = "Could not inspect EpicLoot APIs: " + error.Message; }
            }
            if (compatibilityWarning.Reason != null)
            {
                Logger.LogError("No exploration patches installed. " + compatibilityWarning.Reason);
                return;
            }
            try { InstallSupportedPatches(); }
            catch (Exception error)
            {
                compatibilityWarning.Reason = CompatibilityWarning.HookFailureReason;
                index?.Reset();
                index = null;
                try { harmony?.UnpatchSelf(); }
                catch (Exception cleanupError) { Logger.LogError("Could not clean up exploration patches: " + cleanupError); }
                Logger.LogError("Could not install exploration patches. Exploration restriction is inactive: " + error);
            }
        }

        // Do not JIT/resolve EpicLoot's internal bridge until after checking its API contract.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InstallSupportedPatches()
        {
            _ = NativeBridge.LocateMethod;
            index = new ExploredCentreIndex(this, Logger);
            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(EpicLootNoFogDestinationsPlugin).Assembly);
            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded: new offers/placements use explored centres; existing quests are unchanged.");
        }

        private void Update()
        {
            index?.CheckContext();
            if (compatibilityWarning.Reason == null || !enabledSetting.Value || !Player.m_localPlayer) return;
            try
            {
                compatibilityWarning.TryShow(enabledSetting.Value, WorldGenerator.instance,
                    Player.m_localPlayer.GetPlayerID(), UnifiedPopup.IsAvailable() && !UnifiedPopup.IsVisible() && !Console.IsVisible(),
                    message => ShowWarningPopup(CompatibilityWarning.Header, message));
            }
            catch (Exception error)
            {
                if (!reportedWarningError)
                {
                    Logger.LogError("Could not display compatibility warning: " + error);
                    reportedWarningError = true;
                }
            }
        }
        private static void ShowWarningPopup(string header, string message)
        {
            UnifiedPopup.Push(new WarningPopup(header, message, () =>
            {
                UnifiedPopup.Pop();
                Instance?.Logger.LogInfo("Compatibility warning popup acknowledged.");
            }, localizeText: false));
            UnifiedPopup.SetFocus();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterWarningPreview()
        {
            // UI-only, not a cheat/network command. The static callback retains no plugin instance.
            _ = new Terminal.ConsoleCommand("epiclootnofog_warningpreview",
                "Preview the compatibility warning; does not change EpicLoot, settings or restrictions.",
                args =>
                {
                    var plugin = Instance;
                    if (plugin != null) plugin.PreviewWarning(args.Context);
                    else args.Context.AddString("EpicLoot: No Fog Destinations is not loaded.");
                });
        }

        private void PreviewWarning(Terminal terminal)
        {
            if (!Player.m_localPlayer || !UnifiedPopup.IsAvailable())
            {
                terminal.AddString("Enter a world and wait for the UI before previewing the warning.");
                return;
            }
            if (UnifiedPopup.IsVisible())
            {
                terminal.AddString("Close the existing popup before previewing the warning.");
                return;
            }
            if (previewQueued) return;
            var context = new RequestContext<Player, WorldGenerator, Minimap>(
                Player.m_localPlayer, WorldGenerator.instance, Minimap.instance);
            int frame = Time.frameCount;
            float deadline = Time.realtimeSinceStartup + 10f;
            terminal.AddString("Queuing warning preview; the console will close automatically.");
            // SendInput reactivates its input field AFTER running this command. Hide
            // the console now and deliver on a later frame, after that handler finishes.
            if (Console.instance) Console.instance.m_chatWindow.gameObject.SetActive(false);
            previewQueued = true;
            try { StartCoroutine(ShowPreviewLater(context, frame, deadline)); }
            catch (Exception error)
            {
                previewQueued = false;
                Logger.LogError("Could not queue compatibility warning preview: " + error);
            }
        }

        private IEnumerator ShowPreviewLater(RequestContext<Player, WorldGenerator, Minimap> context, int frame, float deadline)
        {
            try
            {
                while (context.Player && context.Matches(Player.m_localPlayer, WorldGenerator.instance, Minimap.instance) &&
                    Time.realtimeSinceStartup < deadline)
                {
                    if (CompatibilityWarning.PreviewReady(frame, Time.frameCount, Console.IsVisible(),
                        UnifiedPopup.IsAvailable(), UnifiedPopup.IsVisible()))
                    {
                        try { ShowWarningPopup("PREVIEW — " + CompatibilityWarning.Header, CompatibilityWarning.PreviewMessage()); }
                        catch (Exception error) { Logger.LogError("Could not preview compatibility warning: " + error); }
                        yield break;
                    }
                    yield return null;
                }
                Logger.LogWarning("Compatibility warning preview cancelled: session changed or UI stayed unavailable.");
            }
            finally { previewQueued = false; }
        }

        private void OnDestroy()
        {
            index?.Reset();
            harmony?.UnpatchSelf();
            Instance = null;
        }

        private bool TryPoint(Heightmap.Biome biome, out Vector3 point, bool recheck = false)
        {
            point = Vector3.zero;
            try { return index != null && index.TryGet(biome, out point, recheck); }
            catch (Exception error)
            {
                if (!reportedError) { Logger.LogError("Explored placement unavailable; refusing new purchases/acceptance: " + error); reportedError = true; }
                return false;
            }
        }

        private string UnavailableReason => index?.Building == true ? Scanning : MissingCentre;
        private static void Tell(string message)
        {
            if (Player.m_localPlayer) Player.m_localPlayer.Message(MessageHud.MessageType.Center, message);
        }

        private void RefreshButton(object panel)
        {
            if (!Active) return;
            Button? button = null;
            Heightmap.Biome biome = Heightmap.Biome.None;
            if (panel is MerchantListPanel<TreasureMapListElement> treasure)
            {
                var row = NativeBridge.SelectedTreasure(treasure);
                if (!row || row.AlreadyPurchased) return;
                button = treasure.MainButton;
                biome = row.Biome;
            }
            else if (panel is MerchantListPanel<BountyListElement> bounty)
            {
                var row = NativeBridge.SelectedBounty(bounty);
                if (!row || row.BountyInfo.State != BountyState.Available) return;
                button = bounty.MainButton;
                biome = row.BountyInfo.Biome;
            }
            if (!button) return;
            if (!TryPoint(biome, out _))
            {
                button.interactable = false;
                var tooltip = button.GetComponent<UITooltip>() ?? button.gameObject.AddComponent<UITooltip>();
                tooltip.m_text = UnavailableReason;
            }
            else
            {
                // Leave vanilla affordability/progression decisions intact. Bounty buttons do
                // not clear their tooltip every frame, so remove only our own diagnostic text.
                var tooltip = button.GetComponent<UITooltip>();
                if (tooltip && (tooltip.m_text == MissingCentre || tooltip.m_text == Scanning)) tooltip.m_text = "";
            }
        }

        private bool CanClick(object panel)
        {
            if (!Active) return true;
            Heightmap.Biome biome;
            if (panel is MerchantListPanel<TreasureMapListElement> treasure)
            {
                var row = NativeBridge.SelectedTreasure(treasure);
                if (!row || row.AlreadyPurchased) return true;
                biome = row.Biome;
            }
            else if (panel is MerchantListPanel<BountyListElement> bounty)
            {
                var row = NativeBridge.SelectedBounty(bounty);
                if (!row || row.BountyInfo.State != BountyState.Available) return true;
                biome = row.BountyInfo.Biome;
            }
            else return true;
            if (TryPoint(biome, out _, true)) return true;
            Tell(UnavailableReason);
            return false; // Before EpicLoot's action latch, currency removal or save mutation.
        }

        // This factory deliberately contains no yield: capture the request now, not
        // when Unity eventually starts enumerating it.
        private IEnumerator CreatePlacementRoutine(Heightmap.Biome biome, Action<bool, Vector3> callback)
        {
            var context = new RequestContext<Player, WorldGenerator, Minimap>(
                Player.m_localPlayer, WorldGenerator.instance, Minimap.instance);
            float deadline = Time.realtimeSinceStartup + 30f;
            var request = new PlacementRequest<Vector3>(callback, Vector3.zero,
                error => Logger.LogError("Placement request error; failed validation completes as failure, and upstream callbacks are never retried: " + error));
            return request.Guard(Locate(biome, context, request, deadline));
        }

        private IEnumerator Locate(Heightmap.Biome biome, RequestContext<Player, WorldGenerator, Minimap> context,
            PlacementRequest<Vector3> request, float deadline)
        {
            bool success = false;
            Vector3 point = Vector3.zero;
            while (context.Player && context.Matches(Player.m_localPlayer, WorldGenerator.instance, Minimap.instance) &&
                Time.realtimeSinceStartup < deadline)
            {
                if (TryPoint(biome, out point, true)) { success = true; break; }
                if (index?.Building != true || Time.realtimeSinceStartup >= deadline) break;
                yield return null;
            }
            if (success)
            {
                // Recheck the exact returned centre immediately before handing control back to
                // EpicLoot. Its existing callbacks commit/charge only on successful placement.
                success = context.Player && context.Map && Time.realtimeSinceStartup < deadline &&
                    context.Matches(Player.m_localPlayer, WorldGenerator.instance, Minimap.instance) &&
                    NativeBridge.IsExplored(context.Map!, point);
            }
            if (success)
            {
                Logger.LogInfo($"New {biome} adventure centre: ({point.x:0.0}, {point.z:0.0}); explored=True.");
                index?.ForgetOffer(biome);
            }
            else
            {
                Tell(MissingCentre);
                Logger.LogInfo($"No explored {biome} centre: placement refused without purchase/acceptance.");
            }
            request.Complete(success, point);
        }

        private static IEnumerable<MethodBase> PanelMethods(string name)
        {
            foreach (string typeName in new[] { "TreasureMapListPanel", "AvailableBountiesListPanel" })
            {
                var type = AccessTools.TypeByName("EpicLoot.Adventure.Feature." + typeName) ?? throw new MissingMemberException(typeName);
                yield return AccessTools.DeclaredMethod(type, name) ?? throw new MissingMethodException(typeName, name);
            }
        }

        [HarmonyPatch]
        private static class OfferButtonPatch
        {
            private static IEnumerable<MethodBase> TargetMethods() => PanelMethods("RefreshButton");
            private static void Postfix(object __instance) => Instance?.RefreshButton(__instance);
        }

        [HarmonyPatch]
        private static class OfferClickPatch
        {
            private static IEnumerable<MethodBase> TargetMethods() => PanelMethods("OnMainButtonClicked");
            private static bool Prefix(object __instance) => Instance?.CanClick(__instance) ?? true;
        }

        [HarmonyPatch]
        private static class PlacementPatch
        {
            private static MethodBase TargetMethod() => NativeBridge.LocateMethod;
            private static bool Prefix(Heightmap.Biome biome, Action<bool, Vector3> onComplete, ref IEnumerator __result)
            {
                var plugin = Instance;
                if (plugin == null || !plugin.Active) return true;
                __result = plugin.CreatePlacementRoutine(biome, onComplete);
                return false;
            }
        }

        [HarmonyPatch]
        private static class RevealPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(Minimap), "Explore", new[] { typeof(int), typeof(int) });
                yield return AccessTools.Method(typeof(Minimap), "ExploreOthers", new[] { typeof(int), typeof(int) });
            }
            private static void Postfix(bool __result) { if (__result) Instance?.index?.Revealed(); }
        }

        [HarmonyPatch]
        private static class ResetMapPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(Minimap), "Reset");
                yield return AccessTools.Method(typeof(Minimap), "ResetSharedMapData");
                yield return AccessTools.Method(typeof(Minimap), "SetMapData");
                yield return AccessTools.Method(typeof(Minimap), "ResetAndExplore", new[] { typeof(BitArray), typeof(BitArray) });
            }
            private static void Postfix() => Instance?.index?.Reset();
        }
    }
}




