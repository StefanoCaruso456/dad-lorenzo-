using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// Entry point for a scene. For the gray-box it just auto-starts a run so you can
    /// press Play and immediately hop. Once a main menu exists, turn <see cref="autoStart"/>
    /// off and have the menu call <see cref="GameManager.StartRun"/>.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [Tooltip("Start a run automatically on load (gray-box). Off once a menu drives StartRun.")]
        [SerializeField] private bool autoStart = true;

        private void Start()
        {
            if (autoStart && gameManager != null) gameManager.StartRun();
        }
    }
}
