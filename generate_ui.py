import os

ui_dir = r"E:\Quality diagnostics center\LabSystem.UI"
views_dir = os.path.join(ui_dir, "Views")
viewmodels_dir = os.path.join(ui_dir, "ViewModels")

os.makedirs(views_dir, exist_ok=True)
os.makedirs(viewmodels_dir, exist_ok=True)

ui_files = {
    "App.xaml": """<Application x:Class="LabSystem.UI.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <materialDesign:BundledTheme BaseTheme="Light" PrimaryColor="DeepPurple" SecondaryColor="Lime" />
                <DictionaryReference TargetType="materialDesign:Defaults" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
""",
    "App.xaml.cs": """using System;
using System.IO;
using System.Windows;
using LabSystem.Core.Interfaces;
using LabSystem.Data;
using LabSystem.Data.Repositories;
using LabSystem.Services;
using SimpleInjector;
using Serilog;

namespace LabSystem.UI
{
    public partial class App : Application
    {
        public static Container Container { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize Serilog
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDir);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(Path.Combine(logDir, "lab_.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Log.Fatal(args.ExceptionObject as Exception, "Unhandled exception occurred.");
                MessageBox.Show("A critical error occurred. Please check the logs.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            // Initialize DB
            using (var db = new LabDbContext())
            {
                db.Database.Initialize(false);
                
                // Run V1__init.sql if tables don't exist
                try
                {
                    db.Database.ExecuteSqlCommand("SELECT 1 FROM Patients LIMIT 1;");
                }
                catch
                {
                    string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "LabSystem.Data", "Migrations", "V1__init.sql");
                    if (File.Exists(scriptPath))
                    {
                        var sql = File.ReadAllText(scriptPath);
                        db.Database.ExecuteSqlCommand(sql);
                    }
                    
                    // Run seed if provided
                    string seedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "seed.sql");
                    if (File.Exists(seedPath))
                    {
                        var seedSql = File.ReadAllText(seedPath);
                        db.Database.ExecuteSqlCommand(seedSql);
                    }
                }
            }

            // Setup SimpleInjector
            Container = new Container();
            Container.Options.DefaultScopedLifestyle = new SimpleInjector.Lifestyles.ThreadScopedLifestyle();

            // Register DbContext
            Container.Register<LabDbContext>(Lifestyle.Scoped);

            // Register Repositories
            Container.Register(typeof(IRepository<>), typeof(Repository<>), Lifestyle.Scoped);
            Container.Register<IPatientRepository, PatientRepository>(Lifestyle.Scoped);
            Container.Register<ITestOrderRepository, TestOrderRepository>(Lifestyle.Scoped);
            Container.Register<IResultRepository, ResultRepository>(Lifestyle.Scoped);

            // Register Services
            Container.Register<IAuthService, AuthService>(Lifestyle.Scoped);
            Container.Register<IOrderService, OrderService>(Lifestyle.Scoped);
            Container.Register<IResultService, ResultService>(Lifestyle.Scoped);
            Container.Register<IPdfReportService, PdfReportService>(Lifestyle.Scoped);
            Container.Register<IBackupService, SqliteBackupService>(Lifestyle.Scoped);

            // Register ViewModels
            Container.Register<ViewModels.MainViewModel>();
            Container.Register<ViewModels.LoginViewModel>();

            Container.Verify();

            var mainWindow = new MainWindow();
            mainWindow.DataContext = Container.GetInstance<ViewModels.MainViewModel>();
            mainWindow.Show();
        }
    }
}
""",
    "App.config": """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <section name="entityFramework" type="System.Data.Entity.Internal.ConfigFile.EntityFrameworkSection, EntityFramework, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" requirePermission="false" />
  </configSections>
  <connectionStrings>
    <add name="LabDbContext" connectionString="Data Source=.\lab.db;Version=3;" providerName="System.Data.SQLite.EF6" />
  </connectionStrings>
  <entityFramework>
    <providers>
      <provider invariantName="System.Data.SqlClient" type="System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer" />
      <provider invariantName="System.Data.SQLite.EF6" type="System.Data.SQLite.EF6.SQLiteProviderServices, System.Data.SQLite.EF6" />
      <provider invariantName="System.Data.SQLite" type="System.Data.SQLite.EF6.SQLiteProviderServices, System.Data.SQLite.EF6" />
    </providers>
  </entityFramework>
  <system.data>
    <DbProviderFactories>
      <remove invariant="System.Data.SQLite.EF6" />
      <add name="SQLite Data Provider (Entity Framework 6)" invariant="System.Data.SQLite.EF6" description=".NET Framework Data Provider for SQLite (Entity Framework 6)" type="System.Data.SQLite.EF6.SQLiteProviderFactory, System.Data.SQLite.EF6" />
      <remove invariant="System.Data.SQLite" />
      <add name="SQLite Data Provider" invariant="System.Data.SQLite" description=".NET Framework Data Provider for SQLite" type="System.Data.SQLite.SQLiteFactory, System.Data.SQLite" />
    </DbProviderFactories>
  </system.data>
</configuration>
""",
    "ViewModels/ViewModelBase.cs": """using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LabSystem.UI.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
""",
    "ViewModels/RelayCommand.cs": """using System;
using System.Windows.Input;

namespace LabSystem.UI.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);
    }
}
""",
    "ViewModels/MainViewModel.cs": """using System.Windows.Input;
using LabSystem.Core.Interfaces;

namespace LabSystem.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;
        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set { _currentViewModel = value; OnPropertyChanged(); }
        }

        public ICommand BackupCommand { get; }

        public MainViewModel(IBackupService backupService)
        {
            // Initial view is Login
            CurrentViewModel = App.Container.GetInstance<LoginViewModel>();

            BackupCommand = new RelayCommand(o => {
                backupService.BackupNow();
                System.Windows.MessageBox.Show("Backup completed successfully.");
            });
        }
    }
}
""",
    "ViewModels/LoginViewModel.cs": """using System.Windows.Input;
using LabSystem.Core.Interfaces;

namespace LabSystem.UI.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private string _pin;
        private string _errorMessage;

        public string Pin
        {
            get => _pin;
            set { _pin = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            LoginCommand = new RelayCommand(ExecuteLogin);
        }

        private void ExecuteLogin(object obj)
        {
            // Simple mock for staffId = 1
            if (_authService.VerifyPin(1, Pin))
            {
                ErrorMessage = "Login successful! (Dashboard not yet implemented)";
                // Proceed to Dashboard...
            }
            else
            {
                ErrorMessage = "Invalid PIN.";
            }
        }
    }
}
""",
    "MainWindow.xaml": """<Window x:Class="LabSystem.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        mc:Ignorable="d"
        Title="Medical Lab Management System" Height="600" Width="800"
        TextElement.Foreground="{DynamicResource MaterialDesignBody}"
        TextElement.FontWeight="Regular"
        TextElement.FontSize="13"
        TextOptions.TextFormattingMode="Ideal"
        TextOptions.TextRenderingMode="Auto"
        Background="{DynamicResource MaterialDesignPaper}"
        FontFamily="{DynamicResource MaterialDesignFont}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <materialDesign:ColorZone Mode="PrimaryMid" Padding="16">
            <DockPanel>
                <TextBlock VerticalAlignment="Center" Margin="16 0 0 0" Style="{StaticResource MaterialDesignHeadline6TextBlock}">Lab Management</TextBlock>
                <Button Command="{Binding BackupCommand}" HorizontalAlignment="Right" DockPanel.Dock="Right" Style="{StaticResource MaterialDesignFlatButton}">BACKUP NOW</Button>
            </DockPanel>
        </materialDesign:ColorZone>

        <ContentControl Grid.Row="1" Content="{Binding CurrentViewModel}">
            <ContentControl.Resources>
                <DataTemplate DataType="{x:Type local:LoginViewModel}" xmlns:local="clr-namespace:LabSystem.UI.ViewModels">
                    <!-- Basic Login View Embedded for simplicity -->
                    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" Width="300">
                        <materialDesign:Card Padding="32" Margin="16">
                            <StackPanel>
                                <TextBlock Style="{StaticResource MaterialDesignHeadline5TextBlock}" Margin="0 0 0 16">Staff Login</TextBlock>
                                <PasswordBox materialDesign:HintAssist.Hint="Enter PIN (e.g. 1234)" 
                                             PasswordChanged="PinBox_PasswordChanged"
                                             Margin="0 0 0 16" />
                                <TextBlock Foreground="Red" Text="{Binding ErrorMessage}" Margin="0 0 0 8" />
                                <Button Command="{Binding LoginCommand}" Content="LOGIN" />
                            </StackPanel>
                        </materialDesign:Card>
                    </StackPanel>
                </DataTemplate>
            </ContentControl.Resources>
        </ContentControl>
    </Grid>
</Window>
""",
    "MainWindow.xaml.cs": """using System.Windows;
using System.Windows.Controls;
using LabSystem.UI.ViewModels;

namespace LabSystem.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void PinBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && passwordBox.DataContext is LoginViewModel loginVm)
            {
                loginVm.Pin = passwordBox.Password;
            }
        }
    }
}
"""
}

for name, content in ui_files.items():
    with open(os.path.join(ui_dir, name), "w", encoding="utf-8") as f:
        f.write(content)

print("UI files generated.")
