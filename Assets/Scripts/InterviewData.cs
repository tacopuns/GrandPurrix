using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewInterview", menuName = "Interview/Interview Data")]
public class InterviewData : ScriptableObject
{
    public string paparazziName;
    public List<QuestionData> questions;
}
