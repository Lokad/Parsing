using System;
using NUnit.Framework;

namespace Lokad.Parsing.Test
{
    [TestFixture]
    public sealed class source_location
    {
        [Test]
        public void empty()
        {
            var e = default(SourceLocation);
            Assert.AreEqual(0, e.Position);
            Assert.AreEqual(0, e.Line);
            Assert.AreEqual(0, e.Column);
            Assert.AreEqual("0:0", e.ToString());
        }

        [Test]
        public void to_string()
        {
            var s = new SourceLocation(18, 2, 5);
            Assert.AreEqual("2:5", s.ToString());
        }

        [Test]
        public void equality()
        {
            var s = new SourceLocation(18, 2, 5);
            var sa = new SourceLocation(17, 2, 5);
            var sb = new SourceLocation(18, 3, 5);
            var sc = new SourceLocation(18, 2, 4);
            var sd = new SourceLocation(18, 2, 5);

            Assert.IsTrue(s.Equals(s));
            Assert.IsFalse(s.Equals(sa));
            Assert.IsFalse(s.Equals(sb));
            Assert.IsFalse(s.Equals(sc));
            Assert.IsTrue(s.Equals(sd));
        }

        [Test]
        public void shift_column()
        {
            var positive = new SourceLocation(18, 2, 5).ShiftColumn(3);
            Assert.AreEqual(21, positive.Position);
            Assert.AreEqual(2, positive.Line);
            Assert.AreEqual(8, positive.Column);
            
            var negative = new SourceLocation(18, 2, 5).ShiftColumn(-3);
            Assert.AreEqual(15, negative.Position);
            Assert.AreEqual(2, negative.Line);
            Assert.AreEqual(2, negative.Column);

            var borderline = new SourceLocation(18, 2, 5).ShiftColumn(-4);
            Assert.AreEqual(14, borderline.Position);
            Assert.AreEqual(2, borderline.Line);
            Assert.AreEqual(1, borderline.Column);
            
            var zero = new SourceLocation(18, 2, 5).ShiftColumn(0);
            Assert.AreEqual(18, zero.Position);
            Assert.AreEqual(2, zero.Line);
            Assert.AreEqual(5, zero.Column);

            Assert.Catch<ArgumentException>(
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                () => new SourceLocation(18, 2, 5).ShiftColumn(-5),
                "Invalid shift (column 5, shift -5)");
        }
    }
}
