/*
 The MIT License (MIT)

Copyright (c) 2013 yamamura tatsuhiko

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

public class Fade : MonoBehaviour
{
    IFade fade;

    void Start()
    {
        Init();
        fade.Range = cutoutRange;
    }

    float cutoutRange;

    public void SetStartRange()
    {
        EnsureInit();
        cutoutRange = 1.0f;
        if (fade != null)
        {
            fade.Range = cutoutRange;
        }
    }

    void Init()
    {
        fade = GetComponent<IFade>();
    }

    void EnsureInit()
    {
        if (fade == null)
        {
            Init();
        }
    }

    void OnValidate()
    {
        Init();
        fade.Range = cutoutRange;
    }

    IEnumerator FadeoutCoroutine(float time, System.Action action)
    {
        // deltaTime方式：現在のcutoutRangeから0へ、time秒かけて進める
        // cutoutRangeは 1=完全に覆う / 0=完全に開く（元実装の挙動を維持）
        float start = cutoutRange;

        if (time <= 0f)
        {
            cutoutRange = 0f;
            fade.Range = cutoutRange;
            if (action != null) action();
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / time);

            cutoutRange = Mathf.Lerp(start, 0f, p);
            fade.Range = cutoutRange;

            yield return null;
        }

        cutoutRange = 0f;
        fade.Range = cutoutRange;

        if (action != null)
        {
            action();
        }
    }

    IEnumerator FadeinCoroutine(float time, System.Action action)
    {
        // deltaTime方式：現在のcutoutRangeから1へ、time秒かけて進める
        float start = cutoutRange;

        if (time <= 0f)
        {
            cutoutRange = 1f;
            fade.Range = cutoutRange;
            if (action != null) action();
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / time);

            cutoutRange = Mathf.Lerp(start, 1f, p);
            fade.Range = cutoutRange;

            yield return null;
        }

        cutoutRange = 1f;
        fade.Range = cutoutRange;

        if (action != null)
        {
            action();
        }
    }

    public Coroutine FadeOut(float time, System.Action action)
    {
        EnsureInit();
        if (fade == null)
        {
            Debug.LogWarning("Fade component not found.", this);
            return null;
        }
        StopAllCoroutines();
        return StartCoroutine(FadeoutCoroutine(time, action));
    }

    public Coroutine FadeOut(float time)
    {
        return FadeOut(time, null);
    }

    public Coroutine FadeIn(float time, System.Action action)
    {
        EnsureInit();
        if (fade == null)
        {
            Debug.LogWarning("Fade component not found.", this);
            return null;
        }
        StopAllCoroutines();
        return StartCoroutine(FadeinCoroutine(time, action));
    }

    public Coroutine FadeIn(float time)
    {
        return FadeIn(time, null);
    }
}
