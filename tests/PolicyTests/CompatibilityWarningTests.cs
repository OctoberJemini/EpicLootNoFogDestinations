using System;
using ValheimEpicLootNoFogDestinations;

internal static class CompatibilityWarningTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception("FAILED: " + name);
            checks++;
        }

        foreach (var version in new[] { new Version(0,13,3), new Version(0,14,13), new Version(0,15,4), new Version(0,16,0), new Version(1,0,0) })
            Check(CompatibilityWarning.VersionReason(version) == null, "detected version proceeds to API checks: " + version);
        Check(CompatibilityWarning.VersionReason(null)!.Contains("not detected"), "missing dependency explained");
        string reason = "Required EpicLoot placement API changed";
        string message = CompatibilityWarning.Message(reason);
        Check(CompatibilityWarning.Header.Contains("INACTIVE"), "inactive header");
        Check(message.Contains("NOT active"), "inactive restriction explained");
        Check(message.Contains("NEW bounties and treasures") && message.Contains("unexplored"), "fog consequence explained");
        Check(message.Contains("does not block EpicLoot"), "native fallback explained");
        Check(CompatibilityWarning.HookFailureReason.Contains("BepInEx log"), "recovery explained");
        string preview = CompatibilityWarning.PreviewMessage();
        Check(preview.Contains("PREVIEW ONLY"), "preview clearly labelled");
        Check(preview.Contains("actual restriction are not changed"), "preview leaves behavior unchanged");
        Check(preview.Contains("API changed"), "preview exercises API warning");
        Check(!CompatibilityWarning.PreviewReady(100, 100, false, true, false), "preview cannot display in command execution frame");
        Check(!CompatibilityWarning.PreviewReady(100, 99, false, true, false), "preview cannot display before command execution");
        Check(!CompatibilityWarning.PreviewReady(100, 101, true, true, false), "visible console delays preview");
        Check(!CompatibilityWarning.PreviewReady(100, 101, false, false, false), "missing popup UI delays preview");
        Check(!CompatibilityWarning.PreviewReady(100, 101, false, true, true), "existing popup delays preview");
        Check(CompatibilityWarning.PreviewReady(100, 101, false, true, false), "preview delivered only on later console-free frame");

        object world = new object();
        long playerId = 17;
        int shown = 0;
        string? delivered = null;
        void Show(string text) { shown++; delivered = text; }
        var notice = new CompatibilityWarning();
        Check(!notice.TryShow(true, world, playerId, true, Show), "healthy add-on shows nothing");
        notice.Reason = reason;
        Check(!notice.TryShow(false, world, playerId, true, Show), "intentionally disabled add-on does not warn");
        Check(!notice.TryShow(true, null, playerId, true, Show), "no world: defer");
        Check(!notice.TryShow(true, world, 0, true, Show), "player not initialized: defer");
        Check(!notice.TryShow(true, world, playerId, false, Show), "UI unavailable or other popup visible: defer");
        Check(shown == 0, "deferred conditions do not display or consume warning");
        Check(notice.TryShow(true, world, playerId, true, Show), "ready session receives warning");
        Check(shown == 1 && delivered == message, "correct warning delivered once");
        Check(!notice.TryShow(true, world, playerId, true, Show), "no per-frame warning spam");
        Check(!notice.TryShow(false, world, playerId, true, Show) &&
            !notice.TryShow(true, world, playerId, true, Show), "config toggle does not duplicate acknowledged-session warning");
        Check(!notice.TryShow(true, world, playerId, true, Show), "same player ID after respawn does not repeat warning");
        Check(notice.TryShow(true, world, playerId + 1, true, Show), "different character warns");
        Check(notice.TryShow(true, new object(), playerId + 1, true, Show), "different world session warns");

        var retry = new CompatibilityWarning { Reason = CompatibilityWarning.HookFailureReason };
        bool threw = false;
        try { retry.TryShow(true, world, playerId, true, _ => throw new Exception("UI failed")); }
        catch (Exception) { threw = true; }
        Check(threw, "failed delivery reported to caller");
        Check(retry.TryShow(true, world, playerId, true, Show), "failed delivery can be retried when UI works");
        Check(!retry.TryShow(true, world, playerId, true, Show), "successful retry is deduplicated");

        Console.WriteLine($"PASS: {checks} compatibility-warning checks.");
        return checks;
    }
}




