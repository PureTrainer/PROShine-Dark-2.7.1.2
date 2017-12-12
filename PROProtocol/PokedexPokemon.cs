using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PROProtocol
{
    public class PokedexPokemon
    {
        // 1: Seen | 2 : Captured | 3 : Obtained by evolving
        public int id { get;  set; }
        public string name { get;  set; }
        public int pokeid2 { get; set; }        

        public List<string> Area = new List<string>();

        public string Type1 = string.Empty;

        public string Type2 = string.Empty;

        public string Abilities { get; set; }
        internal PokedexPokemon(int id, int pokeid)
        {
            this.id = id;
            name = PokemonNamesManager.Instance.Names[pokeid];
            pokeid2 = pokeid;
        }
        public override string ToString()
        {
            return name;
        }

        public bool isCaught()
        {
            return (id == 2 || id == 3);
        }
        public bool IsSeen()
        {
            return id == 1;
        }
        public bool IsEvolved()
        {
            return id == 3;
        }
    }
}
