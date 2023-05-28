﻿using System.Collections.Generic;

namespace ResignBSP
{
    internal static class ByteExtensions
    {
        private static readonly int[] Empty = new int[0];

        public static int[] Locate(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
            {
                return Empty;
            }

            List<int>? list = new();

            for (int i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                {
                    continue;
                }

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }

        private static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
            {
                return false;
            }

            for (int i = 0; i < candidate.Length; i++)
            {
                if (array[position + i] != candidate[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
        }

    }
}
