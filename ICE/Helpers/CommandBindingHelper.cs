using System.Linq;
using System.Windows.Input;

namespace Microsoft.Research.ICE.Helpers
{
    public static class CommandBindingHelper
    {
        public static void Replace(this CommandBindingCollection commandBindingCollection, CommandBinding commandBinding)
        {
            CommandBinding[] array = commandBindingCollection.OfType<CommandBinding>().ToArray();
            foreach (CommandBinding commandBinding2 in array)
            {
                if (commandBinding2.Command == commandBinding.Command)
                {
                    commandBindingCollection.Remove(commandBinding2);
                }
            }
            commandBindingCollection.Add(commandBinding);
        }
    }
}