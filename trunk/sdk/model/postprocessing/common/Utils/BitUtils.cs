using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
    class BitUtils
    {
        static int[] _bitcounts;

        static BitUtils()
        {
            _bitcounts = new int[65536];
            int position1 = -1;
            int position2 = -1;
            
            for (int i = 1; i < 65536; i++, position1++)
            {
                if (position1 == position2)
                {
                    position1 = 0;
                    position2 = i;
                }
                _bitcounts[i] = _bitcounts[position1] + 1;
            }
        }

        public static int GetBitCount(int value)
        {
            var ret = _bitcounts[value & 65535] + _bitcounts[(value >> 16) & 65535];
            return ret;
        }
    }
}
