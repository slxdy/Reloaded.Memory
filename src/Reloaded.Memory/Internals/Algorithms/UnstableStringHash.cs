using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Reloaded.Memory.Utilities;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace Reloaded.Memory.Internals.Algorithms;

/// <summary>
///     Unstable string hash algorithm.
///     Each implementation prioritises speed, and different machines may produce different results.
/// </summary>
internal static class UnstableStringHash
{
    /// <summary>
    ///     Faster hashcode for strings; but does not randomize between application runs.
    /// </summary>
    /// <param name="text">The string for which to get hash code for.</param>
    /// <remarks>
    ///     'Use this if and only if 'Denial of Service' attacks are not a concern (i.e. never used for free-form user input),
    ///     or are otherwise mitigated.
    /// </remarks>
    [ExcludeFromCodeCoverage] // "Cannot be accurately measured without multiple architectures. Known good impl." This is still tested tho.
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe nuint GetHashCodeUnstable(this ReadOnlySpan<char> text)
    {
        var length = text.Length; // Span has no guarantee of null terminator.
#if NET7_0_OR_GREATER
        // For short strings below size of nuint, we need separate approach; so we use legacy runtime approach
        // for said cold case.

        // ReSharper disable once InvertIf
        if (length >= sizeof(nuint) / sizeof(char)) // <= do not invert, hot path.
        {
            // Note. In these implementations we leave some (< sizeof(nuint)) data from the hash.
            // For our use of hashing file paths, this is okay, as files with different names but same extension
            // would still hash differently. If I were to PR this to runtime though, this would need fixing.

            // AVX Version
            // Ideally I could rewrite this in full Vector256 but I don't know how to get it to emit VPMULUDQ for the multiply operation.
            if (Avx2.IsSupported && length >= sizeof(Vector256<ulong>) / sizeof(char) * 4) // over 128 bytes + AVX
                return text.UnstableHashAvx2();

            // Over 64 bytes + Vector128. Supported on all x64 and ARM64 processors.
            if (Vector128.IsHardwareAccelerated && length >= sizeof(Vector128<ulong>) / sizeof(char) * 4)
                return text.UnstableHashVec128();

            return text.UnstableHashNonVector();
        }
#endif

        return text.UnstableHashNonVector();
    }

    #if NET7_0_OR_GREATER
    internal static unsafe UIntPtr UnstableHashVec128(this ReadOnlySpan<char> text)
    {
        fixed (char* src = &text.GetPinnableReference())
        {
            var length = text.Length; // Span has no guarantee of null terminator.
            nuint hash1 = (5381 << 16) + 5381;
            var hash2 = hash1;
            var ptr = (nuint*)(src);

            var prime = Vector128.Create((ulong)0x100000001b3);
            var hash1_128 = Vector128.Create(0xcbf29ce484222325);
            var hash2_128 = Vector128.Create(0xcbf29ce484222325);

            while (length >= sizeof(Vector128<ulong>) / sizeof(char) * 4) // 64 byte chunks.
            {
                length -= (sizeof(Vector128<ulong>) / sizeof(char)) * 4;
                hash1_128 = Vector128.Xor(hash1_128, Vector128.Load((ulong*)ptr));
                hash1_128 = Vector128.Multiply(hash1_128.AsUInt32(), prime.AsUInt32()).AsUInt64();

                hash2_128 = Vector128.Xor(hash2_128, Vector128.Load((ulong*)ptr + 2));
                hash2_128 = Vector128.Multiply(hash2_128.AsUInt32(), prime.AsUInt32()).AsUInt64();

                hash1_128 = Vector128.Xor(hash1_128, Vector128.Load((ulong*)ptr + 4));
                hash1_128 = Vector128.Multiply(hash1_128.AsUInt32(), prime.AsUInt32()).AsUInt64();

                hash2_128 = Vector128.Xor(hash2_128, Vector128.Load((ulong*)ptr + 6));
                hash2_128 = Vector128.Multiply(hash2_128.AsUInt32(), prime.AsUInt32()).AsUInt64();
                ptr += (sizeof(Vector128<ulong>) / sizeof(nuint)) * 4;
            }

            while (length >= sizeof(Vector128<ulong>) / sizeof(char)) // 16 byte chunks.
            {
                length -= sizeof(Vector128<ulong>) / sizeof(char);
                hash1_128 = Vector128.Xor(hash1_128, Vector128.Load((ulong*)ptr));
                hash1_128 = Vector128.Multiply(hash1_128.AsUInt32(), prime.AsUInt32()).AsUInt64();
                ptr += (sizeof(Vector128<ulong>) / sizeof(nuint));
            }

            // Flatten
            hash1_128 ^= hash2_128;
            if (sizeof(nuint) == 8)
            {
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (nuint)hash1_128[0];
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (nuint)hash1_128[1];
            }
            else
            {
                var hash1Uint = hash1_128.AsUInt32();
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (hash1Uint[0] * hash1Uint[1]);
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (hash1Uint[2] * hash1Uint[3]);
            }

            // 4/8 byte remainders
            while (length >= (sizeof(nuint) / sizeof(char)))
            {
                length -= (sizeof(nuint) / sizeof(char));
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                ptr += 1;
            }

            return hash1 + (hash2 * 1566083941);
        }
    }

    internal static unsafe UIntPtr UnstableHashAvx2(this ReadOnlySpan<char> text)
    {
        fixed (char* src = &text.GetPinnableReference())
        {
            var length = text.Length; // Span has no guarantee of null terminator.
            nuint hash1 = (5381 << 16) + 5381;
            var hash2 = hash1;
            var ptr = (nuint*)(src);

            var prime = Vector256.Create((ulong)0x100000001b3);
            var hash1Avx = Vector256.Create(0xcbf29ce484222325);
            var hash2Avx = Vector256.Create(0xcbf29ce484222325);

            while (length >= sizeof(Vector256<ulong>) / sizeof(char) * 4) // 128 byte chunks.
            {
                length -= (sizeof(Vector256<ulong>) / sizeof(char)) * 4;
                hash1Avx = Avx2.Xor(hash1Avx, Avx.LoadVector256((ulong*)ptr));
                hash1Avx = Avx2.Multiply(hash1Avx.AsUInt32(), prime.AsUInt32());

                hash2Avx = Avx2.Xor(hash2Avx, Avx.LoadVector256((ulong*)ptr + 4));
                hash2Avx = Avx2.Multiply(hash2Avx.AsUInt32(), prime.AsUInt32());

                hash1Avx = Avx2.Xor(hash1Avx, Avx.LoadVector256((ulong*)ptr + 8));
                hash1Avx = Avx2.Multiply(hash1Avx.AsUInt32(), prime.AsUInt32());

                hash2Avx = Avx2.Xor(hash2Avx, Avx.LoadVector256((ulong*)ptr + 12));
                hash2Avx = Avx2.Multiply(hash2Avx.AsUInt32(), prime.AsUInt32());
                ptr += (sizeof(Vector256<ulong>) / sizeof(nuint)) * 4;
            }

            while (length >= sizeof(Vector256<ulong>) / sizeof(char)) // 32 byte chunks.
            {
                length -= sizeof(Vector256<ulong>) / sizeof(char);
                hash1Avx = Avx2.Xor(hash1Avx, Avx.LoadVector256((ulong*)ptr));
                hash1Avx = Avx2.Multiply(hash1Avx.AsUInt32(), prime.AsUInt32());
                ptr += (sizeof(Vector256<ulong>) / sizeof(nuint));
            }

            // Flatten
            hash1Avx = Avx2.Xor(hash1Avx, hash2Avx);
            if (sizeof(nuint) == 8)
            {
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (nuint)hash1Avx[0];
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (nuint)hash1Avx[1];
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (nuint)hash1Avx[2];
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (nuint)hash1Avx[3];
            }
            else
            {
                var hash1Uint = hash1Avx.AsUInt32();
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (hash1Uint[0] * hash1Uint[1]);
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (hash1Uint[2] * hash1Uint[3]);
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (hash1Uint[3] * hash1Uint[4]);
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ (hash1Uint[5] * hash1Uint[6]);
            }

            // 4/8 byte remainders
            while (length >= (sizeof(nuint) / sizeof(char)))
            {
                length -= (sizeof(nuint) / sizeof(char));
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                ptr += 1;
            }

            return hash1 + (hash2 * 1566083941);
        }
    }
    #endif

    internal static unsafe UIntPtr UnstableHashNonVector(this ReadOnlySpan<char> text)
    {
        fixed (char* src = &text.GetPinnableReference())
        {
            var length = text.Length; // Span has no guarantee of null terminator.
            nuint hash1 = (5381 << 16) + 5381;
            var hash2 = hash1;
            var ptr = (nuint*)(src);

            // Non-vector accelerated version here.
            // 32/64 byte loop
            while (length >= (sizeof(nuint) / sizeof(char)) * 8)
            {
                length -= (sizeof(nuint) / sizeof(char)) * 8;
                hash1 = (Polyfills.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                hash2 = (Polyfills.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                hash1 = (Polyfills.RotateLeft(hash1, 5) + hash1) ^ ptr[2];
                hash2 = (Polyfills.RotateLeft(hash2, 5) + hash2) ^ ptr[3];
                hash1 = (Polyfills.RotateLeft(hash1, 5) + hash1) ^ ptr[4];
                hash2 = (Polyfills.RotateLeft(hash2, 5) + hash2) ^ ptr[5];
                hash1 = (Polyfills.RotateLeft(hash1, 5) + hash1) ^ ptr[6];
                hash2 = (Polyfills.RotateLeft(hash2, 5) + hash2) ^ ptr[7];
                ptr += 8;
            }

            // 16/32 byte
            if (length >= (sizeof(nuint) / sizeof(char)) * 4)
            {
                length -= (sizeof(nuint) / sizeof(char)) * 4;
                hash1 = (Polyfills.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                hash2 = (Polyfills.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                hash1 = (Polyfills.RotateLeft(hash1, 5) + hash1) ^ ptr[2];
                hash2 = (Polyfills.RotateLeft(hash2, 5) + hash2) ^ ptr[3];
                ptr += 4;
            }

            // 8/16 byte
            if (length >= (sizeof(nuint) / sizeof(char)) * 2)
            {
                length -= (sizeof(nuint) / sizeof(char)) * 2;
                hash1 = (Polyfills.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                hash2 = (Polyfills.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                ptr += 2;
            }

            // 4/8 byte
            if (length >= (sizeof(nuint) / sizeof(char)))
                hash1 = (Polyfills.RotateLeft(hash1, 5) + hash1) ^ ptr[0];

            return hash1 + (hash2 * 1566083941);
        }
    }
}
