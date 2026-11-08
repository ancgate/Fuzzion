#if NETFRAMEWORK
// Lets the test project use records and init-only properties on .NET Framework.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
#endif
