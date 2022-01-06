using System;

namespace Geco.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ConsoleColorAttribute: Attribute
    {
        public ConsoleColor ConsoleColor { get; }

        public ConsoleColorAttribute(ConsoleColor consoleColor)
        {
            ConsoleColor = consoleColor;
        }
    }
}