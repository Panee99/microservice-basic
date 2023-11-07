namespace Play.Catalog.Service.Entities;

using Play.Common;

public class Item : IEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTimeOffset CreatedDate { get; set; }
}