using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Phonix.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class WordTest
    {
        public static Word GetTestWord()
        {
            var fms = new FeatureMatrix[] 
            { 
                FeatureMatrixTest.MatrixA, 
                FeatureMatrixTest.MatrixB, 
                FeatureMatrixTest.MatrixC
            };

            return new Word(fms);
        }

        public static string SpellSlice(IWordSlice slice)
        {
            SymbolSet ss = SymbolSetTest.GetTestSet();
            StringBuilder str = new StringBuilder();
            foreach (Segment seg in slice)
            {
                try
                {
                    str.Append(ss.Spell(seg.Matrix));
                }
                catch (SpellingException)
                {
                    str.Append(seg.Matrix.ToString());
                }
            }
            return str.ToString();
        }

        [Test]
        public void Ctor()
        {
            var word = GetTestWord();
            Assert.IsNotNull(word);
        }

        [Test]
        public void GetSliceEnumeratorRightward()
        {
            var word = GetTestWord();

            var iter = word.GetSliceEnumerator(Direction.Rightward);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("abc", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("bc", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("c", SpellSlice(iter.Current));

            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void GetSliceEnumeratorLeftward()
        {
            var word = GetTestWord();

            var iter = word.GetSliceEnumerator(Direction.Leftward);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("c", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("bc", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("abc", SpellSlice(iter.Current));

            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void GetFilteredSliceEnumeratorRightward()
        {
            var word = GetTestWord();
            var fs = FeatureSetTest.GetTestSet();
            var fm = new FeatureMatrix(new FeatureValue[] { 
                    fs.Get<ScalarFeature>("sc").Value(1)
            });
            var filter = new MatrixMatcher(fm);

            var iter = word.GetSliceEnumerator(Direction.Rightward, filter);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("ac", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("c", SpellSlice(iter.Current));
            
            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void GetFilteredSliceEnumeratorLeftward()
        {
            var word = GetTestWord();
            var fs = FeatureSetTest.GetTestSet();
            var fm = new FeatureMatrix(new FeatureValue[] { 
                    fs.Get<ScalarFeature>("sc").Value(1)
            });
            var filter = new MatrixMatcher(fm);

            var iter = word.GetSliceEnumerator(Direction.Leftward, filter);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("c", SpellSlice(iter.Current));
            
            Assert.IsTrue(iter.MoveNext());
            Assert.AreEqual("ac", SpellSlice(iter.Current));
            
            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void SegmentMoveNextPrev()
        {
            var word = GetTestWord();
            var slice = word.GetSliceEnumerator(Direction.Rightward);
            slice.MoveNext();
            var iter = slice.Current.GetEnumerator();

            try
            {
                Assert.Fail("should not be able to access iter.Current" + iter.Current);
            }
            catch (InvalidOperationException)
            {
            }

            // should be able to MoveNext three times
            Assert.IsTrue(iter.MoveNext());
            Assert.AreSame(FeatureMatrixTest.MatrixA, iter.Current.Matrix);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreSame(FeatureMatrixTest.MatrixB, iter.Current.Matrix);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreSame(FeatureMatrixTest.MatrixC, iter.Current.Matrix);
            Assert.IsFalse(iter.MoveNext());
            Assert.IsFalse(iter.MoveNext()); // check that this MoveNext()=false repeats

            try
            {
                Assert.Fail("should not be able to access iter.Current" + iter.Current);
            }
            catch (InvalidOperationException)
            {
            }

            // should be able to MovePrev three times
            Assert.IsTrue(iter.MovePrev());
            Assert.AreSame(FeatureMatrixTest.MatrixC, iter.Current.Matrix);
            Assert.IsTrue(iter.MovePrev());
            Assert.AreSame(FeatureMatrixTest.MatrixB, iter.Current.Matrix);
            Assert.IsTrue(iter.MovePrev());
            Assert.AreSame(FeatureMatrixTest.MatrixA, iter.Current.Matrix);
            Assert.IsFalse(iter.MovePrev());
            Assert.IsFalse(iter.MovePrev()); // check that this MovePrev()=false repeats

            try
            {
                Assert.Fail("should not be able to access iter.Current" + iter.Current);
            }
            catch (InvalidOperationException)
            {
            }

            // should be able to MoveNext again after moving back
            Assert.IsTrue(iter.MoveNext());
            Assert.AreSame(FeatureMatrixTest.MatrixA, iter.Current.Matrix);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreSame(FeatureMatrixTest.MatrixB, iter.Current.Matrix);
            Assert.IsTrue(iter.MoveNext());
            Assert.AreSame(FeatureMatrixTest.MatrixC, iter.Current.Matrix);
            Assert.IsFalse(iter.MoveNext());

        }

        [Test]
        public void SegmentInsertBefore()
        {
            var word = GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetMutableEnumerator();

            try
            {
                sliceIter.InsertBefore(new MutableSegment(FeatureMatrixTest.MatrixC));
                Assert.Fail("should have thrown exception");
            }
            catch (InvalidOperationException)
            {
            }

            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixA, sliceIter.Current.Matrix);
            sliceIter.InsertBefore(new MutableSegment(FeatureMatrixTest.MatrixC));

            Assert.IsTrue(sliceIter.MoveNext(), "second MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current.Matrix);
            sliceIter.InsertBefore(new MutableSegment(FeatureMatrixTest.MatrixC));

            Assert.IsTrue(sliceIter.MoveNext(), "third MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);
            sliceIter.InsertBefore(new MutableSegment(FeatureMatrixTest.MatrixC));

            Assert.IsFalse(sliceIter.MoveNext(), "last MoveNext()");
            sliceIter.InsertBefore(new MutableSegment(FeatureMatrixTest.MatrixC));

            iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();
            Assert.AreEqual("cacbccc", SpellSlice(iter.Current));
        }

        [Test]
        public void SegmentInsertAfter()
        {
            var word = GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetMutableEnumerator();

            sliceIter.InsertAfter(new MutableSegment(FeatureMatrixTest.MatrixC));
            Assert.IsTrue(sliceIter.MoveNext(), "zeroeth MoveNext() (inserted before starting)");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);

            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixA, sliceIter.Current.Matrix);
            sliceIter.InsertAfter(new MutableSegment(FeatureMatrixTest.MatrixC));
            Assert.IsTrue(sliceIter.MoveNext(), "second MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);

            Assert.IsTrue(sliceIter.MoveNext(), "third MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current.Matrix);
            sliceIter.InsertAfter(new MutableSegment(FeatureMatrixTest.MatrixC));
            Assert.IsTrue(sliceIter.MoveNext(), "fourth MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);

            Assert.IsTrue(sliceIter.MoveNext(), "fifth MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);
            sliceIter.InsertAfter(new MutableSegment(FeatureMatrixTest.MatrixC));
            Assert.IsTrue(sliceIter.MoveNext(), "sixth MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);

            Assert.IsFalse(sliceIter.MoveNext(), "last MoveNext()");

            try
            {
                sliceIter.InsertAfter(new MutableSegment(FeatureMatrixTest.MatrixC));
                Assert.Fail("should have thrown exception");
            }
            catch (InvalidOperationException)
            {
            }

            iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();
            Assert.AreEqual("cacbccc", SpellSlice(iter.Current));
        }

        [Test]
        public void SegmentDelete()
        {
            var word = GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetMutableEnumerator();

            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixA, sliceIter.Current.Matrix);
            sliceIter.Delete();

            Assert.IsTrue(sliceIter.MoveNext(), "second MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current.Matrix);
            sliceIter.Delete();

            Assert.IsTrue(sliceIter.MoveNext(), "third MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix);
            sliceIter.Delete();

            Assert.IsFalse(sliceIter.MoveNext(), "last MoveNext()");

            iter = word.GetSliceEnumerator(Direction.Rightward);
            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void LeftBoundary()
        {
            var word = GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Rightward);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetEnumerator();

            Assert.IsTrue(Word.LeftBoundary.Matches(null, sliceIter), "left boundary matches at first segment");
            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixA, sliceIter.Current.Matrix, "next iter is A");
            Assert.IsFalse(Word.LeftBoundary.Matches(null, sliceIter), "left boundary doesn't match after first segment");
        }

        [Test]
        public void RightBoundary()
        {
            var word = GetTestWord();
            var iter = word.GetSliceEnumerator(Direction.Leftward);
            iter.MoveNext();
            iter.MoveNext(); // get two segments in the slice

            var slice = iter.Current;
            var sliceIter = slice.GetEnumerator();
            sliceIter.MoveNext();

            Assert.IsFalse(Word.RightBoundary.Matches(null, sliceIter), "right boundary doesn't match before last segment");
            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixC, sliceIter.Current.Matrix, "next iter is C");
            Assert.IsTrue(Word.RightBoundary.Matches(null, sliceIter), "right boundary matches at last segment");
        }

        [Test]
        public void LeftBoundaryFiltered()
        {
            var word = GetTestWord();
            var fs = FeatureSetTest.GetTestSet();
            var filter = new MatrixMatcher(new IMatchable[] { fs.Get<Feature>("bn2").NullValue });

            var iter = word.GetSliceEnumerator(Direction.Rightward, filter);
            iter.MoveNext();

            var slice = iter.Current;
            var sliceIter = slice.GetEnumerator();

            Assert.IsTrue(Word.LeftBoundary.Matches(null, sliceIter), "left boundary matches at first segment");
            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current.Matrix, "next iter is B");
            Assert.IsFalse(Word.LeftBoundary.Matches(null, sliceIter), "left boundary doesn't match after first segment");
        }

        [Test]
        public void RightBoundaryFiltered()
        {
            var word = GetTestWord();
            var fs = FeatureSetTest.GetTestSet();
            var filter = new MatrixMatcher(new IMatchable[] { fs.Get<BinaryFeature>("bn").PlusValue });

            var iter = word.GetSliceEnumerator(Direction.Rightward, filter);
            iter.MoveNext(); // get one segment into the slice

            var slice = iter.Current;
            var sliceIter = slice.GetEnumerator();
            sliceIter.MoveNext();

            Assert.IsFalse(Word.RightBoundary.Matches(null, sliceIter), "right boundary doesn't match before last segment");
            Assert.IsTrue(sliceIter.MoveNext(), "first MoveNext()");
            Assert.AreSame(FeatureMatrixTest.MatrixB, sliceIter.Current.Matrix, "next iter is B");
            Assert.IsTrue(Word.RightBoundary.Matches(null, sliceIter), "right boundary matches at last segment");
        }
    }
}