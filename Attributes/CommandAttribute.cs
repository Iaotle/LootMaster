using System;

namespace DalamudPluginProjectTemplate.Attributes
{
  [AttributeUsage(AttributeTargets.Method)]
  public class CommandAttribute : Attribute
  {
    public string Command { get; }

    public CommandAttribute(string command) => this.Command = command;
  }
}
