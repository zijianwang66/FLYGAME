using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = "8字飞行";
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text progressText;

        private IEnumerator Start()
        {
            string selectedScene = SceneLoadRequest.TargetSceneName;
            if (!Application.CanStreamedLevelBeLoaded(selectedScene))
            {
                selectedScene = targetSceneName;
            }

            if (progressText != null)
            {
                progressText.text = $"正在加载 {selectedScene}  0%";
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(selectedScene);
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                SetProgress(operation.progress / 0.9f, selectedScene);
                yield return null;
            }

            SetProgress(1f, selectedScene);
            yield return new WaitForSeconds(0.35f);
            operation.allowSceneActivation = true;
        }

        private void SetProgress(float progress, string sceneName)
        {
            progress = Mathf.Clamp01(progress);
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (progressText != null)
            {
                progressText.text = $"正在加载 {sceneName}  {Mathf.RoundToInt(progress * 100f)}%";
            }
        }
    }
}
