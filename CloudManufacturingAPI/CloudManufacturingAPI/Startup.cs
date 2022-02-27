using CloudManufacturingAPI.Repositories.Machine;
using CloudManufacturingAPI.Repositories.SystemManagement;
using CloudManufacturingAPI.Repositories.Work;
using CloudManufacturingAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace CloudManufacturingAPI
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
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                    });
            });
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Cloud Manufacturing API - Documentation",
                    Version = "v1",
                    Description = "Example API for Cloud Manufacturing systems - ProyGrado - Santiago Chiappa, Emiliano Videla.",
                });
            });
            services.AddSingleton<IMachineRepository, MachineRepository>();
            services.AddSingleton<IWorkRepository, WorkRepository>();
            services.AddSingleton<ISystemRepository, SystemRepository>();
            services.AddSingleton<IScheduler, Scheduler>();
            services.AddSingleton<IMachineHttpClient, MachineHttpClient>();
            var instance = CloudManufacturingDBAccess.DBAccess.GetInstance();
            instance.Initialize(Configuration.GetConnectionString("DefaultConnection"));
            services.AddSingleton<IHostedService, MonitoringService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cloud Manufacturing API V1");
                c.DocumentTitle = "Cloud Manufacturing API";
                c.RoutePrefix = string.Empty;
            });
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
