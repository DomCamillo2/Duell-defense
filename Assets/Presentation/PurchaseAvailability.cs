using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public enum PurchaseState { Available, Unaffordable, Limit, Pending, Inactive, Locked }

    /// <summary>One visual state for the entire purchase card; functional rejection remains independent.</summary>
    public sealed class PurchaseAvailability : MonoBehaviour
    {
        Button button;
        Graphic[] graphics;
        Color[] original;
        Material[] originalMaterials;
        static Material grayscale;
        static int users;
        bool initialized;
        public PurchaseState State { get; private set; }
        public void Initialize(Button owner)
        {
            button=owner;graphics=UiKit.ButtonContent(owner).GetComponentsInChildren<Graphic>(true);
            original=new Color[graphics.Length];for(int i=0;i<graphics.Length;i++)original[i]=graphics[i].color;
            originalMaterials=new Material[graphics.Length];for(int i=0;i<graphics.Length;i++)originalMaterials[i]=graphics[i].material;
            if(grayscale==null)
            {
                var shader=Resources.Load<Shader>("DefenseHudGrayscale");
                if(shader!=null)grayscale=new Material(shader){name="Shared HUD grayscale",hideFlags=HideFlags.HideAndDontSave};
            }
            users++;initialized=true;
            var colors=button.colors;colors.disabledColor=Color.white;button.colors=colors;
        }
        public void Apply(PurchaseState state, Color background)
        {
            if(button==null)return;State=state;bool available=state==PurchaseState.Available;
            button.interactable=available;
            for(int i=0;i<graphics.Length;i++)
            {
                var graphic=graphics[i];if(graphic==null||graphic.name=="Rejected purchase")continue;
                Color color=graphic==button.targetGraphic?background:original[i];
                if(!available){float grey=Mathf.Clamp(color.grayscale,.24f,.82f);color=new Color(grey,grey,grey,color.a);}
                graphic.color=color;
                if(graphic is Image portrait&&portrait.sprite!=null)
                    graphic.material=!available&&grayscale!=null?grayscale:originalMaterials[i];
            }
        }
        void OnDestroy()
        {
            if(!initialized)return;
            if(--users==0&&grayscale!=null){Destroy(grayscale);grayscale=null;}
        }
    }
}
