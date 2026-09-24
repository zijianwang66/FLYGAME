using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DroneMicroClass
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = "DroneFigureEightTrainingUnity";
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text progressText;

        private IEnumerator Start()
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                SetProgress(operation.progress / 0.9f);
                yield return null;
            }

            SetProgress(1f);
            yield return new WaitForSeconds(0.35f);
            operation.allowSceneActivation = true;
        }

        private void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (progressText != null)
            {
                progressText.text = $"Loading {Mathf.RoundToInt(progress * 100f)}%";
            }
        }
    }
}
