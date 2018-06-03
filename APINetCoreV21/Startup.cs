using System;
using System.Threading;
using System.Threading.Tasks;
using APINetCoreV21.Entities;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Core;
using OpenIddict.Models;

namespace APINetCoreV21
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddDbContext<DatabaseContext>(opt =>
            {
                opt.UseSqlServer(Configuration.GetConnectionString("SecurityConnection"));
                opt.UseOpenIddict();
            });
            services.Configure<IdentityOptions>(opt =>
            {
                opt.ClaimsIdentity.UserNameClaimType = OpenIdConnectConstants.Claims.Name;
                opt.ClaimsIdentity.UserIdClaimType = OpenIdConnectConstants.Claims.Subject;
                opt.ClaimsIdentity.RoleClaimType = OpenIdConnectConstants.Claims.Role;
                opt.Password.RequireDigit = false;
                opt.Password.RequiredLength = 8;
                opt.Password.RequireLowercase = false;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireUppercase = false;
            });

            // Register the Identity services.
            services.AddIdentity<UserEntity, RoleEntity>()
                .AddEntityFrameworkStores<DatabaseContext>()
                .AddDefaultTokenProviders();

            // Add OpenIddict services
            services.AddOpenIddict(options =>
            {
                // Register the Entity Framework stores.
                options.AddEntityFrameworkCoreStores<DatabaseContext>();

                // Register the ASP.NET Core MVC binder used by OpenIddict
                options.AddMvcBinders();

                // Enable the authorization endpoints
                options.EnableTokenEndpoint("/api/token");


                options.AllowPasswordFlow();

            });

            // enable CORS 
            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // Add default data for Page table

                IServiceScopeFactory scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();

                using (IServiceScope scope = scopeFactory.CreateScope())
                {
                    var databaseContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<RoleEntity>>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();

                    bool createDefaultData = true; // we can not create default data to start dev server faster
                    if (createDefaultData)
                    {
                        CreateDefaultRoles(roleManager).Wait();
                        var user = new UserEntity
                        {
                            UserName = "admin",
                            Email = "admin@gmail.com",
                            FirstName = "Admin",
                            LastName = "Le",
                            IsActive = true
                        };
                        string password = "Admin@123";
                        CreateDefaultUsers(roleManager, userManager, user, password).Wait();
                    }
                }

            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseCors(
             options => options.WithOrigins("*")
             .AllowAnyMethod()
             .AllowAnyHeader()
            );

            app.UseAuthentication();

            app.UseMvc();

        }

        private static async Task CreateDefaultRoles(RoleManager<RoleEntity> roleManager)
        {
            await roleManager.CreateAsync(new RoleEntity(Constant.ROLE_OWNER));

        }
        private static async Task CreateDefaultUsers(RoleManager<RoleEntity> roleManager,
           UserManager<UserEntity> userManager,
           UserEntity user, string password)
        {
            var currentUser = await userManager.FindByNameAsync(user.UserName);
            if (currentUser == null)
            {
                await userManager.CreateAsync(user, password);
                await userManager.AddToRoleAsync(user, "OWNER");
                await userManager.UpdateAsync(user);
            }

        }
    }
}
