using UnityEngine;

namespace Hood.Interact {
    public interface Interactiveble {
        /// <summary>
        /// When an Character wish to interact with it
        /// </summary>
        /// <param name="user"></param>
        public void Interact(GameObject user);

        /// <summary>
        /// The text InteractUI will show the text to the player
        /// </summary>
        /// <returns></returns>
        public string GetDisplayText();
    }

}

