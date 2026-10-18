using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegend.Arg
{
    public sealed class ArgPhotoView : MonoBehaviour
    {
        public string imageId;
        public string title;
        public string caption;
        [Tooltip("Assign the final photo here. Leave empty for the paper prototype.")]
        public RawImage photograph;
        [Tooltip("Disable when exact clue text is already baked into the photo.")]
        public GameObject clueOverlay;
    }
}
