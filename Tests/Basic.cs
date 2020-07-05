using IS4.TypeSpecializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class Basic
    {
        [TestMethod]
        public void NoMatchTest()
        {
            var specializer = new Specializer(typeof(MatchType1<>), typeof(MatchType2<>));
            Assert.IsNull(specializer.Specialize(typeof(int?)));
        }

        [TestMethod]
        public void FallbackTest()
        {
            var specializer = new Specializer(typeof(MatchType1<>), typeof(MatchType2<>));
            specializer.Fallback = typeof(object);
            Assert.AreEqual(specializer.Specialize(typeof(int?))?.GetType(), typeof(object));
        }

        [TestMethod]
        public void SingleMatchTest()
        {
            var specializer = new Specializer(typeof(MatchType1<>), typeof(MatchType2<>));
            var results = specializer.SpecializeAll(typeof(int)).ToList();
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results[0].GetType(), typeof(MatchType2<int>));
        }

        [TestMethod]
        public void MultipleMatchTest()
        {
            var specializer = new Specializer(typeof(MatchType1<>), typeof(MatchType2<>));
            var results = specializer.SpecializeAll(typeof(string)).ToList();
            Assert.AreEqual(results.Count, 2);
            Assert.AreEqual(results[0].GetType(), typeof(MatchType1<string>));
            Assert.AreEqual(results[1].GetType(), typeof(MatchType2<string>));
        }

        [TypeArgument(nameof(T))]
        class MatchType1<T> where T : class
        {

        }

        [TypeArgument(nameof(T))]
        class MatchType2<T> where T : IConvertible
        {

        }

        [TestMethod]
        public void GenericMatchTest()
        {
            var specializer = new Specializer(typeof(MatchType3<int>));
            Assert.IsTrue(specializer.SpecializeAll(typeof(int)).Any(), "same type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(long)).Any(), "other type was accepted");
        }

        [TypeArgument(nameof(T))]
        class MatchType3<T> where T : struct
        {

        }
    }
}