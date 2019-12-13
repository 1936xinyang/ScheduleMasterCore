using System;
using System.Linq;
using System.Reflection;
using Hos.ScheduleMaster.Core;
using Hos.ScheduleMaster.Core.Models;
using Hos.ScheduleMaster.Core.Repository;
using Hos.ScheduleMaster.Web.Extension;
using Hos.ScheduleMaster.Web.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
            //services.AddControllersWithViews();
            //services.AddControllers();
            services.AddHosControllers(this);
            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(GlobalExceptionFilter));
                //options.OutputFormatters.Add(new SystemTextJsonOutputFormatter(
                //    new System.Text.Json.JsonSerializerOptions
                //    {

                //    }
                //    ));
            }).AddNewtonsoftJson(option =>
            {
                //option.JsonSerializerOptions.PropertyNamingPolicy = null;
                ////����ѭ������
                option.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                //��ʹ���շ���ʽ��key
                option.SerializerSettings.ContractResolver = new DefaultContractResolver();
                //����ʱ���ʽ
                option.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
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
            services.AddDbContext<SmDbContext>(option => option.UseMySql(Configuration.GetConnectionString("MysqlConnection")));

            //ע��Uow����
            services.AddScoped<IUnitOfWork, UnitOfWork<SmDbContext>>();
            //�Զ�ע������ҵ��service
            services.AddAppServices();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseCookiePolicy();
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllers();
                endpoints.MapControllerRoute(
                     name: "default",
                     pattern: "{controller}/{action=Index}/{id?}");
            });
            //����ȫ�ֻ���
            ConfigurationCache.RootServiceProvider = app.ApplicationServices;
            ConfigurationCache.SetNode(Configuration.GetSection("NodeSetting").Get<NodeSetting>());
            ConfigurationCache.Reload();
            //��ʼ����־������
            Core.Log.LogManager.Init();
            //ע��ڵ�
            AppStart.NodeRegistry.Register();
            //��ʼ��ϵͳ����
            FluentScheduler.JobManager.Initialize(new AppStart.SystemSchedulerRegistry());
            FluentScheduler.JobManager.JobException += info => Core.Log.LogHelper.Error("An error just happened with a FluentScheduler job: ", info.Exception);

            appLifetime.ApplicationStopping.Register(OnStopping);
        }
        private void OnStopping()
        {
            // Perform on-stopping activities here
            Core.Log.LogManager.Shutdown();
        }

    }

    public class HosControllerActivator : IControllerActivator
    {
        public object Create(ControllerContext actionContext)
        {
            var controllerType = actionContext.ActionDescriptor.ControllerTypeInfo.AsType();
            var instance = actionContext.HttpContext.RequestServices.GetRequiredService(controllerType);
            PropertyActivate(instance, actionContext.HttpContext.RequestServices);
            return instance;
        }

        public virtual void Release(ControllerContext context, object controller)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }
            if (controller is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void PropertyActivate(object service, IServiceProvider provider)
        {
            var serviceType = service.GetType();
            var properties = serviceType.GetProperties().AsEnumerable().Where(x => x.Name.StartsWith("_"));
            foreach (PropertyInfo property in properties)
            {
                var autowiredAttr = property.GetCustomAttribute<AutowiredAttribute>();
                if (autowiredAttr != null)
                {
                    //��DI������ȡʵ��
                    var innerService = provider.GetService(property.PropertyType);
                    if (innerService != null)
                    {
                        //�ݹ�������Ƕ������
                        PropertyActivate(innerService, provider);
                        //���Ը�ֵ
                        property.SetValue(service, innerService);
                    }
                }
            }
        }
    }
}
