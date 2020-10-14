namespace EC.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class productsmigration : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CartItems",
                c => new
                    {
                        ItemID = c.Int(nullable: false, identity: true),
                        ProductId = c.Int(nullable: false),
                        Total = c.Double(nullable: false),
                        CartId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ItemID)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .ForeignKey("dbo.ShoppingCarts", t => t.CartId, cascadeDelete: true)
                .Index(t => t.ProductId)
                .Index(t => t.CartId);
            
            CreateTable(
                "dbo.Products",
                c => new
                    {
                        ProductId = c.Int(nullable: false, identity: true),
                        ProductName = c.String(nullable: false),
                        Description = c.String(nullable: false),
                        Quantity = c.Int(nullable: false),
                        Date = c.DateTime(nullable: false),
                        Price = c.Double(nullable: false),
                        ImagePath = c.String(),
                    })
                .PrimaryKey(t => t.ProductId);
            
            CreateTable(
                "dbo.Categories",
                c => new
                    {
                        CategoryID = c.Int(nullable: false, identity: true),
                        Description = c.String(nullable: false, maxLength: 200),
                    })
                .PrimaryKey(t => t.CategoryID);
            
            CreateTable(
                "dbo.ShoppingCarts",
                c => new
                    {
                        CartId = c.Int(nullable: false, identity: true),
                        Total = c.Double(nullable: false),
                        Date = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.CartId);
            
            CreateTable(
                "dbo.OrderDetails",
                c => new
                    {
                        OrderNumber = c.Int(nullable: false, identity: true),
                        Email = c.String(nullable: false),
                        FirstName = c.String(nullable: false),
                        LastName = c.String(nullable: false),
                        Date = c.DateTime(nullable: false),
                        Phone = c.String(nullable: false),
                        Country = c.String(nullable: false),
                        Address = c.String(nullable: false),
                        Address2 = c.String(nullable: false),
                        City = c.String(nullable: false),
                        CartId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.OrderNumber)
                .ForeignKey("dbo.ShoppingCarts", t => t.CartId, cascadeDelete: true)
                .Index(t => t.CartId);
            
            CreateTable(
                "dbo.CategoryProducts",
                c => new
                    {
                        Category_CategoryID = c.Int(nullable: false),
                        Product_ProductId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Category_CategoryID, t.Product_ProductId })
                .ForeignKey("dbo.Categories", t => t.Category_CategoryID, cascadeDelete: true)
                .ForeignKey("dbo.Products", t => t.Product_ProductId, cascadeDelete: true)
                .Index(t => t.Category_CategoryID)
                .Index(t => t.Product_ProductId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.OrderDetails", "CartId", "dbo.ShoppingCarts");
            DropForeignKey("dbo.CartItems", "CartId", "dbo.ShoppingCarts");
            DropForeignKey("dbo.CategoryProducts", "Product_ProductId", "dbo.Products");
            DropForeignKey("dbo.CategoryProducts", "Category_CategoryID", "dbo.Categories");
            DropForeignKey("dbo.CartItems", "ProductId", "dbo.Products");
            DropIndex("dbo.CategoryProducts", new[] { "Product_ProductId" });
            DropIndex("dbo.CategoryProducts", new[] { "Category_CategoryID" });
            DropIndex("dbo.OrderDetails", new[] { "CartId" });
            DropIndex("dbo.CartItems", new[] { "CartId" });
            DropIndex("dbo.CartItems", new[] { "ProductId" });
            DropTable("dbo.CategoryProducts");
            DropTable("dbo.OrderDetails");
            DropTable("dbo.ShoppingCarts");
            DropTable("dbo.Categories");
            DropTable("dbo.Products");
            DropTable("dbo.CartItems");
        }
    }
}
