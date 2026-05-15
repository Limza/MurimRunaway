using UnityEngine;
using TMPro;

namespace MurimRunaway.Battle.View
{
    /// <summary>current/max를 받아 Fill width + 텍스트를 갱신하는 자원 게이지 헬퍼.</summary>
    public sealed class ResourceBar : MonoBehaviour
    {
        [SerializeField] private RectTransform _track;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private TMP_Text _valueText;

        public void SetValue(int current, int max)
        {
            var ratio = max <= 0 ? 0f : (float)current / max;
            var size = _fill.sizeDelta;
            size.x = _track.rect.width * ratio;
            _fill.sizeDelta = size;

            if (_valueText != null)
                _valueText.text = $"{current} / {max}";
        }
    }
}