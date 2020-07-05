using System;
using System.Collections.Generic;
using System.Linq;

namespace IS4.TypeSpecializer
{
    /// <summary>
    /// Describes a constraint for <see cref="TypeArgumentAttribute"/> and <see cref="SpecializeForAttribute"/>.
    /// </summary>
    public abstract class Constraint
    {
        public IReadOnlyList<Constraint> Arguments { get; }

        public Constraint(IReadOnlyList<Constraint> arguments)
        {
            Arguments = arguments;
        }

        public override string ToString()
        {
            if(!Arguments.Any()) return String.Empty;
            return "(" + String.Join(", ", Arguments.Select(a => a.ToString())) + ")";
        }
    }

    public class NullConstraint : Constraint
    {
        public static readonly NullConstraint Instance = new NullConstraint();

        private NullConstraint() : base(Array.Empty<Constraint>())
        {

        }

        public override string ToString()
        {
            return "null" + base.ToString();
        }
    }

    public abstract class TypeConstraint : Constraint
    {
        public ConstraintVariance Variance { get; }

        public TypeConstraint(ConstraintVariance variance, IReadOnlyList<Constraint> arguments) : base(arguments)
        {
            Variance = variance;
        }

        public override string ToString()
        {
            if(Variance == ConstraintVariance.Invariant) return base.ToString();
            return " " + Variance.ToString() + base.ToString();
        }
    }

    public class ConcreteTypeConstraint : TypeConstraint
    {
        public Type Type { get; }

        public ConcreteTypeConstraint(ConstraintVariance variance, Type type) : this(variance, ExtractTypeArguments(type, out var args), args)
        {

        }

        public ConcreteTypeConstraint(ConstraintVariance variance, Type type, params Constraint[] arguments) : this(variance, type, (IReadOnlyList<Constraint>)arguments)
        {

        }

        public ConcreteTypeConstraint(ConstraintVariance variance, Type type, IReadOnlyList<Constraint> arguments) : base(variance, arguments)
        {
            Type = type;
        }

        private static Type ExtractTypeArguments(Type type, out IReadOnlyList<Constraint> args)
        {
            if(type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                var definition = type.GetGenericTypeDefinition();
                args = type.GetGenericArguments().Zip(definition.GetGenericArguments(), (tArg, tParam) => new ConcreteTypeConstraint(Specializer.GetVariance(tParam), tArg)).ToList();
                return definition;
            }else if(type.IsArray)
            {
                int rank = type.GetArrayRank();
                var arr = new Constraint[2];
                var intType = typeof(EncodedInt32Zero);
                for(int j = 0; j < rank; j++)
                {
                    intType = typeof(EncodedInt32<>).MakeGenericType(intType);
                }
                arr[0] = new ConcreteTypeConstraint(ConstraintVariance.Invariant, intType);
                arr[1] = new ConcreteTypeConstraint(ConstraintVariance.Invariant, type.GetElementType());
                args = arr;
                return typeof(ArrayMarker<,>);
            }else if(type.IsByRef)
            {
                args = new[] { new ConcreteTypeConstraint(ConstraintVariance.Invariant, type.GetElementType()) };
                return typeof(ByRefMarker<>);
            }else if(type.IsPointer)
            {
                args = new[] { new ConcreteTypeConstraint(ConstraintVariance.Invariant, type.GetElementType()) };
                return typeof(PointerMarker<>);
            }
            args = Array.Empty<Constraint>();
            return type;
        }

        public ConcreteTypeConstraint(ConstraintVariance variance, SpecialConstraint type, params Constraint[] arguments) : this(variance, GetMarkerType(type), arguments)
        {

        }

        public ConcreteTypeConstraint(ConstraintVariance variance, SpecialConstraint type, IReadOnlyList<Constraint> arguments) : this(variance, GetMarkerType(type), arguments)
        {

        }

        private static Type GetMarkerType(SpecialConstraint type)
        {
            switch(type)
            {
                case SpecialConstraint.MdArray:
                    return typeof(ArrayMarker<,>);
                case SpecialConstraint.ByRef:
                    return typeof(ByRefMarker<>);
                case SpecialConstraint.Pointer:
                    return typeof(PointerMarker<>);
                case SpecialConstraint.ValueType:
                    return typeof(ValueTypeMarker);
                case SpecialConstraint.ReferenceType:
                    return typeof(ReferenceTypeMarker);
                default:
                    throw new ArgumentException(nameof(type));
            }
        }

        public override string ToString()
        {
            return Type + base.ToString();
        }
    }

    public class ParameterConstraint : TypeConstraint
    {
        public string Name { get; }

        public ParameterConstraint(ConstraintVariance variance, string name) : base(variance, Array.Empty<Constraint>())
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name + base.ToString();
        }
    }

    public class NotConstraint : Constraint
    {
        public NotConstraint(IReadOnlyList<Constraint> arguments) : base(arguments)
        {

        }

        public override string ToString()
        {
            return "Not" + base.ToString();
        }
    }
}
