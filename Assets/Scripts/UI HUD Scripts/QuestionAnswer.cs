using System.Collections.Generic;
[System.Serializable]
public class Paparazzi
{
    public string name;
    public int favorPoints;

    public Paparazzi(string name)
    {
        this.name = name;
        this.favorPoints = 0;
    }
}

[System.Serializable]
public class QuestionAnswer
{
    public string question;
    public AnswerOption[] answers;
}

[System.Serializable]
public class AnswerOption
{
    public string answerText;
    public int value; // +1, -1, +2, etc.
}


