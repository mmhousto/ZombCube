using UnityEngine;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{
    /// <summary>Shows a random loading screen image each time the loading screen appears.</summary>
    [RequireComponent(typeof(Image))]
    public class RandomLoadingImage : MonoBehaviour
    {
        public Sprite[] images;

        private void OnEnable()
        {
            if (images == null || images.Length == 0) return;
            Sprite pick = images[Random.Range(0, images.Length)];
            if (pick != null) GetComponent<Image>().sprite = pick;
        }
    }
}
