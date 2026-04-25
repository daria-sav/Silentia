using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/Character Intro Sequence")]
public class CharacterIntroSequence : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public CharacterProfile profile;    
        [TextArea(3, 6)]
        public string introText;            
    }

    public Entry[] entries;
}