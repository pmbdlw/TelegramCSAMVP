using CSASystemAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace CSASystemAPI.Controllers;

/// <summary>
/// 客服账号接口
/// </summary>
[ApiController]
[Route("[controller]")]
public class CustomerServiceAccountController : ControllerBase
{
// 1. 增删改查客服账号
// 2. Leader客服给下属客服分配账号
// 3. Leader客服回收下属客服账号
// 4. 客服登录/退出
// 5. 加载客服管理的Telegram账号列表
// 6. 获取客服管理的Telegram账号未读消息
// 7. 实时接收telegram通知
    private readonly ILogger<CustomerServiceAccountController> _logger;

    public CustomerServiceAccountController(ILogger<CustomerServiceAccountController> logger)
    {
        _logger = logger;
    }
    
    [HttpPost]
    public IActionResult Login([FromBody] CustomerServiceAccount customerServiceAccount)
    {
        return Ok(customerServiceAccount);
    }
    
    [HttpGet]
    public IEnumerable<CustomerServiceAccount> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new CustomerServiceAccount(index, $"user-{index}", index % 2 == 0))
            .ToArray();
    }
}