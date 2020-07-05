using System;
using System.Collections.Generic;

namespace IS4.TypeSpecializer
{
    /// <summary>
    /// When applied to a method or a constructor, indicates the type arguments that should be used when looking for the value of the argument.
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class SpecializeForAttribute : Attribute
    {
        /// <summary>
        /// The list of types that will be used when providing a value for the parameter.
        /// </summary>
        public IReadOnlyList<Constraint> NewTypes { get; }

        /// <summary>
        /// The direction in the list of pattern relative to the current one.
        /// </summary>
        public SpecializeDirection Direction { get; set; }

        /// <summary>
        /// True if the value of <see cref="Direction"/> applies even to subsequent matched patterns.
        /// </summary>
        public bool GlobalDirection { get; set; }

        /// <summary>
        /// Specifies that if the matches for subsequent parameters succeed, no alternative values will be tried for this parameter.
        /// </summary>
        public bool Definitive { get; set; }

        /// <summary>
        /// Creates a new instance of the attribute.
        /// </summary>
        /// <param name="newTypes">
        /// An array of type specifiers, with syntax equivalent to that of <see cref="TypeArgumentAttribute.TypeArgumentAttribute(object[])"/>.
        /// However, only specifiers that denote a single type could be used. Variance specifiers are ignored.
        /// </param>
        public SpecializeForAttribute(params object[] newTypes)
        {
            NewTypes = Specializer.ParseConstraintList(newTypes, Array.Empty<ConstraintVariance>());
        }
    }
}
