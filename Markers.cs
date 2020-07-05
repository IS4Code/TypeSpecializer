using System;
using System.Collections.Generic;
using System.Text;

namespace IS4.TypeSpecializer
{
    /// <summary>
    /// Types implementing this interface are used in place of <see cref="SpecialConstraint"/> values when matching types.
    /// </summary>
    public interface IMarker
    {

    }

    /// <summary>
    /// Represents an array type.
    /// </summary>
    /// <typeparam name="TRank">The rank of the array, represented via <see cref="EncodedInt32{T}"/>.</typeparam>
    /// <typeparam name="T">The element type of the array.</typeparam>
    public class ArrayMarker<TRank, T> : IMarker where TRank : struct, IEncodedInt32
    {

    }

    /// <summary>
    /// Represents a constant <see cref="int"/> value.
    /// </summary>
    public interface IEncodedInt32
    {
        /// <summary>
        /// The actual value.
        /// </summary>
        int Value { get; }
    }

    /// <summary>
    /// Represents a constant <see cref="int"/> value created by adding 1 to the value of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The encoded inner value.</typeparam>
    public struct EncodedInt32<T> : IEncodedInt32 where T : struct, IEncodedInt32
    {
        /// <summary>
        /// The actual value.
        /// </summary>
        public int Value => default(T).Value + 1;
    }

    /// <summary>
    /// Represents a constant <see cref="int"/> value with value 0.
    /// </summary>
    public struct EncodedInt32Zero : IEncodedInt32
    {
        /// <summary>
        /// The actual value.
        /// </summary>
        public int Value => 0;
    }

    /// <summary>
    /// Represents a pointer type.
    /// </summary>
    /// <typeparam name="T">The element type of the pointer.</typeparam>
    public struct PointerMarker<T> : IMarker
    {

    }

    /// <summary>
    /// Represents a by-ref type.
    /// </summary>
    /// <typeparam name="T">The element type of the by-ref type.</typeparam>
    public struct ByRefMarker<T> : IMarker
    {

    }

    /// <summary>
    /// Represents any value type.
    /// </summary>
    public struct ValueTypeMarker : IMarker
    {

    }

    /// <summary>
    /// Represents any reference type.
    /// </summary>
    public class ReferenceTypeMarker : IMarker
    {

    }
}
