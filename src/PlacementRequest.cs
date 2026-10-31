using System;
using System.Collections;

namespace ValheimEpicLootNoFogDestinations
{
    // Construct outside the iterator: iterator bodies do not run until MoveNext.
    internal sealed class RequestContext<TPlayer, TWorld, TMap>
        where TPlayer : class where TWorld : class where TMap : class
    {
        internal readonly TPlayer? Player;
        internal readonly TWorld? World;
        internal readonly TMap? Map;

        internal RequestContext(TPlayer? player, TWorld? world, TMap? map)
        {
            Player = player;
            World = world;
            Map = map;
        }

        internal bool Matches(TPlayer? player, TWorld? world, TMap? map) =>
            Player != null && World != null && Map != null &&
            ReferenceEquals(Player, player) && ReferenceEquals(World, world) && ReferenceEquals(Map, map);
    }

    // Guards this mod's frame-yielding iterator; it does not execute nested iterators.
    internal sealed class PlacementRequest<TPoint>
    {
        private readonly Action<bool, TPoint>? callback;
        private readonly TPoint failurePoint;
        private readonly Action<Exception> report;
        private bool completed;

        internal PlacementRequest(Action<bool, TPoint>? callback, TPoint failurePoint, Action<Exception> report)
        {
            this.callback = callback;
            this.failurePoint = failurePoint;
            this.report = report;
        }

        internal void Complete(bool success, TPoint point)
        {
            if (completed) return;
            // Upstream may commit save/currency state before throwing. Never retry it.
            completed = true;
            try { callback?.Invoke(success, success ? point : failurePoint); }
            catch (Exception error) { Report(error); }
        }

        private void Report(Exception error)
        {
            // A broken log listener must not prevent failure completion either.
            try { report(error); }
            catch (Exception) { }
        }

        private bool Step(IEnumerator routine, out object? current)
        {
            current = null;
            try
            {
                if (!routine.MoveNext()) return false;
                current = routine.Current;
                return true;
            }
            catch (Exception error) { Report(error); return false; }
        }

        internal IEnumerator Guard(IEnumerator routine)
        {
            try
            {
                while (!completed && Step(routine, out var current)) yield return current;
            }
            finally
            {
                try { (routine as IDisposable)?.Dispose(); }
                catch (Exception error) { Report(error); }
                // Covers validation errors, early termination and explicit disposal.
                // External coroutine cancellation without disposal is outside our control.
                if (!completed) Complete(false, failurePoint);
            }
        }
    }
}
