TypeSpecializer
==========

_TypeSpecializer_ is a .NET Standard 2.1 library for utilising a technique similar to C++'s partial template specialization, but at runtime and with generic types.

This library can be used when an instance of a specific type should be created at runtime based on a combination of several type arguments. It is suitable for cases where the behaviour of a generic type or method cannot be implemented by purely generic code, and instead must be specialized for various kinds of types.

## Capabilities
This library creates instances of custom types that specify the constraints for type arguments via custom attributes in declarative style. It uses advanced pattern matching (for constructed types) and supports inheritance (in both ways). Multiple instances can also be returned as a lazy sequence, and constructing an instance can be done via a constructor or a custom static `Create` method, supporting additional dependency injection for its arguments as other specialized instances.

## Documentation
See the [wiki](//github.com/IllidanS4/TypeSpecializer/wiki) for documentation and examples.

## Example
There is no general method in .NET to obtain the size of any `object` in term of the count of its elements, as `ICollection<>` doesn't implement any non-generic interface that provides the count. This library can be used to select the proper way of obtaining the count based on the argument.
```cs
public static void Test()
{
    var dict = new Dictionary<int, int> { { 1, 2 }, { 2, 3 }, { 3, 4 } };
    Assert.AreEqual(dict.Count, GetCount(dict));
}

public static int GetCount<T>(T obj)
{
    var specializer = new Specializer(
        typeof(GenericCollectionCountProvider<>),
        typeof(NonGenericCollectionCountProvider)
    );
    var provider = specializer.Specialize(typeof(T)) as ICountProvider<T>;
    if(provider == null)
    {
        throw new ArgumentException(nameof(obj));
    }
    return provider.GetCount(obj);
}

interface ICountProvider<in T>
{
    int GetCount(T obj);
}

[TypeArgument(ConstraintVariance.Contravariant, typeof(ICollection<>), nameof(T))]
class GenericCollectionCountProvider<T> : ICountProvider<ICollection<T>>
{
    public int GetCount(ICollection<T> obj)
    {
        return obj.Count;
    }
}

[TypeArgument(ConstraintVariance.Contravariant, typeof(ICollection))]
class NonGenericCollectionCountProvider : ICountProvider<ICollection>
{
    public int GetCount(ICollection obj)
    {
        return obj.Count;
    }
}
```