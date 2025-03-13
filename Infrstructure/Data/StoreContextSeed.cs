using Core.Entities;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrstructure.Data
{
    public class StoreContextSeed
    {
        public static async Task SeedAsync(StoreContext context)
        {
            if (!context.Products.Any())
            {
                var productData = await File.ReadAllTextAsync("../Infrstructure/Data/SeedData/products.json");

                var products = JsonSerializer.Deserialize<List<Product>>(productData);
                if (products.IsNullOrEmpty()) return;
                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }
        }
    }
}
