using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PilotSim.Core;
using PilotSim.Data;
using PilotSim.Data.Models;
using PilotSim.Server.Services;
using Moq;
using Xunit;

namespace PilotSim.Tests;

public class ScoringReproTests
{
    // Simple deterministic fake instructor to test reproducibility window
    private class DeterministicInstructor : IInstructorService
    {
        public Task<InstructorVerdict> ScoreAsync(string transcript, object state, Difficulty difficulty, CancellationToken cancellationToken)
        {
            // Deterministic scoring: normalized based on length buckets
            double norm = Math.Clamp(transcript.Length / 50.0, 0, 1);
            int delta = (int)Math.Round(norm * 10) - 5; // range roughly -5..+5
            var verdict = new InstructorVerdict(
                Critical: new List<string>(),
                Improvements: new List<string>(),
                ExemplarReadback: null,
                Normalized: norm,
                ScoreDelta: delta,
                BlockReason: string.Empty,
                Components: new List<ComponentScore>{ new ComponentScore("LEN", "PhraseAccuracy", "info", 1.0, norm, delta, "Length derived") },
                PhraseAccuracy: norm,
                Ordering: null,
                Omissions: null,
                Safety: null,
                SafetyFlag: false,
                RubricVersion: "v1-test"
            );
            return Task.FromResult(verdict);
        }
    }

    [Fact]
    public async Task DeterministicInstructorProducesStableNormalizedScores()
    {
        var instructor = new DeterministicInstructor();
        var samples = new [] { "SHORT CALL", new string('A', 25), new string('B', 50) };
        var results = new List<double>();
        foreach (var s in samples)
        {
            var verdict = await instructor.ScoreAsync(s, new { }, Difficulty.Basic, CancellationToken.None);
            results.Add(verdict.Normalized);
        }

        // Assert monotonic non-decreasing for these length buckets
        Assert.True(results.SequenceEqual(results.OrderBy(x => x)), "Normalized scores should be non-decreasing with longer transcripts in deterministic test");
        // Assert value bounds
        Assert.All(results, r => Assert.InRange(r, 0, 1));
    }
}
