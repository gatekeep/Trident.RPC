/**
 * Copyright (c) 2008-2023 Bryan Biedenkapp., All Rights Reserved.
 * MIT Open Source. Use is subject to license terms.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 */
/*
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including 
 * without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject 
 * to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN 
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
/*
 * Based on code from Lidgren Network v3
 * Copyright (C) 2010 Michael Lidgren., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;

using TridentFramework.RPC.Net.Message;

namespace TridentFramework.RPC.Net
{
    /// <summary>
    /// Utility methods
    /// </summary>
    public static class NetUtility
    {
        /**
         * Constants
         */
        public const int NumTotalChannels = 99;

        public const int ChannelsPerDeliveryMethod = 32;

        public const int NumSequenceNumbers = 1024;

        public const int HeaderByteSize = 5;

        public const int UnreliableWindowSize = 128;
        public const int ReliableOrderedWindowSize = 64;
        public const int ReliableSequencedWindowSize = 64;
        public const int DefaultWindowSize = 64;

        public const int MaxFragmentationGroups = ushort.MaxValue - 1;

        public const int UnfragmentedMessageHeaderSize = 5;

        /// <summary>
        /// Number of channels which needs a sequence number to work
        /// </summary>
        public const int NumSequencedChannels = ((int)MessageType.UserReliableOrdered1 + ChannelsPerDeliveryMethod) - (int)MessageType.UserSequenced1;

        /// <summary>
        /// Number of reliable channels
        /// </summary>
        public const int NumReliableChannels = ((int)MessageType.UserReliableOrdered1 + ChannelsPerDeliveryMethod) - (int)MessageType.UserReliableUnordered;

        public const string ConnResetMessage = "Connection was reset by remote host";

        // The +1 ensures NextDouble doesn't generate 1.0
        public const double REAL_UNIT_INT = 1.0 / ((double)int.MaxValue + 1.0);

        public const double REAL_UNIT_UINT = 1.0 / ((double)uint.MaxValue + 1.0);
        public const uint Y = 842502087, Z = 3579807591, W = 273326509;

        private static int gExtraSeed = 42;
        private static uint x, y, z, w;
        private static readonly SHA256 sha = SHA256.Create();
        private static IPAddress broadcastAddress;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes static members of the NetUtility class.
        /// </summary>
        static NetUtility()
        {
            Reinitialise(GetSeed(REAL_UNIT_INT));
        }

        /// <summary>
        /// Create a semi-random seed based on an object
        /// </summary>
        /// <returns></returns>
        public static int GetSeed(object forObject)
        {
            // mix some semi-random properties
            int seed = (int)Environment.TickCount;
            seed ^= forObject.GetHashCode();

            int extraSeed = System.Threading.Interlocked.Increment(ref gExtraSeed);

            return seed + extraSeed;
        }

        /// <summary>
        /// Reinitializes using an int value as a seed.
        /// </summary>
        /// <param name="seed"></param>
        public static void Reinitialise(int seed)
        {
            // The only stipulation stated for the xorshift RNG is that at least one of
            // the seeds x,y,z,w is non-zero. We fulfill that requirement by only allowing
            // resetting of the x seed
            x = (uint)seed;
            y = Y;
            z = Z;
            w = W;
        }

        /// <summary>
        /// Generates a random double. Values returned are from 0.0 up to but not including 1.0.
        /// </summary>
        /// <returns></returns>
        public static double NextDouble()
        {
            uint t = x ^ (x << 11);
            x = y;
            y = z;
            z = w;

            // Here we can gain a 2x speed improvement by generating a value that can be cast to
            // an int instead of the more easily available uint. If we then explicitly cast to an
            // int the compiler will then cast the int to a double to perform the multiplication,
            // this final cast is a lot faster than casting from a uint to a double. The extra cast
            // to an int is very fast (the allocated bits remain the same) and so the overall effect
            // of the extra cast is a significant performance improvement.
            //
            // Also note that the loss of one bit of precision is equivalent to what occurs within
            // System.Random.
            return REAL_UNIT_INT * (int)(0x7FFFFFFF & (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8))));
        }

        /// <summary>
        /// Generates a random single. Values returned are from 0.0 up to but not including 1.0.
        /// </summary>
        /// <returns></returns>
        public static float NextSingle()
        {
            return (float)NextDouble();
        }

        /// <summary>
        /// Fills the provided byte array with random bytes.
        /// This method is functionally equivalent to System.Random.NextBytes().
        /// </summary>
        /// <param name="buffer"></param>
        public static void NextBytes(byte[] buffer)
        {
            // Fill up the bulk of the buffer in chunks of 4 bytes at a time.
            uint x1 = x, y1 = y, z1 = z, w1 = w;
            int i = 0;
            uint t;
            for (int bound = buffer.Length - 3; i < bound;)
            {
                // Generate 4 bytes.
                // Increased performance is achieved by generating 4 random bytes per loop.
                // Also note that no mask needs to be applied to zero out the higher order bytes before
                // casting because the cast ignores thos bytes. Thanks to Stefan Troschütz for pointing this out.
                t = x1 ^ (x1 << 11);
                x1 = y;
                y1 = z;
                z1 = w;
                w1 = (w1 ^ (w1 >> 19)) ^ (t ^ (t >> 8));

                buffer[i++] = (byte)w1;
                buffer[i++] = (byte)(w1 >> 8);
                buffer[i++] = (byte)(w1 >> 16);
                buffer[i++] = (byte)(w1 >> 24);
            }

            // Fill up any remaining bytes in the buffer.
            if (i < buffer.Length)
            {
                // Generate 4 bytes.
                t = x1 ^ (x1 << 11);
                x1 = y;
                y1 = z;
                z1 = w;
                w = (w1 ^ (w1 >> 19)) ^ (t ^ (t >> 8));

                buffer[i++] = (byte)w1;
                if (i < buffer.Length)
                {
                    buffer[i++] = (byte)(w1 >> 8);
                    if (i < buffer.Length)
                    {
                        buffer[i++] = (byte)(w1 >> 16);
                        if (i < buffer.Length)
                        {
                            buffer[i] = (byte)(w1 >> 24);
                        }
                    }
                }
            }

            x = x1;
            y = y1;
            z = z1;
            w = w1;
        }

        /// <summary>
        /// Get IPv4 endpoint from notation (xxx.xxx.xxx.xxx) or hostname and port number
        /// </summary>
        /// <returns></returns>
        public static IPEndPoint Resolve(string ipOrHost, int port)
        {
            IPAddress adr = Resolve(ipOrHost);
            return new IPEndPoint(adr, port);
        }

        /// <summary>
        /// Get IPv4 address from notation (xxx.xxx.xxx.xxx) or hostname
        /// </summary>
        /// <returns></returns>
        public static IPAddress Resolve(string ipOrHost)
        {
            if (string.IsNullOrEmpty(ipOrHost))
                throw new ArgumentException("Supplied string must not be empty", "ipOrHost");

            ipOrHost = ipOrHost.Trim();

            IPAddress ipAddress = null;
            if (IPAddress.TryParse(ipOrHost, out ipAddress))
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                    return ipAddress;
                throw new ArgumentException("This method will not currently resolve other than ipv4 addresses");
            }

            // ok must be a host name
            IPHostEntry entry;
            try
            {
                entry = Dns.GetHostEntry(ipOrHost);
                if (entry == null)
                    return null;

                // check each entry for a valid IP address
                foreach (IPAddress ipCurrent in entry.AddressList)
                {
                    if (ipCurrent.AddressFamily == AddressFamily.InterNetwork)
                        return ipCurrent;
                }

                return null;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.HostNotFound)
                    return null;
                else
                    throw;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetCachedBroadcastAddress()
        {
            if (broadcastAddress == null)
                broadcastAddress = GetBroadcastAddress();
            return broadcastAddress;
        }

        /// <summary>
        /// Get the local machines network interface.
        /// </summary>
        /// <returns>Network Interface</returns>
        private static NetworkInterface GetNetworkInterface()
        {
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            if (computerProperties == null)
                return null;

            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            if (nics == null || nics.Length < 1)
                return null;

            NetworkInterface best = null;
            foreach (NetworkInterface adapter in nics)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback || adapter.NetworkInterfaceType == NetworkInterfaceType.Unknown)
                    continue;
                if (!adapter.Supports(NetworkInterfaceComponent.IPv4))
                    continue;
                if (best == null)
                    best = adapter;
                if (adapter.OperationalStatus != OperationalStatus.Up)
                    continue;

                // A computer could have several adapters (more than one network card)
                // here but just return the first one for now...
                return adapter;
            }
            return best;
        }

        /// <summary>
        /// Returns the physical (MAC) address for the first usable network interface
        /// </summary>
        /// <returns></returns>
        public static PhysicalAddress GetMACAddress()
        {
            NetworkInterface ni = GetNetworkInterface();
            if (ni == null)
                return null;
            return ni.GetPhysicalAddress();
        }

        /// <summary>
        /// Create a hex string from an Int64 value
        /// </summary>
        /// <returns></returns>
        public static string ToHexString(long data)
        {
            return ToHexString(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Create a hex string from an array of bytes
        /// </summary>
        /// <returns></returns>
        public static string ToHexString(byte[] data)
        {
            char[] c = new char[data.Length * 2];
            byte b;
            for (int i = 0; i < data.Length; ++i)
            {
                b = (byte)(data[i] >> 4);
                c[i * 2] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = (byte)(data[i] & 0xF);
                c[i * 2 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
            return new string(c);
        }

        /// <summary>
        /// Gets my broadcast IP address (not necessarily external) and subnet mask
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetBroadcastAddress()
        {
            var ni = GetNetworkInterface();
            if (ni == null)
                return null;

            var properties = ni.GetIPProperties();
            foreach (UnicastIPAddressInformation unicastAddress in properties.UnicastAddresses)
            {
                if (unicastAddress != null && unicastAddress.Address != null && unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    var mask = unicastAddress.IPv4Mask;
                    byte[] ipAdressBytes = unicastAddress.Address.GetAddressBytes();
                    byte[] subnetMaskBytes = mask.GetAddressBytes();

                    if (ipAdressBytes.Length != subnetMaskBytes.Length)
                        throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

                    byte[] broadcastAddress = new byte[ipAdressBytes.Length];
                    for (int i = 0; i < broadcastAddress.Length; i++)
                    {
                        broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
                    }
                    return new IPAddress(broadcastAddress);
                }
            }
            return IPAddress.Broadcast;
        }

        /// <summary>
        /// Gets my local IP address (not necessarily external) and subnet mask
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetMyAddress(out IPAddress mask)
        {
            NetworkInterface ni = GetNetworkInterface();
            if (ni == null)
            {
                mask = null;
                return null;
            }

            IPInterfaceProperties properties = ni.GetIPProperties();
            foreach (UnicastIPAddressInformation unicastAddress in properties.UnicastAddresses)
            {
                if (unicastAddress != null && unicastAddress.Address != null && unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    mask = unicastAddress.IPv4Mask;
                    return unicastAddress.Address;
                }
            }

            mask = null;
            return null;
        }

        /// <summary>
        /// Returns true if the IPEndPoint supplied is on the same subnet as this host
        /// </summary>
        /// <returns></returns>
        public static bool IsLocal(IPEndPoint endpoint)
        {
            if (endpoint == null)
                return false;
            return IsLocal(endpoint.Address);
        }

        /// <summary>
        /// Returns true if the IPAddress supplied is on the same subnet as this host
        /// </summary>
        /// <returns></returns>
        public static bool IsLocal(IPAddress remote)
        {
            IPAddress mask;
            IPAddress local = GetMyAddress(out mask);

            if (mask == null)
                return false;

            uint maskBits = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
            uint remoteBits = BitConverter.ToUInt32(remote.GetAddressBytes(), 0);
            uint localBits = BitConverter.ToUInt32(local.GetAddressBytes(), 0);

            // compare network portions
            return (remoteBits & maskBits) == (localBits & maskBits);
        }

        /// <summary>
        /// Returns how many bits are necessary to hold a certain number
        /// </summary>
        /// <returns></returns>
        public static int BitsToHoldUInt(uint value)
        {
            int bits = 1;
            while ((value >>= 1) != 0)
                bits++;
            return bits;
        }

        /// <summary>
        /// Returns how many bits are necessary to hold a certain number
        /// </summary>
        public static int BitsToHoldUInt64(ulong value)
        {
            int bits = 1;
            while ((value >>= 1) != 0)
                bits++;
            return bits;
        }

        /// <summary>
        /// Returns how many bytes are required to hold a certain number of bits
        /// </summary>
        /// <returns></returns>
        public static int BytesToHoldBits(int numBits)
        {
            return (numBits + 7) / 8;
        }

        /// <summary>
        /// Swap the byte order of a unsigned integer.
        /// </summary>
        /// <param name="value">Value to swap</param>
        /// <returns>Swapped value</returns>
        internal static uint SwapByteOrder(uint value)
        {
            return
                ((value & 0xff000000) >> 24) |
                ((value & 0x00ff0000) >> 8) |
                ((value & 0x0000ff00) << 8) |
                ((value & 0x000000ff) << 24);
        }

        /// <summary>
        /// Swap the byte order of an unsigned 64-bit integer.
        /// </summary>
        /// <param name="value">Value to swap</param>
        /// <returns>Swapped value</returns>
        internal static ulong SwapByteOrder(ulong value)
        {
            return
                ((value & 0xff00000000000000L) >> 56) |
                ((value & 0x00ff000000000000L) >> 40) |
                ((value & 0x0000ff0000000000L) >> 24) |
                ((value & 0x000000ff00000000L) >> 8) |
                ((value & 0x00000000ff000000L) << 8) |
                ((value & 0x0000000000ff0000L) << 24) |
                ((value & 0x000000000000ff00L) << 40) |
                ((value & 0x00000000000000ffL) << 56);
        }

        /// <summary>
        /// Compare the elements in two raw byte arrays.
        /// </summary>
        /// <param name="one">First array</param>
        /// <param name="two">Second array</param>
        /// <returns>True if arrays are the same, otherwise false</returns>
        internal static bool CompareElements(byte[] one, byte[] two)
        {
            if (one.Length != two.Length)
                return false;

            for (int i = 0; i < one.Length; i++)
                if (one[i] != two[i])
                    return false;
            return true;
        }

        /// <summary>
        /// Convert a hexadecimal string to a byte array
        /// </summary>
        /// <returns></returns>
        public static byte[] ToByteArray(string hexString)
        {
            byte[] retval = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length; i += 2)
                retval[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            return retval;
        }

        /// <summary>
        /// Converts a number of bytes to a shorter, more readable string representation
        /// </summary>
        /// <returns></returns>
        public static string ToHumanReadable(long bytes)
        {
            if (bytes < 4000) // 1-4 kb is printed in bytes
                return bytes + " bytes";
            if (bytes < 1000 * 1000) // 4-999 kb is printed in kb
                return Math.Round(((double)bytes / 1000.0), 2) + " kilobytes";
            return Math.Round(((double)bytes / (1000.0 * 1000.0)), 2) + " megabytes"; // else megabytes
        }

        internal static int RelativeSequenceNumber(int nr, int expected)
        {
            int retval = ((nr + NumSequenceNumbers) - expected) % NumSequenceNumbers;
            if (retval > (NumSequenceNumbers / 2))
                retval -= NumSequenceNumbers;
            return retval;
        }

        /// <summary>
        /// Gets the window size used internally in the library for a certain delivery method
        /// </summary>
        /// <returns></returns>
        public static int GetWindowSize(DeliveryMethod method)
        {
            switch (method)
            {
                case DeliveryMethod.Unknown:
                    return 0;

                case DeliveryMethod.Unreliable:
                case DeliveryMethod.UnreliableSequenced:
                    return UnreliableWindowSize;

                case DeliveryMethod.ReliableOrdered:
                    return ReliableOrderedWindowSize;

                case DeliveryMethod.ReliableSequenced:
                case DeliveryMethod.ReliableUnordered:
                default:
                    return DefaultWindowSize;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="val"></param>
        /// <param name="fill"></param>
        /// <returns></returns>
        public static Guid LongToGuid(long val, bool fill = false)
        {
            byte[] guidBytes = new byte[16];
            byte[] longBytes = BitConverter.GetBytes(val);
            if (fill)
                Buffer.BlockCopy(longBytes, 0, guidBytes, 0, longBytes.Length);
            Buffer.BlockCopy(longBytes, 0, guidBytes, longBytes.Length, longBytes.Length);
            return new Guid(guidBytes);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static long GuidToLong(Guid val)
        {
            byte[] guidBytes = val.ToByteArray();
            return BitConverter.ToInt64(guidBytes, 8);
        }

        /// <summary>
        /// Helper to compute the SHA256 if the given bytes.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] ComputeSHAHash(byte[] bytes)
        {
            return sha.ComputeHash(bytes, 0, bytes.Length);
        }

        /**
         * deep magic follows...Never meddle in the affairs of wizards, for you are crunchy and
         * taste good with ketchup.
         */

        // shell sort
        internal static void SortMembersList(System.Reflection.MemberInfo[] list)
        {
            int h;
            int j;
            System.Reflection.MemberInfo tmp;

            h = 1;
            while (h * 3 + 1 <= list.Length)
                h = 3 * h + 1;

            while (h > 0)
            {
                for (int i = h - 1; i < list.Length; i++)
                {
                    tmp = list[i];
                    j = i;
                    while (true)
                    {
                        if (j >= h)
                        {
                            if (string.Compare(list[j - h].Name, tmp.Name, StringComparison.InvariantCulture) > 0)
                            {
                                list[j] = list[j - h];
                                j -= h;
                            }
                            else
                                break;
                        }
                        else
                            break;
                    }

                    list[j] = tmp;
                }
                h /= 3;
            }
        }

        internal static DeliveryMethod GetDeliveryMethod(MessageType mtp)
        {
            if (mtp >= MessageType.UserReliableOrdered1)
                return DeliveryMethod.ReliableOrdered;
            else if (mtp >= MessageType.UserReliableSequenced1)
                return DeliveryMethod.ReliableSequenced;
            else if (mtp >= MessageType.UserReliableUnordered)
                return DeliveryMethod.ReliableUnordered;
            else if (mtp >= MessageType.UserSequenced1)
                return DeliveryMethod.UnreliableSequenced;
            return DeliveryMethod.Unreliable;
        }
    } // public static class NetUtility
} // namespace TridentFramework.RPC.Net
