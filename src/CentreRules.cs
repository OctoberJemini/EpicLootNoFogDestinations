using System;
using System.Collections;

namespace ValheimEpicLootNoFogDestinations
{
    internal static class CentreRules
    {
        internal static bool Visible(BitArray own, BitArray shared, int index)
        {
            return index >= 0 && index < own.Length && index < shared.Length && (own[index] || shared[index]);
        }

        internal static void PixelCentre(int index, int size, float pixelSize, out float x, out float z)
        {
            if (size <= 0 || index < 0 || (long)index >= (long)size * size ||
                pixelSize <= 0 || float.IsNaN(pixelSize) || float.IsInfinity(pixelSize))
                throw new ArgumentOutOfRangeException(nameof(index));
            // Minimap.WorldToPixel rounds to the nearest integer, not to a pixel's corner.
            x = (index % size - size / 2) * pixelSize;
            z = (index / size - size / 2) * pixelSize;
        }

        internal static double Radius(float x, float z) => Math.Sqrt((double)x * x + (double)z * z);
        internal static bool InBand(double radius, float min, float max) => radius >= min && radius <= max;
        internal static double BandDistance(double radius, float min, float max) =>
            radius < min ? min - radius : radius > max ? radius - max : 0;
    }
}
