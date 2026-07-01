using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AppSceneBootstrap : MonoBehaviour
{
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField, Min(0f)] private float startupDelay;

    private IEnumerator Start()
    {
        // Give Unity one rendered frame to finish application-level startup
        // before the XR title scene begins loading.
        yield return null;

        if (startupDelay > 0f)
            yield return new WaitForSecondsRealtime(startupDelay);

        if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogError($"AppSceneBootstrap: Build Settings에 '{titleSceneName}' 씬이 없습니다.", this);
            yield break;
        }

        yield return SceneManager.LoadSceneAsync(titleSceneName, LoadSceneMode.Single);
    }
}
