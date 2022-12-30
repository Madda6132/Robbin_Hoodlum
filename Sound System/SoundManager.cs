using System.Collections;
using UnityEngine;

// Handles requests for sounds.

namespace Hood.Audio {
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour {

        [SerializeField] Board manageBoard;
        [SerializeField] static float soundFadeTime = 1;

        AudioSource audioSource;

        float startVolume = 0;
        private void Awake() {
            audioSource = GetComponent<AudioSource>();
            startVolume = audioSource.volume;
        }

        /// <summary> 
        /// Play from the listName. 
        /// Play clip at index or Set to negative for random audio clip. 
        /// Play clip at volume unless it's below 0 then play at current volume.
        /// Play clip at pitch between min and max pitch (Normal 1).
        /// </summary>
        public AudioClip PlayClip(string listName, int clipIndex = -1, float volume = -1f, float minPitch = 1, 
            float maxPitch = 1, bool stopSounds = false) {

            if(stopSounds) audioSource.Stop();
            //Pitch settings 
            audioSource.pitch = minPitch == maxPitch ? maxPitch : Random.Range(minPitch, maxPitch + 0.001f);
            
            //Volume setting
            float _volume = volume < 0 ? audioSource.volume : volume;

            //Index setting
            int? index = clipIndex < 0 ? null : clipIndex;
            AudioClip clip = StaticAudioManager.GetSound(manageBoard, listName, ref index);
            if (clip == null) return clip;
             

            audioSource.PlayOneShot(clip, _volume);
            return clip;
        }


        public AudioClip PlayClip(AudioClipArgumentContainer container) => 
            PlayClip(container.ListName, container.ClipIndex, container.Volume, container.MinPitch, 
                container.MaxPitch, container.StopSounds);

        public AudioClip PlayMusic(string listName, int clipIndex = -1, float volume = -1f,
            float minPitch = 1, float maxPitch = 1) {

            audioSource.Stop();
            //Pitch settings 
            float pitch = minPitch == maxPitch ? maxPitch : Random.Range(minPitch, maxPitch + 0.001f);

            //Volume setting
            float _volume = volume < 0 ? audioSource.volume : volume;

            //Index setting
            int? index = clipIndex < 0 ? null : clipIndex;
            AudioClip clip = StaticAudioManager.GetSound(manageBoard, listName, ref index);
            if (clip == null) return clip;

            StopAllCoroutines();
            StartCoroutine(MusicFader(clip, _volume, pitch));
            return clip;
        }



        private IEnumerator MusicFader(AudioClip audioClip, float volume, float pitch) {

            float _CurrentVolume = startVolume;

            while (_CurrentVolume > 0) {

                _CurrentVolume -= Time.deltaTime * 0.5f;
                audioSource.volume = _CurrentVolume;
                yield return null;
            }

            audioSource.pitch = pitch;
            audioSource.clip = audioClip;
            audioSource.volume = volume;
            audioSource.Play();

            while (_CurrentVolume < volume) {

                _CurrentVolume = Mathf.Clamp(_CurrentVolume + (Time.deltaTime * soundFadeTime), 0, volume);
                audioSource.volume = _CurrentVolume;
                yield return null;
            }
        }

        [System.Serializable]
        public class AudioClipArgumentContainer {

            public string listName = "";
            public int clipIndex = -1;
            public float volume = -1f;
            public float minPitch = 1;
            public float maxPitch = 1;
            public bool stopSounds = false;

            public AudioClipArgumentContainer(string listName = "", int clipIndex = -1, float volume = -1f,
                float minPitch = 1, float maxPitch = 1, bool stopSounds = false) {
                this.listName = listName;
                this.clipIndex = clipIndex;
                this.volume = volume;
                this.minPitch = minPitch;
                this.maxPitch = maxPitch;
                this.stopSounds = stopSounds;
            }

            public string ListName => listName;
            public int ClipIndex => clipIndex;
            public float Volume => volume;
            public float MinPitch => minPitch;
            public float MaxPitch => maxPitch;
            public bool StopSounds => stopSounds;
        }
    }

}
