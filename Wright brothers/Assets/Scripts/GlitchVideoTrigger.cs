using UnityEngine;
using UnityEngine.Video;

public class GlitchVideoTrigger : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    // Deze functie wordt aangeroepen door de Animation Event
    public void PlayVideo()
    {
        videoPlayer.Play();
    }
}
