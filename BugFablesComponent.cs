﻿using LiveSplit.BugFables.UI;
using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.BugFables
{
  public class BugFablesComponent : LogicComponent
  {
    private LiveSplitState liveSplitState;
    private TimerModel timerModel;
    private GameMemory gameMemory;
    private LiveSplitLogic logic;
    private SettingsUserControl settingsUserControl;

    private bool blockEvents = false;

    public BugFablesComponent(LiveSplitState state)
    {
      liveSplitState = state;
      timerModel = new();
      timerModel.CurrentState = liveSplitState;
      gameMemory = new();
      settingsUserControl = new(state);
      logic = new(gameMemory, settingsUserControl);

      liveSplitState.OnReset += OnReset;
      liveSplitState.OnStart += OnStart;
    }

    private void OnStart(object sender, EventArgs e)
    {
      if (!blockEvents)
        logic.ResetLogic();
    }

    private void OnReset(object sender, TimerPhase t)
    {
      if (!blockEvents)
      {
        logic.ResetLogic();
        gameMemory.ResetEverything();
      }
    }

    public override string ComponentName => BugFablesFactory.AutosplitterName;

    public override void Dispose()
    {
      liveSplitState.OnStart -= OnStart;
      liveSplitState.OnReset -= OnReset;
    }

    public override XmlNode GetSettings(XmlDocument document)
    {
      XmlNode result = settingsUserControl.SaveSettings(document);
      if (settingsUserControl.settingsHasChanged)
      {
        logic.ResetLogic();
        settingsUserControl.settingsHasChanged = false;
      }
      return result;
    }

    public override Control GetSettingsControl(LayoutMode mode)
    {
      return settingsUserControl;
    }

    public override void SetSettings(XmlNode settings)
    {
      settingsUserControl.LoadSettings(settings);
      logic.ResetLogic();
    }

    public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
    {
      if (!gameMemory.ProcessHook())
        return;

      if (logic.ShouldStart())
      {
        blockEvents = true;
        if (liveSplitState.CurrentPhase == TimerPhase.Running)
          timerModel.Reset();
        timerModel.Start();
        blockEvents = false;
      }

      if (liveSplitState.CurrentPhase == TimerPhase.Running)
      {
        if (logic.ShouldSplit(liveSplitState.CurrentSplitIndex, liveSplitState.Run.Count))
          timerModel.Split();
      }
    }
  }
}
