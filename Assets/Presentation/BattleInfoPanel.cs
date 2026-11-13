using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    /// <summary>The shield stays in the canvas so its native raycast depth exists before opening.</summary>
    public sealed class BattleInfoPanel : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        RectTransform panel;
        CanvasGroup visuals;
        Graphic shield;
        Vector2 resting;
        float progress, from, target, began, duration;
        bool reduced,blocking;
        Transform navigationRoot;
        GameObject previousFocus;
        public void SetNavigationRoot(Transform value){navigationRoot=value;}
        public bool Blocking => blocking;
        public bool Open { get; private set; }
        public float Progress => progress;
        public void Initialize(RectTransform content)
        {
            panel=content;resting=panel.anchoredPosition;visuals=panel.gameObject.AddComponent<CanvasGroup>();shield=GetComponent<Graphic>();
            CloseImmediately();
        }
        public void SetReducedEffects(bool value)
        {
            reduced=value;if(value){duration=0;progress=target;blocking=Open;Apply();}
        }
        public void Show(Vector2 position)
        {
            if(!Open&&EventSystem.current!=null)previousFocus=EventSystem.current.currentSelectedGameObject;
            resting=position;blocking=true;Open=true;Apply();Animate(1,.18f);
            UiPointerGate.RestrictNavigation(panel);
            if(EventSystem.current!=null)
            {
                var first=panel.GetComponentInChildren<Button>();
                EventSystem.current.SetSelectedGameObject(first!=null?first.gameObject:null);
            }
        }
        public void Close(){if(!Blocking)return;Open=false;Animate(0,.13f);}
        public void CloseImmediately()
        {Open=blocking=false;progress=from=target=duration=0;Apply();}
        void Animate(float value,float seconds)
        {
            from=progress;target=value;began=Time.unscaledTime;duration=reduced?0:seconds;
            if(reduced){progress=target;blocking=Open;Apply();}
        }
        void Update()
        {
            if(duration<=0)return;float t=Mathf.Clamp01((Time.unscaledTime-began)/duration);
            progress=Mathf.Lerp(from,target,t*t*(3-2*t));
            if(t>=1){duration=0;blocking=Open;}Apply();
        }
        void Apply()
        {
            if(panel==null)return;panel.anchoredPosition=resting+new Vector2((1-progress)*12,0);
            visuals.alpha=progress;visuals.blocksRaycasts=blocking;visuals.interactable=blocking;
            shield.raycastTarget=blocking;shield.color=new Color(0,0,0,blocking?.08f:0);
            if(blocking)UiPointerGate.SetScope(this,transform,navigationRoot);
            else
            {
                UiPointerGate.ClearScope(this);
                if(EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject!=null
                    &&EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
                    EventSystem.current.SetSelectedGameObject(previousFocus!=null&&previousFocus.activeInHierarchy?previousFocus:null);
                previousFocus=null;
            }
        }
        public void OnPointerDown(PointerEventData e){}
        public void OnPointerClick(PointerEventData e)
        {if(e.button==PointerEventData.InputButton.Left)Close();}
        void OnDisable(){Open=blocking=false;progress=from=target=duration=0;Apply();}
        void OnDestroy(){UiPointerGate.ClearScope(this);}
    }
    /// <summary>Details consume taps instead of bubbling to the outside-dismiss surface.</summary>
    public sealed class BattleInfoSurface : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        public void OnPointerDown(PointerEventData e){}
        public void OnPointerClick(PointerEventData e){}
    }
}
