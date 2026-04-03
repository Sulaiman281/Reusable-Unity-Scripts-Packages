namespace WitChess
{
    public class Result
    {
        public EPlayer Winner { get; }
        public EEndReason Reason { get; set; }

        public Result(EPlayer winner, EEndReason reason)
        {
            Winner = winner;
            Reason = reason;
        }

        public override string ToString()
        {
            return $"{(Winner == EPlayer.None ? "Draw " : $"{Winner} Wins")} by {Reason}";
        }

        public static Result Win(EPlayer winner)
        {
            return new Result(winner, EEndReason.Checkmate);
        }

        public static Result Draw(EEndReason reason)
        {
            return new Result(EPlayer.None, reason);
        }
    }
}