using System.Collections.Generic;

namespace WitChess
{
    public class Counting
    {
        private readonly Dictionary<EPieceType, int> white = new();
        private readonly Dictionary<EPieceType, int> black = new();

        public int TotalCount { get; private set; }

        public Counting()
        {
            TotalCount = 0;
            foreach (EPieceType type in System.Enum.GetValues(typeof(EPieceType)))
            {
                white[type] = 0;
                black[type] = 0;
            }
        }

        public void Increment(EPieceType type, EPlayer player)
        {
            TotalCount++;
            if (player == EPlayer.White)
            {
                white[type]++;
            }
            else
            {
                black[type]++;
            }
        }

        public int White(EPieceType type)
        {
            return white[type];
        }

        public int Black(EPieceType type)
        {
            return black[type];
        }

        public override string ToString()
        {
            string str = "";
            foreach (EPieceType type in System.Enum.GetValues(typeof(EPieceType)))
            {
                str += $"{type}: {white[type]} {black[type]}\n";
            }
            return str;
        }

    }
}