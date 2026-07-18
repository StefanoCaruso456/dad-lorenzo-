using CrossHop.Core;
using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// Follows the player forward and, crucially, keeps creeping forward on its own —
    /// idling lets the death line catch up, forcing constant forward pressure.
    /// When the player falls behind the line, it triggers a scroll death.
    /// </summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private GridSettings grid;
        [SerializeField] private PlayerController player;

        [Tooltip("Camera offset from the followed point (isometric-ish angle).")]
        [SerializeField] private Vector3 offset = new(0f, 10f, -7f);

        [Tooltip("How fast the camera eases toward its target row.")]
        [SerializeField] private float followLerp = 4f;

        [Tooltip("Constant forward creep in rows/second — the pressure that kills idlers.")]
        [SerializeField] private float autoScrollSpeed = 0.6f;

        [Tooltip("How many rows behind the camera focus the death line sits.")]
        [SerializeField] private int deathLineRowsBehind = 6;

        private float _focusRow;   // the row the camera is centred on (world units via grid)
        private bool _running;

        public void Begin()
        {
            _focusRow = 0f;
            _running = true;
            SnapToFocus();
        }

        public void Stop() => _running = false;

        private void LateUpdate()
        {
            if (!_running || player == null) return;

            // Target is the max of steady creep and the player's progress.
            float creepTarget = _focusRow + autoScrollSpeed * Time.deltaTime;
            float playerTarget = player.FurthestRow;
            _focusRow = Mathf.Max(creepTarget, Mathf.Lerp(_focusRow, playerTarget, followLerp * Time.deltaTime));

            SnapToFocus();

            if (player.IsAlive && player.Row < _focusRow - deathLineRowsBehind)
                player.KillByScroll();
        }

        private void SnapToFocus()
        {
            Vector3 focusWorld = new(0f, 0f, _focusRow * grid.cellSize);
            transform.position = focusWorld + offset;
            transform.LookAt(focusWorld);
        }
    }
}
