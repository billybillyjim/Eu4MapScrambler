using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eu4MapScrambler
{
    class Province
    {
        public Color color;
        public HashSet<Color> adjacentColors = new HashSet<Color>();
        public List<Province> adjacentProvinces = new List<Province>();
        public List<string> adjacentProvinceIDs = new List<string>();
        public string owner = "unset";
        public string religion;
        public string culture;
        public string id;
        public int firstX;
        public int firstY;
        public bool used = false;
        public bool check = false;
        

        public Province(Color c)
        {
            color = c;
        }
        public void AddColor(Color c)
        {
            if(c == color)
            {
                return;
            }
            adjacentColors.Add(c);
        }
        public void AddProvince(Province p)
        {
            if (adjacentProvinces.Contains(p) || p.id == id)
            {
                return;
            }
            adjacentProvinces.Add(p);
        }
        public void SetOwner(Country c)
        {
            owner = c.tag;
            religion = c.religion;
            culture = c.culture;
        }
 
        public Province GetUnusedAdjacentProvince()
        {
            foreach(Province p in adjacentProvinces)
            {
                if(p.used == false)
                {
                    return p;
                }
            }
            return null;
            
        }
    }
}
