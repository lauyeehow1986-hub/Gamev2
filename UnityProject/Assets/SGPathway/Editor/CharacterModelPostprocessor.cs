using UnityEditor;

namespace SGPathway.EditorTools
{
    /// <summary>
    /// Configures imported models for the character pipeline. Two zones:
    /// <list type="bullet">
    /// <item><c>Content/Characters</c> — Character-Creator hero models: Humanoid rig
    /// (avatar from the model's own skeleton), blendshapes kept (facial expressions),
    /// authored normals/tangents preserved (CC ships good ones), materials imported via
    /// the FBX's material description so base/normal maps hook up. Parsed once at import.</item>
    /// <item><c>Content/Animations</c> — Mixamo (and similar) clips: Humanoid rig so the
    /// motion retargets onto any Humanoid avatar; no materials (animation-only FBX).</item>
    /// </list>
    /// Other models are untouched.
    /// </summary>
    public sealed class CharacterModelPostprocessor : AssetPostprocessor
    {
        private const string CharacterRoot = "Assets/SGPathway/Content/Characters";
        private const string AnimationRoot = "Assets/SGPathway/Content/Animations";

        private string Norm => assetPath == null ? null : assetPath.Replace('\\', '/');
        private bool IsCharacter => Norm != null && Norm.StartsWith(CharacterRoot);
        private bool IsAnimation => Norm != null && Norm.StartsWith(AnimationRoot);

        private void OnPreprocessModel()
        {
            if (!IsCharacter && !IsAnimation) return;
            var mi = (ModelImporter)assetImporter;

            // Humanoid for both: hero models get an avatar; clips retarget onto it.
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            if (IsCharacter)
            {
                mi.importBlendShapes = true;
                mi.importNormals = ModelImporterNormals.Import;
                mi.importTangents = ModelImporterTangents.Import;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                mi.materialLocation = ModelImporterMaterialLocation.External;
            }
            else // animation-only: no materials, no blendshapes
            {
                mi.importBlendShapes = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
            }
        }
    }
}
