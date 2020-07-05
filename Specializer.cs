using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IS4.TypeSpecializer
{
    /// <summary>
    /// A utility class for producing instances of types based on combinations of provided type arguments.
    /// </summary>
    public class Specializer
    {
        /// <summary>
        /// A list of types that are tried in sequential order when looking for a match.
        /// </summary>
        public IReadOnlyList<Type> Patterns { get; set; }

        /// <summary>
        /// A general type that that is used to construct a value if no patten is matched.
        /// </summary>
        public Type? Fallback { get; set; }

        /// <summary>
        /// A list of exceptions that are filtered out when a new object is constructed. By default, only exceptions deriving from <see cref="SystemException"/> are filtered.
        /// </summary>
        public IReadOnlyList<Type>? CreationExceptions { get; set; }

        /// <summary>
        /// Creates a new instance of the type.
        /// </summary>
        /// <param name="patterns">An array of type patterns, assigned to <see cref="Patterns"/>.</param>
        public Specializer(params Type[] patterns)
        {
            Patterns = patterns;
            CreationExceptions = new[] { typeof(SystemException) };
        }

        /// <summary>
        /// Creates a new instance of the type.
        /// </summary>
        public Specializer() : this(Type.EmptyTypes)
        {

        }

        /// <summary>
        /// Looks for the first type that successfully matches the combination of types in <paramref name="typeArguments"/>.
        /// </summary>
        /// <param name="typeArguments">An array of types that are used to select the appropriate pattern.</param>
        /// <returns>An instance of the type that matches the provided type arguments.</returns>
        public object? Specialize(params Type[] typeArguments)
        {
            return Specialize((IReadOnlyList<Type>)typeArguments);
        }

        /// <summary>
        /// Looks for the first type that successfully matches the combination of types in <paramref name="typeArguments"/>.
        /// </summary>
        /// <param name="typeArguments">A list of types that are used to select the appropriate pattern.</param>
        /// <returns>An instance of the type that matches the provided type arguments.</returns>
        public object? Specialize(IReadOnlyList<Type> typeArguments)
        {
            if(Fallback == null)
            {
                return SpecializeAll(typeArguments).FirstOrDefault();
            }
            return SpecializeAll(typeArguments).Concat(SpecializeTypeAll(Fallback, typeArguments, Patterns.Count, 0, Patterns.Count)).FirstOrDefault();
        }

        /// <summary>
        /// Looks for types that successfully matches the combination of types in <paramref name="typeArguments"/>.
        /// </summary>
        /// <param name="typeArguments">An array of types that are used to select the appropriate pattern.</param>
        /// <returns>A sequence of instances of all types that match the provided type arguments.</returns>
        public IEnumerable<object> SpecializeAll(params Type[] typeArguments)
        {
            return SpecializeAll((IReadOnlyList<Type>)typeArguments);
        }

        /// <summary>
        /// Looks for types that successfully matches the combination of types in <paramref name="typeArguments"/>.
        /// </summary>
        /// <param name="typeArguments">A list of types that are used to select the appropriate pattern.</param>
        /// <returns>A sequence of instances of all types that match the provided type arguments.</returns>
        public IEnumerable<object> SpecializeAll(IReadOnlyList<Type> typeArguments)
        {
            return SpecializeAll(typeArguments, 0, Patterns.Count, 0, Patterns.Count);
        }

        private IEnumerable<object> SpecializeAll(IReadOnlyList<Type> typeArguments, int start, int stop, int globalStart, int globalStop)
        {
            for(int i = start; i < stop; i++)
            {
                var type = Patterns[i];
                bool any = false;
                foreach(var obj in SpecializeTypeAll(type, typeArguments, i, globalStart, globalStop))
                {
                    yield return obj;
                    any = true;
                }
                if(any && type.GetCustomAttribute<DefinitiveAttribute>() != null)
                {
                    yield break;
                }
            }
        }

        private IEnumerable<object> SpecializeTypeAll(Type pattern, IReadOnlyList<Type> typeArguments, int i, int globalStart, int globalStop)
        {
            if(pattern == null) yield break;
            var mapping = pattern.GetCustomAttributes<TypeArgumentAttribute>().ToList();

            var typeArgs = new Dictionary<string, Type>();
            if(!pattern.IsGenericTypeDefinition)
            {
                if(pattern.ContainsGenericParameters)
                {
                    throw new InvalidOperationException($"Pattern type {pattern} must be a generic type definition or a closed constructed type.");
                }else if(pattern.IsGenericType)
                {
                    var defArgs = pattern.GetGenericArguments();
                    pattern = pattern.GetGenericTypeDefinition();
                    var defParams = pattern.GetGenericArguments();
                    for(int j = 0; j < defParams.Length; j++)
                    {
                        typeArgs[defParams[j].Name] = defArgs[j];
                    }
                }
            }
            if(pattern.IsGenericTypeDefinition)
            {
                var typeParams = pattern.GetGenericArguments();
                var args = new Type[typeParams.Length];

                foreach(var _ in MatchArguments(typeArguments, mapping, 0, typeArgs))
                {
                    bool set = true;
                    for(int j = 0; j < typeParams.Length; j++)
                    {
                        if(!typeArgs.TryGetValue(typeParams[j].Name, out args[j]!))
                        {
                            set = false;
                            break;
                        }
                    }
                    if(!set)
                    {
                        continue;
                    }

                    Type finalType;
                    try{
                        finalType = MakeGenericType(pattern, args);
                    }catch(ArgumentException)
                    {
                        continue;
                    }

                    foreach(var result in GetAllInstances(finalType, i, globalStart, globalStop, typeArguments, typeArgs))
                    {
                        yield return result;
                    }
                }
            }else{
                foreach(var _ in MatchArguments(typeArguments, mapping, 0, typeArgs))
                {
                    foreach(var result in GetAllInstances(pattern, i, globalStart, globalStop, typeArguments, typeArgs))
                    {
                        yield return result;
                    }
                }
            }
        }

        private IEnumerable<object> GetAllInstances(Type finalType, int i, int globalStart, int globalStop, IReadOnlyList<Type> typeArguments, IDictionary<string, Type> typeArgs)
        {
            foreach(var method in finalType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if(method.Name == "Create")
                {
                    bool any = false;
                    foreach(var args in GetAllParamCombinations(method, i, globalStart, globalStop, typeArguments, typeArgs))
                    {
                        object? obj;
                        try{
                            obj = method.Invoke(null, args);
                        }catch(Exception exc) when(CreationExceptions != null && CreationExceptions.Any(t => t.IsAssignableFrom(exc.GetType())))
                        {
                            continue;
                        }
                        if(obj is IEnumerable collection)
                        {
                            foreach(var val in collection)
                            {
                                if(val != null)
                                {
                                    yield return val;
                                    any = true;
                                }
                            }
                        }else if(obj != null)
                        {
                            yield return obj;
                            any = true;
                        }
                    }
                    if(any && method.GetCustomAttribute<DefinitiveAttribute>() != null)
                    {
                        yield break;
                    }
                }
            }

            foreach(var ctor in finalType.GetConstructors())
            {
                bool any = false;
                foreach(var args in GetAllParamCombinations(ctor, i, globalStart, globalStop, typeArguments, typeArgs))
                {
                    object obj;
                    try{
                        obj = ctor.Invoke(args);
                    }catch(Exception exc) when(CreationExceptions.Any(t => t.IsAssignableFrom(exc.GetType())))
                    {
                        continue;
                    }
                    yield return obj;
                    any = true;
                }
                if(any && ctor.GetCustomAttribute<DefinitiveAttribute>() != null)
                {
                    yield break;
                }
            }
        }

        private IEnumerable<object[]> GetAllParamCombinations(MethodBase method, int i, int globalStart, int globalStop, IReadOnlyList<Type> typeArguments, IDictionary<string, Type> typeArgs)
        {
            var ctorParams = method.GetParameters();
            return GetAllParamCombinations(ctorParams, new object[ctorParams.Length], 0, i, globalStart, globalStop, typeArguments, typeArgs);
        }

        private IEnumerable<object[]> GetAllParamCombinations(ParameterInfo[] ctorParams, object[] args, int index, int i, int globalStart, int globalStop, IReadOnlyList<Type> typeArguments, IDictionary<string, Type> typeArgs)
        {
            if(index == ctorParams.Length)
            {
                yield return args;
                yield break;
            }

            var param = ctorParams[index];
            var adapter = param.GetCustomAttribute<SpecializeForAttribute>();
            if(adapter != null)
            {
                var type = param.ParameterType;
                if(type.IsByRef) type = type.GetElementType()!;

                var newTypes = new List<Type>();
                ExtractTypes(newTypes, adapter.NewTypes, typeArgs);
                int innerStart = adapter.Direction == SpecializeDirection.Following ? Math.Max(globalStart, i + 1) : globalStart;
                int innerStop = adapter.Direction == SpecializeDirection.Preceding ? Math.Min(globalStop, i): globalStop;
                int innerGlobalStart, innerGlobalStop;
                if(adapter.GlobalDirection)
                {
                    innerGlobalStart = innerStart;
                    innerGlobalStop = innerStop;
                }else{
                    innerGlobalStart = globalStart;
                    innerGlobalStop = globalStop;
                }

                var collection = SpecializeAll(newTypes, innerStart, innerStop, innerGlobalStart, innerGlobalStop);
                if(type.Equals(typeof(IEnumerable)) || type.Equals(typeof(IEnumerable<object>)))
                {
                    args[index] = collection;
                }else{
                    bool any = false;
                    foreach(var obj in collection)
                    {
                        any = true;
                        args[index] = obj;
                        bool any2 = false;
                        foreach(var arr in GetAllParamCombinations(ctorParams, args, index + 1, i, globalStart, globalStop, typeArguments, typeArgs))
                        {
                            yield return arr;
                            any2 = true;
                        }
                        if(any2 && adapter.Definitive)
                        {
                            break;
                        }
                    }
                    if(any || !param.HasDefaultValue)
                    {
                        yield break;
                    }
                    args[index] = Type.Missing;
                }
            }else if(param.HasDefaultValue)
            {
                args[index] = Type.Missing;
            }else{
                yield break;
            }
            foreach(var arr in GetAllParamCombinations(ctorParams, args, index + 1, i, globalStart, globalStop, typeArguments, typeArgs))
            {
                yield return arr;
            }
        }

        private static void ExtractTypes(List<Type> newTypes, IReadOnlyList<Constraint> output, IDictionary<string, Type> typeArgs)
        {
            foreach(var constraint in output)
            {
                switch(constraint)
                {
                    case ParameterConstraint paramConstraint:
                        if(!typeArgs.TryGetValue(paramConstraint.Name, out var paramType))
                        {
                            throw new InvalidOperationException($"Generic parameter {paramConstraint.Name} does not have an assigned type.");
                        }
                        newTypes.Add(paramType);
                        break;
                    case ConcreteTypeConstraint typeConstraint:
                        var type = typeConstraint.Type;
                        if(type.IsGenericTypeDefinition)
                        {
                            var args = new List<Type>();
                            ExtractTypes(args, typeConstraint.Arguments, typeArgs);
                            type = MakeGenericType(type, args);
                        }
                        newTypes.Add(type);
                        break;
                    default:
                        throw new NotSupportedException($"Constraint {constraint} is not allowed in a {nameof(SpecializeForAttribute)}.");
                }
            }
        }

        private static Type MakeGenericType(Type? definition, IReadOnlyList<Type> arguments)
        {
            if(definition == null)
            {
                return arguments[0];
            }else if(definition.Equals(typeof(ArrayMarker<,>)) && typeof(IEncodedInt32).IsAssignableFrom(arguments[0]))
            {
                int rank = ((IEncodedInt32)Activator.CreateInstance(arguments[0])!).Value;
                return arguments[1].MakeArrayType(rank);
            }else if(definition.Equals(typeof(PointerMarker<>)))
            {
                return arguments[0].MakePointerType();
            }else if(definition.Equals(typeof(ByRefMarker<>)))
            {
                return arguments[0].MakeByRefType();
            }else if(!definition.IsGenericTypeDefinition && arguments.Count == 0)
            {
                return definition;
            }else{
                var args = arguments.ToArray();
                var typeParams = definition.GetGenericArguments();
                for(int i = 0; i < args.Length && i < typeParams.Length; i++)
                {
                    var arg = args[i];
                    if(arg.IsArray && typeParams[i].GetGenericParameterConstraints().Contains(typeof(IMarker)))
                    {
                        int rank = arg.GetArrayRank();
                        var rankType = typeof(EncodedInt32Zero);
                        for(int j = 0; j < rank; j++)
                        {
                            rankType = typeof(EncodedInt32<>).MakeGenericType(rankType);
                        }
                        arg = typeof(ArrayMarker<,>).MakeGenericType(rankType, arg.GetElementType()!);
                    }else if(arg.IsByRef)
                    {
                        arg = typeof(ByRefMarker<>).MakeGenericType(arg.GetElementType()!);
                    }else if(arg.IsPointer)
                    {
                        arg = typeof(PointerMarker<>).MakeGenericType(arg.GetElementType()!);
                    }
                }
                return definition.MakeGenericType(args);
            }
        }

        private static IEnumerable MatchArguments(IReadOnlyList<Type> typeArguments, IReadOnlyList<TypeArgumentAttribute> mapping, int index, IDictionary<string, Type> typeArgs)
        {
            if(index >= mapping.Count)
            {
                yield return null;
                yield break;
            }
            if(index >= typeArguments.Count)
            {
                yield break;
            }
            var attr = mapping[index];
            foreach(var _ in MatchConstraints(typeArguments[index], attr.Constraints, 0, ConstraintVariance.Ambivariant, typeArgs))
            {
                bool any = false;
                foreach(var result in MatchArguments(typeArguments, mapping, index + 1, typeArgs))
                {
                    yield return result;
                    any = true;
                }
                if(any && attr.Definitive)
                {
                    yield break;
                }
            }
        }

        private static IEnumerable MatchConstraints(Type typeArgument, IReadOnlyList<Constraint> constraints, int constraintIndex, ConstraintVariance outerVariance, IDictionary<string, Type> typeArgs)
        {
            if(constraintIndex >= constraints.Count)
            {
                yield return null;
                yield break;
            }
            var following = MatchConstraints(typeArgument, constraints, constraintIndex + 1, outerVariance, typeArgs);
            switch(constraints[constraintIndex])
            {
                case NullConstraint _:
                    break;
                case ParameterConstraint paramConstraint:
                    if(!typeArgs.TryGetValue(paramConstraint.Name, out var type))
                    {
                        typeArgs[paramConstraint.Name] = typeArgument;
                        try{
                            foreach(var result in following)
                            {
                                yield return result;
                            }
                        }finally{
                            typeArgs.Remove(paramConstraint.Name);
                        }
                    }else foreach(var concreteType in UnifyConstraint(typeArgument, type, paramConstraint, outerVariance, typeArgs))
                    {
                        foreach(var result in following)
                        {
                            yield return result;
                        }
                    }
                    break;
                case ConcreteTypeConstraint typeConstraint:
                    foreach(var concreteType in UnifyConstraint(typeArgument, typeConstraint.Type, typeConstraint, outerVariance, typeArgs))
                    {
                        foreach(var result in following)
                        {
                            yield return result;
                        }
                    }
                    break;
                case NotConstraint notConstraint:
                    var matchedTypes = UnifyInstantiation(new[] { typeArgument }, notConstraint.Arguments, new Type[notConstraint.Arguments.Count], 0, outerVariance, typeArgs);
                    if(!matchedTypes.Cast<object>().Any())
                    {
                        foreach(var result in following)
                        {
                            yield return result;
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException($"Constraint {constraints[constraintIndex]} is not allowed for a {nameof(TypeArgumentAttribute)}.");
            }
        }

        private static ConstraintVariance CombineVariance(ConstraintVariance outer, ConstraintVariance inner)
        {
            switch(outer)
            {
                case ConstraintVariance.Invariant:
                    return outer;
                case ConstraintVariance.Covariant:
                    switch(inner)
                    {
                        case ConstraintVariance.Invariant:
                        case ConstraintVariance.Covariant:
                        case ConstraintVariance.Contravariant:
                            return inner;
                        default:
                            return ConstraintVariance.Covariant;
                    }
                case ConstraintVariance.Contravariant:
                    switch(inner)
                    {
                        case ConstraintVariance.Invariant:
                            return inner;
                        case ConstraintVariance.Covariant:
                            return ConstraintVariance.Contravariant;
                        case ConstraintVariance.Contravariant:
                            return ConstraintVariance.Covariant;
                        default:
                            return ConstraintVariance.Contravariant;
                    }
                default:
                    return inner;
            }
        }

        private static IEnumerable<Type> UnifyConstraint(Type typeArgument, Type type, TypeConstraint typeConstraint, ConstraintVariance outerVariance, IDictionary<string, Type> typeArgs)
        {
            var constraintArgs = new Type[typeConstraint.Arguments.Count];
            var variance = CombineVariance(outerVariance, typeConstraint.Variance);
            IEnumerable<Type> instantiations;
            switch(variance)
            {
                case ConstraintVariance.Covariant:
                    instantiations = GetInverseInstantiations(typeArgument, type);
                    break;
                case ConstraintVariance.Contravariant:
                    instantiations = GetInstantiations(typeArgument, type);
                    break;
                case ConstraintVariance.Invariant:
                    instantiations = GetInstantiations(typeArgument, type).Intersect(GetInverseInstantiations(typeArgument, type));
                    break;
                default:
                    instantiations = GetInstantiations(typeArgument, type).Union(GetInverseInstantiations(typeArgument, type));
                    break;
            }
            foreach(var typeInstantiation in instantiations)
            {
                var instantiationArgs = typeInstantiation.GetGenericArguments();
                foreach(var _ in UnifyInstantiation(instantiationArgs, typeConstraint.Arguments, constraintArgs, 0, variance, typeArgs))
                {
                    Type result;
                    try{
                        result = MakeGenericType(type, constraintArgs);
                    }catch(ArgumentException)
                    {
                        continue;
                    }
                    switch(variance)
                    {
                        case ConstraintVariance.Invariant:
                            if(result.Equals(typeArgument))
                            {
                                yield return result;
                            }
                            break;
                        case ConstraintVariance.Covariant:
                            if(typeArgument.IsAssignableFrom(result))
                            {
                                yield return result;
                            }
                            break;
                        case ConstraintVariance.Contravariant:
                            if(result.IsAssignableFrom(typeArgument))
                            {
                                yield return result;
                            }
                            break;
                        default:
                            if(typeArgument.IsAssignableFrom(result) || result.IsAssignableFrom(typeArgument))
                            {
                                yield return result;
                            }
                            break;
                    }
                }
            }
        }

        private static IEnumerable UnifyInstantiation(Type[] instantiationArgs, IReadOnlyList<Constraint> constraints, Type[] constraintArgs, int index, ConstraintVariance outerVariance, IDictionary<string, Type> typeArgs)
        {
            if(index >= constraints.Count)
            {
                yield return null;
                yield break;
            }
            var following = UnifyInstantiation(instantiationArgs, constraints, constraintArgs, index + 1, outerVariance, typeArgs);
            switch(constraints[index])
            {
                case NullConstraint _:
                    constraintArgs[index] = instantiationArgs[index];
                    foreach(var result in following)
                    {
                        yield return result;
                    }
                    break;
                case ParameterConstraint paramConstraint:
                    if(!typeArgs.TryGetValue(paramConstraint.Name, out var type))
                    {
                        constraintArgs[index] = typeArgs[paramConstraint.Name] = instantiationArgs[index];
                        try{
                            foreach(var result in following)
                            {
                                yield return result;
                            }
                        }finally{
                            typeArgs.Remove(paramConstraint.Name);
                        }
                    }else foreach(var concreteType in UnifyConstraint(instantiationArgs[index], type, paramConstraint, outerVariance, typeArgs))
                    {
                        constraintArgs[index] = concreteType;
                        foreach(var result in following)
                        {
                            yield return result;
                        }
                    }
                    break;
                case ConcreteTypeConstraint typeConstraint:
                    foreach(var concreteType in UnifyConstraint(instantiationArgs[index], typeConstraint.Type, typeConstraint, outerVariance, typeArgs))
                    {
                        constraintArgs[index] = concreteType;
                        foreach(var result in following)
                        {
                            yield return result;
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException($"Constraint {constraints[index]} is not allowed for a {nameof(TypeArgumentAttribute)}.");
            }
        }

        private static IEnumerable<Type> GetInstantiations(Type typeArgument, Type? typeConstraint)
        {
            foreach(var baseClass in GetAllTypes(typeArgument))
            {
                if(typeConstraint == null)
                {
                    yield return baseClass;
                }else if(baseClass.Equals(typeConstraint))
                {
                    yield return baseClass;
                }else if(baseClass.IsGenericType && baseClass.GetGenericTypeDefinition().Equals(typeConstraint))
                {
                    yield return baseClass;
                }else if(baseClass.IsArray && typeConstraint.Equals(typeof(ArrayMarker<,>)))
                {
                    int rank = baseClass.GetArrayRank();
                    var rankType = typeof(EncodedInt32Zero);
                    for(int i = 0; i < rank; i++)
                    {
                        rankType = typeof(EncodedInt32<>).MakeGenericType(rankType);
                    }
                    yield return typeof(ArrayMarker<,>).MakeGenericType(rankType, baseClass.GetElementType()!);
                }else if(baseClass.IsPointer && typeConstraint.Equals(typeof(PointerMarker<>)))
                {
                    yield return typeof(PointerMarker<>).MakeGenericType(baseClass.GetElementType()!);
                }else if(baseClass.IsByRef && typeConstraint.Equals(typeof(ByRefMarker<>)))
                {
                    yield return typeof(ByRefMarker<>).MakeGenericType(baseClass.GetElementType()!);
                }else if(baseClass.IsValueType && typeConstraint.Equals(typeof(ValueTypeMarker)))
                {
                    yield return baseClass;
                }else if(!baseClass.IsValueType && typeConstraint.Equals(typeof(ReferenceTypeMarker)))
                {
                    yield return baseClass;
                }
            }
        }

        private static IEnumerable<Type> GetInverseInstantiations(Type typeArgument, Type? typeConstraint)
        {
            if(typeConstraint == null)
            {
                yield return typeArgument;
                yield break;
            }
            foreach(var superClass in GetAllTypes(typeConstraint))
            {
                if(superClass.Equals(typeArgument))
                {
                    yield return typeArgument;
                }else if(superClass.IsGenericTypeDefinition && superClass.Equals(typeArgument.GetGenericTypeDefinition()))
                {
                    yield return typeArgument;
                }else if(superClass.Equals(typeof(ArrayMarker<,>)) && typeArgument.IsArray)
                {
                    int rank = typeArgument.GetArrayRank();
                    var rankType = typeof(EncodedInt32Zero);
                    for(int i = 0; i < rank; i++)
                    {
                        rankType = typeof(EncodedInt32<>).MakeGenericType(rankType);
                    }
                    yield return typeof(ArrayMarker<,>).MakeGenericType(rankType, typeArgument.GetElementType()!);
                }else if(superClass.Equals(typeof(PointerMarker<>)) && typeArgument.IsPointer)
                {
                    yield return typeof(PointerMarker<>).MakeGenericType(typeArgument.GetElementType()!);
                }else if(superClass.Equals(typeof(ByRefMarker<>)) && typeArgument.IsByRef)
                {
                    yield return typeof(ByRefMarker<>).MakeGenericType(typeArgument.GetElementType()!);
                }else if(superClass.Equals(typeof(ValueTypeMarker)) && typeArgument.IsValueType)
                {
                    yield return typeArgument;
                }else if(superClass.Equals(typeof(ReferenceTypeMarker)) && !typeArgument.IsValueType)
                {
                    yield return typeArgument;
                }
            }
        }

        private static IEnumerable<Type> GetAllTypes(Type? type)
        {
            if(type == null) yield break;
            yield return type;
            foreach(var interfaceType in type.GetInterfaces())
            {
                yield return interfaceType;
            }
            type = type.BaseType;
            while(type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }


        internal static IReadOnlyList<Constraint> ParseConstraintList(object[] constraints, IEnumerable<ConstraintVariance> variances)
        {
            var list = new List<Constraint>();
            int i = -1;
            ParseConstraintList(list, -1, constraints, variances, ref i);
            return list;
        }

        private static void ParseConstraintList(IList<Constraint> list, int count, object[] constraints, IEnumerable<ConstraintVariance> variances, ref int i)
        {
            var venum = variances.GetEnumerator();
            ConstraintVariance? variance = null;
            while(i < constraints.Length - 1 && count != 0)
            {
                i++;
                if(variance == null)
                {
                    if(venum.MoveNext())
                    {
                        variance = venum.Current;
                    }else{
                        variance = ConstraintVariance.Invariant;
                    }
                }
                switch(constraints[i])
                {
                    case null:
                        list.Add(NullConstraint.Instance);
                        variance = null;
                        break;
                    case string name:
                        list.Add(new ParameterConstraint(variance.GetValueOrDefault(), name));
                        variance = null;
                        break;
                    case Type type:
                        if(type.IsGenericTypeDefinition)
                        {
                            var args = new List<Constraint>();
                            ParseConstraintList(args, type.GetGenericArguments().Length, constraints, type.GetGenericArguments().Select(GetVariance), ref i);
                            list.Add(new ConcreteTypeConstraint(variance.GetValueOrDefault(), type, args));
                        }else if(type.IsArray)
                        {
                            int rank = type.GetArrayRank();
                            var rankType = typeof(EncodedInt32Zero);
                            for(int j = 0; j < rank; j++)
                            {
                                rankType = typeof(EncodedInt32<>).MakeGenericType(rankType);
                            }
                            list.Add(new ConcreteTypeConstraint(variance.GetValueOrDefault(), typeof(ArrayMarker<,>), new ConcreteTypeConstraint(ConstraintVariance.Invariant, rankType), new ConcreteTypeConstraint(ConstraintVariance.Invariant, type.GetElementType()!)));
                        }else if(type.IsByRef)
                        {
                            list.Add(new ConcreteTypeConstraint(variance.GetValueOrDefault(), typeof(ByRefMarker<>), new ConcreteTypeConstraint(ConstraintVariance.Invariant, type.GetElementType()!)));
                        }else if(type.IsPointer)
                        {
                            list.Add(new ConcreteTypeConstraint(variance.GetValueOrDefault(), typeof(PointerMarker<>), new ConcreteTypeConstraint(ConstraintVariance.Invariant, type.GetElementType()!)));
                        }else{
                            list.Add(new ConcreteTypeConstraint(variance.GetValueOrDefault(), type));
                        }
                        variance = null;
                        break;
                    case SpecialConstraint simple:
                        switch(simple)
                        {
                            case SpecialConstraint.Array:
                            {
                                var args = new List<Constraint>();
                                args.Add(new ConcreteTypeConstraint(ConstraintVariance.Invariant, typeof(EncodedInt32<EncodedInt32Zero>)));
                                ParseConstraintList(args, 1, constraints, Array.Empty<ConstraintVariance>(), ref i);
                                list.Add(new ConcreteTypeConstraint(variance.GetValueOrDefault(), simple, args));
                            }
                            break;
                            case SpecialConstraint.MdArray:
                            {
                                var args = new List<Constraint>();
                                ParseConstraintList(args, 2, constraints, Array.Empty<ConstraintVariance>(), ref i);
                                list.Add(new ConcreteTypeConstraint(variance.GetValueOrDefault(), simple, args));
                            }
                            break;
                            case SpecialConstraint.Pointer:
                            case SpecialConstraint.ByRef:
                            {
                                var args = new List<Constraint>();
                                ParseConstraintList(args, 1, constraints, Array.Empty<ConstraintVariance>(), ref i);
                                list.Add(new ConcreteTypeConstraint(variance.GetValueOrDefault(), simple, args));
                            }
                            break;
                            case SpecialConstraint.ValueType:
                            case SpecialConstraint.ReferenceType:
                            {
                                list.Add(new ConcreteTypeConstraint(variance.GetValueOrDefault(), simple));
                            }
                            break;
                            case SpecialConstraint.Not:
                            {
                                var args = new List<Constraint>();
                                ParseConstraintList(args, 1, constraints, new[] { variance.GetValueOrDefault() }, ref i);
                                list.Add(new NotConstraint(args));
                            }
                            break;
                            default:
                                throw new ArgumentException($"Unrecognized special constraint {simple}.", nameof(constraints));
                        }
                        variance = null;
                        break;
                    case int number:
                        var intType = typeof(EncodedInt32Zero);
                        for(int j = 0; j < number; j++)
                        {
                            intType = typeof(EncodedInt32<>).MakeGenericType(intType);
                        }
                        list.Add(new ConcreteTypeConstraint(variance.GetValueOrDefault(), intType));
                        variance = null;
                        break;
                    case ConstraintVariance newVariance:
                        variance = newVariance;
                        count++;
                        break;
                    default:
                        throw new ArgumentException($"Unrecognized constraint {constraints[i]}.", nameof(constraints));
                }
                count--;
            }
            if(count > 0)
            {
                throw new ArgumentException("Constraint arguments are missing.", nameof(constraints));
            }
        }

        internal static ConstraintVariance GetVariance(Type typeParam)
        {
            switch(typeParam.GenericParameterAttributes & GenericParameterAttributes.VarianceMask)
            {
                case GenericParameterAttributes.Covariant | GenericParameterAttributes.Contravariant:
                    return ConstraintVariance.Ambivariant;
                case GenericParameterAttributes.Covariant:
                    return ConstraintVariance.Covariant;
                case GenericParameterAttributes.Contravariant:
                    return ConstraintVariance.Contravariant;
                default:
                    return ConstraintVariance.Invariant;
            }
        }

        internal static IEnumerable<ConstraintVariance> AnyVariances = GetAnyVariances();

        private static IEnumerable<ConstraintVariance> GetAnyVariances()
        {
            while(true) yield return ConstraintVariance.Ambivariant;
        }
    }
}
