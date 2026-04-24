using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ElectronicInventoryManagementSystem.Helpers; // Gọi RoleConstants vào đây

namespace ElectronicInventoryManagementSystem.Helpers
{
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;

            if (user == null || !user.IsInRole(RoleConstants.Admin))
            {
                context.Result = new JsonResult(new
                {
                    message = "Lỗi bảo mật: Chỉ Admin mới được thực hiện hành động này!",
                    status = 403
                })
                {
                    StatusCode = 403
                };
            }
            base.OnActionExecuting(context);
        }
    }
}