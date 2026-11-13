using System.Collections.Generic;
using DefenseDuell.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        readonly HashSet<string> dismissedLanLobbies=new HashSet<string>();
        RectTransform discoveryBanner;
        Text discoveryText;
        string discoveryLobbyId;
        void RefreshDiscovery()
        {
            bool eligible=isActiveAndEnabled&&(page=="home"||page=="connect")&&session.Status==ConnectionState.Idle;
            session.LanDiscoveryEnabled=eligible;
            if(!eligible||modal!=null){if(discoveryBanner!=null)discoveryBanner.gameObject.SetActive(false);return;}
            LanLobbyInfo candidate=null;
            foreach(var lobby in session.DiscoveredLanLobbies)if(!dismissedLanLobbies.Contains(lobby.LobbyId)){candidate=lobby;break;}
            if(candidate==null){if(discoveryBanner!=null)discoveryBanner.gameObject.SetActive(false);return;}
            if(discoveryBanner==null)
            {
                float w=Mathf.Min(width-48,820);
                discoveryBanner=BattlePanel(screen,"Nearby LAN lobby",(width-w)*.5f,84,w,86,HudPanel).rectTransform;
                discoveryBanner.GetComponent<Graphic>().raycastTarget=true;
                discoveryText=UiKit.TextAt(discoveryBanner,"",16,8,w-300,68,16,HudText);
                BattleButton(discoveryBanner,"Beitreten",w-276,16,132,54,JoinNearbyLobby,HudAction,17).name="LAN discovery join";
                BattleButton(discoveryBanner,"Später",w-136,16,120,54,()=>{if(discoveryLobbyId!=null){if(dismissedLanLobbies.Count>=64)dismissedLanLobbies.Clear();dismissedLanLobbies.Add(discoveryLobbyId);}RefreshDiscovery();},HudCard,16).name="LAN discovery dismiss";
            }
            discoveryLobbyId=candidate.LobbyId;
            discoveryBanner.gameObject.SetActive(true);discoveryBanner.SetAsLastSibling();
            discoveryText.text="RAUM IM WLAN GEFUNDEN\n"+candidate.MapName+" · "+candidate.FormatName+" · "+candidate.PlayerCount+"/"+candidate.Capacity;
        }
        void JoinNearbyLobby()
        {
            if(modal!=null||UiPointerGate.Active||discoveryLobbyId==null||!(page=="home"||page=="connect")||session.Status!=ConnectionState.Idle)return;
            if(!session.JoinDiscoveredLanLobby(discoveryLobbyId)){notice="Dieser Raum ist nicht mehr verfügbar.";RefreshDiscovery();return;}
            connectionPage="lan";dirty=true;
        }
    }
}
