using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using PieShop.Models;

namespace PieShop.Data
{
    public class ShoppingCart
    {
        private readonly AppDbContext _appDbContext;
        public string ShoppingCartId { get; set; }
        public List<ShoppingCartItem> ShoppingCartItems { get; set; }

        public ShoppingCart(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public static ShoppingCart GetCart(IServiceProvider services)
        {
            // Gains access to the HttpContext session info
            // The HttpContext has all the information about the Http request
            ISession session = services.GetRequiredService<IHttpContextAccessor>()?
                .HttpContext.Session;

            // We get here an instance of the AppDbContext to be able to gain access to the DBSet<ShoppingCartItem> ShoppingCartItems
            var context = services.GetService<AppDbContext>();

            // Checking if there is a Guid already and if not, it creates one
            string cartId = session.GetString("CartId") ?? Guid.NewGuid().ToString();
            
            // giving a value to the new guid
            session.SetString("CartId", cartId);

            // Creates a new instance of hte ShoppingCart
            return new ShoppingCart(context){ ShoppingCartId = cartId };

        }

        public void AddToCart(Pie pie, int amount)
        {
            //We check if there is a pie of that type already in the ShoppingCart,
            //looking for an item with the pieId and the shoppingCartId
            var shoppingCartItem = _appDbContext.ShoppingCartItems.SingleOrDefault(s =>
                s.Pie.PieId == pie.PieId && s.ShoppingCartId == ShoppingCartId);

            //If there is not a pie of that type already on the cart,
            //we create a shoppingCartItem for that pie
            if (shoppingCartItem == null)
            {
                shoppingCartItem = new ShoppingCartItem
                {
                    ShoppingCartId = ShoppingCartId,
                    Pie = pie,
                    Amount = 1
                };
                //Add the item to the appContext
                _appDbContext.ShoppingCartItems.Add(shoppingCartItem);
            }
            else
            {
                shoppingCartItem.Amount++;
            }

            _appDbContext.SaveChanges();
        }

        public int RemoveFromCart(Pie pie)
        {
            var shoppingCartItem = _appDbContext.ShoppingCartItems.SingleOrDefault(s =>
                s.Pie.PieId == pie.PieId && s.ShoppingCartId == ShoppingCartId);

            var localAmount = 0;

            if (shoppingCartItem != null)
            {
                if (shoppingCartItem.Amount > 1)
                {
                    shoppingCartItem.Amount--;
                    localAmount = shoppingCartItem.Amount;
                }
                else
                {
                    _appDbContext.ShoppingCartItems.Remove(shoppingCartItem);
                }
            }

            _appDbContext.SaveChanges(); 

            return localAmount;
        }

        public List<ShoppingCartItem> GetShoppingCartItems()
        {
            return ShoppingCartItems ?? _appDbContext.ShoppingCartItems.Where(c => c.ShoppingCartId == ShoppingCartId)
                           .Include(s => s.Pie)
                           .ToList();
        }

        public int GetShoppingCartAmountItems()
        {
            var totalAmount = _appDbContext.ShoppingCartItems
                .Where(s => s.ShoppingCartId == ShoppingCartId)
                .Select(i => i.Amount)
                .Sum();

            return totalAmount;
        }

        public void ClearCart()
        {
            var shoppingCartItems = _appDbContext.ShoppingCartItems.Where(s => s.ShoppingCartId == ShoppingCartId);

            _appDbContext.ShoppingCartItems.RemoveRange(shoppingCartItems);

            _appDbContext.SaveChanges();
        }

        public decimal GetShoppingCartTotal()
        {
            var shoppingCartTotal = _appDbContext.ShoppingCartItems
                .Where(s => s.ShoppingCartId == ShoppingCartId)
                .Select(i => i.Pie.Price * i.Amount)
                .Sum();

            return shoppingCartTotal;
        }

    }
}
