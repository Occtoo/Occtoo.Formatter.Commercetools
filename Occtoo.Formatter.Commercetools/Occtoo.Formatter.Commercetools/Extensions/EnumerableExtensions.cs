namespace Occtoo.Formatter.Commercetools.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<(int Index, IEnumerable<T> Batch)> CreateBatches<T>(this IEnumerable<T> source, int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than 0.", nameof(batchSize));

        using var enumerator = source.GetEnumerator();
        var index = 0;
        while (enumerator.MoveNext())
        {
            var batch = YieldBatch(enumerator, batchSize);
            yield return (Index: index++, Batch: batch);
        }
    }

    private static IEnumerable<T> YieldBatch<T>(this IEnumerator<T> source, int batchSize)
    {
        do
        {
            yield return source.Current;
        }
        while (--batchSize > 0 && source.MoveNext());
    }
}

