using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// Paints a fixed colour onto a group of renderers via MaterialPropertyBlock on each
    /// activation — the reliable way to colour voxel parts (wheels, windows, a beak) that
    /// keep Unity's valid default material instead of a custom one that can render magenta.
    /// Pair with <see cref="RandomVoxelColor"/> for the parts that vary per spawn.
    /// </summary>
    public sealed class VoxelTint : MonoBehaviour
    {
        [SerializeField] private Renderer[] targets;
        [SerializeField] private Color color = Color.white;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _mpb;

        private void OnEnable()
        {
            if (targets == null || targets.Length == 0) return;
            _mpb ??= new MaterialPropertyBlock();
            foreach (Renderer r in targets)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, color);
                _mpb.SetColor(ColorId, color);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
