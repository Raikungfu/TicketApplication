using Microsoft.EntityFrameworkCore;
using TicketApplication.Models;

namespace TicketApplication.Data
{
    public static class SeedData
    {
        public static void Initialize(ModelBuilder modelBuilder)
        {
            // Seed Events
            modelBuilder.Entity<Event>().HasData(
                new Event
                {
                    Id = "EVT001",
                    Title = "Music Concert",
                    Description = "An amazing music concert with popular bands.",
                    Location = "Central Park",
                    Date = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd"),
                    Price = "50.00",
                    NumberOfTickets = 100,
                    Image = "concert.jpg",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Admin"
                },
                new Event
                {
                    Id = "EVT002",
                    Title = "Art Exhibition",
                    Description = "An exhibition showcasing local artists.",
                    Location = "City Gallery",
                    Date = DateTime.Now.AddDays(45).ToString("yyyy-MM-dd"),
                    Price = "25.00",
                    NumberOfTickets = 50,
                    Image = "art_exhibition.jpg",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Admin"
                },
                new Event
                {
                    Id = "EVT003",
                    Title = "Tech Conference",
                    Description = "A conference about the latest in tech.",
                    Location = "Convention Center",
                    Date = DateTime.Now.AddDays(60).ToString("yyyy-MM-dd"),
                    Price = "150.00",
                    NumberOfTickets = 200,
                    Image = "tech_conference.jpg",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Admin"
                },
                new Event
                {
                    Id = "EVT004",
                    Title = "Food Festival",
                    Description = "A festival featuring food from around the world.",
                    Location = "City Square",
                    Date = DateTime.Now.AddDays(75).ToString("yyyy-MM-dd"),
                    Price = "10.00",
                    NumberOfTickets = 300,
                    Image = "food_festival.jpg",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Admin"
                },
                new Event
                {
                    Id = "EVT005",
                    Title = "Film Screening",
                    Description = "Exclusive screening of an award-winning film.",
                    Location = "Cinema Hall",
                    Date = DateTime.Now.AddDays(20).ToString("yyyy-MM-dd"),
                    Price = "30.00",
                    NumberOfTickets = 80,
                    Image = "film_screening.jpg",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Admin"
                }
            );

            // Seed Tickets
            modelBuilder.Entity<Ticket>().HasData(
                new Ticket
                {
                    Id = "TCKT001",
                    Title = "VIP Pass",
                    Description = "Access to VIP area in the concert.",
                    EventId = "EVT001", // Match this with an existing Event Id
                    Status = "Available",
                    Price = 100.00m,
                    Quantity = 20,
                    Zone = "VIP",
                    Image = "vip_pass.jpg",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Admin"
                },
                new Ticket
                {
                    Id = "TCKT002",
                    Title = "General Admission",
                    Description = "Standard entry ticket for the concert.",
                    EventId = "EVT001", // Match this with an existing Event Id
                    Status = "Available",
                    Price = 50.00m,
                    Quantity = 80,
                    Zone = "General",
                    Image = "general_admission.jpg",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Admin"
                },
                new Ticket
                {
                    Id = "TCKT003",
                    Title = "Exhibition Pass",
                    Description = "Access to the art exhibition.",
                    EventId = "EVT002", // Match this with an existing Event Id
                    Status = "Available",
                    Price = 25.00m,
                    Quantity = 50,
                    Zone = "Standard",
                    Image = "exhibition_pass.jpg",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Admin"
                },
                new Ticket
                {
                    Id = "TCKT004",
                    Title = "Full Access Pass",
                    Description = "Access to all areas of the tech conference.",
                    EventId = "EVT003", // Match this with an existing Event Id
                    Status = "Available",
                    Price = 150.00m,
                    Quantity = 100,
                    Zone = "All-Access",
                    Image = "conference_full.jpg",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Admin"
                },
                new Ticket
                {
                    Id = "TCKT005",
                    Title = "Day Pass",
                    Description = "Access to the food festival for one day.",
                    EventId = "EVT004", // Match this with an existing Event Id
                    Status = "Available",
                    Price = 10.00m,
                    Quantity = 300,
                    Zone = "General",
                    Image = "food_day_pass.jpg",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Admin"
                },
                new Ticket
                {
                    Id = "TCKT006",
                    Title = "Screening Ticket",
                    Description = "Single ticket for the film screening.",
                    EventId = "EVT005", // Match this with an existing Event Id
                    Status = "Available",
                    Price = 30.00m,
                    Quantity = 80,
                    Zone = "Cinema",
                    Image = "film_screening_ticket.jpg",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Admin"
                }
            );
        }
    }
}
