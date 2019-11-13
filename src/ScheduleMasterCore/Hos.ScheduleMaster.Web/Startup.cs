using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hos.ScheduleMaster.Core;
using Hos.ScheduleMaster.Core.Models;
using Hos.ScheduleMaster.Core.Repository;
using Hos.ScheduleMaster.Web.Extension;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql;

namespace Hos.ScheduleMaster.Web
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
            services.AddMemoryCache();
            services.AddOptions();
            services.AddHttpContextAccessor();
            services.AddControllersWithViews();
            services.AddMvc(options =>
            {
                //options.OutputFormatters.Add(new SystemTextJsonOutputFormatter(
                //    new System.Text.Json.JsonSerializerOptions
                //    {

                //    }
                //    ));
            }).AddJsonOptions(option =>
            {
                ////����ѭ������
                //option.JsonSerializerOptions.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                ////��ʹ���շ���ʽ��key
                //option.SerializerSettings.ContractResolver = new DefaultContractResolver();
                ////����ʱ���ʽ
                //option.SerializerSettings.DateFormatString = "yyyy-MM-dd";
            });

            //����authorrize
            services.AddAuthentication(b =>
            {
                b.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                b.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                b.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddCookie(b =>
            {
                b.LoginPath = "/login";
                b.Cookie.Name = "msc_auth_name";
                b.Cookie.Path = "/";
                b.Cookie.HttpOnly = true;
                //b.Cookie.Expiration = new TimeSpan(2, 0, 0);
                b.ExpireTimeSpan = new TimeSpan(2, 0, 0);
            });
            //EF���ݿ�������
            services.AddDbContext<TaskDbContext>(option => option.UseMySql(Configuration.GetConnectionString("MysqlConnection")));

            //ע��Uow����
            services.AddScoped<IUnitOfWork, UnitOfWork<TaskDbContext>>();
            //�Զ�ע������ҵ��service
            services.AddAppServices();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCookiePolicy();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllers();
                endpoints.MapControllerRoute(
                     name: "default",
                     pattern: "{controller=Login}/{action=Index}/{id?}");
            });

            //����ȫ�ֻ���
            ConfigurationCache.ServiceProvider = app.ApplicationServices;
            ConfigurationCache.Refresh();
            //using (var serviceScope = app.ApplicationServices.CreateScope())
            //{
            //    var context = serviceScope.ServiceProvider.GetService<Core.Repository.IUnitOfWork>();

            //    // Seed the database.
            //}
            //var sd = app.ApplicationServices.GetRequiredService<Core.Models.TaskDbContext>();
        }
    }
}
