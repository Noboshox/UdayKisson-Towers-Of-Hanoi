using UnityEngine;

public class quitbutton : MonoBehaviour
{
    void Start()
    {
        
    }
    void Update()
    {
        
    }
    public void quitgame()
    {
        Application.Quit();
        Debug.Log("Game has ended"); // used to check if the quit button works
    }
}
