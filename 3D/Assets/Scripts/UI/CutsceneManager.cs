using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace GameUI
{
    public class CutsceneManager : MonoBehaviour
    {
        [Header("Cutscene Components")]
        public GameObject cutscenePanel;
        public VideoPlayer cutsceneVideoPlayer;
        private string sceneToLoadAfterCutscene;

        [Header("Loading Screen")]
        public GameObject loadingPanel;

        private void Start()
        {
            // Tắt loading và panel video lúc bắt đầu
            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            if (cutscenePanel != null) 
                cutscenePanel.SetActive(false);

            // Đăng ký sự kiện khi video kết thúc
            if (cutsceneVideoPlayer != null)
            {
                cutsceneVideoPlayer.loopPointReached += OnCutsceneFinished;
            }
        }

        public void PlayCutscene(string targetScene)
        {
            if (cutscenePanel != null && cutsceneVideoPlayer != null)
            {
                sceneToLoadAfterCutscene = targetScene;
                cutscenePanel.SetActive(true);
                cutsceneVideoPlayer.Play();
            }
            else
            {
                Debug.LogWarning("Cutscene components are missing. Loading scene directly.");
                SceneManager.LoadScene(targetScene);
            }
        }

        private void OnCutsceneFinished(VideoPlayer vp)
        {
            StartCoroutine(LoadSceneAsync());
        }

        // Hàm này sẽ được gán vào nút Skip
        public void SkipCutscene()
        {
            StartCoroutine(LoadSceneAsync());
        }

        private IEnumerator LoadSceneAsync()
        {
            if (cutsceneVideoPlayer != null)
            {
                cutsceneVideoPlayer.Stop(); // Dừng video
                cutsceneVideoPlayer.enabled = false; // Tắt luồng video 
            }

            // Hiển thị màn hình Loading
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }

            // Đợi vài frame để màn hình Loading kịp vẽ ra trước khi quá trình load làm lag game
            yield return null;
            yield return null;
            yield return null;

            // Tải scene mới dưới nền
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoadAfterCutscene);
            
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
    }
}
