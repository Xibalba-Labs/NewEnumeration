using System;

namespace Headspring
{
    [Obsolete("This legacy enumeration API is obsolete. Consider migrating namespaces to Xibalbalabs.Enumeration.Core in the future.")]
    public abstract class Enumeration<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        TEnumeration> : Xibalbalabs.Enumeration.Core.Enumeration<TEnumeration>
        where TEnumeration : Enumeration<TEnumeration>
    {
        protected Enumeration(int value, string displayName) : base(value, displayName) { }
    }

    [Obsolete("This legacy enumeration API is obsolete. Consider migrating namespaces to Xibalbalabs.Enumeration.Core in the future.")]
    public abstract class Enumeration<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        TEnumeration, TValue> : Xibalbalabs.Enumeration.Core.Enumeration<TEnumeration, TValue>
        where TEnumeration : Enumeration<TEnumeration, TValue>
        where TValue : IComparable
    {
        protected Enumeration(TValue value, string displayName) : base(value, displayName) { }
    }
}
