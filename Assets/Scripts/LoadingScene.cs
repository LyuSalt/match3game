using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using DG.Tweening;

public class LoadingScene : MonoBehaviour
{
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public string sceneToLoad = "SampleScene";

    public float animationDuration = 0.5f;
    public float pulseScale = 1.2f;

    private Coroutine pulseCoroutine;
    private bool isDestroying = false;

    void Start()
    {
        // Анимация появления слайдера
        progressSlider.transform.DOLocalMoveY(-100f, 0f);
        progressSlider.transform.DOLocalMoveY(0f, 0.6f).SetEase(Ease.OutBack);

        // Анимация появления текста
        progressText.alpha = 0f;
        progressText.DOFade(1f, 0.5f).SetDelay(0.2f);

        StartCoroutine(LoadSceneAsync());
    }

    // Этот метод вызывается, когда объект уничтожается (при переходе на новую сцену)
    void OnDestroy()
    {
        isDestroying = true;
        // Останавливаем все твины на этом объекте
        transform.DOKill();
        // Останавливаем корутину, если она ещё работает
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
    }

    IEnumerator LoadSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        pulseCoroutine = StartCoroutine(PulseSlider());

        while (!operation.isDone)
        {
            // Если объект уничтожается — выходим из корутины
            if (isDestroying) yield break;

            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            progressSlider.DOValue(progress, animationDuration).SetEase(Ease.OutQuad);

            if (progressText != null)
            {
                int currentPercent = Mathf.RoundToInt(progress * 100);
                progressText.text = currentPercent + "%";
                progressText.transform.DOPunchScale(Vector3.one * 0.1f, 0.15f).SetEase(Ease.OutQuad);
            }

            if (operation.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.8f);
                if (pulseCoroutine != null)
                    StopCoroutine(pulseCoroutine);
                // Останавливаем все твины перед активацией сцены
                transform.DOKill();
                operation.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    IEnumerator PulseSlider()
    {
        while (true)
        {
            // Если объект уничтожается — выходим
            if (isDestroying) yield break;

            if (progressSlider != null && progressSlider.fillRect != null)
            {
                Transform fillTransform = progressSlider.fillRect;
                if (fillTransform != null && fillTransform.gameObject != null)
                {
                    fillTransform.DOScale(pulseScale, 0.3f).SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            if (!isDestroying && fillTransform != null && fillTransform.gameObject != null)
                                fillTransform.DOScale(1f, 0.3f).SetEase(Ease.InQuad);
                        });
                }
            }
            yield return new WaitForSeconds(0.6f);
        }
    }
}