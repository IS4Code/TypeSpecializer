using System;

namespace IS4.TypeSpecializer
{
    /// <summary>
    /// A special constraint representing either a concrete special type, or a class of types.
    /// </summary>
    public enum SpecialConstraint
    {
        /// <summary>
        /// A single-dimensional array. Must be followed by the element type.
        /// </summary>
        Array,
        /// <summary>
        /// A multi-dimensional array. Must be followed by its rank and element type.
        /// </summary>
        MdArray,
        /// <summary>
        /// A by-ref type. Must be followed by the element type.
        /// </summary>
        ByRef,
        /// <summary>
        /// A pointer type. Must be followed by the element type.
        /// </summary>
        Pointer,
        /// <summary>
        /// Any value type.
        /// </summary>
        ValueType,
        /// <summary>
        /// Any reference type.
        /// </summary>
        ReferenceType,
        /// <summary>
        /// The complement of the following type.
        /// </summary>
        Not
    }

    /// <summary>
    /// Specifies the behaviour of matching regarding inheritance and type relations.
    /// </summary>
    public enum ConstraintVariance
    {
        /// <summary>
        /// Only the actual type argument is considered when matching with the constraint type.
        /// </summary>
        Invariant,
        /// <summary>
        /// The type argument and all its ancestors are considered when matching with the constraint type.
        /// </summary>
        Covariant,
        /// <summary>
        /// The type argument and all its descendants are considered when matching with the constraint type.
        /// </summary>
        Contravariant,
        /// <summary>
        /// The type argument and both its ancestors and descendants are considered when matching with the constraint type.
        /// </summary>
        Ambivariant
    }

    /// <summary>
    /// Used by <see cref="SpecializeForAttribute"/> to indicate which patterns can be considered next in relation to the current one.
    /// </summary>
    public enum SpecializeDirection
    {
        /// <summary>
        /// All patterns are considered.
        /// </summary>
        Any,
        /// <summary>
        /// Only the patterns following the current one are considered.
        /// </summary>
        Following,
        /// <summary>
        /// Only the patterns preceding the current one are considered.
        /// </summary>
        Preceding
    }
}
