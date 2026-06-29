using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class Menu : MonoBehaviour
{
    [Header("Настройки сцен")]
    public string gameSceneName = "SampleScene"; // Имя игровой сцены

    [Header("Настройки звука (опционально)")]
    public AudioClip clickSound; // Звук при нажатии

    [Header("Ссылка на кнопку")]
    public Button startButton; 

    private AudioSource audioSource;
    private bool isProcessing = false; // Блокировка повторных кликов

    void Start()
    {
        // Настраиваем AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && clickSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    /// <summary>
    /// Метод для кнопки "Начать игру".
    /// Анимирует нажатие и загружает сцену.
    /// </summary>
    public void StartGame()
    {
        if (isProcessing) return; // Защита от двойного клика
        isProcessing = true;

        // Проигрываем звук (если назначен)
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // Создаём последовательность анимаций
        Sequence seq = DOTween.Sequence();

        // 1. Утопание (уменьшение)
        seq.Append(startButton.transform.DOScale(new Vector3(0.85f, 0.85f, 1f), 0.1f)
            .SetEase(Ease.InQuad));

        // 2. Возврат с пружинкой
        seq.Append(startButton.transform.DOScale(Vector3.one, 0.25f)
            .SetEase(Ease.OutBack));

        // 3. После завершения анимации загружаем сцену
        seq.OnComplete(() =>
        {
            SceneManager.LoadScene(gameSceneName);
        });
    }

    /// <summary>
    /// Метод для кнопки "Выход".
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Выход из игры...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}