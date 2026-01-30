        ConfigureServices((context, services) =>
        {
            // Services
//{[{
            services.AddTransient<INavigationViewService, NavigationViewService>();
            services.AddTransient<INavigationService, NavigationService>();
            services.AddTransient<INavigationConfigService, NavigationConfigService>();
//}]}

            // Views and ViewModels
//{[{
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();
            services.AddTransient<Views.MainPage>();
            services.AddTransient<MainViewModel>();
//}]}
