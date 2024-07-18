using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHandler : MonoBehaviour
{

    public int SceneA = 1;
    public int SceneB = 2;
    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToAScene(int SceneA)
    {
        SceneManager.LoadScene(SceneA);
    }

    public void GoToBScene(int SceneB)
    {
        SceneManager.LoadScene(SceneB);
    }
}
