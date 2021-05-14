using System;
using Microsoft.Extensions.DependencyInjection;

namespace Geco.Common
{
    /// <summary>
    /// Represents a service to be registered in DI automatically 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute : Attribute
    {
        public Type ContractType { get; }
        public ServiceLifetime Lifetime { get; }

        public ServiceAttribute(Type contractType, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
            Lifetime = lifetime;
        }
    }
}