using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegend.Arg
{
    [Serializable] public class ArgPageLink
    {
        public Button button;
        public string target;
    }

    // These are authored scene objects, not layouts recreated by the runtime.
    public sealed class ArgPageView : MonoBehaviour
    {
        public string pageId;
        public string address, source;
        public string[] searchTerms;
        public bool restricted;
        public TMP_Text titleText, bodyText;
        public ArgPageLink[] links = Array.Empty<ArgPageLink>();
        public Button photoButton;
        public string imageId;
        [Header("Optional login / lookup form")]
        public TMP_InputField userId, password, recordNumber;
        public Button submit;
        public TMP_Text feedback;

        public WebPage Definition() => new WebPage
        {
            id = pageId, address = address, source = source, terms = searchTerms,
            title = titleText.text, body = bodyText.text, restricted = restricted, image = imageId,
            links = links.Select(link => new PageLink { label = link.button.GetComponentInChildren<TMP_Text>().text, target = link.target }).ToArray()
        };
    }
}
