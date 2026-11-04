using System;

namespace Fuzzion.SimilarityRatio.Scorer;

public interface ICachedRatioScorer: IDisposable
{
    double Score(string input2);
}
