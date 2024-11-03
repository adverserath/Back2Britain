using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Back2Britain.Utility
{
    // https://github.com/Thealexbarney/LibHac/blob/master/src/LibHac/FsSystem/ValueStringBuilder.cs
    public ref struct ValueStringBuilder
    {
        private char[] _arrayToReturnToPool;
        private Span<char> _chars;
        private int _pos;

        public ValueStringBuilder(Span<char> initialBuffer)
        {
            _arrayToReturnToPool = null;
            _chars = initialBuffer;
            _pos = 0;
        }

        public ValueStringBuilder(int initialCapacity)
        {
            _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
            _chars = _arrayToReturnToPool;
            _pos = 0;
        }

        public int Length
        {
            get => _pos;
            set
            {
                Debug.Assert(value >= 0);
                Debug.Assert(value <= _chars.Length);
                _pos = value;
            }
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity > _chars.Length)
                Grow(capacity - _chars.Length);
        }

        public override string ToString()
        {
            string s = _chars.Slice(0, _pos).ToString();
            Dispose();
            return s;
        }

        public void Insert(int index, ReadOnlySpan<char> s)
        {
            int count = s.Length;

            if (_pos > (_chars.Length - count))
            {
                Grow(count);
            }

            int remaining = _pos - index;
            _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
            s.CopyTo(_chars.Slice(index));
            _pos += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(char c)
        {
            int pos = _pos;
            if ((uint)pos < (uint)_chars.Length)
            {
                _chars[pos] = c;
                _pos = pos + 1;
            }
            else
            {
                GrowAndAppend(c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(string s)
        {
            int pos = _pos;
            if (s.Length == 1 && (uint)pos < (uint)_chars.Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
            {
                _chars[pos] = s[0];
                _pos = pos + 1;
            }
            else
            {
                AppendSlow(s);
            }
        }

        private void AppendSlow(string s)
        {
            int pos = _pos;
            if (pos > _chars.Length - s.Length)
            {
                Grow(s.Length);
            }

            s.AsSpan().CopyTo(_chars.Slice(pos));
            _pos += s.Length;
        }

        public void Append(ReadOnlySpan<char> value)
        {
            int pos = _pos;
            if (pos > _chars.Length - value.Length)
            {
                Grow(value.Length);
            }

            value.CopyTo(_chars.Slice(_pos));
            _pos += value.Length;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Replace(ReadOnlySpan<char> oldChars, ReadOnlySpan<char> newChars, int startIndex, int count)
        {
            var slice = _chars.Slice(startIndex, count);

            var indexOf = slice.IndexOf(oldChars);

            if (indexOf == -1)
            {
                return;
            }

            if (newChars.Length > oldChars.Length)
            {
                int i = 0;

                for (; i < oldChars.Length; ++i)
                {
                    slice[indexOf + i] = newChars[i];
                }

                Insert(indexOf + i, newChars.Slice(i));
            }
            else if (newChars.Length < oldChars.Length)
            {
                int i = 0;

                for (; i < newChars.Length; ++i)
                {
                    slice[indexOf + i] = newChars[i];
                }

                Remove(indexOf + i, oldChars.Length - i);
            }
            else
            {
                newChars.CopyTo(slice.Slice(0, oldChars.Length));
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(int startIndex, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (length > _pos - startIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (startIndex == 0)
            {
                _chars = _chars.Slice(length);
            }
            else if (startIndex + length == _pos)
            {
                _chars = _chars.Slice(0, startIndex);
            }
            else
            {
                // Somewhere in the middle, this will be slow
                _chars.Slice(startIndex + length).CopyTo(_chars.Slice(startIndex));
            }

            _pos -= length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowAndAppend(char c)
        {
            Grow(1);
            Append(c);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow(int requiredAdditionalCapacity)
        {
            Debug.Assert(requiredAdditionalCapacity > 0);

            char[] poolArray = ArrayPool<char>.Shared.Rent(Math.Max(_pos + requiredAdditionalCapacity, _chars.Length * 2));

            _chars.CopyTo(poolArray);

            char[] toReturn = _arrayToReturnToPool;
            _chars = _arrayToReturnToPool = poolArray;
            if (toReturn != null)
            {
                ArrayPool<char>.Shared.Return(toReturn);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            char[] toReturn = _arrayToReturnToPool;
            this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
            if (toReturn != null)
            {
                ArrayPool<char>.Shared.Return(toReturn);
            }
        }
    }
}