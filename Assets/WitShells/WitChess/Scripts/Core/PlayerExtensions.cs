namespace WitChess
{
    public static class PlayerExtensions
    {
        public static EPlayer Opponent(this EPlayer player)
        {
            return player == EPlayer.White ? EPlayer.Black : EPlayer.White;
        }
    }
}