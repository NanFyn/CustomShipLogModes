﻿using System;
using System.Collections.Generic;
using System.Linq;
using CustomShipLogModes.API;
using UnityEngine;

namespace CustomShipLogModes;

public class ModeSelectorMode : ShipLogMode
{
    public ItemListWrapper itemList;

    private List<Tuple<ShipLogMode,string>> _modes = new();

    private ScreenPromptList _upperRightPromptList;
    private OWAudioSource _oneShotSource;

    private ScreenPrompt _closePrompt;
    private ScreenPrompt _selectPrompt;
    
    private string _prevEntryId;
    private ShipLogMode _goBackMode;

    private void UpdateAvailableModes()
    {
        List<Tuple<ShipLogMode,string>> modes = CustomShipLogModes.Instance.GetAvailableNamedModes(); 
        if (!modes.SequenceEqual(_modes))
        {
            _modes = modes;

            List<Tuple<string, bool, bool, bool>> items = new();
            for (var i = 0; i < _modes.Count; i++)
            {
                items.Add(new Tuple<string, bool, bool, bool>(GetModeName(i), false, false, false));
            }
            itemList.SetItems(items);
        }
    }

    private string GetModeName(int i)
    {
        return _modes[i].Item2;
    }

    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        _upperRightPromptList = upperRightPromptList;
        _oneShotSource = oneShotSource;
        
        itemList.SetName(GetName());

        SetupPrompts();
    }

    public static string GetName()
    {
        return TranslationHandler.GetTranslation("SELECT_MODE");
    }

    private void SetupPrompts()
    {
        // The text is updated
        _closePrompt = new ScreenPrompt(Input.PromptCommands(Input.Action.CloseModeSelector), "");
        _selectPrompt = new ScreenPrompt(Input.PromptCommands(Input.Action.SelectMode), "");
    }

    private void UpdatePromptsVisibility()
    {
        int goBackFind = GoBackModeIndex();
        bool canGoBack = goBackFind != -1;
        _closePrompt.SetVisibility(canGoBack);
        if (canGoBack)
        {
            _closePrompt.SetText(TranslationHandler.GetTranslation("GO_BACK").Replace("{0}", GetModeName(goBackFind)));
        }

        _selectPrompt.SetVisibility(true); // This is always possible I guess?
        _selectPrompt.SetText(TranslationHandler.GetTranslation("SELECT").Replace("{0}", GetModeName(itemList.GetSelectedIndex())));
    }

    // TODO: Review removed modes, etc. Index still working?
    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        itemList.Open();

        // Yes, I'm using this sound for this, but it actually sounds similar to the vanilla modes enter sounds
        _oneShotSource.PlayOneShot(AudioType.Ghost_Laugh);
        _prevEntryId = entryID;

        UpdateAvailableModes();
        int goBackIndex = GoBackModeIndex();
        if (goBackIndex != -1)
        {
            itemList.SetSelectedIndex(goBackIndex);
        }

        UpdatePromptsVisibility(); // Just in case?

        PromptManager promptManager = Locator.GetPromptManager();
        promptManager.AddScreenPrompt(_closePrompt, _upperRightPromptList, TextAnchor.MiddleRight);
        promptManager.AddScreenPrompt(_selectPrompt, _upperRightPromptList, TextAnchor.MiddleRight);
    }

    private int GoBackModeIndex()
    {
        return _modes.FindIndex(m => m.Item1 == _goBackMode);
    }

    public override void ExitMode()
    {
        itemList.Close();

        PromptManager promptManager = Locator.GetPromptManager();
        promptManager.RemoveScreenPrompt(_closePrompt);
        promptManager.RemoveScreenPrompt(_selectPrompt);
    }

    public override void UpdateMode()
    {
        itemList.UpdateList();
        
        // Just in case a mode was disabled/added/renamed, do we really need to check this now?
        UpdateAvailableModes();

        UpdatePromptsVisibility();
        if (_closePrompt._isVisible && Input.IsNewlyPressed(Input.Action.CloseModeSelector))
        {
            CustomShipLogModes.Instance.RequestChangeMode(_goBackMode); // It could be inactive but ok
            return;
        }
        if (Input.IsNewlyPressed(Input.Action.SelectMode))
        {
            CustomShipLogModes.Instance.RequestChangeMode(_modes[itemList.GetSelectedIndex()].Item1);
        }
    }

    public void SetGoBackMode(ShipLogMode mode)
    {
        _goBackMode = mode;
    }

    public override void OnEnterComputer()
    {
        // No-op
    }

    public override void OnExitComputer()
    {
        // No-op
    }
    
    public override string GetFocusedEntryID()
    {
        return _prevEntryId;
    }

    public override bool AllowCancelInput()
    {
        // We use the "go back" (close mod selector) instead
        return false;
    }

    public override bool AllowModeSwap()
    {
        // You can only "go back" or select a mode
        return false;
    }
}
