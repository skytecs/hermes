using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Utilities
{
    public class BasicAuthenticationMiddleware 
    {
        private readonly ILogger<BasicAuthenticationMiddleware> _logger;
        private readonly RequestDelegate _next;
        private readonly string _password;

        public BasicAuthenticationMiddleware(RequestDelegate next, string password, ILogger<BasicAuthenticationMiddleware> logger)
        {
            Check.NotNull(logger, nameof(logger));
            Check.NotEmpty(password, nameof(password));
            _logger = logger;
            _password = password;
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            string authHeader = context.Request.Headers["Authorization"];

            try
            {
                if (authHeader != null && authHeader.StartsWith("Basic"))
                {
                    string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();


                    string usernamePassword = new UTF8Encoding().GetString(Convert.FromBase64String(encodedUsernamePassword));

                    int seperatorIndex = usernamePassword.IndexOf(':');

                    var username = usernamePassword.Substring(0, seperatorIndex);
                    var password = usernamePassword.Substring(seperatorIndex + 1);

                    if (string.CompareOrdinal(password, _password) == 0)
                    {
                        context.User = new ClaimsPrincipal(new ClaimsIdentity("Basic", username, password));
                        return _next(context);
                    }
                }
            }
            catch(Exception e)
            {
                _logger.Error(e);
            }

            context.Response.StatusCode = 403;
            return context.Response.WriteAsync("Password is incorrect.");

        }
    }
}
