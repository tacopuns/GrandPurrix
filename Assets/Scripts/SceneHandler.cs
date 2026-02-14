using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHandler : MonoBehaviour
{

    private int SceneA = 1;
    private int sceneB = 2;
    private int sceneC = 3;
    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToAScene()
    {
        SceneManager.LoadScene(SceneA);
    }

    public void GoToBScene()
    {
        SceneManager.LoadScene(sceneB);
    }

    public void GoToCScene()
    {
        SceneManager.LoadScene(sceneC);
    }
}
