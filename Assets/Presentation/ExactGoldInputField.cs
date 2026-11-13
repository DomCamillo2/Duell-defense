using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    /// <summary>Exact decimal backing with a bounded text mesh. Long values are replaced atomically,
    /// copied in full, or explicitly cleared for a new draft; they are never truncated for display.</summary>
    public sealed class ExactGoldInputField : InputField
    {
        public const int InlineCharacterLimit=1024;
        public int MaximumInputCharacters=196569;
        public Action<string> RejectedInput;
        public event Action<string> ExactValueChanged;
        string exactValue="";
        bool permissionReadOnly,synchronizing,observedFocus;
        string focusExactValue="";
        public string ExactValue=>exactValue;
        public bool IsSummarized=>exactValue.Length>InlineCharacterLimit;
        // The semantic permission and UGUI's temporary summary lock are deliberately separate.
        public new bool readOnly
        {
            get=>permissionReadOnly;
            set{permissionReadOnly=value; base.readOnly=value||IsSummarized;}
        }
        protected override void OnEnable()
        {base.OnEnable();onValueChanged.AddListener(ReadVisibleEdit);}
        protected override void OnDisable()
        {onValueChanged.RemoveListener(ReadVisibleEdit);base.OnDisable();}
        static bool IsDigits(string value)
        {
            if(value==null)return false;
            foreach(char c in value)if(c<'0'||c>'9')return false;
            return true;
        }
        bool IsValidInput(string value)=>value!=null&&value.Length<=MaximumInputCharacters&&IsDigits(value);
        static string VisibleValue(string value)=>value.Length<=InlineCharacterLimit?value:
            value.Substring(0,10)+"…"+value.Substring(value.Length-8)+" ("+value.Length+" Stellen)";
        void EndOldEdit()
        {
            bool wasSynchronizing=synchronizing;synchronizing=true;
            try{if(isFocused)DeactivateInputField();observedFocus=false;}
            finally{synchronizing=wasSynchronizing;}
        }
        void SetExact(string value,bool notify)
        {
            bool changed=exactValue!=value;
            // Do not let the native keyboard continue writing into a summary text field.
            if(value.Length>InlineCharacterLimit&&isFocused)EndOldEdit();
            exactValue=value;bool wasSynchronizing=synchronizing;synchronizing=true;
            try{base.readOnly=permissionReadOnly||IsSummarized;base.SetTextWithoutNotify(VisibleValue(value));}
            finally{synchronizing=wasSynchronizing;}
            if(changed&&notify)ExactValueChanged?.Invoke(value);
        }
        public void SetExactValueWithoutNotify(string value)
        {
            if(!IsValidInput(value))throw new ArgumentException("Exact decimal exceeds negotiated input limits or contains non-digits.",nameof(value));
            if(exactValue!=value)EndOldEdit();
            SetExact(value,false);
        }
        public bool TryReplaceExactValue(string candidate)
        {
            if(permissionReadOnly)return false;
            if(!IsValidInput(candidate))
            {RejectedInput?.Invoke("Ungültige Goldzahl. Der Betrag wurde nicht geändert.");return false;}
            SetExact(candidate,true);return true;
        }
        public bool BeginReplacement()
        {
            if(permissionReadOnly)return false;
            EndOldEdit();SetExact("",true);ActivateInputField();return true;
        }
        public void CopyExactValue()=>GUIUtility.systemCopyBuffer=exactValue;
        void ReadVisibleEdit(string value)
        {
            if(synchronizing)return;
            if(permissionReadOnly||IsSummarized){SetExact(exactValue,false);return;}
            if(!IsValidInput(value))
            {SetExact(exactValue,false);RejectedInput?.Invoke("Ungültige Goldzahl. Der Betrag wurde nicht geändert.");return;}
            SetExact(value,true);
        }
        protected override void LateUpdate()
        {
            // Android/iOS keyboard updates bypass Append. Inspect the entire candidate before
            // base UGUI can strip a newline, run its per-character loop, or expose a giant mesh.
            if(isFocused&&!observedFocus){focusExactValue=exactValue;observedFocus=true;}
            if(!isFocused)observedFocus=false;
            if(isFocused&&m_Keyboard!=null&&m_Keyboard.status==TouchScreenKeyboard.Status.Canceled)
            {
                // UGUI normally restores m_OriginalText on cancel. Do this before its native
                // text setter can momentarily publish an unconfirmed long keyboard candidate.
                string restore=focusExactValue;EndOldEdit();SetExact(restore,!permissionReadOnly);return;
            }
            if(isFocused&&m_Keyboard!=null&&!permissionReadOnly&&m_Keyboard.text!=base.text)
            {
                string candidate=m_Keyboard.text;
                if(!IsValidInput(candidate))
                {
                    m_Keyboard.text=base.text;
                    RejectedInput?.Invoke("Ungültige Goldzahl. Der Betrag wurde nicht geändert.");
                }
                else if(candidate.Length>InlineCharacterLimit)
                {
                    SetExact(candidate,true); // Closes the old keyboard before constructing the summary.
                    return;
                }
            }
            base.LateUpdate();
            if(!isFocused)observedFocus=false;
        }
        protected override void Append(char input)=>Append(input.ToString());
        protected override void Append(string input)
        {
            if(permissionReadOnly||string.IsNullOrEmpty(input))return;
            if(!IsDigits(input))
            {RejectedInput?.Invoke("Nur ganze Zahlen eingeben. Der Betrag wurde nicht geändert.");return;}
            if(IsSummarized){TryReplaceExactValue(input);return;}
            int start=Math.Max(0,Math.Min(exactValue.Length,Math.Min(selectionAnchorPosition,selectionFocusPosition)));
            int end=Math.Max(start,Math.Min(exactValue.Length,Math.Max(selectionAnchorPosition,selectionFocusPosition)));
            if((long)exactValue.Length-(end-start)+input.Length>MaximumInputCharacters)
            {RejectedInput?.Invoke("Diese Goldzahl ist zu groß. Der Betrag wurde nicht geändert.");return;}
            string candidate=exactValue.Remove(start,end-start).Insert(start,input);
            SetExact(candidate,true);
            if(!IsSummarized)caretPosition=start+input.Length;
        }
    }
}
