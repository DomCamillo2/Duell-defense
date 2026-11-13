using System;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
#endif
namespace DefenseDuell.Networking
{
    /// <summary>Held only while app-visible LAN discovery is active; never a wake lock or OS notification.</summary>
    internal sealed class AndroidLanMulticastLease : IDisposable
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject lease;
#endif
        public void SetActive(bool active)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!active){Dispose();return;}
            if(lease!=null)return;
            try
            {
                using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
                using var context=activity.Call<AndroidJavaObject>("getApplicationContext");
                using var wifi=context.Call<AndroidJavaObject>("getSystemService","wifi");
                lease=wifi.Call<AndroidJavaObject>("createMulticastLock","DefenseDuell-LAN");
                lease.Call("setReferenceCounted",false);lease.Call("acquire");
            }
            catch(AndroidJavaException){Dispose();}
#endif
        }
        public void Dispose()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(lease==null)return;
            try{if(lease.Call<bool>("isHeld"))lease.Call("release");}catch(AndroidJavaException){}
            lease.Dispose();lease=null;
#endif
        }
    }
}
