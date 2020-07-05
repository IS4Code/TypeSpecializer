using System;

namespace IS4.TypeSpecializer
{
    /// <summary>
    /// Indicates that when a pattern type, method, or constructor is matched, the following ones will not be considered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class DefinitiveAttribute : Attribute
    {

    }
}
