using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    // private void Start()
    // {
    //     //Invoke("Play",300);
    // }
    public Image blackImage;
    [SerializeField] private float alpha;

    IEnumerator FadeOut()
    {
        alpha = 0;

        while (alpha < 0.999)
        {
            alpha += Time.deltaTime;
            blackImage.color = new Color(0, 0, 0, alpha);
            yield return null; //yield return new waitforSeconds(0);
            //yield return new WaitForSeconds(100f);
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        
    }

    IEnumerator FadeIn()
    {
        alpha = 1;
        while (alpha > 0)
        {
            alpha -= Time.deltaTime;
            blackImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
    }
    

    public void Play()
    {
        StartCoroutine(FadeOut());
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        Debug.LogWarning("Player Has Play");
        //StartCoroutine(FadeIn());
    }

    public void Quit()
    {
        Application.Quit();
        Debug.LogWarning("Player Has Quit The Game");
    }

}
