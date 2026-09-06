using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace KMU.HisOrder.MVC.Hubs
{
    public class ChatHub : Hub
    {
        // 用戶連線 ID 列表
        public static Dictionary<string, List<string>> ConnIDList = new Dictionary<string, List<string>>();

        /// <summary>
        /// 連線事件
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var irooms = httpContext.Request.Query["irooms"].ToString();
            if (string.IsNullOrWhiteSpace(irooms))
            {
                irooms = "clinic";  //null視為診間呼叫
            }

            if (ConnIDList.ContainsKey(Context.ConnectionId) == false)
            {
                ConnIDList.Add(Context.ConnectionId, irooms.Split('|').ToList());
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 離線事件
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            if (ConnIDList.ContainsKey(Context.ConnectionId))
            {
                ConnIDList.Remove(Context.ConnectionId);
            }             

            await base.OnDisconnectedAsync(ex);
        }

        /// <summary>
        /// 傳遞訊息
        /// </summary>
        public async Task SendMessage(string sendToRoom, string callNO)
        {
            var obj = new { iRoom = sendToRoom, iNO = callNO };
            var str = JsonConvert.SerializeObject(obj);

            if (string.IsNullOrWhiteSpace(sendToRoom) == false && ConnIDList.Any())
            {
                foreach(var item in ConnIDList)
                {
                    var rooms = item.Value;
                    if (rooms.Where(r => r == sendToRoom).Any())
                    {
                        await Clients.Client(item.Key).SendAsync("UpdContent", str);
                    }
                }
            }
        }
    }
}
