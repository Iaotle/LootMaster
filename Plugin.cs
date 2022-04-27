using Dalamud.Configuration;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using DalamudPluginProjectTemplate.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace DalamudPluginProjectTemplate
{
  public class Plugin : IDalamudPlugin, IDisposable
  {
    internal static DalamudPluginProjectTemplate.Configuration config;
    private static IntPtr lootsAddr;
    internal static DalamudPluginProjectTemplate.Plugin.RollItemRaw rollItemRaw;
    private PluginCommandManager<DalamudPluginProjectTemplate.Plugin> commandManager;
    private PluginUI ui;

    [PluginService]
    public static CommandManager CommandManager { get; set; }

    [PluginService]
    public static DalamudPluginInterface pi { get; set; }

    [PluginService]
    public static SigScanner SigScanner { get; set; }

    [PluginService]
    public static ChatGui ChatGui { get; set; }

    public static List<LootItem> LootItems => ((IEnumerable<LootItem>) DalamudPluginProjectTemplate.Plugin.ReadArray<LootItem>(DalamudPluginProjectTemplate.Plugin.lootsAddr + 16, 16)).Where<LootItem>((Func<LootItem, bool>) (i => i.Valid)).ToList<LootItem>();

    public string Name => "LootMaster";

    public Plugin()
    {
      DalamudPluginProjectTemplate.Plugin.lootsAddr = DalamudPluginProjectTemplate.Plugin.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 89 44 24 60", 0);
      DalamudPluginProjectTemplate.Plugin.rollItemRaw = Marshal.GetDelegateForFunctionPointer<DalamudPluginProjectTemplate.Plugin.RollItemRaw>(DalamudPluginProjectTemplate.Plugin.SigScanner.ScanText("41 83 F8 ?? 0F 83 ?? ?? ?? ?? 48 89 5C 24 08"));
      DalamudPluginProjectTemplate.Plugin.config = (DalamudPluginProjectTemplate.Configuration) DalamudPluginProjectTemplate.Plugin.pi.GetPluginConfig() ?? new DalamudPluginProjectTemplate.Configuration();
      DalamudPluginProjectTemplate.Plugin.config.Initialize(DalamudPluginProjectTemplate.Plugin.pi);
      this.ui = new PluginUI();
      DalamudPluginProjectTemplate.Plugin.pi.UiBuilder.Draw += new Action(this.ui.Draw);
      DalamudPluginProjectTemplate.Plugin.pi.UiBuilder.OpenConfigUi += (Action) (() =>
      {
        PluginUI ui = this.ui;
        ui.IsVisible = !ui.IsVisible;
      });
      this.commandManager = new PluginCommandManager<DalamudPluginProjectTemplate.Plugin>(this, DalamudPluginProjectTemplate.Plugin.pi);
    }

    private void RollItem(RollOption option, int index)
    {
      LootItem lootItem = DalamudPluginProjectTemplate.Plugin.LootItems[index];
      PluginLog.Information(string.Format("{0} [{1}] {2} Id: {3:X} rollState: {4} rollOption: {5}", (object) option, (object) index, (object) lootItem.ItemId, (object) lootItem.ObjectId, (object) lootItem.RollState, (object) lootItem.RolledState), Array.Empty<object>());
      DalamudPluginProjectTemplate.Plugin.rollItemRaw(DalamudPluginProjectTemplate.Plugin.lootsAddr, option, (uint) index);
    }

    [DalamudPluginProjectTemplate.Attributes.Command("/need")]
    [HelpMessage("Roll need for everything. If impossible, roll greed.")]
    public void NeedCommand(string command, string args)
    {
      int num1 = 0;
      int num2 = 0;
      for (int index = 0; index < DalamudPluginProjectTemplate.Plugin.LootItems.Count; ++index)
      {
        if (!DalamudPluginProjectTemplate.Plugin.LootItems[index].Rolled)
        {
          if (DalamudPluginProjectTemplate.Plugin.LootItems[index].RollState == RollState.UpToNeed)
          {
            this.RollItem(RollOption.Need, index);
            ++num1;
          }
          else
          {
            this.RollItem(RollOption.Greed, index);
            ++num2;
          }
        }
      }
      if (!DalamudPluginProjectTemplate.Plugin.config.EnableChatLogMessage)
        return;
      ChatGui chatGui = DalamudPluginProjectTemplate.Plugin.ChatGui;
      List<Payload> payloadList = new List<Payload>();
      payloadList.Add((Payload) new TextPayload("Need "));
      payloadList.Add((Payload) new UIForegroundPayload((ushort) 575));
      payloadList.Add((Payload) new TextPayload(num1.ToString()));
      payloadList.Add((Payload) new UIForegroundPayload((ushort) 0));
      payloadList.Add((Payload) new TextPayload(" item" + (num1 > 1 ? "s" : "") + ", greed "));
      payloadList.Add((Payload) new UIForegroundPayload((ushort) 575));
      payloadList.Add((Payload) new TextPayload(num2.ToString()));
      payloadList.Add((Payload) new UIForegroundPayload((ushort) 0));
      payloadList.Add((Payload) new TextPayload(" item" + (num2 > 1 ? "s" : "") + "."));
      SeString seString = new SeString(payloadList);
      chatGui.Print(seString);
    }

    [DalamudPluginProjectTemplate.Attributes.Command("/greed")]
    [HelpMessage("Greed on all items.")]
    public void GreedCommand(string command, string args)
    {
      int num = 0;
      for (int index = 0; index < DalamudPluginProjectTemplate.Plugin.LootItems.Count; ++index)
      {
        if (!DalamudPluginProjectTemplate.Plugin.LootItems[index].Rolled)
        {
          this.RollItem(RollOption.Greed, index);
          ++num;
        }
      }
      if (!DalamudPluginProjectTemplate.Plugin.config.EnableChatLogMessage)
        return;
      ChatGui chatGui = DalamudPluginProjectTemplate.Plugin.ChatGui;
      List<Payload> payloadList = new List<Payload>();
      payloadList.Add((Payload) new TextPayload("Greed "));
      payloadList.Add((Payload) new UIForegroundPayload((ushort) 575));
      payloadList.Add((Payload) new TextPayload(num.ToString()));
      payloadList.Add((Payload) new UIForegroundPayload((ushort) 0));
      payloadList.Add((Payload) new TextPayload(" item" + (num > 1 ? "s" : "") + "."));
      SeString seString = new SeString(payloadList);
      chatGui.Print(seString);
    }

    [DalamudPluginProjectTemplate.Attributes.Command("/pass")]
    [HelpMessage("Pass on things you haven't rolled for yet.")]
    public void PassCommand(string command, string args)
    {
      int num = 0;
      for (int index = 0; index < DalamudPluginProjectTemplate.Plugin.LootItems.Count; ++index)
      {
        if (!DalamudPluginProjectTemplate.Plugin.LootItems[index].Rolled)
        {
          this.RollItem(RollOption.Pass, index);
          ++num;
        }
      }
      if (!DalamudPluginProjectTemplate.Plugin.config.EnableChatLogMessage)
        return;
      ChatGui chatGui = DalamudPluginProjectTemplate.Plugin.ChatGui;
      List<Payload> payloadList = new List<Payload>();
      payloadList.Add((Payload) new TextPayload("Pass "));
      payloadList.Add((Payload) new UIForegroundPayload((ushort) 575));
      payloadList.Add((Payload) new TextPayload(num.ToString()));
      payloadList.Add((Payload) new UIForegroundPayload((ushort) 0));
      payloadList.Add((Payload) new TextPayload(" item" + (num > 1 ? "s" : "") + "."));
      SeString seString = new SeString(payloadList);
      chatGui.Print(seString);
    }

    [DalamudPluginProjectTemplate.Attributes.Command("/passall")]
    [HelpMessage("Passes on all, even if you rolled on them previously.")]
    public void PassAllCommand(string command, string args)
    {
      int num = 0;
      for (int index = 0; index < DalamudPluginProjectTemplate.Plugin.LootItems.Count; ++index)
      {
        if (DalamudPluginProjectTemplate.Plugin.LootItems[index].RolledState != RollOption.Pass)
        {
          this.RollItem(RollOption.Pass, index);
          ++num;
        }
      }
      if (!DalamudPluginProjectTemplate.Plugin.config.EnableChatLogMessage)
        return;
      ChatGui chatGui = DalamudPluginProjectTemplate.Plugin.ChatGui;
      List<Payload> payloadList = new List<Payload>();
      payloadList.Add((Payload) new TextPayload("Pass all "));
      payloadList.Add((Payload) new UIForegroundPayload((ushort) 575));
      payloadList.Add((Payload) new TextPayload(num.ToString()));
      payloadList.Add((Payload) new UIForegroundPayload((ushort) 0));
      payloadList.Add((Payload) new TextPayload(" item" + (num > 1 ? "s" : "") + "."));
      SeString seString = new SeString(payloadList);
      chatGui.Print(seString);
    }

    public static T[] ReadArray<T>(IntPtr unmanagedArray, int length) where T : struct
    {
      int num = Marshal.SizeOf(typeof (T));
      T[] objArray = new T[length];
      for (int index = 0; index < length; ++index)
      {
        IntPtr ptr = new IntPtr(unmanagedArray.ToInt64() + (long) (index * num));
        objArray[index] = Marshal.PtrToStructure<T>(ptr);
      }
      return objArray;
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposing)
        return;
      this.commandManager.Dispose();
      DalamudPluginProjectTemplate.Plugin.pi.SavePluginConfig((IPluginConfiguration) DalamudPluginProjectTemplate.Plugin.config);
      DalamudPluginProjectTemplate.Plugin.pi.UiBuilder.Draw -= new Action(this.ui.Draw);
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    internal delegate void RollItemRaw(IntPtr lootIntPtr, RollOption option, uint lootItemIndex);
  }
}
