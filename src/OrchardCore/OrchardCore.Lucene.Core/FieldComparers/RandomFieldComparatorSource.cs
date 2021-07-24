using System;
using Lucene.Net.Search;

namespace OrchardCore.Lucene.FieldComparers
{
    internal class RandomFieldComparatorSource : FieldComparerSource
    {
        public override FieldComparer NewComparer(string fieldname, int numHits, int sortPos, bool reversed)
        {
            return new RandomOrderFieldComparator(fieldname, null);
        }
    }

    internal class RandomOrderFieldComparator : FieldComparer.NumericComparer<int>
    {
        private readonly Random _random = new Random();

        public RandomOrderFieldComparator(string field, int? missingValue) : base(field, missingValue)
        {
        }

        public override int Compare(int slot1, int slot2) => _random.Next(-1, 2);

        public override int CompareBottom(int doc) => _random.Next(-1, 2);

        public override void Copy(int slot, int doc)
        {
        }

        public override void SetBottom(int slot)
        {
        }

        public override void SetTopValue(object value)
        {
        }

        public override int CompareTop(int doc) => _random.Next(-1, 2);

        public override IComparable this[int slot] => _random.Next();
    }
}
