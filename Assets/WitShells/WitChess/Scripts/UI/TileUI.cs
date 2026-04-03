using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WitChess
{
    public class TileUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _highlightImage;
        [SerializeField] private Image _pieceImage;

        public UnityAction<TileUI> OnTileClicked;

        public void SetColor(Color color)
        {
            _backgroundImage.color = color;
        }

        public void SetHighlight(bool isHighlighted, Color highlightColor)
        {
            _highlightImage.enabled = isHighlighted;
            _highlightImage.color = highlightColor;
        }

        public void SetPieceSprite(Sprite sprite)
        {
            _pieceImage.sprite = sprite;
            _pieceImage.enabled = sprite != null;
        }

        public Sprite GetPieceSprite() => _pieceImage.sprite;


        public void OnPointerClick(PointerEventData eventData)
        {
            OnTileClicked?.Invoke(this);
        }
    }
}