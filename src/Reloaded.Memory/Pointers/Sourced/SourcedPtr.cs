﻿using Reloaded.Memory.Interfaces;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Reloaded.Memory.Pointers.Sourced;

/// <summary>
///     A blittable single level pointer type that you can use with generic types which has an attached source.
/// </summary>
/// <typeparam name="T">The item behind the blittable pointer.</typeparam>
/// <typeparam name="TSource">The source the operations on the pointer are performed on.</typeparam>
[PublicAPI]
public unsafe struct SourcedPtr<
#if NET5_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
    T, TSource> : IEquatable<SourcedPtr<T, TSource>> where T : unmanaged where TSource : ICanReadWriteMemory
{
    /// <summary>
    ///     The pointer to the value.
    ///     For reference only; only use for pointers in RAM (based on <see cref="Memory{T}" />), otherwise use methods!
    /// </summary>
    /// <remarks>Only use for pointers in same process.</remarks>
    public Ptr<T> Pointer;

    /// <summary>
    ///     The source from which the data is read/written from.
    /// </summary>
    public TSource Source;

    /// <summary>
    ///     Creates a sourced pointer given the raw pointer and the source.
    /// </summary>
    /// <param name="pointer">The raw pointer.</param>
    /// <param name="source">The source behind the pointer.</param>
    public SourcedPtr(Ptr<T> pointer, TSource source)
    {
        Pointer = pointer;
        Source = source;
    }

    /// <inheritdoc cref="SourcedPtr{T,TSource}.AsRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T AsRef() => ref Pointer.AsRef();

    /// <summary>
    ///     Gets the value at the address where the current pointer points to.
    /// </summary>
    /// <returns>The value at the pointer's address.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get() => Pointer.Get(Source);

    /// <summary>
    ///     Gets the value at the address where the current pointer points to.
    /// </summary>
    /// <param name="value">The value at the pointer's address.</param>
    public void Get(out T value) => Pointer.Get(Source, out value);

    /// <summary>
    ///     Sets the value where the current pointer is pointing to.
    /// </summary>
    /// <param name="value">The value to set at the pointer's address.</param>
    public void Set(in T value) => Pointer.Set(Source, value);

    /// <summary>
    ///     Gets the value at the address where the current pointer points to plus the index offset.
    /// </summary>
    /// <param name="index">The index offset of the element.</param>
    /// <returns>The value at the pointer's address plus the index offset.</returns>
    public T Get(int index) => Pointer.Get(Source, index);

    /// <summary>
    ///     Gets the value at the address where the current pointer points to plus the index offset.
    /// </summary>
    /// <param name="index">The index offset of the element.</param>
    /// <param name="value">The value at the pointer's address plus the index offset.</param>
    public void Get(int index, out T value) => Pointer.Get(Source, index, out value);

    /// <summary>
    ///     Sets the value where the current pointer is pointing to plus the index offset.
    /// </summary>
    /// <param name="index">The index offset of the element.</param>
    /// <param name="value">The value to set at the pointer's address plus the index offset.</param>
    public void Set(int index, in T value) => Pointer.Set(Source, index, value);

    /// <summary>
    ///     Compares two <see cref="SourcedPtr{T,TSource}" /> instances for equality.
    /// </summary>
    /// <param name="left">The left <see cref="SourcedPtr{T,TSource}" /> to compare.</param>
    /// <param name="right">The right <see cref="SourcedPtr{T,TSource}" /> to compare.</param>
    /// <returns>True if the pointers are equal, false otherwise.</returns>
    public static bool operator ==(SourcedPtr<T, TSource> left, SourcedPtr<T, TSource> right) => left.Equals(right);

    /// <summary>
    ///     Compares two <see cref="SourcedPtr{T,TSource}" /> instances for inequality.
    /// </summary>
    /// <param name="left">The left <see cref="SourcedPtr{T,TSource}" /> to compare.</param>
    /// <param name="right">The right <see cref="SourcedPtr{T,TSource}" /> to compare.</param>
    /// <returns>True if the pointers are not equal, false otherwise.</returns>
    public static bool operator !=(SourcedPtr<T, TSource> left, SourcedPtr<T, TSource> right) => !left.Equals(right);

    /// <summary>
    ///     Adds an integer offset to a <see cref="SourcedPtr{T,TSource}" />.
    /// </summary>
    /// <param name="pointer">The <see cref="SourcedPtr{T,TSource}" /> to add the offset to.</param>
    /// <param name="offset">The integer offset to add.</param>
    /// <returns>A new <see cref="SourcedPtr{T,TSource}" /> with the updated address.</returns>
    public static SourcedPtr<T, TSource> operator +(SourcedPtr<T, TSource> pointer, int offset)
        => new(pointer.Pointer + offset, pointer.Source);

    /// <summary>
    ///     Subtracts an integer offset from a <see cref="SourcedPtr{T,TSource}" />.
    /// </summary>
    /// <param name="pointer">The <see cref="SourcedPtr{T,TSource}" /> to subtract the offset from.</param>
    /// <param name="offset">The integer offset to subtract.</param>
    /// <returns>A new <see cref="SourcedPtr{T,TSource}" /> with the updated address.</returns>
    public static SourcedPtr<T, TSource> operator -(SourcedPtr<T, TSource> pointer, int offset)
        => new(pointer.Pointer - offset, pointer.Source);

    /// <summary>
    ///     Increments the address of the <see cref="SourcedPtr{T,TSource}" /> by the size of <typeparamref name="T" />.
    /// </summary>
    /// <param name="pointer">The <see cref="SourcedPtr{T,TSource}" /> to increment.</param>
    /// <returns>A new <see cref="SourcedPtr{T,TSource}" /> with the incremented address.</returns>
    public static SourcedPtr<T, TSource> operator ++(SourcedPtr<T, TSource> pointer)
        => new(pointer.Pointer + 1, pointer.Source);

    /// <summary>
    ///     Decrements the address of the <see cref="SourcedPtr{T,TSource}" /> by the size of <typeparamref name="T" />.
    /// </summary>
    /// <param name="pointer">The <see cref="SourcedPtr{T,TSource}" /> to decrement.</param>
    /// <returns>A new <see cref="SourcedPtr{T,TSource}" /> with the decremented address.</returns>
    public static SourcedPtr<T, TSource> operator --(SourcedPtr<T, TSource> pointer)
        => new(pointer.Pointer - 1, pointer.Source);

    /// <summary>
    ///     Determines if the <see cref="SourcedPtr{T,TSource}" /> is considered "true" in a boolean context.
    /// </summary>
    /// <param name="operand">The <see cref="SourcedPtr{T,TSource}" /> to evaluate.</param>
    /// <returns>True if the underlying pointer is not null, false otherwise.</returns>
    public static bool operator true(SourcedPtr<T, TSource> operand) => operand.Pointer.Pointer != null;

    /// <summary>
    ///     Determines if the <see cref="SourcedPtr{T,TSource}" /> is considered "true" in a boolean context.
    /// </summary>
    /// <param name="operand">The <see cref="SourcedPtr{T,TSource}" /> to evaluate.</param>
    /// <returns>True if the underlying pointer is not null, false otherwise.</returns>
    public static bool operator false(SourcedPtr<T, TSource> operand) => operand.Pointer.Pointer == null;

    /// <inheritdoc />
    public bool Equals(SourcedPtr<T, TSource> other) => Pointer.Equals(other.Pointer) &&
                                                        EqualityComparer<TSource>.Default.Equals(Source, other.Source);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SourcedPtr<T, TSource> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => (Pointer.GetHashCode() * 397) ^ EqualityComparer<TSource>.Default.GetHashCode(Source);

    /// <inheritdoc />
    public override string ToString() => $"SourcedPtr<{typeof(T).Name}, {typeof(TSource).Name}> ({Pointer})";
}
