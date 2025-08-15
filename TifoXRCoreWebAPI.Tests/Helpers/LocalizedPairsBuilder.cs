using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for LocalizedPairs used in tests.
    /// Creates isolated instances (no shared mutable state).
    /// </summary>
    public sealed class LocalizedPairsBuilder
    {
        private string _key = "sample_key";
        private readonly List<LocalizedValue> _values = new();

        public LocalizedPairsBuilder WithKey(string key)
        { _key = key; return this; }

        public LocalizedPairsBuilder Add(string localeId, string value)
        {
            _values.Add(new LocalizedValue
            {
                LocaleId = localeId?.ToLowerInvariant(),
                Value = value
            });
            return this;
        }

        public LocalizedPairsBuilder AddMany(IEnumerable<(string localeId, string value)> pairs)
        {
            foreach (var (loc, val) in pairs)
                Add(loc, val);
            return this;
        }

        public LocalizedPairsBuilder Clear()
        { _values.Clear(); return this; }

        public LocalizedPairs Build()
        {
            // defensive copy so each build is independent
            return new LocalizedPairs
            {
                Key = _key,
                Values = _values.Select(v => new LocalizedValue
                {
                    LocaleId = v.LocaleId,
                    Value = v.Value
                }).ToList()
            };
        }
    }
}
