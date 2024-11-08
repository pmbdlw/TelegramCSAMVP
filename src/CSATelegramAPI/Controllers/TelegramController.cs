using CSAFacade;
using CSATelegramAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace CSATelegramAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class TelegramController : ControllerBase
{
    private readonly ILogger<TelegramController> _logger;
    private readonly TelegramManager _telegramManager;

    public TelegramController(ILogger<TelegramController> logger)
    {
        _logger = logger;
        _telegramManager = TelegramManager.Instance;
        // _telegramManager.OnLoginCodeRequired += HandleLoginCodeRequired;
    }

    [HttpGet("Account/{phoneNumber}")]
    public async Task<IActionResult> GetAccount(string phoneNumber)
    {
        await _telegramManager.LoadPhoneNumbers([phoneNumber]);
        var account = _telegramManager.GetAccountInfo(phoneNumber);
        return Ok(account);
    }

    
    [HttpGet("Account/{phoneNumber}/Login")]
    public async Task<IActionResult> LoginClient(string phoneNumber)
    {
        try
        {
            var loginTask = _telegramManager.LoginClient(phoneNumber);
            await loginTask;
            return Ok(new { message = "Login successful" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    // [HttpPost("submit-code")]
    // public IActionResult SubmitCode([FromBody] SubmitCodeRequest request)
    // {
    //     try
    //     {
    //         _telegramManager.SubmitLoginCode(request.PhoneNumber, request.Code);
    //         return Ok(new { message = "Code submitted" });
    //     }
    //     catch (Exception ex)
    //     {
    //         return BadRequest(new { error = ex.Message });
    //     }
    // }
    
    [HttpGet("Account/{phoneNumber}/Logout")]
    public async Task<IActionResult> LogoutClient(string phoneNumber)
    {
        if (_telegramManager.GetAccountInfo(phoneNumber) != null)
        {
            _telegramManager.LogoutClient(phoneNumber);
        }
        return Ok();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <returns></returns>
    [HttpGet("Contacts")]
    public async Task<IActionResult> GetContacts(string phoneNumber)
    {
        await _telegramManager.LoadPhoneNumbers([phoneNumber]);
        var contacts = await _telegramManager.GetContacts(phoneNumber);
        return Ok(contacts);
    }
    
    [HttpGet("Messages")]
    public async Task<IActionResult> GetMessages(string phoneNumber, long conversationId, int limit = 100)
    {
        await _telegramManager.LoadPhoneNumbers([phoneNumber]);
        var messages = await _telegramManager.GetMessages(phoneNumber, conversationId, limit);
        return Ok(messages);
    }
    
    [HttpGet("Conversions")]
    public async Task<IActionResult> GetConversions(string phoneNumber)
    {
        await _telegramManager.LoadPhoneNumbers([phoneNumber]);
        var conversions = await _telegramManager.GetConversations(phoneNumber);
        return Ok(conversions);
    }
    
    [HttpPost("Messages/Send")]
    public async Task<IActionResult> SendMessage(string phoneNumber, long conversationId, string message)
    {
        await _telegramManager.LoadPhoneNumbers([phoneNumber]);
        await _telegramManager.SendMessage(phoneNumber, conversationId, message);
        return Ok();
    }
}