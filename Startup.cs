using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using IdentityApp.Models;
using IdentityApp.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace IdentityApp
{
    public class Startup
    {
        public Startup(IConfiguration config) => Configuration = config;
        private IConfiguration Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddDbContext<ProductDbContext>(opts => {
                opts.UseSqlServer(
                    Configuration["ConnectionStrings:AppDataConnection"]);
            });
            services.AddHttpsRedirection(opts => {
                opts.HttpsPort = 44350;
            });
            services.AddDbContext<IdentityDbContext>(opts => {
                opts.UseSqlServer(
                    Configuration["ConnectionStrings:IdentityConnection"],
                    opts => opts.MigrationsAssembly("IdentityApp")
                );
            });
            services.AddScoped<IEmailSender, ConsoleEmailSender>();
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.SignIn.RequireConfirmedAccount = true;
                })
                .AddEntityFrameworkStores<IdentityDbContext>()
                .AddDefaultTokenProviders();
            services.Configure<SecurityStampValidatorOptions>(opts => {
                opts.ValidationInterval = System.TimeSpan.FromMinutes(1);
            });
            services.AddScoped<TokenUrlEncoderService>();
            services.AddScoped<IdentityEmailService>(); ;
            services.AddAuthentication()
                .AddFacebook(options => {
                    options.AppId = Configuration["Facebook:AppId"];
                    options.AppSecret = Configuration["Facebook:AppSecret"];
                })
                .AddGoogle(options => {
                    options.ClientId = Configuration["Google:ClientId"];
                    options.ClientSecret = Configuration["Google:ClientSecret"];
                }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opts => {
                    opts.TokenValidationParameters.ValidateAudience = false;
                    opts.TokenValidationParameters.ValidateIssuer = false;
                    opts.TokenValidationParameters.IssuerSigningKey
                        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                            Configuration["BearerTokens:Key"]));
                });
                //.AddTwitter(options => {
                //options.ConsumerKey = Configuration["Twitter:ApiKey"];
                //options.ConsumerSecret = Configuration["Twitter:ApiSecret"];
                //});
            services.ConfigureApplicationCookie(opts => {
                opts.LoginPath = "/Identity/SignIn";
                opts.LogoutPath = "/Identity/SignOut";
                opts.AccessDeniedPath = "/Identity/Forbidden";
                opts.Events.DisableRedirectionForApiClients();
            });
            services.AddCors(opts => {
                opts.AddDefaultPolicy(builder => {
                    builder.WithOrigins("http://localhost:5100")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();
            });
            app.SeedUserStoreForDashboard();
        }
    }
}
