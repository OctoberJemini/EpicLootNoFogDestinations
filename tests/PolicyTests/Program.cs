using System;
using System.Collections;
using ValheimEpicLootNoFogDestinations;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    checks++;
}
var own = new BitArray(16);
var shared = new BitArray(16);
Check(!CentreRules.Visible(own, shared, 5), "fog rejected");
own[5] = true;
Check(CentreRules.Visible(own, shared, 5), "personal exploration");
shared[9] = true;
Check(CentreRules.Visible(own, shared, 9), "shared exploration");
shared[9] = false;
Check(!CentreRules.Visible(own, shared, 9), "shared-map reset");
Check(!CentreRules.Visible(own, shared, -1), "negative index");
Check(!CentreRules.Visible(own, shared, 16), "map bounds");
Check(!CentreRules.Visible(own, new BitArray(1), 5), "mismatched shared data");
CentreRules.PixelCentre(10, 4, 12, out float x, out float z);
Check(x == 0 && z == 0, "world centre is integer pixel centre");
CentreRules.PixelCentre(0, 4, 12, out x, out z);
Check(x == -24 && z == -24, "negative world coordinates");
CentreRules.PixelCentre(15, 4, 12, out x, out z);
Check(x == 12 && z == 12, "last pixel centre");
CentreRules.PixelCentre(9, 4, 12, out x, out z);
Check(x == -12 && z == 0, "x/z ordering matches Minimap");
Check(CentreRules.Radius(3000, -4000) == 5000, "world-centre radius");
Check(CentreRules.InBand(5000, 4000, 6000), "preferred radius band");
Check(CentreRules.InBand(4000, 4000, 6000), "inclusive minimum");
Check(CentreRules.InBand(6000, 4000, 6000), "inclusive maximum");
Check(!CentreRules.InBand(7000, 4000, 6000), "outside band");
Check(CentreRules.BandDistance(5000, 4000, 6000) == 0, "in-band fallback distance");
Check(CentreRules.BandDistance(3000, 4000, 6000) == 1000, "below-band fallback distance");
Check(CentreRules.BandDistance(8000, 4000, 6000) == 2000, "above-band fallback distance");
Check(CentreRules.Visible(own, shared, 5) && !CentreRules.Visible(own, shared, 6), "centre-only: adjacent fog does not disqualify visible centre");
Console.WriteLine($"PASS: {checks} explored-centre policy checks.");
PlacementRequestTests.Run();
CompatibilityWarningTests.Run();
