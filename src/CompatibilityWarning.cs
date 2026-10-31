using System;

namespace ValheimEpicLootNoFogDestinations
{
    internal sealed class CompatibilityWarning
    {
        internal const string MinimumVersion = "0.14.4";
        internal const string SupportedVersion = "0.15.4";
        internal const string SupportedVersions = "0.14.4–0.14.5, 0.14.7–0.14.13 and 0.15.1–0.15.4 (API checks)";
        internal const string Header = "EpicLoot: No Fog Destinations — INACTIVE";
        internal const string HookFailureReason = "The required EpicLoot hooks could not be installed. Check the BepInEx log and update/reinstall this add-on.";
        internal string? Reason;
        private WeakReference? warnedWorld;
        private long warnedPlayer;

        internal static string? VersionReason(System.Version? installed)
        {
            return installed == null ? "EpicLoot not detected. Install EpicLoot and restart the game." : null;
        }

        internal static string Message(string reason) =>
            "The explored-destination restriction is NOT active.\n\n" +
            "NEW bounties and treasures may be placed in unexplored map areas. " +
            "This add-on does not block EpicLoot from running.\n\n" +
            "Previously checked EpicLoot versions: " + SupportedVersions + ".\n" + reason;

        internal static string PreviewMessage() =>
            "PREVIEW ONLY: EpicLoot, settings and the actual restriction are not changed.\n\n" +
            Message("Example: a required EpicLoot placement API changed. Check the BepInEx log and update this add-on.");

        internal static bool PreviewReady(int requestedFrame, int currentFrame, bool consoleVisible, bool popupAvailable, bool popupVisible) =>
            currentFrame > requestedFrame && !consoleVisible && popupAvailable && !popupVisible;

        internal bool TryShow(bool enabled, object? world, long playerId, bool uiReady, Action<string> show)
        {
            if (!enabled || Reason == null || world == null || playerId == 0 || !uiReady) return false;
            if (ReferenceEquals(warnedWorld?.Target, world) && warnedPlayer == playerId) return false;
            show(Message(Reason));
            // Only mark after delivery succeeds; do not keep an old world alive.
            warnedWorld = new WeakReference(world);
            warnedPlayer = playerId;
            return true;
        }
    }
}





