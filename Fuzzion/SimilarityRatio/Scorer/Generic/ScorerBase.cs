using System;

namespace Fuzzion.SimilarityRatio.Scorer.Generic;

public abstract class ScorerBase<T> : IRatioScorer<T> where T : IEquatable<T>
{
    public abstract double Score(T[] input1, T[] input2);
}
