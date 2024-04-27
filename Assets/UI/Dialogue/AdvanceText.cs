using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class AdvanceTextPreprocessor : ITextPreprocessor
{
    public Dictionary<int, float> IntervalDictionary;

    public AdvanceTextPreprocessor()
    {
        IntervalDictionary = new Dictionary<int, float>();
    }

    public string PreprocessText(string text)
    {
        IntervalDictionary.Clear();

        string processingText = text;
        string pattern = "<.*?>";
        Match match = Regex.Match(processingText, pattern);

        while (match.Success)
        {
            string label = match.Value.Substring(1, match.Length - 2);

            if (float.TryParse(label, out float result))
            {
                IntervalDictionary[match.Index - 1] = result;
            }

            //去除所有符合pattern的正则
            processingText = processingText.Remove(match.Index, match.Length);

            //再匹配一次，看是否还有，直至推出循环
            match = Regex.Match(processingText, pattern);
        }

        //仅仅删除<>加数字的
        processingText = text;
        //正则表达式
        // * 代表前一个字符出现零次或者多次
        // + 代表前一个字符出现一次或多次
        // ? 代表前一个字符出现零次或一次
        // . 代表任意字符   \.代表唯一字符 .
        pattern = @"<(\d+)(.\d+)?>";
        processingText = Regex.Replace(processingText, pattern, "");
        return processingText;
    }
}

public class AdvanceText : TextMeshProUGUI
{
    public AdvanceText() //构造函数
    {
        //tectPreprocessor 预处理器
        textPreprocessor = new AdvanceTextPreprocessor();
    }

    private AdvanceTextPreprocessor SelfPreprocessor => (AdvanceTextPreprocessor)textPreprocessor;

    //打印方法
    public void ShowTextByTyping(string content)
    {
        SetText(content);
        //new WaitForSecondsRealtime(0.2);
        StartCoroutine(Typing());
    }

    private int _typingIndex;
    private float _defaultInterval = 0.06f;


    //由于是逐字打印所以需要一个协程
    IEnumerator Typing()
    {
        ForceMeshUpdate(); //强制网格更新
        for (int i = 0; i < m_characterCount; i++)
        {
            SetSingleCharacterAlpha(i, 0);
        }

        _typingIndex = 0;
        while (_typingIndex < m_characterCount)
        {
            
            SetSingleCharacterAlpha(_typingIndex, 255);

            if (textInfo.characterInfo[_typingIndex].isVisible)
            {
                StartCoroutine(FadeInCharacter(_typingIndex));
            }
            
            if (SelfPreprocessor.IntervalDictionary.TryGetValue(_typingIndex, out float result))
            {
                yield return new WaitForSecondsRealtime(result);
            }
            else
            {
                yield return new WaitForSecondsRealtime(_defaultInterval);
            }
            //yield return new WaitForSecondsRealtime(_defaultInterval);
            _typingIndex++;
        }
    }

    // newAlpha的范围是0-255
    private void SetSingleCharacterAlpha(int index, byte newAlpha)
    {
        TMP_CharacterInfo charInfo = textInfo.characterInfo[index];
        int matIndex = charInfo.materialReferenceIndex;
        int vertIndex = charInfo.vertexIndex;
        for (int i = 0; i < 4; i++)
        {
            textInfo.meshInfo[matIndex].colors32[vertIndex + i].a = newAlpha;
        }

        UpdateVertexData();
    }

    IEnumerator FadeInCharacter(int index, float duration = 0.2f)
    {
        if (duration <= 0)
        {
            SetSingleCharacterAlpha(index, 255);
        }
        else
        {
            float timer = 0;
            while (timer < duration)
            {
                timer = Mathf.Min(timer + Time.unscaledDeltaTime, duration);
                SetSingleCharacterAlpha(index, (byte)(255 * (timer / duration)));
                yield return null;
            }
        }
    }
}