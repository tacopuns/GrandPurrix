using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHandler : MonoBehaviour
{

    private readonly int sceneA = 1;
    private readonly int sceneB = 2;
    private readonly int sceneC = 3;
    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToAScene()
    {
        SceneManager.LoadScene(sceneA);
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
