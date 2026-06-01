using UnityEngine;

namespace Perpectivas
{
    [ExecuteAlways]
    public class RenderTextureController : MonoBehaviour
    {
        [SerializeField] private Camera sourceCamera;
        [SerializeField] private Renderer monitorRenderer;
        [SerializeField] private RenderTexture renderTexture;
        [SerializeField] private int textureWidth = 1024;
        [SerializeField] private int textureHeight = 576;

        private RenderTexture _runtimeTexture;

        private void OnEnable() => Configure();
        private void OnValidate() => Configure();

        private void OnDisable()
        {
            if (sourceCamera != null && sourceCamera.targetTexture == _runtimeTexture)
                sourceCamera.targetTexture = null;

            if (_runtimeTexture != null)
            {
                _runtimeTexture.Release();

                if (Application.isPlaying)
                    Destroy(_runtimeTexture);
                else
                    DestroyImmediate(_runtimeTexture);

                _runtimeTexture = null;
            }
        }

        public void Configure()
        {
            if (sourceCamera == null || monitorRenderer == null)
                return;

            RenderTexture target = renderTexture;

            if (target == null)
            {
                if (_runtimeTexture == null ||
                    _runtimeTexture.width != textureWidth ||
                    _runtimeTexture.height != textureHeight)
                {
                    if (_runtimeTexture != null)
                    {
                        _runtimeTexture.Release();

                        if (Application.isPlaying)
                            Destroy(_runtimeTexture);
                        else
                            DestroyImmediate(_runtimeTexture);
                    }

                    _runtimeTexture = new RenderTexture(textureWidth, textureHeight, 24)
                    {
                        name = "RT_AlternateDimension_Runtime"
                    };
                }

                target = _runtimeTexture;
            }

            sourceCamera.targetTexture = target;
            Material material = monitorRenderer.sharedMaterial;

            if (Application.isPlaying)
                material = monitorRenderer.material;

            if (material != null)
            {
                material.mainTexture = target;

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetTexture("_EmissionMap", target);
                    material.SetColor("_EmissionColor", Color.white * 1.4f);
                }
            }
        }
    }
}
