namespace QuikBlazor.HttpValues;

internal static class DictionaryExtensions
{
    internal static Dictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> data1, IDictionary<TKey, TValue> data2)
        where TKey : notnull
    {
        var newDict = new Dictionary<TKey, TValue>(data1);
        foreach (var item in data2)
        {
            newDict[item.Key] = item.Value;
        }

        return newDict;
    }
}
