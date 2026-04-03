namespace WitChess
{
    public static class BoardFactory
    {
        public static Board CreateStandard()
        {
            Board board = new Board();
            board[0,0]=new Rook(EPlayer.White);   board[0,1]=new Knight(EPlayer.White);
            board[0,2]=new Bishop(EPlayer.White); board[0,3]=new Queen(EPlayer.White);
            board[0,4]=new King(EPlayer.White);   board[0,5]=new Bishop(EPlayer.White);
            board[0,6]=new Knight(EPlayer.White); board[0,7]=new Rook(EPlayer.White);
            for (int c = 0; c < 8; c++) board[1, c] = new Pawn(EPlayer.White);

            board[7,0]=new Rook(EPlayer.Black);   board[7,1]=new Knight(EPlayer.Black);
            board[7,2]=new Bishop(EPlayer.Black); board[7,3]=new Queen(EPlayer.Black);
            board[7,4]=new King(EPlayer.Black);   board[7,5]=new Bishop(EPlayer.Black);
            board[7,6]=new Knight(EPlayer.Black); board[7,7]=new Rook(EPlayer.Black);
            for (int c = 0; c < 8; c++) board[6, c] = new Pawn(EPlayer.Black);
            return board;
        }
    }
}