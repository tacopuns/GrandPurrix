using System.Collections.Generic;
[System.Serializable]
public class Paparazzi
{
    public string name;
    public int favorPoints;
    //public List<QuestionAnswer> questions;

    public Paparazzi(string name)
    {
        this.name = name;
        this.favorPoints = 0;
        //this.questions = new List<QuestionAnswer>();
    }
}

[System.Serializable]
public class QuestionAnswer
{
    public string question;
    public string[] answers;
}


