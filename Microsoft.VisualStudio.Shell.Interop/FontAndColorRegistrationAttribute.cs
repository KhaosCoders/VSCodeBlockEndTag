using System;

namespace Microsoft.VisualStudio.Shell.Interop;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
internal sealed class FontAndColorRegistrationAttribute : RegistrationAttribute
{
    public FontAndColorRegistrationAttribute(Type providerType, String name, String category)
    {
        Name = name;
        Provider = providerType.GUID;
        Category = new Guid(category);
    }

    public override void Register(RegistrationContext context)
    {
        if (context == null)
        {
            return;
        }

        context.Log.WriteLine("FontAndColors:    Name:{0}, Category:{1:B}, Package:{2:B}", Name, Category, Provider);
        using var key = context.CreateKey($"FontAndColors\\{Name}");
        key.SetValue("Category", Category.ToString("B"));
        key.SetValue("Package", Provider.ToString("B"));
    }

    public override void Unregister(RegistrationContext context)
    {
        context?.RemoveKey($"FontAndColors\\{Name}");
    }

    public string Name { get; set; }
    public Guid Category { get; set; }
    public Guid Provider { get; set; }
}
