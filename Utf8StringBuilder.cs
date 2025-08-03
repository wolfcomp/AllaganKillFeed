using System.Runtime.CompilerServices;

namespace AllaganKillFeed;

public class Utf8StringBuilder
{
    internal byte[] chunkChars;
    internal int capacity;
    internal int chunkLength;
    internal int maxCapacity;
    internal const int DefaultCapacity = 256;

    public Utf8StringBuilder(int capacity = DefaultCapacity) { }

    public Utf8StringBuilder(string? value) : this(value, DefaultCapacity) { }

    public Utf8StringBuilder(string? value, int capacity = DefaultCapacity) : this(value, 0, value?.Length ?? 0, capacity) { }

    public Utf8StringBuilder(string? value, int startIndex, int length, int capacity = DefaultCapacity) : this(Encoding.UTF8.GetBytes(value ?? string.Empty, startIndex, length).AsSpan(), capacity) { }

    public Utf8StringBuilder(ReadOnlySpan<byte> value, int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        maxCapacity = int.MaxValue;
        if (capacity == 0)
        {
            capacity = DefaultCapacity;
        }
        capacity = Math.Max(capacity, value.Length);

        chunkChars = new byte[capacity];
        chunkLength = value.Length;

        value.CopyTo(chunkChars);
    }

    public override string ToString() => Encoding.UTF8.GetString(chunkChars, 0, chunkLength);

    public int Length => chunkLength;

    public void Clear() => chunkLength = 0;

    public void Append(string value) => Append(value, 0, value.Length);

    public void Append(string value, int startIndex, int length) => AppendInternal(Encoding.UTF8.GetBytes(value, startIndex, length).AsSpan());

    public unsafe void Append(ReadOnlySpan<byte> value) => AppendInternal(value.IndexOf(byte.MinValue) < 0 ? value : value[..value.IndexOf(byte.MinValue)]);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [SkipLocalsInit]
    private void AppendInternal(ReadOnlySpan<byte> value)
    {
        if (chunkLength + value.Length > capacity)
        {
            capacity = Math.Max(capacity * 2, chunkLength + value.Length);
            var newChunkChars = new byte[capacity];
            chunkChars.AsSpan().CopyTo(newChunkChars);
            chunkChars = newChunkChars;
        }

        value.CopyTo(chunkChars.AsSpan(chunkLength));
        chunkLength += value.Length;
    }

    public ReadOnlySpan<byte> AsSpan() => chunkChars.AsSpan(0, chunkLength);

    public byte[] ToArray() => AsSpan().ToArray();
}