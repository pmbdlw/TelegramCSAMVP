using System.Net;
using Microsoft.AspNetCore.Mvc;
using TelegramCSAMVP.Web.Services;

namespace TelegramCSAMVP.Web.Controllers;

public class TelegramManagementController : Controller
{
    private readonly TelegramServices _tgServices;
    public TelegramManagementController(TelegramServices tgServices)
    {
        _tgServices = tgServices;
    }
    // GET
    public async Task<IActionResult> Index()
    {
        var result = await _tgServices.GetMyTGAccountInfo("+64273126510");
         
        return View(model:result);
    }
    
    
    // GET
    public IActionResult Contacts(string phonenumber)
    {
        
        return View();
    }
    public async Task<IActionResult> Conversations(string phoneNumber)
    {
        phoneNumber ??= "+64273126510";
        var result = await _tgServices.GetMyTGConversations(phoneNumber);
        ViewBag.PhoneNumber = phoneNumber;
        return View(result);
    }
    
    // GET
    public async Task<IActionResult> Messages(string conversationId)
    {
        var result = await _tgServices.GetMyTGMessages("+64273126510",conversationId);
        ViewBag.PhoneNumber = WebUtility.UrlEncode("+64273126510");
        ViewBag.ConversationId = conversationId;
        return View(result);
    }
}