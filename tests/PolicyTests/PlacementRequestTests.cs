using System;
using System.Collections;
using System.Collections.Generic;
using ValheimEpicLootNoFogDestinations;

internal static class PlacementRequestTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception("FAILED: " + name);
            checks++;
        }

        object player = new object(), world = new object(), map = new object();
        object originalPlayer = player, originalWorld = world, originalMap = map;
        var captured = new RequestContext<object, object, object>(player, world, map);
        Check(captured.Matches(player, world, map), "original request context matches");
        Check(!captured.Matches(new object(), world, map), "changed player rejected");
        Check(!captured.Matches(player, new object(), map), "changed world rejected");
        Check(!captured.Matches(player, world, new object()), "changed map rejected");
        Check(!captured.Matches(null, world, map), "logout rejected");
        Check(!new RequestContext<object, object, object>(null, world, map).Matches(null, world, map), "missing initial player rejected");
        Check(!new RequestContext<object, object, object>(player, null, map).Matches(player, null, map), "missing initial world rejected");
        Check(!new RequestContext<object, object, object>(player, world, null).Matches(player, world, null), "missing initial map rejected");

        var results = new List<(bool Success, int Point)>();
        var errors = new List<Exception>();
        PlacementRequest<int> NewRequest() => new PlacementRequest<int>(
            (success, point) => results.Add((success, point)), -1, errors.Add);
        void Clear() { results.Clear(); errors.Clear(); }
        void Drain(IEnumerator routine) { while (routine.MoveNext()) { } }

        // The iterator deliberately starts after the player/world/map are changed.
        IEnumerator Delayed(RequestContext<object, object, object> context, PlacementRequest<int> request)
        {
            bool same = context.Matches(player, world, map);
            request.Complete(same, 42);
            yield break;
        }
        var delayedRequest = NewRequest();
        var delayed = delayedRequest.Guard(Delayed(captured, delayedRequest));
        player = new object(); world = new object(); map = new object();
        Check(ReferenceEquals(captured.Player, originalPlayer) && ReferenceEquals(captured.World, originalWorld) &&
            ReferenceEquals(captured.Map, originalMap), "snapshot not recaptured before first step");
        Drain(delayed);
        Check(results.Count == 1 && results[0] == (false, -1), "delayed old-context request fails once");
        Check(errors.Count == 0, "context mismatch is an ordinary failure");

        Clear();
        var pendingContext = new RequestContext<object, object, object>(player, world, map);
        var pendingRequest = NewRequest();
        IEnumerator Pending()
        {
            yield return null;
            pendingRequest.Complete(pendingContext.Matches(player, world, map), 42);
        }
        var pending = pendingRequest.Guard(Pending());
        Check(pending.MoveNext() && results.Count == 0, "waiting request yields without committing");
        world = new object();
        Drain(pending);
        Check(results.Count == 1 && results[0] == (false, -1), "world change during wait fails once");

        Clear();
        var successfulRequest = NewRequest();
        IEnumerator Successful()
        {
            yield return null;
            successfulRequest.Complete(true, 42);
            successfulRequest.Complete(false, -1);
        }
        var successful = successfulRequest.Guard(Successful());
        Drain(successful);
        ((IDisposable)successful).Dispose();
        Check(results.Count == 1 && results[0] == (true, 42), "success callback and point are preserved exactly once");
        Check(errors.Count == 0, "normal success has no errors");

        Clear();
        var validationRequest = NewRequest();
        IEnumerator ValidationFailure()
        {
            yield return null;
            throw new InvalidOperationException("simulated final visibility check failure");
        }
        Drain(validationRequest.Guard(ValidationFailure()));
        Check(results.Count == 1 && results[0] == (false, -1), "final validation exception returns failure once");
        Check(errors.Count == 1, "validation exception reported");

        Clear();
        Drain(NewRequest().Guard(new TestIterator(() => throw new Exception("first step"))));
        Check(results.Count == 1 && results[0] == (false, -1), "exception before first yield returns failure once");
        Check(errors.Count == 1, "first-step exception reported");

        Clear();
        Drain(NewRequest().Guard(new TestIterator(() => true, () => throw new Exception("current"))));
        Check(results.Count == 1 && results[0] == (false, -1), "Current getter exception returns failure once");

        Clear();
        Drain(NewRequest().Guard(new TestIterator(() => false)));
        Check(results.Count == 1 && results[0] == (false, -1), "early termination returns failure once");

        Clear();
        var cancelled = NewRequest().Guard(new TestIterator(() => true));
        Check(cancelled.MoveNext(), "cancellable request started");
        ((IDisposable)cancelled).Dispose();
        ((IDisposable)cancelled).Dispose();
        Check(results.Count == 1 && results[0] == (false, -1), "explicit disposal returns failure once");

        Clear();
        Drain(NewRequest().Guard(new TestIterator(() => false, dispose: () => throw new Exception("dispose"))));
        Check(results.Count == 1 && results[0] == (false, -1), "cleanup exception cannot prevent failure completion");
        Check(errors.Count == 1, "cleanup exception reported");

        Clear();
        var loggingFailure = new PlacementRequest<int>((success, point) => results.Add((success, point)),
            -1, _ => throw new Exception("log listener"));
        Drain(loggingFailure.Guard(new TestIterator(() => throw new Exception("validation"))));
        Check(results.Count == 1 && results[0] == (false, -1), "broken logger cannot prevent failure completion");

        Clear();
        int commits = 0;
        var upstreamFailure = new PlacementRequest<int>((success, point) =>
        {
            commits++;
            throw new Exception("upstream threw after committing");
        }, -1, errors.Add);
        upstreamFailure.Complete(true, 42);
        upstreamFailure.Complete(false, -1);
        Drain(upstreamFailure.Guard(new TestIterator(() => false)));
        Check(commits == 1, "throwing upstream callback is never retried");
        Check(errors.Count == 1, "upstream error reported");

        Clear();
        var failureCallbackThrows = new PlacementRequest<int>((success, point) =>
        {
            results.Add((success, point));
            throw new Exception("failure callback");
        }, -1, errors.Add);
        Drain(failureCallbackThrows.Guard(new TestIterator(() => false)));
        Check(results.Count == 1 && results[0] == (false, -1), "throwing failure callback is invoked only once");
        Check(errors.Count == 1, "failure callback error reported");

        Console.WriteLine($"PASS: {checks} placement lifecycle checks.");
        return checks;
    }

    private sealed class TestIterator : IEnumerator, IDisposable
    {
        private readonly Func<bool> move;
        private readonly Func<object?> current;
        private readonly Action dispose;
        internal TestIterator(Func<bool> move, Func<object?>? current = null, Action? dispose = null)
        {
            this.move = move;
            this.current = current ?? (() => null);
            this.dispose = dispose ?? (() => { });
        }
        public object? Current => current();
        public bool MoveNext() => move();
        public void Reset() => throw new NotSupportedException();
        public void Dispose() => dispose();
    }
}
