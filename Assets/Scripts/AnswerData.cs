using UnityEngine;

[System.Serializable]
public class AnswerData
{
    [TextArea] public string answerText;
    public int value; // +1, +2, etc.
}
