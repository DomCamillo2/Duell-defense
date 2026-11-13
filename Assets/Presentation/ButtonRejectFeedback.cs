using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    /// <summary>Functional rejection cue over a stable button hit rectangle; never changes its base color.</summary>
    public sealed class ButtonRejectFeedback : MonoBehaviour
    {
        Image overlay;
        float until;
        public int RejectionCount { get; private set; }
        public bool Visible=>overlay!=null&&overlay.gameObject.activeSelf;
        public void Initialize(Button button)
        {
            var rect=UiKit.Rect("Rejected purchase",button.targetGraphic.transform);UiKit.Fill(rect);
            overlay=rect.gameObject.AddComponent<RoundedImage>();overlay.raycastTarget=false;overlay.color=new Color(.9f,.08f,.06f,.38f);overlay.gameObject.SetActive(false);
        }
        public void Play(){if(overlay==null)return;RejectionCount++;until=Time.unscaledTime+.32f;overlay.transform.SetAsLastSibling();overlay.gameObject.SetActive(true);}
        void Update(){if(Visible&&Time.unscaledTime>=until)overlay.gameObject.SetActive(false);}
        public void Clear(){until=0;if(overlay!=null)overlay.gameObject.SetActive(false);}
        void OnDisable()=>Clear();
    }
}
