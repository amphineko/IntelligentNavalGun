﻿using Sakuno.KanColle.Amatsukaze.Game.Proxy;
using Sakuno.KanColle.Amatsukaze.Game.Services;
using Sakuno.KanColle.Amatsukaze.Services;
using Sakuno.KanColle.Amatsukaze.Services.Browser;
using Sakuno.KanColle.Amatsukaze.ViewModels;
using Sakuno.KanColle.Amatsukaze.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Sakuno.KanColle.Amatsukaze
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public static MainWindowViewModel Root { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUtil.UIDispatcher = Dispatcher;

            Environment.CurrentDirectory = Path.GetDirectoryName(GetType().Assembly.Location);

            if (!Debugger.IsAttached)
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            base.OnStartup(e);

            if (e.Args.Length >= 3 && e.Args[0] == "browser")
            {
                var rLayoutEngine = e.Args[1];
                var rHostProcessID = int.Parse(e.Args[2]);

                new BrowserWrapper(rLayoutEngine, rHostProcessID);

                Task.Factory.StartNew(() =>
                {
                    Process.GetProcessById(rHostProcessID).WaitForExit();
                    Process.GetCurrentProcess().Kill();
                }, TaskCreationOptions.LongRunning);

                return;
            }

            StringResources.Instance.Initialize();
            StatusBarService.Instance.Initialize();
            RecordService.Instance.Initialize();
            QuestProgressService.Instance.Initialize();
            MapService.Instance.Initialize();
            ExpeditionService.Instance.Initialize();
            EnemyEncounterService.Instance.Initialize();

            Preference.Load();
            StringResources.Instance.LoadMainResource(Preference.Current.Language);
            StringResources.Instance.LoadExtraResource(Preference.Current.ExtraResourceLanguage);

            CacheService.Instance.Initialize();
            NotificationService.Instance.Initialize();

            PluginService.Instance.Initialize();

            KanColleProxy.Start();

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            Task.Factory.StartNew(UpdateService.Instance.CheckForUpdate);

            if (e.Args.Any(r => r.OICEquals("--background")))
                return;

            MainWindow = new MainWindow();
            MainWindow.DataContext = Root = new MainWindowViewModel();
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Preference.Save();
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ExceptionObject.ToString(), ProductInfo.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }
}
