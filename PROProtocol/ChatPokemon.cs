using System;

namespace PROProtocol
{
    public class ChatPokemon
    {
        public int Id { get; private set; }
        public string Name => PokemonNamesManager.Instance.Names[Id];
        public int Level { get; private set; }
        public int Health { get; private set; }

        public bool IsShiny { get; private set; }
        public string Gender { get; private set; }
        public PokemonNature Nature { get; private set; }
        public PokemonAbility Ability { get; private set; }
        public int Happiness { get; private set; }
        public PokemonStats Stats { get; private set; }
        public PokemonStats IV { get; private set; }
        public int Form { get; private set; }
        public PokemonType Type1 { get; private set; }
        public PokemonType Type2 { get; private set; }

        public string Types
        {
            get
            {
                if (Type2 == PokemonType.None)
                    return Type1.ToString();

                return Type1 + "/" + Type2;
            }
        }

        //id,level,shiny[0|1],iv_hp,iv_atk,iv_def,iv_spd,iv_spatk,iv_spdef,atk,def,spd,spatk,spdef,hp,[F|M|?],Ability,Nature,happiness,form
        public ChatPokemon(string[] data)
        {
            Id = Convert.ToInt32(data[0]);
            Level = Convert.ToInt32(data[1]);
            Health = Convert.ToInt32(data[14]);

            IsShiny = data[2] == "1";
            Gender = data[15] == "?" ? data[15] : data[15].ToUpper();

            Form = Convert.ToInt32(data[19]);
            Nature = new PokemonNature(Convert.ToInt32(data[17]));
            Ability = new PokemonAbility(Convert.ToInt32(data[16]));
            Happiness = Convert.ToInt32(data[18]);

            Stats = new PokemonStats(data, 9, Health);
            IV = new PokemonStats(data, 3);

            Type1 = TypesManager.Instance.Type1[Id];
            Type2 = TypesManager.Instance.Type2[Id];
        }
    }
}