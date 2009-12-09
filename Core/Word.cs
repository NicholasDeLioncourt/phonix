using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Phonix
{
    public interface SegmentEnumerator : IEnumerator<FeatureMatrix>
    {
        bool IsFirst { get; }
        bool IsLast { get; }
        bool MovePrev();
        new FeatureMatrix Current { get; }
    }

    public interface MutableSegmentEnumerator : SegmentEnumerator
    {
        void InsertBefore(FeatureMatrix fm);
        void InsertAfter(FeatureMatrix fm);
        void Delete();
        new FeatureMatrix Current { get; set; }
    }

    public interface IWordSlice
    {
        SegmentEnumerator GetEnumerator();
        MutableSegmentEnumerator GetMutableEnumerator();
    }

    public enum Direction
    {
        Rightward,
        Leftward
    }

    public class Word : IEnumerable<FeatureMatrix>
    {
        private readonly LinkedList<FeatureMatrix> _list;

        public Word(IEnumerable<FeatureMatrix> fms)
        {
            _list = new LinkedList<FeatureMatrix>( fms );
        }

        private class WordSegment : MutableSegmentEnumerator
        {
            private LinkedListNode<FeatureMatrix> _node;
            private LinkedListNode<FeatureMatrix> _startNode;
            private LinkedListNode<FeatureMatrix> _lastNode;
            private readonly IMatrixMatcher _filter;

            private bool _beforeFirst = false;
            private bool _afterLast = false;

            public WordSegment(LinkedListNode<FeatureMatrix> node, IMatrixMatcher filter)
            {
                if (node == null)
                {
                    throw new ArgumentNullException("node");
                }
                if (filter == null)
                {
                    throw new ArgumentNullException("filter");
                }
                if (node.List == null)
                {
                    throw new SegmentDeletedException();
                }

                _startNode = node;
                _lastNode = node.List.Last;
                _filter = filter;
                Reset();
            }

#region IEnumerator<FeatureMatrix> members

            public bool MoveNext()
            {
                RuleContext ctx = new RuleContext();
                if (!_beforeFirst)
                {
                    _node = _node.Next;
                }
                while (_node != null && !_filter.Matches(ctx, _node.Value))
                {
                    _node = _node.Next;
                }
                _beforeFirst = false;

                if (_node == null)
                {
                    _afterLast = true;
                    _node = _lastNode;
                }
                return !_afterLast;
            }

            public FeatureMatrix Current
            {
                get 
                { 
                    CheckValid();
                    return _node.Value; 
                }
                set 
                {
                    CheckValid();
                    _node.Value = value; 
                }
            }

            public void Reset()
            {
                _node = _startNode;
                _beforeFirst = true;
                _afterLast = false;
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
            }

#endregion

#region SegmentEnumerator members

            public bool IsFirst
            {
                get 
                { 
                    CheckValid();
                    return _node.List.First == _node; 
                }
            }

            public bool IsLast
            {
                get 
                { 
                    CheckValid();
                    return _node.List.Last == _node; 
                }
            }

            public bool MovePrev()
            {
                RuleContext ctx = new RuleContext();

                if (!_afterLast)
                {
                    _node = _node.Previous;
                }
                while (_node != null && !_filter.Matches(ctx, _node.Value))
                {
                    _node = _node.Previous;
                }
                _afterLast = false;

                if (_node == null)
                {
                    _beforeFirst = true;
                    _node = _startNode.List.First;
                }

                return !_beforeFirst;
            }

#endregion

#region MutableSegmentEnumerator members

            public void InsertBefore(FeatureMatrix fm)
            {
                if (_node == _startNode && _beforeFirst)
                {
                    // can't insert before you've started iterating
                    throw new InvalidOperationException();
                }
                else if (_node == _lastNode && _afterLast)
                {
                    // we've gone past the end, but we can still add "before",
                    // which means after the last node.
                    _lastNode.List.AddAfter(_lastNode, fm);
                }
                else
                {
                    _node.List.AddBefore(_node, fm);
                }
            }

            public void InsertAfter(FeatureMatrix fm)
            {
                if (_node == _lastNode &&  _afterLast)
                {
                    throw new InvalidOperationException();
                }
                else if (_node == _startNode && _beforeFirst)
                {
                    // we haven't started yet, so "after" actually means before
                    // our start node. we also move the start node back
                    _node.List.AddBefore(_node, fm);
                    _node = _startNode = _node.Previous;
                }
                else
                {
                    _node.List.AddAfter(_node, fm);
                }
            }

            public void Delete()
            {
                CheckValid();
                var deleted = _node;
                _node = _node.Next;
                deleted.List.Remove(deleted);
                _beforeFirst = true;
            }

#endregion

            private void CheckValid()
            {
                if (_beforeFirst || _afterLast)
                {
                    throw new InvalidOperationException();
                }
            }

        }

        private class WordSlice : IWordSlice
        {
            private LinkedListNode<FeatureMatrix> _node;
            private readonly IMatrixMatcher _filter;

            public WordSlice(LinkedListNode<FeatureMatrix> node, IMatrixMatcher filter)
            {
                if (node == null)
                {
                    throw new ArgumentNullException();
                }
                _node = node;
                _filter = filter;
            }

            public MutableSegmentEnumerator GetMutableEnumerator()
            {
                return new WordSegment(_node, _filter);
            }

            public SegmentEnumerator GetEnumerator()
            {
                return GetMutableEnumerator();
            }

            public override string ToString()
            {
                StringBuilder str = new StringBuilder();
                var currNode = _node;
                while (currNode != null)
                {
                    str.Append(currNode.Value.ToString());
                    currNode = currNode.Next;
                }
                return str.ToString();
            }
        }

        private class BoundarySegment : IRuleSegment
        {

            // note that the boundary segments are tricksy. since there are
            // only ever two instances, we reference compare against those
            // instances to determine our behavior.

            public bool Matches(RuleContext ctx, SegmentEnumerator segment)
            {
                if (this == Word.LeftBoundary)
                {
                    if (segment.MoveNext() && segment.IsFirst)
                    {
                        // move back to undo this move
                        segment.MovePrev();
                        return true;
                    }
                    return false;
                }
                else if (this == Word.RightBoundary)
                {
                    return segment.IsLast && !segment.MoveNext();
                }
                return false;
            }

             public void Combine(RuleContext ctx, MutableSegmentEnumerator segment)
             {
                 // nothing to do
             }
        }

        public static IRuleSegment LeftBoundary = new BoundarySegment();

        public static IRuleSegment RightBoundary = new BoundarySegment();

        public IEnumerator<IWordSlice> GetSliceEnumerator(Direction dir)
        {
            return GetSliceEnumerator(dir, MatrixMatcher.AlwaysMatches);
        }

        public IEnumerator<IWordSlice> GetSliceEnumerator(Direction dir, IMatrixMatcher filter)
        {
            LinkedListNode<FeatureMatrix> currNode;
            if (dir == Direction.Rightward)
            {
                currNode = _list.First;
            }
            else
            {
                currNode = _list.Last;
            }
            if (filter == null)
            {
                filter = MatrixMatcher.AlwaysMatches;
            }

            while (currNode != null)
            {
                var nextNodePre = (dir == Direction.Rightward ? currNode.Next : currNode.Previous);
                RuleContext ctx = new RuleContext();

                if (filter.Matches(ctx, currNode.Value))
                {
                    yield return new WordSlice(currNode, filter);
                }

                var nextNodePost = (dir == Direction.Rightward ? currNode.Next : currNode.Previous);

                if (currNode.List == null)
                {
                    // if currNode was detached, then we have to use the
                    // nextNode that was saved before we yielded.
                    currNode = nextNodePre;
                }
                else
                {
                    currNode = nextNodePost;
                }
            }

            yield break;
        }

#region IEnumerable(T) members

        public IEnumerator<FeatureMatrix> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

        public override string ToString()
        {
            System.Text.StringBuilder str = new System.Text.StringBuilder();
            foreach (var fm in _list)
            {
                str.Append(fm.ToString());
            }
            return str.ToString();
        }
    }

}
