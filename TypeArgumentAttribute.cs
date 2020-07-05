using System;
using System.Collections.Generic;

namespace IS4.TypeSpecializer
{
    /// <summary>
    /// Defines constraints for the next type argument provided to the specializer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
    public class TypeArgumentAttribute : Attribute
    {
        /// <summary>
        /// A list of constraints that the type argument must match in order to be accepted.
        /// </summary>
        public IReadOnlyList<Constraint> Constraints { get; }

        /// <summary>
        /// Specifies that if all following matches succeed, no more alternative matches for the constraints should be tried.
        /// </summary>
        public bool Definitive { get; set; }

        /// <summary>
        /// Creates a new instance of the attribute.
        /// </summary>
        /// <param name="constraints">
        /// An array of constraint specifiers. Each specifier could be
        /// <c>null</c> (wildcard, any type is matched),
        /// a string (type variable, could be the name of a generic parameter),
        /// a type (the argument must match this type),
        /// a value of <see cref="SpecialConstraint"/>,
        /// a value of <see cref="ConstraintVariance"/> (sets the variance for the next constraint),
        /// or <see cref="int"/> (transformed to <see cref="EncodedInt32{T}"/>).
        /// </param>
        public TypeArgumentAttribute(params object[] constraints)
        {
            Constraints = Specializer.ParseConstraintList(constraints, Array.Empty<ConstraintVariance>());
        }
    }
}
