using UnityEditor;
using UnityEngine;

namespace CrossHop.EditorTools
{
    /// <summary>
    /// Enforces the right import settings for voxel art automatically, so every model
    /// and texture dropped into Art/Models comes in with crisp cube edges — no manual
    /// per-asset fiddling. Best practice: make the pipeline deterministic, not a
    /// checklist someone has to remember.
    /// </summary>
    public sealed class VoxelAssetPostprocessor : AssetPostprocessor
    {
        private const string ModelsRoot = "Assets/CrossHop/Art/Models";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ModelsRoot)) return;
            var importer = (TextureImporter)assetImporter;
            importer.filterMode = FilterMode.Point;               // no blurring between texels
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
        }

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelsRoot)) return;
            var importer = (ModelImporter)assetImporter;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }
    }
}
