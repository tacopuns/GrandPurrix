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
    public QuestionAnswer currentQuestion;
    private AnswerOption[] currentShuffledAnswers;

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

    /*private void DisplayRandomQuestion()
    {
        if (paparazziController != null && paparazziController.interviewHolder != null &&
            paparazziController.interviewHolder.interviewQuestions != null &&
            paparazziController.interviewHolder.interviewQuestions.Count > 0)
        {
            int randomIndex = Random.Range(0, paparazziController.interviewHolder.interviewQuestions.Count);
            
            QuestionAnswer selectedQuestion = paparazziController.interviewHolder.interviewQuestions[randomIndex];
            currentQuestion = selectedQuestion; 


            nameText.text = paparazziController.paparazziName;
            favorSystem.SetCurrentPaparazzi(paparazziController.paparazziName);

            questionText.text = selectedQuestion.question;

            for (int i = 0; i < answerTexts.Length && i < selectedQuestion.answers.Length; i++)
            {
                answerTexts[i].text = selectedQuestion.answers[i].answerText;
            }
        }
    }*/

    private void DisplayRandomQuestion()
    {
        if (paparazziController != null &&
            paparazziController.interviewHolder != null &&
            paparazziController.interviewHolder.interviewQuestions != null &&
            paparazziController.interviewHolder.interviewQuestions.Count > 0)
        {
            int randomIndex = Random.Range(0, paparazziController.interviewHolder.interviewQuestions.Count);

            currentQuestion = paparazziController.interviewHolder.interviewQuestions[randomIndex];

            nameText.text = paparazziController.paparazziName;
            favorSystem.SetCurrentPaparazzi(paparazziController.paparazziName);

            questionText.text = currentQuestion.question;

            // Shuffle answers
            currentShuffledAnswers = ShuffleAnswers(currentQuestion.answers);

            for (int i = 0; i < answerTexts.Length && i < currentShuffledAnswers.Length; i++)
            {
                answerTexts[i].text = currentShuffledAnswers[i].answerText;
            }
        }
    }

    private AnswerOption[] ShuffleAnswers(AnswerOption[] answers)
    {
        AnswerOption[] shuffled = (AnswerOption[])answers.Clone();

        for (int i = 0; i < shuffled.Length; i++)
        {
            int randomIndex = Random.Range(i, shuffled.Length);
            AnswerOption temp = shuffled[i];
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        return shuffled;
    }

    public void SelectAnswer(int index)
    {
        if (currentShuffledAnswers == null || index >= currentShuffledAnswers.Length)
            return;

        int value = currentShuffledAnswers[index].value;

        favorSystem.ApplyAnswerValue(value);

        Debug.Log("Selected answer value: " + value);
    }


}
