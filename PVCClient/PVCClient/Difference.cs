using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVCClient
{
    static class Difference
    {
        static byte[] SimpleDiff(byte[] left, byte[] right)
        {

            byte[] diff = new byte[right.Length];
            for (int i = 0; i < diff.Length; i++)
            {
                diff[i] = (byte)((right[i] - (i >= left.Length ? 0 : left[i]) + 255) % 255);
            }
            return diff;
        }

        static byte[] MergeSimpleDiffs(byte[] left, byte[] diffs)
        {
            byte[] right = new byte[diffs.Length];
            for(int i = 0; i < diffs.Length; i++)
            {
                right[i] = left.Length < i ? (byte)((diffs[i] + left[i]) % 255) : diffs[i];
            }
            return right;
        }
    }
}
