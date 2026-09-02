using UnityEngine;

namespace GameDevStudio.Core
{
    public sealed class SafeAreaFit : MonoBehaviour
    {
        RectTransform _rect;
        Rect _last;

        void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Apply();
        }

        void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        void Update()
        {
            if (_last != Screen.safeArea)
            {
                Apply();
            }
        }

        void Apply()
        {
            if (_rect == null)
            {
                _rect = GetComponent<RectTransform>();
            }

            if (_rect == null)
            {
                return;
            }

            Rect safe = Screen.safeArea;
            _last = safe;
            float w = Mathf.Max(1, Screen.width);
            float h = Mathf.Max(1, Screen.height);
            _rect.anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
            _rect.anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
