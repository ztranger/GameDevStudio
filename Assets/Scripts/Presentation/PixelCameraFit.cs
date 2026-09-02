using UnityEngine;

namespace GameDevStudio.Presentation
{
    [RequireComponent(typeof(Camera))]
    public sealed class PixelCameraFit : MonoBehaviour
    {
        public int PixelsPerUnit = PixelArtFactory.Ppu;
        public int ReferenceHeight = 180;

        Camera _camera;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.allowMSAA = false;
        }

        void LateUpdate()
        {
            int scale = Mathf.Max(1, Screen.height / Mathf.Max(1, ReferenceHeight));
            _camera.orthographicSize = Screen.height / (2f * PixelsPerUnit * scale);
        }
    }
}
