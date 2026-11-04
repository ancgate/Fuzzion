using System;

namespace Fuzzion.SimilarityRatio.Strategy;

public interface ICachedStrategy : IDisposable
{
    double Calculate(string input2);
}
