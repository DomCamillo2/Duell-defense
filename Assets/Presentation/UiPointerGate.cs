using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    /// <summary>Only one construction gesture can own input. Another finger cannot buy or change its class.</summary>
    internal static class UiPointerGate
    {
        static object owner;
        static int pointer;
        static object scopeOwner;
        static Transform scope, alternateScope;
        public static void SetScope(object value,Transform root,Transform alternate=null)
        {scopeOwner=value;scope=root;alternateScope=alternate;}
        public static void ClearScope(object value)
        {if(!ReferenceEquals(scopeOwner,value))return;scopeOwner=null;scope=alternateScope=null;}
        public static bool AllowsTarget(Component target)=>scope==null||target.transform.IsChildOf(scope)
            ||(alternateScope!=null&&target.transform.IsChildOf(alternateScope));
        public static void RestrictNavigation(Transform root)
        {
            var all=root.GetComponentsInChildren<Selectable>();
            var active=new System.Collections.Generic.List<Selectable>(all.Length);
            foreach(var item in all)if(item.IsActive()&&item.IsInteractable())active.Add(item);
            for(int i=0;i<active.Count;i++)
            {
                var previous=active[(i+active.Count-1)%active.Count];var next=active[(i+1)%active.Count];
                active[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=previous,selectOnDown=next,selectOnLeft=previous,selectOnRight=next};
            }
        }
        public static bool SuppressClick { get; set; }
        public static bool Active=>owner!=null;
        public static bool Allows(int id)=>owner==null||(pointer==id&&!SuppressClick);
        public static bool IsOwner(object value,int id)=>ReferenceEquals(owner,value)&&pointer==id;
        public static bool OwnedBy(object value)=>ReferenceEquals(owner,value);
        public static bool Claim(object value,int id)
        {if(owner!=null)return false;owner=value;pointer=id;SuppressClick=false;return true;}
        public static void Release(object value){if(!ReferenceEquals(owner,value))return;owner=null;SuppressClick=false;pointer=0;}
    }
}
