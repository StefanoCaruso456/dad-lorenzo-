using UnityEngine;

namespace CrossHop.Gameplay
{
    /// <summary>
    /// Recolours a voxel model to a random palette entry every time it's spawned (i.e. on
    /// each pool activation). This is what turns one car prefab into a whole road of
    /// differently-coloured cars — the variety that makes the Crossy-Road look read.
    /// Colours are applied via MaterialPropertyBlock, so all instances still share one
    /// material (no per-instance material allocations).
    /// </summary>
    public sealed class RandomVoxelColor : MonoBehaviour
    {
        [Tooltip("Renderers recoloured together (e.g. a car's body + cabin).")]
        [SerializeField] private Renderer[] targets;
        [Tooltip("Palette to pick from at random on each spawn.")]
        [SerializeField] private Color[] palette;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _mpb;

        private void OnEnable()
        {
            if (targets == null || targets.Length == 0 || palette == null || palette.Length == 0)
                return;

            Color c = palette[Random.Range(0, palette.Length)];
            _mpb ??= new MaterialPropertyBlock();
            foreach (Renderer r in targets)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, c);
                _mpb.SetColor(ColorId, c);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
