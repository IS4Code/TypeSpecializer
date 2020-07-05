using IS4.TypeSpecializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class Variance
    {
        [TestMethod]
        public void InvariantOuterTypeTest()
        {
            var specializer = new Specializer(typeof(InvariantOuterType));
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable)).Any(), "less specific type was accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(Array)).Any(), "same type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(int[])).Any(), "more specific type was accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(List<int>)).Any(), "other type was accepted");
        }

        [TestMethod]
        public void CovariantOuterTypeTest()
        {
            var specializer = new Specializer(typeof(CovariantOuterType));
            Assert.IsTrue(specializer.SpecializeAll(typeof(IEnumerable)).Any(), "less specific type was not accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(Array)).Any(), "same type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(int[])).Any(), "more specific type was accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(List<int>)).Any(), "other type was accepted");
        }

        [TestMethod]
        public void ContravariantOuterTypeTest()
        {
            var specializer = new Specializer(typeof(ContravariantOuterType));
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable)).Any(), "less specific type was accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(Array)).Any(), "same type was not accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(int[])).Any(), "more specific type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(List<int>)).Any(), "other type was accepted");
        }

        [TestMethod]
        public void AmbivariantOuterTypeTest()
        {
            var specializer = new Specializer(typeof(AmbivariantOuterType));
            Assert.IsTrue(specializer.SpecializeAll(typeof(IEnumerable)).Any(), "less specific type was not accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(Array)).Any(), "same type was not accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(int[])).Any(), "more specific type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(List<int>)).Any(), "other type was accepted");
        }

        [TypeArgument(typeof(Array))]
        class InvariantOuterType
        {

        }

        [TypeArgument(ConstraintVariance.Covariant, typeof(Array))]
        class CovariantOuterType
        {

        }

        [TypeArgument(ConstraintVariance.Contravariant, typeof(Array))]
        class ContravariantOuterType
        {

        }

        [TypeArgument(ConstraintVariance.Ambivariant, typeof(Array))]
        class AmbivariantOuterType
        {

        }

        [TestMethod]
        public void InvariantOuterAmbivariantInnerTest()
        {
            var specializer = new Specializer(typeof(InvariantOuterAmbivariantInner));
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable<IEnumerable>)).Any(), "less specific type was accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(IEnumerable<Array>)).Any(), "same type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable<int[]>)).Any(), "more specific type was accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable<List<int>>)).Any(), "other type was accepted");
        }

        [TypeArgument(typeof(IEnumerable<>), ConstraintVariance.Ambivariant, typeof(Array))]
        class InvariantOuterAmbivariantInner
        {

        }

        [TestMethod]
        public void InvariantInnerTypeTest()
        {
            var specializer = new Specializer(typeof(InvariantInnerType));
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable<IEnumerable>)).Any(), "less specific type was accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(IEnumerable<Array>)).Any(), "same type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable<int[]>)).Any(), "more specific type was accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable<List<int>>)).Any(), "other type was accepted");
        }

        [TestMethod]
        public void CovariantInnerTypeTest()
        {
            var specializer = new Specializer(typeof(CovariantInnerType));
            Assert.IsTrue(specializer.SpecializeAll(typeof(IEnumerable<IEnumerable>)).Any(), "less specific type was not accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(IEnumerable<Array>)).Any(), "same type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable<int[]>)).Any(), "more specific type was accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable<List<int>>)).Any(), "other type was accepted");
        }

        [TestMethod]
        public void ContravariantInnerTypeTest()
        {
            var specializer = new Specializer(typeof(ContravariantInnerType));
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable<IEnumerable>)).Any(), "less specific type was accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(IEnumerable<Array>)).Any(), "same type was not accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(IEnumerable<int[]>)).Any(), "more specific type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable<List<int>>)).Any(), "other type was accepted");
        }

        [TestMethod]
        public void AmbivariantInnerTypeTest()
        {
            var specializer = new Specializer(typeof(AmbivariantInnerType));
            Assert.IsTrue(specializer.SpecializeAll(typeof(IEnumerable<IEnumerable>)).Any(), "less specific type was not accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(IEnumerable<Array>)).Any(), "same type was not accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(IEnumerable<int[]>)).Any(), "more specific type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(IEnumerable<List<int>>)).Any(), "other type was accepted");
        }

        [TypeArgument(ConstraintVariance.Ambivariant, typeof(IEnumerable<>), ConstraintVariance.Invariant, typeof(Array))]
        class InvariantInnerType
        {

        }

        [TypeArgument(ConstraintVariance.Ambivariant, typeof(IEnumerable<>), ConstraintVariance.Covariant, typeof(Array))]
        class CovariantInnerType
        {

        }

        [TypeArgument(ConstraintVariance.Ambivariant, typeof(IEnumerable<>), ConstraintVariance.Contravariant, typeof(Array))]
        class ContravariantInnerType
        {

        }

        [TypeArgument(ConstraintVariance.Ambivariant, typeof(IEnumerable<>), ConstraintVariance.Ambivariant, typeof(Array))]
        class AmbivariantInnerType
        {

        }

        [TestMethod]
        public void DefaultInvariantInnerTypeTest()
        {
            var specializer = new Specializer(typeof(DefaultInvariantInnerType));
            Assert.IsFalse(specializer.SpecializeAll(typeof(IList<IEnumerable>)).Any(), "less specific type was accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(IList<Array>)).Any(), "same type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(IList<int[]>)).Any(), "more specific type was accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(IList<List<int>>)).Any(), "other type was accepted");
        }

        [TestMethod]
        public void DefaultCovariantInnerTypeTest()
        {
            var specializer = new Specializer(typeof(DefaultCovariantInnerType));
            Assert.IsTrue(specializer.SpecializeAll(typeof(Func<IEnumerable>)).Any(), "less specific type was not accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(Func<Array>)).Any(), "same type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(Func<int[]>)).Any(), "more specific type was accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(Func<List<int>>)).Any(), "other type was accepted");
        }

        [TestMethod]
        public void DefaultContravariantInnerTypeTest()
        {
            var specializer = new Specializer(typeof(DefaultContravariantInnerType));
            Assert.IsFalse(specializer.SpecializeAll(typeof(Action<IEnumerable>)).Any(), "less specific type was accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(Action<Array>)).Any(), "same type was not accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(Action<int[]>)).Any(), "more specific type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(Action<List<int>>)).Any(), "other type was accepted");
        }

        [TypeArgument(ConstraintVariance.Ambivariant, typeof(IList<>), typeof(Array))]
        class DefaultInvariantInnerType
        {

        }

        [TypeArgument(ConstraintVariance.Ambivariant, typeof(Func<>), typeof(Array))]
        class DefaultCovariantInnerType
        {

        }

        [TypeArgument(ConstraintVariance.Ambivariant, typeof(Action<>), typeof(Array))]
        class DefaultContravariantInnerType
        {

        }

        [TestMethod]
        public void VariantTypeParameterTest()
        {
            var specializer = new Specializer(typeof(VariantTypeParameterType<>));
            Assert.IsTrue(specializer.SpecializeAll(typeof(Array), typeof(Func<Array>)).Any(), "same type was not accepted");
            Assert.IsTrue(specializer.SpecializeAll(typeof(Array), typeof(Func<int[]>)).Any(), "more specific type was not accepted");
            Assert.IsFalse(specializer.SpecializeAll(typeof(Array), typeof(Func<object>)).Any(), "less specific type was accepted");
        }

        [TypeArgument(nameof(T))]
        [TypeArgument(ConstraintVariance.Contravariant, typeof(Func<>), nameof(T))]
        class VariantTypeParameterType<T>
        {

        }
    }
}
