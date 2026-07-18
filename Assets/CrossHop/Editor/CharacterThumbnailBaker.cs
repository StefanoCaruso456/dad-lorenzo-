using System.IO;
using CrossHop.Characters;
using UnityEditor;
using UnityEngine;

namespace CrossHop.EditorTools
{
    /// <summary>
    /// Renders every character's 3D voxel prefab to a transparent 2D sprite for the
    /// selection menu, at a fixed ¾ isometric angle, and assigns it back to the
    /// <see cref="CharacterData.thumbnail"/> slot. Because the icon is baked from the
    /// prefab, the in-game model and the menu sprite can never drift apart — re-bake
    /// after any model change.
    ///
    /// Menu: Tools ▸ CrossHop ▸ Bake Character Icons.
    /// </summary>
    public static class CharacterThumbnailBaker
    {
        private const string OutDir = "Assets/CrossHop/Art/Thumbnails";
        private const int Size = 256;
        private const int BakeLayer = 31; // rendered in isolation via culling mask

        [MenuItem("Tools/CrossHop/Bake Character Icons")]
        public static void BakeAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterData");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[CrossHop] No CharacterData assets found to bake.");
                return;
            }

            Directory.CreateDirectory(OutDir);
            int baked = 0;
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                    if (data == null || data.modelPrefab == null) continue;

                    EditorUtility.DisplayProgressBar("Baking character icons", data.name, (float)i / guids.Length);
                    Sprite sprite = BakeOne(data);
                    if (sprite != null) { AssignThumbnail(data, sprite); baked++; }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CrossHop] Baked {baked} character icon(s) into {OutDir}.");
        }

        private static Sprite BakeOne(CharacterData data)
        {
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };

            var camGo = new GameObject("~IconCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.orthographic = true;
            cam.cullingMask = 1 << BakeLayer;
            cam.targetTexture = rt;
            camGo.transform.rotation = Quaternion.Euler(30f, 45f, 0f); // ¾ isometric

            var lightGo = new GameObject("~IconLight") { hideFlags = HideFlags.HideAndDontSave };
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject model = Object.Instantiate(data.modelPrefab);
            model.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursive(model, BakeLayer);

            Bounds b = CalcBounds(model);
            float radius = Mathf.Max(b.extents.magnitude, 0.01f);
            cam.orthographicSize = radius * 1.05f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = radius * 10f;
            camGo.transform.position = b.center - camGo.transform.forward * (radius * 4f);

            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            string file = $"{OutDir}/{Sanitize(string.IsNullOrEmpty(data.id) ? data.name : data.id)}_icon.png";
            File.WriteAllBytes(file, tex.EncodeToPNG());

            Object.DestroyImmediate(model);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(lightGo);
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(file);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point; // crisp voxel edges
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(file);
        }

        private static void AssignThumbnail(CharacterData data, Sprite sprite)
        {
            var so = new SerializedObject(data);
            so.FindProperty("thumbnail").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Bounds CalcBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }

        private static string Sanitize(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_');
        }
    }
}
