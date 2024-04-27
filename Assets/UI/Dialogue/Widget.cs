using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class Widget : MonoBehaviour
{
    private CanvasGroup _canvasGroup;

    [SerializeField] private AnimationCurve _fadingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private Coroutine _fadeCoroutine;

    public void Fade(float opacity, float duration, Action OnFinished)
    {
        if (duration <= 0)
        {
            _canvasGroup.alpha = opacity;
            OnFinished?.Invoke(); //?. 代表OnFinished有值才会invoke
        }
        else
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(Fading(opacity, duration, OnFinished));
        }
    }

    private IEnumerator Fading(float opacity, float duration, Action OnFinished)
    {
        float timer = 0;
        float start = _canvasGroup.alpha;
        while (timer < duration)
        {
            timer = Mathf.Min(duration, timer + Time.unscaledDeltaTime);
            _canvasGroup.alpha = Mathf.Lerp(start, opacity, _fadingCurve.Evaluate(timer / duration));
            yield return null; //挂起协程，下一帧继续
        }
        OnFinished?.Invoke();
    }
}