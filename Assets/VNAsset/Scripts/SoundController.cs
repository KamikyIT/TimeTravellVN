using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SoundController : MonoBehaviour {

	public static SoundController instance;

	[SerializeField] AudioSource audioSource;

	string currentMusicPath = "";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

	public void PlaySound(string path){
		StartCoroutine(PlaySoundCoroutine(path));
	}

	IEnumerator PlaySoundCoroutine(string path){

		using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + path, AudioTypeFromPath(path)))
        {
			yield return www.Send();

			if (www.isHttpError || www.isNetworkError)
            {
                Debug.Log(www.error);
            }
            else
            {
                AudioClip c = DownloadHandlerAudioClip.GetContent(www);

                while (c.loadState != AudioDataLoadState.Loaded)
                {
                    yield return null;
                }

                audioSource.PlayOneShot(c, 1f);
            }
        }
	}

	public void PlayMusic(string path){

		if(currentMusicPath.Equals(path)) return;

		currentMusicPath = path;

		StartCoroutine(PlayMusicCoroutine(path));
	}

	IEnumerator PlayMusicCoroutine(string path){

		float volume = 0;

		if(audioSource.isPlaying){
			volume = 1f;

			while(volume > 0){
				volume -= Time.deltaTime;
				audioSource.volume = volume;
				yield return null;
			}

			audioSource.Stop();
		}

		using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + path, AudioTypeFromPath(path)))
        {
			yield return www.Send();

			if (!www.isHttpError && !www.isNetworkError)
            {
                audioSource.clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.loop = true;

                while (audioSource.clip.loadState != AudioDataLoadState.Loaded)
                    yield return null;

                audioSource.Play();

                while (volume < 1f)
                {
                    volume += Time.deltaTime;
                    audioSource.volume = volume;
                    yield return null;
                }

                audioSource.volume = 1f;
			}
			else
			{
				Debug.Log(www.error);
			}
        }
	}

	AudioType AudioTypeFromPath(string path){

		AudioType type = AudioType.UNKNOWN;

		string extension = Path.GetExtension(path).ToLower();

		switch(extension){
			case "mp3":
				type = AudioType.MPEG;
			break;

			case "ogg":
				type = AudioType.OGGVORBIS;
			break;

			case "wav":
				type = AudioType.WAV;
			break;
		}

		return type;
	}
}
