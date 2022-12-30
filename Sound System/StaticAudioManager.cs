using System.Collections.Generic;
using UnityEngine;
using System;

// Handles all the sound boards and reports error if a board or index of a sound wasn't found

namespace Hood.Audio {
    public static class StaticAudioManager {

        //Stores all the current boards in this static Dictionary
        
        static Dictionary<Board, ScriptableObjectSoundList> boards = 
            new Dictionary<Board, ScriptableObjectSoundList>();

        static StaticAudioManager() {
            UpdateBoardDictionary();
        }
        
        public static Board AddSoundBoard(ScriptableObjectSoundList soundList) {

            foreach (Board soundBoard in Enum.GetValues(typeof(Board))) {
                if (boards.ContainsKey(soundBoard))
                    if (boards[soundBoard] == null) {
                        boards.Remove(soundBoard);
                    } else {
                        continue;
                    }
                boards.Add(soundBoard, soundList);
                return soundBoard;
            }

            throw new Exception("Out of board types! Add more types in 'ScriptableObjectSoundList' Board!");

        }

        /// <summary>
        /// The soundboard
        /// The name of the list
        /// index enter a value to get a specific clip or leave blank if it should return null
        /// </summary>
        public static AudioClip GetSound(Board soundBoard, string listName, ref int? index) {

            AudioClip holder = null;
            if (boards.ContainsKey(soundBoard)) 
                holder = boards[soundBoard].GetClip(listName, ref index);

            //If no clip was found it's replaced by Sound_Error and sends a message that explains what it tried to find
             if(!holder) {

                Debug.Log("Sound could not be found! Tried to get sound from " + 
                    soundBoard.ToString() + " in " + listName);

                foreach (var item in Resources.LoadAll("ErrorReplacer")) {
                    if (item.name == "Sound_Error") { 
                        holder = (AudioClip)item;
                        
                    }
                }
            }

            return holder;
        }

        //Finds all ScriptableObjectSoundList in the Resources\SoundBoardsfilters out any empty boards.
        //If the Dictionary already contains the key and value a error is thrown.
        public static void UpdateBoardDictionary() {

            foreach (var board in new Dictionary<Board, ScriptableObjectSoundList>(boards)) {

                if (board.Value == null || (boards[board.Key].GetBoardType != board.Key)) 
                    boards.Remove(board.Key);
                
            }

            foreach (var item in Resources.LoadAll("SoundBoards")) {
                if (!(item is ScriptableObjectSoundList)) return;

                ScriptableObjectSoundList _SOSB = (ScriptableObjectSoundList)item;

                if (boards.ContainsKey(_SOSB.GetBoardType)) {
                    if(boards[_SOSB.GetBoardType] == _SOSB) continue;

                    _SOSB.SetIsAddedToList(false);

                    throw new Exception("The list already contains a board of [Board." + 
                        _SOSB.GetBoardType.ToString() + "]" + "\n Make sure only one type of board exists at a time");
                    
                }

                boards.Add(_SOSB.GetBoardType, _SOSB);
                _SOSB.SetIsAddedToList(true);
            }  
        }
    }
}

