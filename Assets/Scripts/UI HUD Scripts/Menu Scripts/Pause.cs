using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    private bool gamePaused = false;
    public GameObject pauseMenu;
    void Start()
    {
        gamePaused = false;
        Time.timeScale = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (!gamePaused)
        {
            if(UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                Time.timeScale = 0;
                gamePaused = true;
                pauseMenu.SetActive(true);
            }
        }
    }

    public void Resume()
    {
        Time.timeScale = 1;
        gamePaused = false;
    }

    public void ReturnToMain()
    {  
        SceneManager.LoadScene("Title");
    }

    public void QuitGame()
    {
        Debug.Log ("Quit!");
        Application.Quit();
    }
    
}
