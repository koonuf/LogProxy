using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace LogProxy.Lib
{
    public static class Utils
    {
        public static void EnsureArraySize(ref byte[] array, int minSize, int contentSize)
        {
            if (array.Length < minSize)
            {
                int newSize = Math.Max(minSize, array.Length << 1);
                byte[] temp = new byte[newSize];
                if (contentSize > 0)
                {
                    Buffer.BlockCopy(array, 0, temp, 0, contentSize);
                }

                array = temp;
            }
        }

        public static byte[] CopyArray(byte[] source, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(source, 0, result, 0, length);
            return result;
        }

        public static byte[] CopyArray(byte[] source, int sourceIndex, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(source, sourceIndex, result, 0, length);
            return result;
        }

        public static bool ArraysEqual(byte[] first, int firstStart, byte[] second, int secondStart, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (first[firstStart + i] != second[secondStart + i])
                {
                    return false;
                }
            }

            return true;
        }

        public static string[] SplitNoEmpty(this string value, string separator, int? max = null)
        {
            if (value == null)
            {
                return null;
            }

            if (max.HasValue)
            {
                return value.Split(new[] { separator }, max.Value, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                return value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public static int ToInt(this string value, string errorMessage)
        { 
            int result;

            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out result))
            {
                throw new InvalidOperationException(errorMessage);
            }

            return result;
        }

        public static TValue SafeGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, string errorMessage)
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                throw new InvalidOperationException(errorMessage);
            }

            return value;
        }

        public static TValue GetValueWithDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                return defaultValue;
            }

            return value;
        }

        public static TValue SafeGetValue<TKey, TValue>(this ILookup<TKey, TValue> lookup, TKey key, string errorMessage) where TValue : class
        {
            TValue value = lookup[key].FirstOrDefault();
            if (value == null)
            {
                throw new InvalidOperationException(errorMessage);
            }

            return value;
        }

        public static TValue GetValueWithDefault<TKey, TValue>(this ILookup<TKey, TValue> lookup, TKey key, TValue defaultValue = null) where TValue : class
        {
            TValue value = lookup[key].FirstOrDefault();
            if (value == null)
            {
                return defaultValue;
            }

            return value;
        }

        public static byte[] GetOffsetContent(byte[] data, int offsetLength)
        {
            if (offsetLength > 0)
            {
                return Utils.CopyArray(data, data.Length - offsetLength, offsetLength);
            }

            return null;
        }

        public static void DisposeSocket(Socket socket)
        {
            if (socket != null)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Dispose();
                }
                catch (SocketException)
                {
                }
                catch (IOException)
                { 
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        public static void CloseSocket(Socket socket)
        {
            if (socket != null)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException)
                {
                }
                catch (IOException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
    }
}
