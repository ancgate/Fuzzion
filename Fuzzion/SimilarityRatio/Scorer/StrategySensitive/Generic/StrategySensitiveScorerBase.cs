using System;
using Fuzzion.SimilarityRatio.Scorer.Generic;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive.Generic;

public abstract class StrategySensitiveScorerBase<T> : ScorerBase<T> where T : IEquatable<T>
{
    protected abstract Func<T[], T[], double> Scorer { get; }
}
