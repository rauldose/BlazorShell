using Autofac;
using BlazorShell.Application.Services;
using BlazorShell.Infrastructure.Services;

public class InfrastructureModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register infrastructure services
        builder.RegisterType<EmailService>().As<IEmailService>().InstancePerLifetimeScope();
        builder.RegisterType<FileStorageService>().As<IFileStorageService>().InstancePerLifetimeScope();
        builder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
    }
}

