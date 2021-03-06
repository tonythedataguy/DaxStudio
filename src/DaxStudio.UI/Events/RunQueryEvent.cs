﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DaxStudio.UI.Model;
using DaxStudio.UI.Interfaces;

namespace DaxStudio.UI.Events
{
    public class RunQueryEvent
    {
        public RunQueryEvent(IResultsTarget target)
        {
            ResultsTarget = target;
            ClearCache = false;
        }
        public RunQueryEvent(IResultsTarget target, bool clearCache)
        {
            ResultsTarget = target;
            ClearCache = clearCache;
        }
        public IResultsTarget ResultsTarget { get; set; }
        public bool ClearCache { get; set; }
    }
}
