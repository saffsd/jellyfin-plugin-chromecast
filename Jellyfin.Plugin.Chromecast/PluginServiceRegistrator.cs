using Jellyfin.Plugin.Chromecast.Discovery;
using Jellyfin.Plugin.Chromecast.Playback;
using Jellyfin.Plugin.Chromecast.Session;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Chromecast;

/// <summary>
/// Registers the background service that discovers Chromecast devices and exposes them as
/// controllable Jellyfin sessions.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHostedService<ChromecastHost>();
        serviceCollection.AddSingleton<IWebSocketListener, RemoteControlListener>();

        // Runs before every controller action so PlaybackInfo requests from a casting Chromecast
        // can have the Dolby audio codecs removed from the transcoding profiles they carry. Added
        // through Configure<MvcOptions> (not AddMvc) because plugins register before Jellyfin
        // calls AddJellyfinApi, and options configuration composes in either order.
        serviceCollection.Configure<MvcOptions>(options =>
            options.Filters.Add<DolbyAudioTranscodeFilter>());
    }
}
