using ElectronicInventoryManagementSystem.Models;

public class Unit
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Product>? Products { get; set; }
}