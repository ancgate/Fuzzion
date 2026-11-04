using System;

namespace Fuzzion.SimilarityRatio.Scorer.Generic;

public interface IRatioScorer<in T> where T : IEquatable<T>
{
    double Score(T[] input1, T[] input2);
}

public interface ICachedRatioScorer<in T> where T : IEquatable<T>
{
    double Score(T[] input2);
}
