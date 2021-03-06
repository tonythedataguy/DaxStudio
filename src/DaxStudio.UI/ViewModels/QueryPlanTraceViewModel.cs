﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Caliburn.Micro;
using DaxStudio.UI.Events;
using DaxStudio.UI.Model;
using Microsoft.AnalysisServices;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;
using DaxStudio.UI.Interfaces;
using DaxStudio.QueryTrace;
using DaxStudio.Interfaces;
using System;
using System.Windows;
using Serilog;

namespace DaxStudio.UI.ViewModels
{
    public class QueryPlanRow {
        public string Operation { get;  set; }
        public string IndentedOperation { get;  set; }
        public int Level { get;  set; }
        public int RowNumber { get;  set; }

        private const int SPACE_PER_LEVEL = 4;
        public virtual void PrepareQueryPlanRow(string line, int rowNumber) {
            RowNumber = rowNumber;
            Level = line.Where(c => c == '\t').Count();
            Operation = line.Trim();
            IndentedOperation = new string(' ', Level * SPACE_PER_LEVEL) + Operation;
        }
        static public BindableCollection<T> PrepareQueryPlan<T>(string physicalQueryPlan,int startingRowNumber)
            where T : QueryPlanRow, new()
        {
            int rowNumber = startingRowNumber;
            return new BindableCollection<T>((
                from row in physicalQueryPlan.Split(new[] { '\r', '\n' })
                where row.Trim().Length > 0
                select row)
            .Select((line) => {
                var operation = new T();
                operation.PrepareQueryPlanRow(line, ++rowNumber);
                return operation;
            }).ToList());
        }

        static public BindableCollection<T> PrepareQueryPlan<T>(string physicalQueryPlan) 
            where T : QueryPlanRow, new() {
            return PrepareQueryPlan<T>(physicalQueryPlan, 0);
        }
    }

    public class PhysicalQueryPlanRow : QueryPlanRow {
        public long? Records { get; set; }

        private const string RecordsPrefix = @"#Records=";
        private const string searchRecords = RecordsPrefix + @"([0-9]*)";
        static Regex recordsRegex = new Regex(searchRecords,RegexOptions.Compiled);

        public override void PrepareQueryPlanRow(string line, int rowNumber) { 
            base.PrepareQueryPlanRow(line, rowNumber);
            var matchRecords = recordsRegex.Match(line);
            if (matchRecords.Success) {
                Records = int.Parse(matchRecords.Value.Substring(RecordsPrefix.Length));
            }
        }
    }

    public class LogicalQueryPlanRow : QueryPlanRow {

    }

    //[Export(typeof(ITraceWatcher)),PartCreationPolicy(CreationPolicy.NonShared)]
    class QueryPlanTraceViewModel: TraceWatcherBaseViewModel, ISaveState
    {
        [ImportingConstructor]
        public QueryPlanTraceViewModel(IEventAggregator eventAggregator, IGlobalOptions globalOptions) : base(eventAggregator, globalOptions)
        {
            _physicalQueryPlanRows = new BindableCollection<PhysicalQueryPlanRow>();
            _logicalQueryPlanRows = new BindableCollection<LogicalQueryPlanRow>();
        }

        protected override List<DaxStudioTraceEventClass> GetMonitoredEvents()
        {
            return new List<DaxStudioTraceEventClass> 
                { DaxStudioTraceEventClass.DAXQueryPlan
                , DaxStudioTraceEventClass.QueryEnd };
        }
    

        // This method is called after the WaitForEvent is seen (usually the QueryEnd event)
        // This is where you can do any processing of the events before displaying them to the UI
        protected override void ProcessResults()
        {

            foreach (var traceEvent in Events)
            {
                if (traceEvent.EventClass == DaxStudioTraceEventClass.DAXQueryPlan
                    && traceEvent.EventSubclass == DaxStudioTraceEventSubclass.DAXVertiPaqLogicalPlan)
                {
                    LogicalQueryPlanText = traceEvent.TextData;
                    PrepareLogicalQueryPlan(traceEvent.TextData);
                    NotifyOfPropertyChange(() => LogicalQueryPlanText);
                }
                if (traceEvent.EventClass == DaxStudioTraceEventClass.DAXQueryPlan 
                    && traceEvent.EventSubclass == DaxStudioTraceEventSubclass.DAXVertiPaqPhysicalPlan)
                {
                    PhysicalQueryPlanText = traceEvent.TextData;
                    PreparePhysicalQueryPlan(traceEvent.TextData);
                    NotifyOfPropertyChange(() => PhysicalQueryPlanText);
                }
                if (traceEvent.EventClass == DaxStudioTraceEventClass.QueryEnd)
                {
                    TotalDuration = traceEvent.Duration;
                    NotifyOfPropertyChange(() => TotalDuration);
                }
            }
        }

        public override void OnReset() {
            IsBusy = false;
            ClearAll();
            ProcessResults();
        }

        protected void PreparePhysicalQueryPlan(string physicalQueryPlan) 
        {
            _physicalQueryPlanRows.AddRange( QueryPlanRow.PrepareQueryPlan<PhysicalQueryPlanRow>(physicalQueryPlan, _physicalQueryPlanRows.Count));
            NotifyOfPropertyChange(() => PhysicalQueryPlanRows);
        }

        protected void PrepareLogicalQueryPlan(string logicalQueryPlan) {
            _logicalQueryPlanRows = QueryPlanRow.PrepareQueryPlan<LogicalQueryPlanRow>(logicalQueryPlan);
            NotifyOfPropertyChange(() => LogicalQueryPlanRows);
        }

        public string PhysicalQueryPlanText { get; private set; }
        public string LogicalQueryPlanText { get; private set; }
        public long TotalDuration { get; private set; }

        private BindableCollection<PhysicalQueryPlanRow> _physicalQueryPlanRows;
        private BindableCollection<LogicalQueryPlanRow> _logicalQueryPlanRows;

        private PhysicalQueryPlanRow _selectedPhysicalRow;
        public PhysicalQueryPlanRow SelectedPhysicalRow {
            get {
                return _selectedPhysicalRow;
            }
            set {
                _selectedPhysicalRow = value;
                NotifyOfPropertyChange(() => SelectedPhysicalRow);
            }
        }

        private LogicalQueryPlanRow _selectedLogicalRow;
        public LogicalQueryPlanRow SelectedLogicalRow {
            get {
                return _selectedLogicalRow;
            }
            set {
                _selectedLogicalRow = value;
                NotifyOfPropertyChange(() => SelectedLogicalRow);
            }
        }

        public BindableCollection<PhysicalQueryPlanRow> PhysicalQueryPlanRows {
            get {
                //var pqp = from r in _physicalQueryPlanRows
                //          select r;
                //return new BindableCollection<PhysicalQueryPlanRow>(pqp);
                return _physicalQueryPlanRows;
            }
            private set { _physicalQueryPlanRows = value; }
        }

        public BindableCollection<LogicalQueryPlanRow> LogicalQueryPlanRows {
            get {
                //var lqp = from r in _logicalQueryPlanRows
                //          select r;
                //return new BindableCollection<LogicalQueryPlanRow>(lqp);
                return _logicalQueryPlanRows;
            }
            private set { _logicalQueryPlanRows = value; }
        }
        
        // IToolWindow interface
        public override string Title
        {
            get { return "Query Plan"; }
            set { }
        }

        public override string ToolTipText
        {
            get
            {
                return "Runs a server trace to capture the Logical and Physical DAX Query Plans";
            }
            set { }
        }

        public override bool FilterForCurrentSession { get { return true; } }

        #region ISaveState Methods

        void ISaveState.Save(string filename)
        {
            var m = new QueryPlanModel()
            {
                PhysicalQueryPlanRows = this.PhysicalQueryPlanRows,
                LogicalQueryPlanRows = this.LogicalQueryPlanRows
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(m, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filename + ".queryPlans", json);
        }

        void ISaveState.Load(string filename)
        {
            filename = filename + ".queryPlans";
            if (!File.Exists(filename)) return;

            _eventAggregator.PublishOnUIThread(new ShowTraceWindowEvent(this));
            string data = File.ReadAllText(filename);
            QueryPlanModel m = JsonConvert.DeserializeObject<QueryPlanModel>(data);

            PhysicalQueryPlanRows = m.PhysicalQueryPlanRows;
            LogicalQueryPlanRows = m.LogicalQueryPlanRows;

            
            NotifyOfPropertyChange(() => PhysicalQueryPlanRows);
            NotifyOfPropertyChange(() => LogicalQueryPlanRows);
        }
        #endregion

        #region Title Bar Button Methods

        public override void ClearAll()
        {
            Events.Clear();
            _physicalQueryPlanRows.Clear();
            _logicalQueryPlanRows.Clear();
            NotifyOfPropertyChange(() => PhysicalQueryPlanRows);
            NotifyOfPropertyChange(() => LogicalQueryPlanRows);
        }

        public override void CopyAll()
        {
            Log.Warning("CopyAll method not implemented for QueryPlanTraceViewModel");
        }
        #endregion
    }
}
