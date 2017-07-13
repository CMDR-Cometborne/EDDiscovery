﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conditions;
using AudioExtensions;

namespace ActionLanguage
{
    public class ActionCoreController
    {
        protected ActionFileList actionfiles;
        protected ActionRun actionrunasync;

        protected ConditionVariables programrunglobalvariables;         // program run, lost at power off, set by GLOBAL or internal 
        protected ConditionVariables persistentglobalvariables;   // user variables, set by user only, including user setting vars like SpeechVolume
        protected ConditionVariables globalvariables;                  // combo of above.

        public ConditionVariables Globals { get { return globalvariables; } }

        protected AudioQueue audiospeech;
        protected AudioQueue audiowave;
        protected SpeechSynthesizer synth;
        protected System.Windows.Forms.Form form;

        public AudioQueue AudioQueueSpeech { get { return audiospeech; } }
        public AudioQueue AudioQueueWave { get { return audiowave; } }
        public SpeechSynthesizer SpeechSynthesizer { get { return synth; } }
        public System.Windows.Forms.Form Form { get { return form; } }

        public ActionCoreController( AudioQueue speech , AudioQueue wave , SpeechSynthesizer synt , System.Windows.Forms.Form frm )
        {
            audiospeech = speech;
            audiowave = wave;
            synth = synt;
            form = frm;

            persistentglobalvariables = new ConditionVariables();
            globalvariables = new ConditionVariables(persistentglobalvariables);        // copy existing user ones into to shared buffer..
            programrunglobalvariables = new ConditionVariables();

            SetInternalGlobal("CurrentCulture", System.Threading.Thread.CurrentThread.CurrentCulture.Name);
            SetInternalGlobal("CurrentCultureInEnglish", System.Threading.Thread.CurrentThread.CurrentCulture.EnglishName);
            SetInternalGlobal("CurrentCultureISO", System.Threading.Thread.CurrentThread.CurrentCulture.ThreeLetterISOLanguageName);

            ActionBase.AddCommand("Break", typeof(ActionBreak), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Call", typeof(ActionCall), ActionBase.ActionType.Call);
            ActionBase.AddCommand("Dialog", typeof(ActionDialog), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("DialogControl", typeof(ActionDialogControl), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Do", typeof(ActionDo), ActionBase.ActionType.Do);
            ActionBase.AddCommand("DeleteVariable", typeof(ActionDeleteVariable), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Expr", typeof(ActionExpr), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Else", typeof(ActionElse), ActionBase.ActionType.Else);
            ActionBase.AddCommand("ElseIf", typeof(ActionElseIf), ActionBase.ActionType.ElseIf);
            ActionBase.AddCommand("End", typeof(ActionEnd), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("ErrorIf", typeof(ActionErrorIf), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("FileDialog", typeof(ActionFileDialog), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("GlobalLet", typeof(ActionGlobalLet), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Global", typeof(ActionGlobal), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("If", typeof(ActionIf), ActionBase.ActionType.If);
            ActionBase.AddCommand("InputBox", typeof(ActionInputBox), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("InfoBox", typeof(ActionInfoBox), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("MessageBox", typeof(ActionMessageBox), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("NonModalDialog", typeof(ActionNonModalDialog), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Return", typeof(ActionReturn), ActionBase.ActionType.Return);
            ActionBase.AddCommand("Pragma", typeof(ActionPragma), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Let", typeof(ActionLet), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Loop", typeof(ActionLoop), ActionBase.ActionType.Loop);
            ActionBase.AddCommand("Rem", typeof(ActionRem), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("PersistentGlobal", typeof(ActionPersistentGlobal), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Print", typeof(ActionPrint), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Say", typeof(ActionSay), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Set", typeof(ActionSet), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Static", typeof(ActionStatic), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Sleep", typeof(ActionSleep), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("While", typeof(ActionWhile), ActionBase.ActionType.While);
            ActionBase.AddCommand("//", typeof(ActionFullLineComment), ActionBase.ActionType.Cmd);
            ActionBase.AddCommand("Else If", typeof(ActionElseIf), ActionBase.ActionType.ElseIf);
        }

        public void SetPeristentGlobal(string name, string value)     // saved on exit
        {
            persistentglobalvariables[name] = globalvariables[name] = value;
        }

        public void SetInternalGlobal(string name, string value)           // internal program vars
        {
            programrunglobalvariables[name] = globalvariables[name] = value;
        }

        public void SetNonPersistentGlobal(string name, string value)         // different name for identification purposes, for sets
        {
            programrunglobalvariables[name] = globalvariables[name] = value;
        }

        public void DeleteVariable(string name)
        {
            programrunglobalvariables.Delete(name);
            persistentglobalvariables.Delete(name);
            globalvariables.Delete(name);
        }

        public void TerminateAll()
        {
            actionrunasync.TerminateAll();
        }

        public virtual void LogLine(string s)
        { }

        public virtual int ActionRun(string triggername, string triggertype, ConditionVariables additionalvars = null,
                                string flagstart = null, bool now = false)
        { return 0; }

        public virtual bool Pragma(string s)    // extend pragmas
        {
            return false;
        }
    }
}
