﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using TicketApplication.Models;

namespace TicketApplication.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<Zone> Zones { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Cart>().HasKey(c => new { c.UserId, c.ZoneId });
            builder.Entity<OrderDetail>().HasKey(od => new { od.OrderId, od.ZoneId });

            // Configure Discount entity
            builder.Entity<Discount>(entity =>
            {
                entity.Property(d => d.DiscountAmount)
                      .HasColumnType("decimal(18, 2)");
                entity.Property(d => d.DiscountPercentage)
                      .HasColumnType("decimal(5, 2)"); // Adjust as needed
            });

            // Configure Order entity
            builder.Entity<Order>(entity =>
            {
                entity.Property(o => o.TotalAmount)
                      .HasColumnType("decimal(18, 2)");
            });

            // Configure OrderDetail entity
            builder.Entity<OrderDetail>(entity =>
            {
                entity.Property(od => od.TotalPrice)
                      .HasColumnType("decimal(18, 2)");
                entity.Property(od => od.UnitPrice)
                      .HasColumnType("decimal(18, 2)");
            });

            // Configure Payment entity
            builder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount)
                      .HasColumnType("decimal(18, 2)");
            });

            // Configure Zone entity (if not already done)
            builder.Entity<Zone>(entity =>
            {
                entity.Property(z => z.Price)
                      .HasColumnType("decimal(18, 2)");
            });

        }



    }
}
