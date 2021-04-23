using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eu4MapScrambler
{
    class Country
    {
        public string tag;
        public string name;
        public int totalOwnedProvinces;
        public int currentOwnedProvinces;
        public string culture;
        public string religion;

        public Country(string t = "")
        {
            tag = t;
        }

    }
}
