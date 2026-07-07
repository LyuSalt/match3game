//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.SceneManagement;
//using System.Collections;
//using TMPro;
//using DG.Tweening;

//public class LoadingScene : MonoBehaviour
//{
//    [Header("UI")]
//    public Slider progressSlider;
//    public TextMeshProUGUI progressText;
//    public string sceneToLoad = "SampleScene";

//    [Header("Появление")]
//    public float appearDuration = 0.6f;

//    [Header("Сглаживание прогресса")]
//    public float smoothDuration = 0.5f; // длительность анимации изменения значения

//    [Header("Пульсация (опционально, по умолчанию выключена)")]
//    public bool enablePulse = false;    // включите только если уверены, что pivot = (0.5,0.5)
//    public float pulseScale = 1.03f;
//    public float pulseDuration = 0.8f;

//    private float targetProgress = 0f;
//    private Tween progressTween;
//    private Tween pulseTween;
//    private bool isDestroying = false;

//    void Start()
//    {
//        // Начальное состояние: слайдер внизу, текст невидим
//        progressSlider.transform.localPosition = new Vector3(0, -100f, 0);
//        progressText.alpha = 0f;

//        // Анимация появления
//        Sequence appearSequence = DOTween.Sequence();
//        appearSequence.Join(progressSlider.transform.DOLocalMoveY(0f, appearDuration).SetEase(Ease.OutBack));
//        appearSequence.Join(progressText.DOFade(1f, 0.5f).SetDelay(0.2f));
//        appearSequence.OnComplete(() => {
//            if (enablePulse) StartPulse();
//        });

//        // Старт загрузки
//        StartCoroutine(LoadSceneAsync());
//    }

//    void StartPulse()
//    {
//        // Безопасная пульсация: если хотите избежать искажений, 
//        // замените масштаб на анимацию цвета или прозрачности (пример в комментариях).
//        Transform sliderTransform = progressSlider.transform;
//        pulseTween = sliderTransform.DOScale(pulseScale, pulseDuration)
//            .SetEase(Ease.InOutSine)
//            .SetLoops(-1, LoopType.Yoyo);

//        // Альтернатива – пульсация через цвет Fill:
//        // Image fillImage = progressSlider.fillRect.GetComponent<Image>();
//        // pulseTween = fillImage.DOColor(new Color(0.2f, 1f, 0.2f), pulseDuration)
//        //     .SetLoops(-1, LoopType.Yoyo);
//    }

//    IEnumerator LoadSceneAsync()
//    {
//        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
//        operation.allowSceneActivation = false;

//        while (!operation.isDone)
//        {
//            if (isDestroying) yield break;

//            // Прогресс: 0..0.9 -> 0..1
//            targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

//            // Плавно анимируем значение слайдера
//            if (progressTween != null && progressTween.IsPlaying())
//                progressTween.Kill();
//            progressTween = progressSlider.DOValue(targetProgress, smoothDuration).SetEase(Ease.OutQuad);

//            // Обновляем текст (синхронно с targetProgress)
//            if (progressText != null)
//            {
//                int percent = Mathf.RoundToInt(targetProgress * 100);
//                progressText.text = $"{percent}%";
//            }

//            // Когда загрузка близка к завершению
//            if (operation.progress >= 0.9f)
//            {
//                // Ждём визуального заполнения до 1
//                yield return new WaitWhile(() => progressSlider.value < 0.99f);
//                yield return new WaitForSeconds(0.2f);

//                // Убиваем все твины и сбрасываем масштаб (на случай пульсации)
//                KillAllTweens();
//                progressSlider.transform.localScale = Vector3.one;

//                operation.allowSceneActivation = true;
//                yield break;
//            }

//            yield return null;
//        }
//    }

//    void Update()
//    {
//        // Обновляем текст по текущему визуальному значению слайдера,
//        // чтобы он бежал плавно вместе с анимацией.
//        if (progressText != null && progressSlider != null)
//        {
//            int percent = Mathf.RoundToInt(progressSlider.value * 100);
//            progressText.text = $"{percent}%";
//        }
//    }

//    void KillAllTweens()
//    {
//        if (progressTween != null) progressTween.Kill();
//        if (pulseTween != null) pulseTween.Kill();
//        transform.DOKill();
//    }

//    void OnDestroy()
//    {
//        isDestroying = true;
//        KillAllTweens();
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using DG.Tweening;

public class LoadingScene : MonoBehaviour
{
    [Header("UI")]
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public string sceneToLoad = "SampleScene";

    [Header("Сглаживание прогресса")]
    public float smoothDuration = 0.5f; // длительность анимации изменения значения

    [Header("Пульсация (опционально)")]
    public bool enablePulse = false;    // включите, только если pivot = (0.5,0.5)
    public float pulseScale = 1.03f;
    public float pulseDuration = 0.8f;

    private float targetProgress = 0f;
    private Tween progressTween;
    private Tween pulseTween;
    private bool isDestroying = false;

    void Start()
    {
        // Слайдер уже на своём месте в сцене – не двигаем его.
        // Текст показываем сразу (или с лёгким появлением – см. ниже)
        progressText.alpha = 0f;
        progressText.DOFade(1f, 0.4f); // можно убрать эту строку, если хотите мгновенное появление

        // Если нужна пульсация – запускаем
        if (enablePulse) StartPulse();

        // Старт загрузки
        StartCoroutine(LoadSceneAsync());
    }

    void StartPulse()
    {
        // Безопасная пульсация через масштаб (pivot должен быть (0.5,0.5))
        Transform sliderTransform = progressSlider.transform;
        pulseTween = sliderTransform.DOScale(pulseScale, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // Альтернатива – анимировать цвет Fill (без искажений)
        // Image fillImage = progressSlider.fillRect.GetComponent<Image>();
        // pulseTween = fillImage.DOColor(new Color(0.2f, 1f, 0.2f), pulseDuration)
        //     .SetLoops(-1, LoopType.Yoyo);
    }

    IEnumerator LoadSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            if (isDestroying) yield break;

            // Прогресс загрузки (0..0.9 -> 0..1)
            targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // Плавно анимируем значение слайдера
            if (progressTween != null && progressTween.IsPlaying())
                progressTween.Kill();
            progressTween = progressSlider.DOValue(targetProgress, smoothDuration).SetEase(Ease.OutQuad);

            // Обновляем текст по целевому прогрессу (чтобы цифры бежали синхронно)
            if (progressText != null)
            {
                int percent = Mathf.RoundToInt(targetProgress * 100);
                progressText.text = $"{percent}%";
            }

            // Когда загрузка почти завершена
            if (operation.progress >= 0.9f)
            {
                // Ждём визуального заполнения до 1
                yield return new WaitWhile(() => progressSlider.value < 0.99f);
                yield return new WaitForSeconds(0.2f);

                // Убиваем все твины и сбрасываем масштаб (на случай пульсации)
                KillAllTweens();
                progressSlider.transform.localScale = Vector3.one;

                operation.allowSceneActivation = true;
                yield break;
            }

            yield return null;
        }
    }

    void Update()
    {
        // Обновляем текст по текущему визуальному значению слайдера,
        // чтобы он бежал плавно вместе с анимацией.
        if (progressText != null && progressSlider != null)
        {
            int percent = Mathf.RoundToInt(progressSlider.value * 100);
            progressText.text = $"{percent}%";
        }
    }

    void KillAllTweens()
    {
        if (progressTween != null) progressTween.Kill();
        if (pulseTween != null) pulseTween.Kill();
        transform.DOKill();
    }

    void OnDestroy()
    {
        isDestroying = true;
        KillAllTweens();
    }
}