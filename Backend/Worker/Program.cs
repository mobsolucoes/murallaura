using HashtagWall.Infrastructure;
using HashtagWall.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<InstagramWebRpaOptions>(
    builder.Configuration.GetSection(InstagramWebRpaOptions.SectionName));
builder.Services.AddScoped<InstagramWebRpaSyncService>();
builder.Services.AddHostedService<InstagramPollingWorker>();

var host = builder.Build();
host.Run();
