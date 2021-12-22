﻿using DGP.Genshin.Common.Exceptions;
using DGP.Genshin.Common.Extensions.System;
using DGP.Genshin.Controls;
using DGP.Genshin.Core;
using DGP.Genshin.Helpers;
using DGP.Genshin.Helpers.Notifications;
using DGP.Genshin.Services.Abstratcions;
using DGP.Genshin.ViewModels;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.Notifications;
using ModernWpf;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DGP.Genshin
{
    /// <summary>
    /// Snap Genshin
    /// </summary>
    public partial class App : Application
    {
        private readonly ToastNotificationHandler toastNotificationHandler = new();
        private readonly SingleInstanceChecker singleInstanceChecker = new("Snap.Genshin");
        private readonly ServiceManager serviceManager = new();

        internal ServiceManager ServiceManager => serviceManager;

        /// <summary>
        /// 覆盖默认类型的 Current
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// 程序根路径
        /// </summary>
        public static string BaseDirectory => AppContext.BaseDirectory;

        #region Dependency Injection Helper
        /// <summary>
        /// 全局消息交换器
        /// </summary>
        public static WeakReferenceMessenger Messenger => WeakReferenceMessenger.Default;

        /// <summary>
        /// 获取应注入的服务
        /// 获取时应使用服务的接口类型
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        /// <exception cref="SnapGenshinInternalException">对应的服务类型未注册</exception>
        public static TService GetService<TService>()
        {
            return Current.serviceManager.Services.GetService<TService>()
                ?? throw new SnapGenshinInternalException($"无法找到 {typeof(TService)} 类型的服务");
        }

        /// <summary>
        /// 获取应注入的视图模型
        /// </summary>
        /// <typeparam name="TViewModel"></typeparam>
        /// <returns></returns>
        public static TViewModel GetViewModel<TViewModel>()
        {
            return GetService<TViewModel>();
        }
        #endregion

        #region LifeCycle
        protected override void OnStartup(StartupEventArgs e)
        {
            EnsureWorkingPath();
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            //handle notification activation
            SetupToastNotificationHandling();
            singleInstanceChecker.Ensure(Current);
            //file operation starts
            this.Log($"Snap Genshin - {Assembly.GetExecutingAssembly().GetName().Version}");
            GetService<ISettingService>().Initialize();
            //app theme
            UpdateAppTheme();

            ConfigureAppCenter();
            //open main window
            base.OnStartup(e);
        }

        private static void ConfigureAppCenter()
        {
            AppCenter.LogLevel = LogLevel.Verbose;
#if DEBUG
            //DEBUG INFO should send to Snap Genshin Debug kanban
            //cause the version of debug is always higher than normal release
            AppCenter.Start("2e4fa440-132e-42a7-a288-22ab1a8606ef", typeof(Analytics), typeof(Crashes));
#else
            //NORMAL INFO should send to Snap Genshin kanban
            AppCenter.Start("b95619e7-cdb2-407e-8cc8-818411c98f3a", typeof(Analytics), typeof(Crashes));
#endif
        }

        private void SetupToastNotificationHandling()
        {
            if (!ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
            {
                //remove toast last time not cleared if it's manually launched
                ToastNotificationManagerCompat.History.Clear();
            }
            ToastNotificationManagerCompat.OnActivated += toastNotificationHandler.OnActivatedByNotification;
        }

        /// <summary>
        /// 设置工作路径
        /// </summary>
        private void EnsureWorkingPath()
        {
            if (Path.GetDirectoryName(AppContext.BaseDirectory) is string workingPath)
            {
                Environment.CurrentDirectory = workingPath;
            }
        }
        private void UpdateAppTheme()
        {
            ThemeManager.Current.ApplicationTheme =
                GetService<ISettingService>().GetOrDefault(Setting.AppTheme, null, Setting.ApplicationThemeConverter);
        }
        protected override void OnExit(ExitEventArgs e)
        {
            if (!singleInstanceChecker.IsExitDueToSingleInstanceRestriction)
            {
                Analytics.TrackEvent("App exited");
                GetService<ISettingService>().UnInitialize();
                GetViewModel<MetadataViewModel>().UnInitialize();
                this.Log($"Exit code:{e.ApplicationExitCode}");
            }
            base.OnExit(e);
        }
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!singleInstanceChecker.IsEnsureingSingleInstance)
            {
                //unhandled exception can't be uploaded automatically
                //so we manually upload it by mark it as error
                Crashes.TrackError(e.ExceptionObject as Exception, new Info("Unhandled Exception", "Uploaded").Build());
                new ExceptionWindow((Exception)e.ExceptionObject).ShowDialog();
            }
        }
        #endregion
    }
}
