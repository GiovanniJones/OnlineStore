﻿using System;
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

namespace EC.Controllers
{
    public class StoreController : Controller
    {
        private ProductContext db = new ProductContext();
        private const int PageSize = 12;
        public async Task<ActionResult> Index(string Search, int? PageNumber, int? min, int? max, int? Filter,int? Price, string Name)
        {
            var products = from p in db.Products.Include(p => p.Categories) select p;
            var categories = from c in db.Categories.Include(c => c.Products) select c;
            ViewBag.Search = Search;
            ViewBag.Price = Price != null && Price == 1 ? Price = 2 : Price = 1;
            ViewBag.Name = !string.IsNullOrEmpty(Name)? Name : ViewBag.Name;
            ViewBag.Max = max;
            ViewBag.Min = min;
            ViewBag.Categories = new SelectList(await categories.ToListAsync(), dataValueField: "CategoryID", dataTextField: "Description");

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
            CartItem item = new CartItem { Product = product, ProductId = product.ProductId, Quantity = 0, CartId = cart.CartId , ShoppingCart = cart, Total = 0.00};
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

            if(duplicate == null)
                cart.CartItems.Add(item);
            else
            {
                cart.CartItems.Remove(duplicate);
                item.Product.Quantity += duplicate.Quantity;
                cart.Total -= duplicate.Total;
                cart.CartItems.Add(item); 
                
            }

            cart.Total += item.Total;
            ViewBag.Message = "Successfully Added to Cart";
            item.Product.Quantity -= item.Quantity;
            db.Entry(item.Product).State = EntityState.Modified;
            await db.SaveChangesAsync();
            Session["ShoppingCart"] = cart;
            return View(item);
        }


        
        public ActionResult ListCart()
        {
            var cart = (ShoppingCart)Session["ShoppingCart"];
            if(cart != null)
                return View(cart);

            return RedirectToAction("Index");

        }

        public ActionResult Checkout()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CheckOut([Bind(Include = "Email,OrderNumber,FirstName,LastName,Date,Phone,Country,Address,CartId")] OrderDetails order)
        {
            var cart = (ShoppingCart)Session["ShoppingCart"];
            var items = cart.CartItems;
            cart.CartItems = null; 
            db.ShoppingCarts.Add(cart); 
            await db.SaveChangesAsync();
            
            foreach(var item in items)
            {
                db.Products.Attach(item.Product);
                item.ShoppingCart = cart;
                item.CartId = cart.CartId;
                db.Carts.Add(item); 
                await db.SaveChangesAsync(); 
            }

            cart.CartItems = items;
            db.Entry<ShoppingCart>(cart).State = EntityState.Modified;
            order.ShoppingCart = cart;
            order.CartId = cart.CartId;
            order.Date = DateTime.Now;
            await db.SaveChangesAsync(); 
            if(!ModelState.IsValid)
                return View(order);

            db.Orders.Add(order);
            await db.SaveChangesAsync();
            ViewBag.Message = "Check out successfully";
            return RedirectToAction("Index");
        }

    }
}