using System;

namespace Lumi.Generators
{
    /// <summary>
    /// Mark a partial class or a property to participate in INotifyPropertyChanged generation.
    /// When applied to a class, the generator adds the INotifyPropertyChanged implementation.
    /// When applied to a property, the generator creates a backing field with change notification.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false)]
    public sealed class ObservableAttribute : Attribute
    {
    }
}
