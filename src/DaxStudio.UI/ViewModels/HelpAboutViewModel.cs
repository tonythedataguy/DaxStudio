﻿using System;
using Caliburn.Micro;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection;
using DaxStudio.UI.Events;
using DaxStudio.Interfaces;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using DaxStudio.Common;

namespace DaxStudio.UI.ViewModels
{
    [Export]
    public class HelpAboutViewModel : Screen
    {
        private IEventAggregator _eventAggregator;
        private IDaxStudioHost _host;

        [ImportingConstructor]
        public HelpAboutViewModel(IEventAggregator eventAggregator, IVersionCheck checker, IDaxStudioHost host) {
            _eventAggregator = eventAggregator;
            _host = host;
            DisplayName = "About DaxStudio";
            CheckingUpdateStatus = true;
            UpdateStatus = "Checking for Updates...";
            NotifyOfPropertyChange(() => UpdateStatus);

            // start version check async
            VersionChecker = checker;
            VersionChecker.PropertyChanged += VersionChecker_PropertyChanged;
            Task.Run(() => 
                {
                    this.VersionChecker.Update(); 
                })
                .ContinueWith((previous)=> {
                    // todo - should we be checking for exceptions in this continuation
                    CheckingUpdateStatus = false;
                    UpdateStatus = VersionChecker.VersionStatus;
                    VersionIsLatest = VersionChecker.VersionIsLatest;
                    DownloadUrl = VersionChecker.DownloadUrl;
                    NotifyOfPropertyChange(() => VersionIsLatest);
                    NotifyOfPropertyChange(() => DownloadUrl);
                    NotifyOfPropertyChange(() => UpdateStatus);
                    NotifyOfPropertyChange(() => CheckingUpdateStatus);
                },TaskScheduler.Default);
        }

        void VersionChecker_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "VersionStatus")
            {
                NotifyOfPropertyChange(() => UpdateStatus);
            }
        }

        public string FullVersionNumber
        {
            get { return System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(3); }
        }

        public string BuildNumber
        {
            get { return System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(); }
        }

        //[Import(typeof(IVersionCheck))]
        public IVersionCheck VersionChecker { get; set; }

        public SortedList<string,string> ReferencedAssemblies
        {
            get
            {
                var l = new SortedList<string,string>();
                var ass = System.Reflection.Assembly.GetExecutingAssembly();
                foreach (var a in ass.GetReferencedAssemblies())
                {
                    if (!l.ContainsKey(a.Name))
                    {
                        l.Add(a.Name, a.Version.ToString());
                    }
                }
                return l; 
            }
        }
        public void Ok()
        {
            TryClose();
        }

        public bool CheckingUpdateStatus
        {
            get;
            set;
        }

        public string UpdateStatus
        {
            get;
            set;
        }
        public string DownloadUrl { get; private set; }
        public bool VersionIsLatest { get; private set; }

        public bool IsLoggingEnabled { get { return _host.DebugLogging; } }

        public void OpenLogFolder()
        {
            Process.Start(Constants.LogFolder);
        }

        public string LogFolder { get { return @"file:///" + Environment.ExpandEnvironmentVariables(Constants.LogFolder); } }
    }

    public class ReferencedAssembly
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }
}
