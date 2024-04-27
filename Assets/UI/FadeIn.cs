using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeIn : MonoBehaviour
{
    // Start is called before the first frame update
    public Image gameBlackImage;

    [SerializeField] private float alpha;

    private void Start()
    {
        StartCoroutine(FadeInScene());
    }

    IEnumerator FadeInScene()
    {
        alpha = 1;
        while (alpha > 0)
        {
            alpha -= Time.deltaTime;
            gameBlackImage.color = new Color(0, 0, 0, alpha);
            //yield return null;
            yield return new WaitForSeconds(0.1f);
        }
    }
}