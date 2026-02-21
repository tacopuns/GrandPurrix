using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class QuestionData
{
    [TextArea] public string questionText;
    public List<AnswerData> answers;
}
