using UnityEngine;

namespace WitChess
{
    [CreateAssetMenu(fileName = "New Skin", menuName = "WitChess/Skin")]
    public class Skin : ScriptableObject
    {
        [Header("White Pieces")]
        public Sprite WhitePawn;
        public Sprite WhiteKnight;
        public Sprite WhiteBishop;
        public Sprite WhiteRook;
        public Sprite WhiteQueen;
        public Sprite WhiteKing;

        [Header("Black Pieces")]
        public Sprite BlackPawn;
        public Sprite BlackKnight;
        public Sprite BlackBishop;
        public Sprite BlackRook;
        public Sprite BlackQueen;
        public Sprite BlackKing;

    }
}