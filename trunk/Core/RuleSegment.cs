using System;
using System.Collections.Generic;
using System.Linq;

namespace Phonix
{
    public interface IRuleSegment
    {

        /* This function should return true if this matches the segment(s) at
         * the next position(s) of the enumeration. When this is called, the
         * iterator will be at the start of the enumeration or on the last
         * segment matched by the previous IRuleSegment, so implementations
         * should call segment.MoveNext() zero or more times (depending on how
         * many segments from the input they consume) _before_ testing
         * segment.Current.
         */
        bool Matches(RuleContext ctx, SegmentEnumerator segment);

        /* This function consumes zero or more segments from the enumeration,
         * modifying them as appropriate. The directions about MoveNext()
         * mentioned for Matches also applies here.
         */
         void Combine(RuleContext ctx, MutableSegmentEnumerator segment); 
    
    }

    public class ContextSegment : IRuleSegment
    {
        private readonly IMatrixMatcher _match;

        public ContextSegment(IMatrixMatcher match)
        {
            _match = match;
        }

        public virtual bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            if (pos.MoveNext() && _match.Matches(ctx, pos.Current))
            {
                return true;
            }
            return false;
        }

        public virtual void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            pos.MoveNext();
        }
    }

    public class MultiSegment : IRuleSegment
    {
        private readonly uint _minMatches;
        private readonly uint? _maxMatches;
        private readonly IEnumerable<IRuleSegment> _match;

        public MultiSegment(IEnumerable<IRuleSegment> match, uint minMatches, uint? maxMatches)
        {
            _match = match;
            _minMatches = minMatches;
            _maxMatches = maxMatches;
        }

        private bool MatchesAll(RuleContext ctx, SegmentEnumerator pos)
        {
            foreach (var seg in _match)
            {
                if (!seg.Matches(ctx, pos))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            uint count = 0;

            // We call pos.Mark() at the beginning and after every successful
            // match. At the end, if we are going to return true, we call
            // pos.Revert(). This is for the case where we have already matched
            // the minimum number of segments, and the next call to MatchesAll
            // returns false but nonetheless consumes some segments from the
            // enumeration. Those segments need to be put back for the next
            // matcher.

            pos.Mark();
            while (MatchesAll(ctx, pos))
            {
                pos.Mark();
                count++;
                if (_maxMatches.HasValue && _maxMatches <= count)
                {
                    break;
                }
            }

            var rv = count >= _minMatches;
            if (rv)
            {
                pos.Revert();
            }

            return rv;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            Matches(ctx, pos);
        }
    }

    public class ActionSegment : IRuleSegment
    {
        private readonly IMatrixMatcher _match;
        private readonly IMatrixCombiner _combo;

        public ActionSegment(IMatrixMatcher match, IMatrixCombiner combo)
        {
            _match = match;
            _combo = combo;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            if (pos.MoveNext() && _match.Matches(ctx, pos.Current))
            {
                return true;
            }
            return false;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            pos.MoveNext();
            pos.Current = _combo.Combine(ctx, pos.Current);
        }
    }

    public class DeletingSegment : IRuleSegment
    {
        private IMatrixMatcher _match;

        public DeletingSegment(IMatrixMatcher match)
        {
            _match = match;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            if (pos.MoveNext() && _match.Matches(ctx, pos.Current))
            {
                return true;
            }
            return false;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            pos.MoveNext();
            pos.Delete();
        }
    }
        
    public class InsertingSegment : IRuleSegment
    {
        private IMatrixCombiner _insert;

        public InsertingSegment(IMatrixCombiner insert)
        {
            _insert = insert;
        }

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            // always return true, but take nothing from the input list
            return true;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            pos.InsertAfter(_insert.Combine(ctx, FeatureMatrix.Empty));
            pos.MoveNext();
        }
    }

    public class StepSegment : IRuleSegment
    {
        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            return pos.MoveNext();
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            Matches(ctx, pos);
        }
    }

    public class BackstepSegment : IRuleSegment
    {
        // this class moves the cursor one step backwards, but doesn't actually
        // do anything to the segments.

        public bool Matches(RuleContext ctx, SegmentEnumerator pos)
        {
            // we only want to return false if we are already before the
            // beginning. This requires us to advance, check IsFirst, then move
            // back

            if (pos.MoveNext() && pos.IsFirst)
            {
                pos.MovePrev();
                return false;
            }
            pos.MovePrev();
            pos.MovePrev();
            return true;
        }

        public void Combine(RuleContext ctx, MutableSegmentEnumerator pos)
        {
            Matches(ctx, pos);
        }
    }
}
