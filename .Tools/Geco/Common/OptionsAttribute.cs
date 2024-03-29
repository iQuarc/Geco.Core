using System;

namespace Geco.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class OptionsAttribute : Attribute
    {
        public OptionsAttribute(Type optionsType)
        {
            OptionType = optionsType ?? throw new ArgumentNullException(nameof(optionsType));
        }

        public Type OptionType { get; }
    }
}