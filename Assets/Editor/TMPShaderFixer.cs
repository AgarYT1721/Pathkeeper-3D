#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

[InitializeOnLoad]
public class TMPShaderFixer
{
    static TMPShaderFixer()
    {
        EditorApplication.delayCall += FixFonts;
    }

    [MenuItem("Tools/Fix TextMeshPro Fonts for URP")]
    public static void FixFonts()
    {
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        Shader targetShader = Shader.Find("TextMeshPro/Mobile/Distance Field");
        if (targetShader == null) targetShader = Shader.Find("TextMeshPro/Distance Field");

        if (targetShader != null && fonts != null)
        {
            foreach (TMP_FontAsset f in fonts)
            {
                if (f.material != null && (f.material.shader == null || f.material.shader.name != targetShader.name))
                {
                    f.material.shader = targetShader;
                    EditorUtility.SetDirty(f.material);
                    EditorUtility.SetDirty(f);
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
