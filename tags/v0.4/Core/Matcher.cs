using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Phonix
{
    public interface IMatrixMatcher : IEnumerable<IMatchable>
    {
        bool Matches(RuleContext ctx, FeatureMatrix matrix);
    }

    public interface IMatchable
    {
        bool Matches(RuleContext ctx, FeatureMatrix matrix);
    }

    public interface IMatchCombine : IMatchable, ICombinable
    {
        // this exists just to provide a convenient way to specify both
        // behaviors
    }

    public class MatrixMatcher : IMatrixMatcher
    {
        public static MatrixMatcher AlwaysMatches = new MatrixMatcher(new IMatchable[] {});
        public static MatrixMatcher NeverMatches = new MatrixMatcher(new IMatchable[] {});

        private readonly IEnumerable<IMatchable> _values;

        public MatrixMatcher(FeatureMatrix fm)
        {
            var list = new List<IMatchable>();
            var iter = fm.GetEnumerator(true);
            while (iter.MoveNext())
            {
                list.Add(iter.Current);
            }
            iter.Dispose();

            _values = list;
        }

        public MatrixMatcher(IEnumerable<IMatchable> values)
        {
            var list = new List<IMatchable>(values);
            _values = list;
        }

        public MatrixMatcher(IEnumerable<IMatchCombine> values)
            : this(new List<IMatchCombine>(values).ConvertAll<IMatchable>(v => v))
        {
        }

#region IEnumerable<AbstractFeatureValue> members

        public IEnumerator<IMatchable> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

        virtual public bool Matches(RuleContext ctx, FeatureMatrix matrix)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }
            if (this == NeverMatches)
            {
                return false;
            }

            foreach (IMatchable match in this)
            {
                if (!match.Matches(ctx, matrix))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class NegativeMatrixMatcher : MatrixMatcher
    {
        public NegativeMatrixMatcher(IMatrixMatcher match)
            : base(match)
        {
        }

        public NegativeMatrixMatcher(IEnumerable<IMatchable> vals)
            : base(vals)
        {
        }

        override public bool Matches(RuleContext ctx, FeatureMatrix matrix)
        {
            return !base.Matches(ctx, matrix);
        }
    }
}
