using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using LogJoint.AutoUpdate;

namespace LogJoint.UI.Presenters
{
    public static class AutoUpdateUtils
    {
        public struct AutoUpdateStatusPresentation
        {
            public string Brief;
            public string Details;
            public bool CanCheckNow;
            public bool Enabled;
        };

        public static AutoUpdateStatusPresentation GetPresentation(this IAutoUpdater model, bool preferShortBrief)
        {
            var brief = new StringBuilder();
            string details = null;
            bool canCheckNow = false;
            bool enabled = true;
            switch (model.State)
            {
                case AutoUpdateState.Disabled:
                case AutoUpdateState.Inactive:
                    brief.Append("NA");
                    enabled = false;
                    break;
                case AutoUpdateState.WaitingRestart:
                    if (preferShortBrief)
                        brief.Append("Restart app to apply update");
                    else
                        brief.Append("New update was downloaded. Restart LogJoint to apply it.");
                    break;
                case AutoUpdateState.Checking:
                    brief.Append("checking for new update...");
                    break;
                case AutoUpdateState.Idle:
                    var lastCheckResult = model.LastUpdateCheckResult;
                    if (lastCheckResult == null)
                    {
                        brief.Append("never checked for update");
                    }
                    else
                    {
                        if (lastCheckResult.ErrorMessage == null)
                        {
                            brief.AppendFormat("You're up to date as of {0}", lastCheckResult.When.ToLocalTime());
                        }
                        else
                        {
                            brief.AppendFormat("update at {0} failed.", lastCheckResult.When.ToLocalTime());
                            details = lastCheckResult.ErrorMessage;
                        }
                    }
                    canCheckNow = true;
                    break;
                case AutoUpdateState.Failed:
                    brief.Append("failure");
                    break;
                case AutoUpdateState.FailedDueToBadInstallationDirectory:
                    brief.Append("bad intallation directory detected.");
                    details = string.Format(
                        @"For automtaic updates to work LogJoint must be installed" +
                        " to a directory allowed to be written by the current user ({0}).",
                        Environment.UserName
                    );
                    break;
                default:
                    brief.Append("?");
                    enabled = false;
                    break;
            }
            return new AutoUpdateStatusPresentation()
            {
                Brief = brief.ToString(),
                CanCheckNow = canCheckNow,
                Enabled = enabled,
                Details = details
            };
        }
    };
};