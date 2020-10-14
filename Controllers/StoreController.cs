using System;
using PagedList;
using PagedList.Mvc;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using EC.Models;
using EC.Models.Context;
using System.Threading.Tasks;
using Bia.Countries.Iso3166; 
namespace EC.Controllers
{
    public class StoreController : Controller
    {
        private ProductContext db = new ProductContext();
        private const int PageSize = 12;
        public async Task<ActionResult> Index(string Search, int? PageNumber, int? min, int? max, List<int> Filter,int? Price, string Name)
        {
            var products = from p in db.Products.Include(p => p.Categories) select p;
            var categories = from c in db.Categories.Include(c => c.Products) select c;
            ViewBag.Search = Search;
            ViewBag.Price = Price != null && Price == 1 ? Price = 2 : Price = 1;
            ViewBag.Name = !string.IsNullOrEmpty(Name)? Name : ViewBag.Name;
            ViewBag.Max = max;
            ViewBag.Min = min;
            ViewBag.Categories = await categories.ToListAsync();
            ViewBag.Filter = Filter;

            if(!string.IsNullOrEmpty(Search))
            {
                
                products =  products.Where(m => m.ProductName.Contains(Search) || m.Description.Contains(Search));
                PageNumber = 1; 
            }

            if(min != null && max == null && min > 0)
            {
                products = products.Where(m => m.Price >= min);
                PageNumber = 1;

            }else if(max != null && min == null && max > 0)
            {
                products = products.Where(m => m.Price <= max);
                PageNumber = 1;
            }else if(max != null && min != null && min < max)
            {
                products = products.Where(m => m.Price >= min && m.Price <= max);
                PageNumber = 1;
            }

            if(Price != null)
            {
                if (Price == 1)
                    products = products.OrderByDescending(m => m.Price);
                else products = products.OrderBy(m => m.Price);
                PageNumber = 1;
            }


            List<IQueryable<Product>> union = null;
            if(Filter != null)
            {
                union = new List<IQueryable<Product>>();
                foreach (int category in Filter)
                    union.Add(products.Where(m => m.Categories.All(p => p.CategoryID == category)));
            }
            
            if(union != null)
            {
                products = union.First();
                foreach (var product in union)
                    products = products.Union(product); 
            }
            

            switch((string)ViewBag.Name)
            {
                case "Name_Descending":
                    products = products.OrderByDescending(m => m.ProductName);
                    break;
                default:
                    products = products.OrderBy(m => m.ProductName);
                    break;
            }

            return View(products.ToPagedList(PageNumber ?? 1, PageSize));
        }


        
        public async Task<ActionResult> AddToCart(int? id)
        {
            var product = await db.Products.FindAsync(id);
            ShoppingCart cart; 
            if(Session["ShoppingCart"] == null)
            {
                Session["ShoppingCart"] = new ShoppingCart { Date = DateTime.Now,CartItems = new List<CartItem>()};
            }
            cart = (ShoppingCart) Session["ShoppingCart"];
            CartItem item = new CartItem { Product = product, ProductId = product.ProductId, Quantity = 0, Total = 0.00};
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddToCart([Bind(Include = "ItemID,ProductId,Total,Quantity")]CartItem item)
        {
           
            var cart = (ShoppingCart)Session["ShoppingCart"];
            item.Product = await db.Products.FindAsync(item.ProductId);
            item.Total = item.Sum();
            var duplicate = cart.CartItems.Find(m => m.ProductId == item.ProductId);

            if (duplicate == null)
            {
                ViewBag.Message = "Already in cart";
                cart.CartItems.Add(item);
            }
            else
            {
                cart.CartItems.Remove(duplicate);
                item.Product.Quantity += duplicate.Quantity;
                cart.Total -= duplicate.Total;
                cart.CartItems.Add(item);
                ViewBag.Message = "Successfully Added to Cart";
            }

            cart.Total += item.Total;
            Session["ShoppingCart"] = cart;
            return View(item);
        }


        
        public ActionResult ListCart()
        {
            var cart = (ShoppingCart)Session["ShoppingCart"];
            if (cart != null)
                return View(cart);

            return View(new ShoppingCart());

        }

        public ActionResult Checkout()
        {
            ViewBag.Countries = new SelectList(Countries.GetAllShortNames(), "shortName");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CheckOut([Bind(Include = "Email,OrderNumber,FirstName,LastName,Date,Phone,Country,Address,Address2,City, CartId")] OrderDetails order)
        {

            if (!ModelState.IsValid)
            {
                ViewBag.Countries = new SelectList(Countries.GetAllShortNames(), "shortName");
                return View(order);
            }

   

            var cart = (ShoppingCart)Session["ShoppingCart"];
            bool OutOfStock = false;

            foreach (var item in cart.CartItems)
            {
                item.Product = await db.Products.FindAsync(item.ProductId);
                if (item.Quantity > item.Product.Quantity)
                {
                    cart.CartItems.Remove(item);
                    OutOfStock = true;
                }
            }
            if (OutOfStock == true)
            {
                Session["ShoppingCart"] = cart;
                ViewBag.Stock = "Some of the items on the cart became out of stock while you were shopping. Oops!!!"; 
                return RedirectToAction("ListCart"); 
            }

            var items = cart.CartItems;
            cart.CartItems = null;
            db.ShoppingCarts.Add(cart);
            await db.SaveChangesAsync(); 
            

            foreach(var item in items)
            {
                item.Product.Quantity -= item.Quantity;  
                db.Entry(item.Product).State = EntityState.Modified;
                item.ShoppingCart = cart;
                item.CartId = cart.CartId;
                db.Carts.Add(item); 
                await db.SaveChangesAsync(); 
            }

            cart.CartItems = items;
            db.Entry<ShoppingCart>(cart).State = EntityState.Modified;
            await db.SaveChangesAsync();

            order.ShoppingCart = cart;
            order.CartId = cart.CartId;
            order.Date = DateTime.Now;
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            ViewBag.Message = "Check out successfully";
            return RedirectToAction("Index");
        }

        [Authorize]
        public ActionResult Orders(string Email, int? search, int? PageNumber)
        {
            var orders = db.Orders.AsQueryable();
            orders = orders.Where(m => m.Email == Email);
 
            ViewBag.Search = search;
            if(search != null)
            {
                orders = orders.Where(m => m.OrderNumber == search);
                PageNumber = 1;
            }


            

            orders =  orders.OrderByDescending(m => m.Date);
            return View(orders.ToPagedList(PageNumber ?? 1, PageSize));
        }

    }
}