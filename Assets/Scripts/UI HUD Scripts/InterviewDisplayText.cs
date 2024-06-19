using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InterviewDisplayText : MonoBehaviour
{
    public PaparazziCarCon paparazziController;
    public FavorSystem favorSystem;

    public TextMeshProUGUI nameText;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI[] answerTexts;

    private bool isQuestionDisplayed = false;

    private void Update()
    {
        if (paparazziController.currentState == PaparazziCarCon.PaparazziState.Interviewing)
        {
            if (!isQuestionDisplayed)
            {
                DisplayRandomQuestion();
                isQuestionDisplayed = true;
            }
        }
        else
        {
            isQuestionDisplayed = false;
        }
    }

    private void DisplayRandomQuestion()
    {
        if (paparazziController != null && paparazziController.interviewHolder != null &&
            paparazziController.interviewHolder.interviewQuestions != null &&
            paparazziController.interviewHolder.interviewQuestions.Count > 0)
        {
            int randomIndex = Random.Range(0, paparazziController.interviewHolder.interviewQuestions.Count);
            QuestionAnswer selectedQuestion = paparazziController.interviewHolder.interviewQuestions[randomIndex];

            nameText.text = paparazziController.paparazziName; // Assuming paparazziController has paparazziName
            favorSystem.SetCurrentPaparazzi(paparazziController.paparazziName);

            questionText.text = selectedQuestion.question;

            for (int i = 0; i < answerTexts.Length && i < selectedQuestion.answers.Length; i++)
            {
                answerTexts[i].text = selectedQuestion.answers[i];
            }
        }
    }
}
