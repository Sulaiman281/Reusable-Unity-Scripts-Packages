namespace WitChess
{
    public enum EEndReason
    {
        Checkmate,
        Stalemate,
        InsufficientMaterial,
        FiftyMoveRule,
        ThreefoldRepetition,
        DrawAgreement,
        Resignation,
        Timeout,
        Ambush
    }
}