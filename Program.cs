using EcoQuest.Controllers;
using EcoQuest.Models;
using EcoQuest.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EcoQuest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<eco_questContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidIssuer = AuthenticationOptions.ISSUER,
                    ValidateAudience = true,
                    ValidAudience = AuthenticationOptions.AUDIENCE,
                    ValidateLifetime = true,
                    IssuerSigningKey = AuthenticationOptions.GetSymmetricSecurityKey(),
                    ValidateIssuerSigningKey = true
                };
            });
            builder.Services.AddAuthorization();

            WebApplication app = builder.Build();

            app.UseAuthentication();
            app.UseAuthorization();

            ApplicationService applicationService = new ApplicationService(app);
            ApplicationController applicationController = new ApplicationController(app, applicationService);

            applicationController.Map();

            app.Run();
        }
    }
}