using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DefenseDuell.Presentation
{
    // One compact rules card bound only to confirmed lobby state.
    // Place under the existing safe-area screen; x/y and width are logical canvas units.
    internal sealed class HostLobbyRulesPanel
    {
        readonly LobbySettingsEditor editor;
        readonly Action<ApplyLobbyOptions> apply;
        readonly Action<ReadyForLobby> ready;
        readonly Func<bool> interactionAllowed;
        bool Allowed => interactionAllowed == null || interactionAllowed();
        readonly Text role, feedback, actionLabel;
        readonly InputField waves;
        readonly Button minus, plus, action, discard;
        readonly Button copyGold, pasteGold, replaceGold;
        readonly ExactGoldInputField gold;
        bool suppressInput;
        readonly Slider waveSlider;
        readonly Button endless;
        readonly Button[] incomeButtons;

        internal HostLobbyRulesPanel(Transform parent, float x, float y, float width,
            LobbySettingsEditor editor, Action<ApplyLobbyOptions> apply, Action<ReadyForLobby> ready, Func<bool> interactionAllowed = null)
        {
            this.editor=editor;this.apply=apply;this.ready=ready;this.interactionAllowed=interactionAllowed;
            var card=HudTheme.PanelAt(parent,"Lobby rules",x,y,width,340,HudTheme.Card,HudFrameStyle.Card).transform;
            UiKit.TextAt(card,"SPIELREGELN",16,10,width-180,28,18,HudTheme.Gold).fontStyle=FontStyle.Bold;
            role=UiKit.TextAt(card,"",width-188,12,172,24,12,HudTheme.Muted,TextAnchor.MiddleRight);
            UiKit.TextAt(card,"Wellen",16,52,108,44,19,HudTheme.Text);
            float controls=width-232;
            minus=HudTheme.ButtonAt(card,"−",controls,48,44,48,()=>{if(Allowed)editor.StepWaves(-1);Refresh();},HudTheme.Panel,24);minus.name="Lobby waves decrease";
            var waveBackground=UiKit.PanelAt(card,"Lobby wave count",controls+50,48,110,48,HudTheme.Panel);waveBackground.raycastTarget=true;
            waves=waveBackground.gameObject.AddComponent<InputField>();waves.textComponent=UiKit.Label(waveBackground.transform,"",24,HudTheme.Text,TextAnchor.MiddleCenter);UiKit.Fill(waves.textComponent.rectTransform,8,5,8,5);
            waves.contentType=InputField.ContentType.Standard;waves.keyboardType=TouchScreenKeyboardType.NumberPad;waves.customCaretColor=true;waves.caretColor=HudTheme.Gold;
            waves.onValueChanged.AddListener(value=>{if(suppressInput)return;bool parsed=int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out int count);bool accepted=Allowed&&editor.Edit(parsed?count:0,editor.DraftGold);Refresh(!accepted,!accepted);});
            plus=HudTheme.ButtonAt(card,"+",controls+166,48,50,48,()=>{if(Allowed)editor.StepWaves(1);Refresh();},HudTheme.Panel,24);plus.name="Lobby waves increase";
            waveSlider=UiKit.SliderAt(card,"Lobby waves slider",16,100,width-164,44,1,200,v=>{if(Allowed&&editor.CanEdit){editor.Edit(v,editor.DraftGold);Refresh();}});
            endless=HudTheme.ButtonAt(card,"",width-140,100,124,44,()=>{if(Allowed)editor.EditRules(!editor.DraftEndless,editor.DraftIncomeMultiplier,editor.DraftFormat,editor.DraftMapId);Refresh();},HudTheme.Panel,14);endless.name="Lobby endless";
            UiKit.TextAt(card,"Startgold",16,156,100,42,18,HudTheme.Text);
            var inputBackground=UiKit.PanelAt(card,"Lobby start gold",128,152,width-144,48,HudTheme.Panel);inputBackground.raycastTarget=true;
            gold=inputBackground.gameObject.AddComponent<ExactGoldInputField>();gold.textComponent=UiKit.Label(inputBackground.transform,"",20,HudTheme.Text);UiKit.Fill(gold.textComponent.rectTransform,12,5,12,5);
            gold.name="Lobby start gold";gold.contentType=InputField.ContentType.Standard;gold.keyboardType=TouchScreenKeyboardType.NumberPad;gold.customCaretColor=true;gold.caretColor=HudTheme.Gold;
            gold.ExactValueChanged+=value=>{if(suppressInput)return;bool accepted=Allowed&&editor.Edit(editor.DraftWaves,value);Refresh(!accepted);};
            copyGold=HudTheme.ButtonAt(card,"Kopie",128,204,68,44,()=>{if(Allowed)gold.CopyExactValue();},HudTheme.Panel,12);copyGold.name="Lobby copy gold";
            pasteGold=HudTheme.ButtonAt(card,"Einfügen",202,204,72,44,()=>{if(Allowed&&editor.CanEdit)gold.TryReplaceExactValue(GUIUtility.systemCopyBuffer);},HudTheme.Panel,12);pasteGold.name="Lobby paste gold";
            replaceGold=HudTheme.ButtonAt(card,"Neu",280,204,60,44,()=>{if(Allowed&&editor.CanEdit){gold.BeginReplacement();gold.ActivateInputField();}},HudTheme.Panel,12);replaceGold.name="Lobby replace gold";
            UiKit.TextAt(card,"LAUFENDES EINKOMMEN",16,254,width-32,22,12,HudTheme.Muted);
            int[] factors={1,2,3,5,10};incomeButtons=new Button[factors.Length];float fw=(width-48)/5;
            for(int i=0;i<factors.Length;i++){int factor=factors[i];incomeButtons[i]=HudTheme.ButtonAt(card,factor+"×",16+i*(fw+4),282,fw,44,()=>{if(Allowed)editor.EditRules(editor.DraftEndless,factor,editor.DraftFormat,editor.DraftMapId);Refresh();},HudTheme.Panel,18);incomeButtons[i].name="Lobby income "+factor;}
            feedback=UiKit.TextAt(parent,"",x,y+346,width,36,14,HudTheme.Muted);feedback.name="Lobby rules feedback";
            gold.RejectedInput=message=>{feedback.text=message;feedback.color=HudTheme.Gold;};
            action=HudTheme.ButtonAt(parent,"",x,y+390,width-104,44,Activate,HudTheme.Action,18);action.name="Lobby rules primary";actionLabel=action.GetComponentInChildren<Text>();
            discard=HudTheme.ButtonAt(parent,"Zurück",x+width-96,y+390,96,44,()=>{if(Allowed)editor.Discard();Refresh();},HudTheme.Panel,15);discard.name="Lobby rules discard";
            Refresh();
        }

        internal void Receive(LobbySettingsView view)
        {
            var old = editor.Confirmed;
            bool resetsInput = old == null || old.RoomEpoch != view.RoomEpoch || old.ConfigRevision != view.ConfigRevision || old.CanEdit && !view.CanEdit;
            if (!editor.Receive(view)) return;
            if (resetsInput && gold.isFocused)
            {
                // End the old edit session before replacing its value. Otherwise Escape can
                // restore InputField's private original text from the preceding revision.
                suppressInput = true;
                gold.DeactivateInputField();
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gold.gameObject)
                    EventSystem.current.SetSelectedGameObject(null);
                suppressInput = false;
            }
            Refresh();
        }
        internal void Rejected(string epoch, Guid requestId, string message)
        { if (editor.Reject(epoch, requestId, message)) Refresh(); }
        internal void ReadyRejected(string epoch, Guid requestId, string message)
        { if (editor.RejectReady(epoch, requestId, message)) Refresh(); }
        void Activate()
        {
            if (!Allowed) return;
            if (editor.IsDirty)
            {
                var request = editor.Apply(); Refresh(); if (request != null) apply?.Invoke(request);
            }
            else { var request = editor.Ready(); Refresh(); if (request != null) ready?.Invoke(request); }
        }
        internal void Refresh(bool updateInput = true, bool updateWaves = true)
        {
            var view = editor.Confirmed;
            role.text = view == null ? "Verbindung …" : view.CanEdit ? "DU BIST HOST" : "VOM HOST FESTGELEGT";
            waves.readOnly=!editor.CanEdit||editor.DraftEndless;
            waveSlider.interactable=editor.CanEdit&&!editor.DraftEndless;waveSlider.SetValueWithoutNotify(editor.DraftWaves);
            endless.interactable=editor.CanEdit;UiKit.ButtonText(endless,editor.DraftEndless?"∞ ENDLOS ✓":"∞ Endlos");UiKit.ButtonColor(endless,editor.DraftEndless?HudTheme.Selected:HudTheme.Panel);
            int[] factors={1,2,3,5,10};for(int i=0;i<incomeButtons.Length;i++){incomeButtons[i].interactable=editor.CanEdit;UiKit.ButtonColor(incomeButtons[i],editor.DraftIncomeMultiplier==factors[i]?HudTheme.Selected:HudTheme.Panel);}
            if (updateWaves && !waves.isFocused)
                waves.SetTextWithoutNotify(view == null ? "" : editor.DraftWaves.ToString(CultureInfo.InvariantCulture));
            minus.gameObject.SetActive(view != null && view.CanEdit);
            plus.gameObject.SetActive(view != null && view.CanEdit);
            minus.interactable = editor.CanEdit && !editor.DraftEndless && editor.DraftWaves > view.Capabilities.MinimumWaves;
            plus.interactable = editor.CanEdit && !editor.DraftEndless && editor.DraftWaves < view.Capabilities.MaximumWaves;
            // Keep read-only amounts selectable/scrollable so very large exact amounts remain inspectable.
            gold.readOnly = !editor.CanEdit;
            gold.characterLimit = 0; // No silent truncation: editor rejects the negotiated payload limit.
            gold.MaximumInputCharacters = view == null ? 1 : view.Capabilities.MaximumGoldDigits;
            if (updateInput && gold.ExactValue != editor.DraftGold) gold.SetExactValueWithoutNotify(editor.DraftGold);
            copyGold.gameObject.SetActive(gold.IsSummarized);
            pasteGold.gameObject.SetActive(gold.IsSummarized && view != null && view.CanEdit);
            replaceGold.gameObject.SetActive(gold.IsSummarized && view != null && view.CanEdit);
            pasteGold.interactable = replaceGold.interactable = editor.CanEdit;
            feedback.text = editor.Error.Length > 0 ? editor.Error : editor.Notice.Length > 0 ? editor.Notice :
                editor.IsDirty ? "Änderungen übernehmen. Danach melden sich alle erneut bereit." : "Sobald alle bereit sind, startet die Partie.";
            feedback.color = editor.Error.Length > 0 ? HudTheme.Gold : HudTheme.Muted;
            actionLabel.text = editor.Pending != null || editor.PendingReady != null ? "Wird bestätigt …" : editor.IsDirty ? "REGELN ÜBERNEHMEN" :
                view != null && view.IsReady ? "BEREIT ✓" : "Ich bin bereit";
            action.interactable = (editor.CanApply || editor.CanReady)&&Allowed;
            discard.gameObject.SetActive(editor.IsDirty && editor.Pending == null && editor.CanEdit);
        }
    }
}
