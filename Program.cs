
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

void Test(bool useNpgsql)
{
    using var context = new MyDbContext(useNpgsql);
    
    var brand1 = "brand1";
    var brand2 = "brand2";

    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();
    context.Products.AddRange(
        new Product { Brand = brand1, Price = 1 },
        new Product { Brand = brand1, Price = 11 },
        new Product { Brand = brand2, Price = 1 },
        new Product { Brand = brand2, Price = 11 }
    );
    context.SaveChanges();

/*
 * Npgsql generates the following
 * SELECT p."Id", p."Brand", p."Price"
 * FROM "Products" AS p
 * WHERE (p."Brand" = $1) OR (p."Brand" = $2) AND p."Price" > 10.0
 */

    var matchingProducts = context.Products
        .Where(x => x.Brand == brand1 | x.Brand == brand2)
        .Where(x => x.Price > 10)
        .ToArray();

    Console.Write(useNpgsql ? "NPGSQL:\t\t" : "SQLSERVER:\t");
    if (!matchingProducts.All(x => new[] { brand1, brand2 }.Contains(x.Brand) && x.Price > 10))
    {
        Console.WriteLine("Failed! Unexpected records returned.");
    }
    else
    {
        Console.WriteLine("Success! Only expected records returned.");
    }
}

Test(useNpgsql: false);
Test(useNpgsql: true);

class Product
{
    [Key]
    public int Id { get; set; }
    public string Brand { get; set; }
    public decimal Price { get; set; }
}
class MyDbContext : DbContext
{
    readonly bool _useNpgql;

    public MyDbContext(bool useNpgql)
    {
        _useNpgql = useNpgql;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_useNpgql)
        {
            optionsBuilder.UseNpgsql("Host=localhost,5432;Database=efcorepg3061;Integrated Security=true"); 
        }
        else
        {
            optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=efcorepg3061;Trusted_Connection=True;Encrypt=False");
        }
    }

    public DbSet<Product> Products { get; set; }
}

