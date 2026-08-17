namespace FinanceApp.Api.Middleware;

/// <summary>
/// 安全响应头中间件
/// 为所有 HTTP 响应添加安全相关的头部，降低 XSS、点击劫持等攻击风险。
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 在响应开始写入之前注册回调，添加安全头
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // 防止浏览器进行 MIME 类型嗅探，强制使用服务器声明的 Content-Type
            headers["X-Content-Type-Options"] = "nosniff";

            // 禁止页面被嵌入 iframe，防止点击劫持攻击
            headers["X-Frame-Options"] = "DENY";

            // 禁用旧版浏览器的 XSS Auditor（现代浏览器推荐设为 0，由 CSP 接管防护）
            headers["X-XSS-Protection"] = "0";

            // 控制 Referer 头的发送策略，防止敏感 URL 信息泄露
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // 内容安全策略：限制页面可加载资源的来源
            // - default-src 'self'：默认只允许同源资源
            // - script-src 'self'：脚本只允许同源（禁止内联脚本，有效防御 XSS）
            // - style-src 'self' 'unsafe-inline'：允许内联样式（Element Plus 组件库需要）
            // - img-src 'self' data:：允许同源图片和 data URI（Base64 图片）
            // - font-src 'self'：字体只允许同源
            // - connect-src：开发环境允许常用开发端口，生产环境只允许同源
            var connectSrc = _env.IsDevelopment()
                ? "connect-src 'self' http://localhost:5187 http://127.0.0.1:5187 http://localhost:5173 http://127.0.0.1:5173"
                : "connect-src 'self'";

            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "font-src 'self'; " +
                connectSrc;

            // 权限策略：禁用不需要的浏览器 API，减少攻击面
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
