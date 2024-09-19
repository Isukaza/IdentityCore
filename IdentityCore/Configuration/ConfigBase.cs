namespace IdentityCore.Configuration;

public static class ConfigBase
{
    private static IConfigurationRoot _testConfiguration;

    /// <summary>
    /// Retrieves the current configuration.
    /// </summary>
    /// <returns>
    /// The current <see cref="IConfigurationRoot"/> if set; otherwise, a default configuration.
    /// </returns>
    /// <remarks>
    /// This method is intended for general use, including development, testing, and production.
    /// </remarks>
    public static IConfigurationRoot GetConfiguration() =>
        _testConfiguration ?? GetDefaultConfiguration();

    /// <summary>
    /// Sets the configuration for unit testing purposes.
    /// </summary>
    /// <param name="configuration">
    /// The <see cref="IConfigurationRoot"/> to be set for testing.
    /// </param>
    /// <remarks>
    /// This method is intended for use only in unit tests. It allows you to inject a 
    /// specific configuration into the system under test, overriding the default 
    /// configuration settings.
    /// </remarks>
    public static void SetConfiguration(IConfigurationRoot configuration)
    {
        _testConfiguration = configuration;
    }

    /// <summary>
    /// Creates a default configuration from the "appsettings.json" file located in the current directory.
    /// </summary>
    /// <returns>
    /// A <see cref="IConfigurationRoot"/> instance built from the default configuration file.
    /// </returns>
    private static IConfigurationRoot GetDefaultConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
}