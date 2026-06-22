using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "JokerPatternLibrary", menuName = "Scriptable Objects/JokerPatternLibrary")]
public class JokerPatternLibrary : ScriptableObject
{
    public List<JokerPatternData> patterns;
}
