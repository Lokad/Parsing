using NUnit.Framework;

namespace Lokad.Parsing.Test
{
    [TestFixture]
    public sealed class source_span
    {
        [Test]
        public void empty()
        {
            var e = default(SourceSpan);
            Assert.AreEqual(0, e.Length);
            Assert.AreEqual(default(SourceLocation), e.Location);
            Assert.AreEqual("0:0-0", e.ToString());
        }

        [Test]
        public void to_string()
        {
            var s = new SourceSpan(new SourceLocation(10, 2, 5), 8);
            Assert.AreEqual("2:5-13", s.ToString());
        }

        [Test]
        public void equality()
        {
            var s = new SourceSpan(new SourceLocation(10, 2, 5), 8);
            var sa = new SourceSpan(new SourceLocation(10, 2, 5), 11);
            var sb = new SourceSpan(new SourceLocation(10, 1, 5), 8);
            var sc = new SourceSpan(new SourceLocation(10, 2, 5), 8);

            Assert.IsTrue(s.Equals(s));
            Assert.IsFalse(s.Equals(sa));
            Assert.IsFalse(s.Equals(sb));
            Assert.IsTrue(s.Equals(sc));
        }

        [Test]
        public void equals_or_contains()
        {
            const int a = 0;
            const int b = 1;
            const int c = 2;
            const int d = 3;
            const int e = 54;

            var ae = new SourceSpan(new SourceLocation(a, 1, 1), e - a);
            var ac = new SourceSpan(new SourceLocation(a, 1, 1), c - a);
            var ce = new SourceSpan(new SourceLocation(c, 2, 1), e - c);
            var bd = new SourceSpan(new SourceLocation(b, 1, 2), d - b);

            Assert.IsTrue(ae.EqualsOrContains(ae));
            Assert.IsTrue(ae.EqualsOrContains(ac));
            Assert.IsTrue(ae.EqualsOrContains(ce));
            Assert.IsTrue(ae.EqualsOrContains(bd));
            
            Assert.IsFalse(ac.EqualsOrContains(ae));
            Assert.IsTrue(ac.EqualsOrContains(ac));
            Assert.IsFalse(ac.EqualsOrContains(ce));
            Assert.IsFalse(ac.EqualsOrContains(bd));
            
            Assert.IsFalse(ce.EqualsOrContains(ae));
            Assert.IsFalse(ce.EqualsOrContains(ac));
            Assert.IsTrue(ce.EqualsOrContains(ce));
            Assert.IsFalse(ce.EqualsOrContains(bd));

            Assert.IsFalse(bd.EqualsOrContains(ae));
            Assert.IsFalse(bd.EqualsOrContains(ac));
            Assert.IsFalse(bd.EqualsOrContains(ce));
            Assert.IsTrue(bd.EqualsOrContains(bd));
        }

        [Test]
        public void shift_column()
        {
            Assert.AreEqual(
                new SourceSpan(new SourceLocation(11, 1, 11).ShiftColumn(5), 10),
                new SourceSpan(new SourceLocation(11, 1, 11), 10).ShiftColumn(5));
        }

        [Test]
        public void with_length()
        {
            Assert.AreEqual(
                new SourceSpan(new SourceLocation(11, 1, 11), 10),
                new SourceSpan(new SourceLocation(11, 1, 11), 13).WithLength(10));
        }

        [Test]
        public void merge_with()
        {
            const int a = 0;
            const int b = 1;
            const int c = 2;
            const int d = 3;
            const int e = 54;

            var ae = new SourceSpan(new SourceLocation(a, 1, 1), e - a);
            var ac = new SourceSpan(new SourceLocation(a, 1, 1), c - a);
            var ce = new SourceSpan(new SourceLocation(c, 2, 1), e - c);
            var bd = new SourceSpan(new SourceLocation(b, 1, 2), d - b);
            var ad = new SourceSpan(new SourceLocation(a, 1, 1), d - a);
            var be = new SourceSpan(new SourceLocation(b, 1, 2), e - b);

            Assert.AreEqual(ae, ae.MergeWith(ae));
            Assert.AreEqual(ae, ae.MergeWith(ac));
            Assert.AreEqual(ae, ae.MergeWith(ce));
            Assert.AreEqual(ae, ae.MergeWith(bd));
            Assert.AreEqual(ae, ae.MergeWith(ad));
            Assert.AreEqual(ae, ae.MergeWith(be));

            Assert.AreEqual(ae, ac.MergeWith(ae));
            Assert.AreEqual(ac, ac.MergeWith(ac));
            Assert.AreEqual(ae, ac.MergeWith(ce));
            Assert.AreEqual(ad, ac.MergeWith(bd));
            Assert.AreEqual(ad, ac.MergeWith(ad));
            Assert.AreEqual(ae, ac.MergeWith(be));

            Assert.AreEqual(ae, ce.MergeWith(ae));
            Assert.AreEqual(ae, ce.MergeWith(ac));
            Assert.AreEqual(ce, ce.MergeWith(ce));
            Assert.AreEqual(be, ce.MergeWith(bd));
            Assert.AreEqual(ae, ce.MergeWith(ad));
            Assert.AreEqual(be, ce.MergeWith(be));

            Assert.AreEqual(ae, bd.MergeWith(ae));
            Assert.AreEqual(ad, bd.MergeWith(ac));
            Assert.AreEqual(be, bd.MergeWith(ce));
            Assert.AreEqual(bd, bd.MergeWith(bd));
            Assert.AreEqual(ad, bd.MergeWith(ad));
            Assert.AreEqual(be, bd.MergeWith(be));

            Assert.AreEqual(ae, ad.MergeWith(ae));
            Assert.AreEqual(ad, ad.MergeWith(ac));
            Assert.AreEqual(ae, ad.MergeWith(ce));
            Assert.AreEqual(ad, ad.MergeWith(bd));
            Assert.AreEqual(ad, ad.MergeWith(ad));
            Assert.AreEqual(ae, ad.MergeWith(be));

            Assert.AreEqual(ae, be.MergeWith(ae));
            Assert.AreEqual(ae, be.MergeWith(ac));
            Assert.AreEqual(be, be.MergeWith(ce));
            Assert.AreEqual(be, be.MergeWith(bd));
            Assert.AreEqual(ae, be.MergeWith(ad));
            Assert.AreEqual(be, be.MergeWith(be));
        }
    }
}
