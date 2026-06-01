using UnityEngine;

namespace Perpectivas
{
    public class DoorController : MonoBehaviour
    {
        [SerializeField] private Transform doorPanel;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 3.2f, 0f);
        [SerializeField] private float openSpeed = 4f;

        private Vector3 _closedLocalPosition;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (doorPanel == null)
                doorPanel = transform;

            _closedLocalPosition = doorPanel.localPosition;
        }

        private void Update()
        {
            Vector3 target = _closedLocalPosition + (_isOpen ? openLocalOffset : Vector3.zero);
            doorPanel.localPosition = Vector3.Lerp(
                doorPanel.localPosition,
                target,
                1f - Mathf.Exp(-openSpeed * Time.deltaTime));
        }

        public void Open() => _isOpen = true;
        public void Close() => _isOpen = false;
        public void Toggle() => _isOpen = !_isOpen;
    }
}
