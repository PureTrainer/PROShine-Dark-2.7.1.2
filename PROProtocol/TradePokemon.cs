using System;

namespace PROProtocol
{
    public class TradePokemon
    {
        public int Id { get; private set; }
        public int Uid { get; private set; }
        public int DatabaseId { get; private set; }
        public int Level
        {
            get
            {
                return Experience.CurrentLevel;
            }
        }
        public PokemonExperience Experience { get; private set; }
        public bool IsShiny { get; private set; }
        public int CurrentHealth { get; private set; }
        public int Happiness { get; private set; }
        public string OriginalTrainer { get; private set; }
        public string ItemHeld { get; private set; }
        public string Gender { get; private set; }
        public PokemonStats IV { get; private set; }
        public PokemonStats EV { get; private set; }
        public PokemonStats Stats { get; private set; }
        public PokemonMove[] Moves { get; private set; }
        public PokemonNature Nature { get; private set; }
        public PokemonAbility Ability { get; private set; }
        public int Form { get; private set; }

        internal TradePokemon(string[] data)
        {
            Id = Convert.ToInt32(data[0]);
            Uid = -1;
            DatabaseId = Convert.ToInt32(data[34]);
            CurrentHealth = Convert.ToInt32(data[14]);
            Happiness = 75;
            Experience = new PokemonExperience(Convert.ToInt32(data[1]), Convert.ToInt32(data[19]), Convert.ToInt32(data[18]));
            Moves = new PokemonMove[4];

            //TODO: identify maxPoints
            int maxPP = 0;
            Moves[0] = new PokemonMove(1, Convert.ToInt32(data[21]), maxPP, maxPP);
            Moves[1] = new PokemonMove(2, Convert.ToInt32(data[22]), maxPP, maxPP);
            Moves[2] = new PokemonMove(3, Convert.ToInt32(data[23]), maxPP, maxPP);
            Moves[3] = new PokemonMove(4, Convert.ToInt32(data[24]), maxPP, maxPP);

            Nature = new PokemonNature(Convert.ToInt32(data[17]));
            Ability = new PokemonAbility(Convert.ToInt32(data[16]));

            IsShiny = (data[2] == "1");
            ItemHeld = data[26];
            OriginalTrainer = data[20];
            Gender = data[15];
            Form = Convert.ToInt32(data[33]);
            Stats = new PokemonStats(data, 9);
            IV = new PokemonStats(data, 4);
            EV = new PokemonStats(data, 27);

        }

        public string Name
        {
            get { return PokemonNamesManager.Instance.Names[Id]; }
        }

        public string Health
        {
            get { return CurrentHealth + "/" + CurrentHealth; }
        }
    }
}
