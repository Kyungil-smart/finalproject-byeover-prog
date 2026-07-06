//담당자: 조규민

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 하우징 DB 이미지 키와 Import Sprite 참조를 연결합니다.
/// </summary>
[Serializable]
// 정규화된 Sprite 키를 기준으로 하우징 이미지 바인딩 탐색
public class HousingSpriteBinding
{
    [Header("이미지 연결")]
    [SerializeField] private string _spriteKey;
    [SerializeField] private Sprite _sprite;

    public string SpriteKey => _spriteKey;
    public Sprite Sprite => _sprite;

    // 정규화된 키와 일치하는 하우징 Sprite 바인딩 탐색
    public static Sprite FindSprite(IReadOnlyList<HousingSpriteBinding> _bindings, string _spriteKey)
    {
        if (_bindings == null || string.IsNullOrWhiteSpace(_spriteKey))
        {
            return null;
        }

        string _normalizedKey = NormalizeSpriteKey(_spriteKey);

        for (int _index = 0; _index < _bindings.Count; _index++)
        {
            HousingSpriteBinding _binding = _bindings[_index];

            if (_binding == null || _binding.Sprite == null)
            {
                continue;
            }

            if (NormalizeSpriteKey(_binding.SpriteKey) != _normalizedKey)
            {
                continue;
            }

            return _binding.Sprite;
        }

        return null;
    }

    private static string NormalizeSpriteKey(string _value)
    {
        if (string.IsNullOrWhiteSpace(_value))
        {
            return string.Empty;
        }

        return Path.GetFileNameWithoutExtension(_value.Trim()).ToLowerInvariant();
    }
}
