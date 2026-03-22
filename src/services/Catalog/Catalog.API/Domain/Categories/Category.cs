namespace Catalog.API;

public class Category
{
    public Category() { }

    public Category(string name, string icon, Guid? parentId = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Invalid name");

        Name = name;
        ParentId = parentId;
    }

    public static Category Create(string name, string icon, Guid? parentId = null)
        => new Category(name, icon, parentId);

    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    public CategoryNameEdited EditName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Invalid name");
        Name = name.Trim();
        return new CategoryNameEdited(Id, Name);
    }

    public CategoryIconChanged ChangeIcon(string icon)
    {
        if (icon is null)
            throw new ArgumentException("Invalid icon");
        Icon = icon.Trim();
        return new CategoryIconChanged(Id, Icon);
    }
}
