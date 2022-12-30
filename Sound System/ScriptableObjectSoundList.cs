using System.Collections.Generic;
using UnityEngine;
using System;


// Handles a list of sounds of a assigned board type

namespace Hood.Audio {

    public enum Board {
        Door,
        Guard,
        PlayerCharacter,
        Pickup,
        Music,
        Game,
        QuestGiver,
        SuperGuard,
        OtherGuard,
        MaleQuestGiver,
        OtherMaleQuesrGiver
    }

    [CreateAssetMenu(fileName = "Sound board", menuName = "Sound Board/Board", order = 0)]
public class ScriptableObjectSoundList : ScriptableObject
{
        [Tooltip("When changing this value make sure to update the list. \n" +
            "Press the Button below to update")]
        [SerializeField] Board myBoard;
         
        [SerializeField] List<AudioListWrapper> myAudioList = new List<AudioListWrapper>();
        bool isAddedToList = false;


        /// <summary>
        /// The name of the list
        /// index enter a value to get a specific clip or leave blank if it should return null
        /// </summary>
        /// <param name="listName">Name of audio clip list</param>
        /// <param name="index">Audio clip index in audio clip list</param>
        /// <returns></returns>
        public AudioClip GetClip(string listName, ref int? index) {

            AudioClip holder = null;
            foreach (var audioList in myAudioList) {
                if (audioList.GetName == listName) {
                    if (!index.HasValue || index.Value < 0 || index.Value >= audioList.GetAudioClip.Length) 
                        index = UnityEngine.Random.Range(0, audioList.GetAudioClip.Length);

                    holder = audioList.GetAudioClip[(int)index.Value];
                    
                    break;
                }
            }

            return holder;
        }

        public Board GetBoardType => myBoard;
        public bool GetIsAddedToList => isAddedToList;
        public void SetIsAddedToList(bool isTrue) => isAddedToList = isTrue;

        [Serializable]
        public class AudioListWrapper {

            [SerializeField] string name;
            [SerializeField] AudioClip[] audioClips;
            

            public AudioClip[] GetAudioClip => audioClips;
            public string GetName => name;
        }



    }
}