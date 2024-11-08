using System.Collections;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Web;

namespace TelegramCSAMVP.Web.Services;

public class TelegramServices
{
     private readonly HttpClient _httpClient;
        
        public TelegramServices(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
    
        public async Task<string> GetMyTGAccountInfo(string phoneNumber)
        {
            // GET 请求
            var response = await _httpClient.GetAsync($"Telegram/Account/{phoneNumber}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        
        public async Task<List<Conversation>> GetMyTGConversations(string phoneNumber)
        {
            // GET 请求
            //var response = await _httpClient.GetFromJsonAsync<List<Conversation>>($"Telegram/Conversions?phoneNumber={phoneNumber}");
            var response = await _httpClient.GetAsync($"Telegram/Conversions?phoneNumber={phoneNumber}");
            
            //response.EnsureSuccessStatusCode();
            //var content = await response.Content.ReadAsStringAsync();
            //Console.WriteLine(content);
            return await response.Content.ReadFromJsonAsync<List<Conversation>>();
        } 
        public async Task<List<TGMessage>> GetMyTGMessages(string phoneNumber,string conversationId)
        {
            // GET 请求
            //var response = await _httpClient.GetFromJsonAsync<List<Conversation>>($"Telegram/Conversions?phoneNumber={phoneNumber}");
            var response = await _httpClient.GetAsync($"Telegram/Messages?phoneNumber={phoneNumber}&conversationId={conversationId}");
            //response.EnsureSuccessStatusCode();
            //var content = await response.Content.ReadAsStringAsync();
            //Console.WriteLine(content);
            return await response.Content.ReadFromJsonAsync<List<TGMessage>>();
        }
}

public record Conversation(long? Id,string Title,int Type,int UnreadCount,string LastMessageDate,string LastMessageText);

// {
// "id": 43965,
// "text": "文件名称: zhm2545723242.mp4\n\n分享链接: https://t.me/WangPanBOT?start=file163016c025a42b0f\n\n文件大小: 942.88 MB",
// "date": "2024-01-30T03:16:56Z",
// "fromId": 0,
// "isOutgoing": false,
// "media": {}
// },
public record TGMessage(long? Id,string Text, DateTime Date,long FromId, bool IsOutgoing,object Media=null);