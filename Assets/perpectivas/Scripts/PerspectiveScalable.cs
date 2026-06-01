using UnityEngine;

namespace Perpectivas
{
    public class PerspectiveScalable : MonoBehaviour
    {
        [SerializeField] private float minimumScaleMultiplier = 0.35f;
        [SerializeField] private float maximumScaleMultiplier = 12f;

        public float MinimumScaleMultiplier => minimumScaleMultiplier;
        public float MaximumScaleMultiplier => maximumScaleMultiplier;
    }
}
