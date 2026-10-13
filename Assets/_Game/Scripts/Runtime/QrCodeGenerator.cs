using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CampusPulse
{
    /// <summary>
    /// Self-contained, pure C# QR Code generator for standard Version 1–6 byte-mode QR codes.
    /// Operates offline without external packages or internet dependencies.
    /// </summary>
    public static class QrCodeGenerator
    {
        // Reed-Solomon GF(256) tables
        private static readonly byte[] ExpTable = new byte[256];
        private static readonly byte[] LogTable = new byte[256];

        static QrCodeGenerator()
        {
            int val = 1;
            for (int i = 0; i < 255; i++)
            {
                ExpTable[i] = (byte)val;
                LogTable[val] = (byte)i;
                val <<= 1;
                if ((val & 0x100) != 0) val ^= 0x11D; // Primitive polynomial x^8 + x^4 + x^3 + x^2 + 1
            }
            ExpTable[255] = ExpTable[0];
        }

        private static byte GfMultiply(byte a, byte b)
        {
            if (a == 0 || b == 0) return 0;
            return ExpTable[(LogTable[a] + LogTable[b]) % 255];
        }

        private static byte[] CalcReedSolomonEcc(byte[] data, int eccCount)
        {
            // Compute generator polynomial of degree eccCount
            byte[] gen = new byte[] { 1 };
            for (int i = 0; i < eccCount; i++)
            {
                byte root = ExpTable[i];
                byte[] next = new byte[gen.Length + 1];
                for (int j = 0; j < gen.Length; j++)
                {
                    next[j] ^= GfMultiply(gen[j], root);
                    next[j + 1] ^= gen[j];
                }
                gen = next;
            }

            // Polynomial division
            byte[] rem = new byte[eccCount];
            for (int i = 0; i < data.Length; i++)
            {
                byte factor = (byte)(data[i] ^ rem[0]);
                for (int j = 0; j < eccCount - 1; j++)
                {
                    rem[j] = (byte)(rem[j + 1] ^ GfMultiply(gen[eccCount - 1 - j], factor));
                }
                rem[eccCount - 1] = GfMultiply(gen[0], factor);
            }
            return rem;
        }

        // Version specs: (Version, TotalCodewords, EccCodewords, RemainderBits, AlignmentPositions)
        private struct QrSpec
        {
            public int Version;
            public int TotalCodewords;
            public int EccCodewords;
            public int RemainderBits;
            public int[] AlignmentPattern;
            public int DataCapacity => TotalCodewords - EccCodewords;
            public int Size => 17 + 4 * Version;
        }

        private static readonly QrSpec[] Specs = new QrSpec[]
        {
            new QrSpec { Version = 1, TotalCodewords = 26, EccCodewords = 7, RemainderBits = 0, AlignmentPattern = new int[0] },
            new QrSpec { Version = 2, TotalCodewords = 44, EccCodewords = 10, RemainderBits = 7, AlignmentPattern = new int[] { 6, 18 } },
            new QrSpec { Version = 3, TotalCodewords = 70, EccCodewords = 15, RemainderBits = 7, AlignmentPattern = new int[] { 6, 22 } },
            new QrSpec { Version = 4, TotalCodewords = 100, EccCodewords = 20, RemainderBits = 7, AlignmentPattern = new int[] { 6, 26 } },
            new QrSpec { Version = 5, TotalCodewords = 134, EccCodewords = 26, RemainderBits = 7, AlignmentPattern = new int[] { 6, 30 } },
            new QrSpec { Version = 6, TotalCodewords = 172, EccCodewords = 36, RemainderBits = 7, AlignmentPattern = new int[] { 6, 34 } }
        };

        public static bool[,] GenerateMatrix(string text)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text);

            // Find smallest fitting version
            QrSpec spec = Specs[0];
            bool found = false;
            foreach (var s in Specs)
            {
                // Byte mode header: 4 bits mode + 8 bits length = 12 bits (1.5 bytes)
                int overhead = s.Version >= 10 ? 3 : 2;
                if (textBytes.Length + overhead <= s.DataCapacity)
                {
                    spec = s;
                    found = true;
                    break;
                }
            }
            if (!found) spec = Specs[Specs.Length - 1]; // Use largest available

            // 1. Bitstream encoding: 0100 (Byte mode) + length (8 bits) + data bytes
            var bitBuffer = new List<bool>();
            AppendBits(bitBuffer, 0b0100, 4); // Byte mode
            AppendBits(bitBuffer, textBytes.Length, 8); // Character count
            foreach (byte b in textBytes) AppendBits(bitBuffer, b, 8);

            // Terminator (up to 4 zeroes)
            int capacityBits = spec.DataCapacity * 8;
            int termLen = Math.Min(4, capacityBits - bitBuffer.Count);
            AppendBits(bitBuffer, 0, termLen);

            // Pad to byte boundary
            while ((bitBuffer.Count % 8) != 0) bitBuffer.Add(false);

            // Pad bytes (0xEC, 0x11)
            byte[] padBytes = { 0xEC, 0x11 };
            int padIdx = 0;
            while (bitBuffer.Count < capacityBits)
            {
                AppendBits(bitBuffer, padBytes[padIdx % 2], 8);
                padIdx++;
            }

            // Convert to byte array
            byte[] dataBytes = new byte[spec.DataCapacity];
            for (int i = 0; i < dataBytes.Length; i++)
            {
                byte val = 0;
                for (int b = 0; b < 8; b++)
                {
                    if (bitBuffer[i * 8 + b]) val |= (byte)(1 << (7 - b));
                }
                dataBytes[i] = val;
            }

            // 2. Compute Reed-Solomon ECC
            byte[] eccBytes = CalcReedSolomonEcc(dataBytes, spec.EccCodewords);

            // Final codeword sequence
            byte[] allCodewords = new byte[spec.TotalCodewords];
            Array.Copy(dataBytes, 0, allCodewords, 0, dataBytes.Length);
            Array.Copy(eccBytes, 0, allCodewords, dataBytes.Length, eccBytes.Length);

            var finalBits = new List<bool>();
            foreach (byte c in allCodewords) AppendBits(finalBits, c, 8);
            for (int r = 0; r < spec.RemainderBits; r++) finalBits.Add(false);

            // 3. Grid setup
            int size = spec.Size;
            bool[,] grid = new bool[size, size];
            bool[,] isFunction = new bool[size, size];

            // Finder patterns
            AddFinderPattern(grid, isFunction, 0, 0);
            AddFinderPattern(grid, isFunction, size - 7, 0);
            AddFinderPattern(grid, isFunction, 0, size - 7);

            // Separators
            for (int i = 0; i < 8; i++)
            {
                MarkFunc(isFunction, 7, i); MarkFunc(isFunction, i, 7);
                MarkFunc(isFunction, size - 8, i); MarkFunc(isFunction, size - 1 - i, 7);
                MarkFunc(isFunction, 7, size - 1 - i); MarkFunc(isFunction, i, size - 8);
            }

            // Timing patterns
            for (int i = 8; i < size - 8; i++)
            {
                grid[6, i] = (i % 2) == 0;
                isFunction[6, i] = true;
                grid[i, 6] = (i % 2) == 0;
                isFunction[i, 6] = true;
            }

            // Dark module
            grid[4 * spec.Version + 9, 8] = true;
            isFunction[4 * spec.Version + 9, 8] = true;

            // Alignment patterns
            if (spec.AlignmentPattern.Length > 0)
            {
                foreach (int r in spec.AlignmentPattern)
                {
                    foreach (int c in spec.AlignmentPattern)
                    {
                        if ((r < 9 && c < 9) || (r < 9 && c > size - 9) || (r > size - 9 && c < 9)) continue;
                        AddAlignmentPattern(grid, isFunction, r - 2, c - 2);
                    }
                }
            }

            // Reserve format bits
            for (int i = 0; i < 9; i++) { isFunction[8, i] = true; isFunction[i, 8] = true; }
            for (int i = size - 8; i < size; i++) { isFunction[8, i] = true; isFunction[i, 8] = true; }

            // 4. Place Data Bits (Zigzag right to left)
            int bitIdx = 0;
            int right = size - 1;
            while (right > 0)
            {
                if (right == 6) right--; // Skip vertical timing column
                for (int vert = 0; vert < size; vert++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int x = right - j;
                        bool upward = ((right + 1) & 2) == 0;
                        int y = upward ? size - 1 - vert : vert;

                        if (!isFunction[y, x])
                        {
                            bool bit = bitIdx < finalBits.Count && finalBits[bitIdx++];
                            // Apply Mask pattern 0: (x + y) % 2 == 0
                            if ((x + y) % 2 == 0) bit = !bit;
                            grid[y, x] = bit;
                        }
                    }
                }
                right -= 2;
            }

            // 5. Add Format Information (ECC L = 01, Mask 0 = 000 -> 01000 -> Format bits 0x77C4 with mask 0x5412)
            // Pre-computed format code for ECC Level L and Mask 0: 0x77C4 ^ 0x5412 = 0x23D6 = 0010 0011 1101 0110
            int formatBits = 0b0010001111010110;
            ApplyFormatBits(grid, size, formatBits);

            return grid;
        }

        public static Texture2D GenerateTexture(string text, int scale = 8, int quietZone = 4)
        {
            bool[,] matrix = GenerateMatrix(text);
            int qrSize = matrix.GetLength(0);
            int totalSize = (qrSize + quietZone * 2) * scale;

            Texture2D tex = new Texture2D(totalSize, totalSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color32 black = new Color32(10, 15, 25, 255);
            Color32 white = new Color32(245, 250, 255, 255);
            Color32[] pixels = new Color32[totalSize * totalSize];

            // Fill white background
            for (int i = 0; i < pixels.Length; i++) pixels[i] = white;

            // Draw QR modules
            for (int r = 0; r < qrSize; r++)
            {
                for (int c = 0; c < qrSize; c++)
                {
                    if (matrix[r, c])
                    {
                        int px = (c + quietZone) * scale;
                        int py = (qrSize - 1 - r + quietZone) * scale; // Invert Y for Unity texture coords

                        for (int dy = 0; dy < scale; dy++)
                        {
                            for (int dx = 0; dx < scale; dx++)
                            {
                                pixels[(py + dy) * totalSize + (px + dx)] = black;
                            }
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static void AddFinderPattern(bool[,] grid, bool[,] isFunc, int startX, int startY)
        {
            for (int y = 0; y < 7; y++)
            {
                for (int x = 0; x < 7; x++)
                {
                    bool isBlack = (y == 0 || y == 6 || x == 0 || x == 6 || (y >= 2 && y <= 4 && x >= 2 && x <= 4));
                    grid[startY + y, startX + x] = isBlack;
                    isFunc[startY + y, startX + x] = true;
                }
            }
        }

        private static void AddAlignmentPattern(bool[,] grid, bool[,] isFunc, int startY, int startX)
        {
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    bool isBlack = (y == 0 || y == 4 || x == 0 || x == 4 || (y == 2 && x == 2));
                    grid[startY + y, startX + x] = isBlack;
                    isFunc[startY + y, startX + x] = true;
                }
            }
        }

        private static void ApplyFormatBits(bool[,] grid, int size, int formatBits)
        {
            // Format bits around top-left
            int[] row1 = { 8, 8, 8, 8, 8, 8, 8, 8, 7, 5, 4, 3, 2, 1, 0 };
            int[] col1 = { 0, 1, 2, 3, 4, 5, 7, 8, 8, 8, 8, 8, 8, 8, 8 };

            // Format bits split between top-right and bottom-left
            int[] row2 = { size - 1, size - 2, size - 3, size - 4, size - 5, size - 6, size - 7, 8, 8, 8, 8, 8, 8, 8, 8 };
            int[] col2 = { 8, 8, 8, 8, 8, 8, 8, size - 8, size - 7, size - 6, size - 5, size - 4, size - 3, size - 2, size - 1 };

            for (int i = 0; i < 15; i++)
            {
                bool bit = ((formatBits >> (14 - i)) & 1) != 0;
                grid[row1[i], col1[i]] = bit;
                grid[row2[i], col2[i]] = bit;
            }
        }

        private static void MarkFunc(bool[,] isFunc, int r, int c)
        {
            if (r >= 0 && r < isFunc.GetLength(0) && c >= 0 && c < isFunc.GetLength(1))
                isFunc[r, c] = true;
        }

        private static void AppendBits(List<bool> list, int value, int count)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                list.Add(((value >> i) & 1) != 0);
            }
        }
    }
}
