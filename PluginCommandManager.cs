using Dalamud.Game.Command;
using Dalamud.Plugin;
using DalamudPluginProjectTemplate.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DalamudPluginProjectTemplate
{
  public class PluginCommandManager<THost> : IDisposable
  {
    private readonly DalamudPluginInterface pluginInterface;
    private readonly (string, CommandInfo)[] pluginCommands;
    private readonly THost host;

    public PluginCommandManager(THost host, DalamudPluginInterface pluginInterface)
    {
      this.pluginInterface = pluginInterface;
      this.host = host;
      this.pluginCommands = ((IEnumerable<MethodInfo>) host.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)).Where<MethodInfo>((Func<MethodInfo, bool>) (method => method.GetCustomAttribute<CommandAttribute>() != null)).SelectMany<MethodInfo, (string, CommandInfo)>(new Func<MethodInfo, IEnumerable<(string, CommandInfo)>>(this.GetCommandInfoTuple)).ToArray<(string, CommandInfo)>();
      Array.Reverse((Array) this.pluginCommands);
      this.AddCommandHandlers();
    }

    private void AddCommandHandlers()
    {
      for (int index = 0; index < this.pluginCommands.Length; ++index)
      {
        (string, CommandInfo) pluginCommand = this.pluginCommands[index];
        DalamudPluginProjectTemplate.Plugin.CommandManager.AddHandler(pluginCommand.Item1, pluginCommand.Item2);
      }
    }

    private void RemoveCommandHandlers()
    {
      for (int index = 0; index < this.pluginCommands.Length; ++index)
        DalamudPluginProjectTemplate.Plugin.CommandManager.RemoveHandler(this.pluginCommands[index].Item1);
    }

    private IEnumerable<(string, CommandInfo)> GetCommandInfoTuple(
      MethodInfo method)
    {
      CommandInfo.HandlerDelegate handlerDelegate = (CommandInfo.HandlerDelegate) Delegate.CreateDelegate(typeof (CommandInfo.HandlerDelegate), this.host, method);
      CommandAttribute customAttribute1 = handlerDelegate.Method.GetCustomAttribute<CommandAttribute>();
      AliasesAttribute customAttribute2 = handlerDelegate.Method.GetCustomAttribute<AliasesAttribute>();
      HelpMessageAttribute customAttribute3 = handlerDelegate.Method.GetCustomAttribute<HelpMessageAttribute>();
      DoNotShowInHelpAttribute customAttribute4 = handlerDelegate.Method.GetCustomAttribute<DoNotShowInHelpAttribute>();
      CommandInfo commandInfo = new CommandInfo(handlerDelegate)
      {
        HelpMessage = customAttribute3?.HelpMessage ?? string.Empty,
        ShowInHelp = customAttribute4 == null
      };
      List<(string, CommandInfo)> valueTupleList = new List<(string, CommandInfo)>();
      valueTupleList.Add(customAttribute1.Command, commandInfo);
      List<(string, CommandInfo)> commandInfoTuple = valueTupleList;
      if (customAttribute2 != null)
      {
        for (int index = 0; index < customAttribute2.Aliases.Length; ++index)
          commandInfoTuple.Add((customAttribute2.Aliases[index], commandInfo));
      }
      return commandInfoTuple;
    }

    public void Dispose() => this.RemoveCommandHandlers();
  }
}
