﻿using Reloaded.Memory.Exceptions;
using Reloaded.Memory.Native.Unix;
using Reloaded.Memory.Native.Windows;
using Reloaded.Memory.Utility;
using static Reloaded.Memory.Native.Windows.Kernel32.MEM_PROTECTION;
using static Reloaded.Memory.Native.Unix.UnixMemoryProtection;

namespace Reloaded.Memory.Enums;

/// <summary>
///     Lists the various memory protection modes available.
/// </summary>
[Flags]
public enum MemoryProtection
{
    /// <summary>
    ///     Allows you to read the memory.
    /// </summary>
    READ = 1 << 0,

    /// <summary>
    ///     Allows you to write the memory.
    /// </summary>
    WRITE = 1 << 1,

    /// <summary>
    ///     Allows you to execute the memory.
    /// </summary>
    EXECUTE = 1 << 2,

    /// <summary>
    ///     Allows you to read, write and execute
    /// </summary>
    READ_WRITE_EXECUTE = READ | WRITE | EXECUTE
}

/// <summary>
///     Extension methods for converting <see cref="MemoryProtection" /> to platform specific values.
/// </summary>
public static class MemoryProtectionExtensions
{
#pragma warning disable CA1416 // This API requires the operating system version to be checked
    /// <summary>
    ///     Converts a <see cref="MemoryProtection" /> to a platform specific value.
    /// </summary>
    /// <param name="protection">The protection to convert.</param>
    /// <returns>A platform specific value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint ToCurrentPlatform(this MemoryProtection protection)
    {
        // Check if is windows
        if (Polyfills.IsWindows())
            return ToWindows(protection);

        if (Polyfills.IsLinux() || Polyfills.IsMacOS())
            return ToUnix(protection);

        ThrowHelpers.ThrowPlatformNotSupportedException();
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint ToUnix(MemoryProtection protection)
    {
        UnixMemoryProtection result = 0;
        if (protection.HasFlagFast(MemoryProtection.READ))
            result |= PROT_READ;
        if (protection.HasFlagFast(MemoryProtection.WRITE))
            result |= PROT_WRITE;
        if (protection.HasFlagFast(MemoryProtection.EXECUTE))
            result |= PROT_EXEC;

        return (nuint)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint ToWindows(MemoryProtection protection)
    {
        Kernel32.MEM_PROTECTION result = 0;

        if (protection.HasFlagFast(MemoryProtection.READ) && protection.HasFlagFast(MemoryProtection.WRITE) &&
            protection.HasFlagFast(MemoryProtection.EXECUTE))
        {
            result = PAGE_EXECUTE_READWRITE;
        }
        else if (protection.HasFlagFast(MemoryProtection.READ) && protection.HasFlagFast(MemoryProtection.WRITE))
        {
            result = PAGE_READWRITE;
        }
        else if (protection.HasFlagFast(MemoryProtection.READ) && protection.HasFlagFast(MemoryProtection.EXECUTE))
        {
            result = PAGE_EXECUTE_READ;
        }
        else if (protection.HasFlagFast(MemoryProtection.WRITE) && protection.HasFlagFast(MemoryProtection.EXECUTE))
        {
            // There is no specific flag for Write + Execute, so we use PAGE_EXECUTE_READWRITE
            result = PAGE_EXECUTE_READWRITE;
        }
        else if (protection.HasFlagFast(MemoryProtection.READ))
        {
            result = PAGE_READONLY;
        }
        else if (protection.HasFlagFast(MemoryProtection.WRITE))
        {
            result = PAGE_READWRITE;
        }
        else if (protection.HasFlagFast(MemoryProtection.EXECUTE))
        {
            result = PAGE_EXECUTE;
        }

        return (nuint)result;
    }
#pragma warning restore CA1416 // This API requires the operating system version to be checked
}
