using UnityEngine;
using UnityEditor;
using System.IO;

public class MakeWhiteSilhouette
{
    // 💡 에셋 폴더에서 우클릭했을 때 바로 쓸 수 있게 메뉴를 만듭니다.
    [MenuItem("Assets/원본 모양 그대로 하얀색 만들기")]
    public static void CreateWhiteSilhouette()
    {
        // 1. Project 창에서 현재 선택한 원본 에셋을 가져옵니다.
        Texture2D selectedTexture = Selection.activeObject as Texture2D;

        if (selectedTexture == null)
        {
            Debug.LogWarning("Project 창에서 원본 이미지(HP_bar)를 먼저 선택해주세요!");
            return;
        }

        // 2. 💡 유니티가 픽셀 데이터를 읽을 수 있는지 권한 체크
        if (!selectedTexture.isReadable)
        {
            Debug.LogError($"[{selectedTexture.name}] 이미지의 인스펙터에서 'Read/Write' (또는 'Read/Write Enabled') 옵션을 체크하고 Apply를 눌러주세요!");
            return;
        }

        int width = selectedTexture.width;
        int height = selectedTexture.height;
        Texture2D whiteTex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color[] originalPixels = selectedTexture.GetPixels();
        Color[] whitePixels = new Color[originalPixels.Length];

        // 3. 원본의 '투명도(Alpha)'는 그대로 유지하고, 색상(RGB)만 하얗게 만듭니다.
        for (int i = 0; i < originalPixels.Length; i++)
        {
            float alpha = originalPixels[i].a;
            whitePixels[i] = new Color(1f, 1f, 1f, alpha);
        }

        whiteTex.SetPixels(whitePixels);
        whiteTex.Apply();

        // 4. 원본 이미지가 있던 폴더에 '_White'를 붙여서 저장합니다.
        string path = AssetDatabase.GetAssetPath(selectedTexture);
        string directory = Path.GetDirectoryName(path);
        string newPath = directory + "/" + selectedTexture.name + "_White.png";

        File.WriteAllBytes(newPath, whiteTex.EncodeToPNG());
        AssetDatabase.Refresh();

        Debug.Log($"[성공] 곡률이 100% 동일한 하얀색 이미지가 생성되었습니다: {newPath}");
    }
}