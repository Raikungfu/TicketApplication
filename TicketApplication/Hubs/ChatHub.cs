using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketApplication.Data;
using TicketApplication.Models.ViewModel;

namespace TicketApplication.Hubs
{
    public class ChatHub : Hub
    {
        public readonly static List<UserViewModel> _Connections = new List<UserViewModel>();
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Leave(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        }

        public override async Task OnConnectedAsync()
        {
            // Logic for when a user connects
            // ...
        }

        public async Task JoinRoom(string roomName, string userName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
            await Clients.Group(roomName).SendAsync("ReceiveNewUserNotification", userName);
        }

        public async Task SendMessageToRoom(string roomName, string messageContent)
        {
            var userName = Context.User.Identity.Name;
            var message = new MessageViewModel
            {
                Room = roomName,
                FromUserName = userName,
                Content = messageContent,
                Timestamp = DateTime.UtcNow
            };

            await Clients.Group(roomName).SendAsync("ReceiveMessage", message)
                 .ConfigureAwait(false);
        }

        public async Task NewMessage(string roomName, MessageViewModel message)
        {
            await Clients.OthersInGroup(roomName).SendAsync("ReceiveMessage", message)
                 .ConfigureAwait(false);
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Logic for when a user disconnects
            // ...
        }
    }

}
