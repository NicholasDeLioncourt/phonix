using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phonix
{

    public class FeatureMatrix : IEnumerable<FeatureValue>
    {
        private readonly List<FeatureValue> _values;
        private readonly int _count = 0;
        private int _hashCode = 0;

        public FeatureMatrix(IEnumerable<FeatureValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            _values = new List<FeatureValue>(new FeatureValue[Feature.InstanceCount]);

            // copy feature values from the input enumeration into our value array
            foreach (var val in values)
            {
                if (val != null)
                {
                    if (val.Feature.Index >= _values.Count)
                    {
                        _values.AddRange(new FeatureValue[(val.Feature.Index + 1) - _values.Count]);
                    }
                    _values[val.Feature.Index] = val;
                }
            }

            // build the hash code based on the enumerated values
            foreach (var fv in this)
            {
                _count++;
                _hashCode ^= fv.GetHashCode();
            }
        }

        public FeatureMatrix(FeatureMatrix matrix)
            : this(matrix._values)
        {
        }

        public readonly static FeatureMatrix Empty = new FeatureMatrix(new FeatureValue[] {});

#region IEnumerable(FeatureValue) members

        public IEnumerator<FeatureValue> GetEnumerator()
        {
            return GetEnumerator(false);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

        public IEnumerator<FeatureValue> GetEnumerator(bool enumerateNullValues)
        {
            foreach (FeatureValue fv in _values)
            {
                if (fv != null && (enumerateNullValues || fv != fv.Feature.NullValue))
                {
                    yield return fv;
                }
            }
            yield break;
        }

        public FeatureValue this[Feature f]
        {
            get
            {
                if (f is NodeFeature)
                {
                    throw new InvalidOperationException("Can't directly access node values");
                }

                try
                {
                    return _values[f.Index] ?? f.NullValue.GetValues(null, this).First();
                }
                catch (ArgumentOutOfRangeException)
                {
                    // extend the list to include the new value
                    _values.AddRange(new FeatureValue[(f.Index + 1) - _values.Count]);
                    return this[f];
                }
            }
        }

        public IEnumerable<FeatureValue> this[NodeFeature node]
        {
            get
            {
                var list = new List<FeatureValue>();
                foreach (var child in node.Children)
                {
                    if (child is NodeFeature)
                    {
                        list.AddRange(this[child as NodeFeature]);
                    }
                    else
                    {
                        list.Add(this[child]);
                    }
                }
                return list;
            }
        }

        public int Weight
        {
            get
            {
                return _count;
            }
        }

        public bool Equals(FeatureMatrix fm)
        {
            if (this.GetHashCode() != fm.GetHashCode())
                return false;

            if (this.Weight != fm.Weight)
                return false;
            
            foreach (var fv in this)
            {
                if (fm[fv.Feature] != fv)
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is FeatureMatrix)
            {
                return Equals((FeatureMatrix)obj);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append("[");

            var list = new List<FeatureValue>(this);
            list.Sort();

            str.Append(String.Join(" ", list.ConvertAll(fv => fv.ToString()).ToArray()));

            str.Append("]");
            return str.ToString();
        }

    }
}
