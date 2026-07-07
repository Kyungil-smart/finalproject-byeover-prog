using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameBackgroundImageController : MonoBehaviour
{
    // ---------- SerializeField ----------
    [SerializeField] Image _backgroundImage;
    [SerializeField] Sprite _defaultBackgroundImage;
    
    // ---------- Const ----------
    private const string PATH = "InGameBG/BG_";
    
    // ---------- 초기화 ----------
    public void SetBackground(int chapterId)
    {
        _backgroundImage ??= GetComponent<Image>();
        
        var loadedBg = Resources.Load<Sprite>($"{PATH}{chapterId}");
        if(loadedBg == null) loadedBg = _defaultBackgroundImage;
        
        _backgroundImage.sprite = loadedBg;
    }
}
