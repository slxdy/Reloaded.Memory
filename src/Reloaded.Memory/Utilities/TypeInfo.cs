﻿#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Runtime.Serialization;

namespace Reloaded.Memory.Utilities;

/// <summary>
///     A group of useful utility methods for determining if a type is blittable.
/// </summary>
public static class TypeInfo
{
    /// <summary>
    ///     Returns true if a type is blittable, else false.
    /// </summary>
    /// <typeparam name="T">The type to verify for blittability.</typeparam>
    /// <remarks>
    ///     This approach is not perfect, it is an approximation; for example, it might not work with generic types
    ///     with blittable (unmanaged) constraints.
    /// </remarks>
    public static bool ApproximateIsBlittable<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
        T>()
    {
        return IsBlittableCache<T>.Value;
    }

    /// <summary>
    ///     Returns true if a type is blittable, else false.
    /// </summary>
    /// <param name="type">Type of item to check.</param>
    /// <remarks>
    ///     This approach is not perfect, it is an approximation; for example, it might not work with generic types
    ///     with blittable (unmanaged) constraints.
    /// </remarks>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072",
        Justification = "Analyzer reflection skill issue in IsBlittable.")]
#endif
    public static bool ApproximateIsBlittable(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
        Type type)
    {
        if (type.IsArray)
        {
            Type? elem = type.GetElementType();
            return elem is { IsValueType: true } && ApproximateIsBlittable(elem);
        }

        try
        {
            var instance = FormatterServices.GetUninitializedObject(type);
            GCHandle.Alloc(instance, GCHandleType.Pinned).Free();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static class IsBlittableCache<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
        T>
    {
        public static readonly bool Value = ApproximateIsBlittable(typeof(T));
    }
}
