using Autofac;
using BlazorShell.Application.Services;
using BlazorShell.Infrastructure.Services;
using BlazorShell.Domain.Repositories;
using BlazorShell.Infrastructure.Repositories;
using BlazorShell.Domain.Events;
using BlazorShell.Infrastructure.Events;

public class InfrastructureModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register infrastructure services
        builder.RegisterType<EmailService>().As<IEmailService>().InstancePerLifetimeScope();
        builder.RegisterType<FileStorageService>().As<IFileStorageService>().InstancePerLifetimeScope();
        builder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
        builder.RegisterType<ModuleRepository>().As<IModuleRepository>().InstancePerLifetimeScope();
        builder.RegisterType<UserRepository>().As<IUserRepository>().InstancePerLifetimeScope();
        builder.RegisterType<AuditLogRepository>().As<IAuditLogRepository>().InstancePerLifetimeScope();
        builder.RegisterType<DomainEventDispatcher>().As<IDomainEventDispatcher>().SingleInstance();
    }
}

