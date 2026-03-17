
using System.Collections.Generic;

namespace Silly_Things.codes.BountyContract
{
    internal class HelperBountyContract
    {
        public static List<MonsterNameBounty> MonsterValues = new List<MonsterNameBounty>();

        // _____________MONSTER VALUE_____________ \\
        public struct MonsterNameBounty
        {
            public string Name;
            public int Value;
            public int ItemCount;

            public MonsterNameBounty(string n, int v, int c)
            {
                Name = n;
                Value = v;
                ItemCount = c;
            }
        }
    }
}
