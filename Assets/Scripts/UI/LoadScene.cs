using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{

    // Update is called once per frame
    public void LoadFirstScene(int level) {
        SceneManager.LoadScene(level);
    }
}
