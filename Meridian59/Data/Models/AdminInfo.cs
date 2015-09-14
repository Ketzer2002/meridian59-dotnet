﻿/*
 Copyright (c) 2012-2013 Clint Banzhaf
 This file is part of "Meridian59 .NET".

 "Meridian59 .NET" is free software: 
 You can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, 
 either version 3 of the License, or (at your option) any later version.

 "Meridian59 .NET" is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 See the GNU General Public License for more details.

 You should have received a copy of the GNU General Public License along with "Meridian59 .NET".
 If not, see http://www.gnu.org/licenses/.
*/

using System;
using System.ComponentModel;
using Meridian59.Common.Interfaces;
using Meridian59.Data.Lists;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Meridian59.Data.Models
{
    /// <summary>
    /// A set of information for the admin console
    /// </summary>
    [Serializable]
    public class AdminInfo : INotifyPropertyChanged, IClearable
    {        
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null) PropertyChanged(this, e);
        }

        #endregion

        #region Fields
        protected readonly BindingList<string> serverResponses = new BindingList<string>();
        protected readonly BaseList<AdminInfoObject> trackedObjects = new BaseList<AdminInfoObject>();
        #endregion

        #region Properties
        /// <summary>
        /// Received text responses from server
        /// </summary>
        public BindingList<string> ServerResponses { get { return serverResponses; } }

        /// <summary>
        /// Tracking responses to 'show object' responses here
        /// </summary>
        public BaseList<AdminInfoObject> TrackedObjects { get { return trackedObjects; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public AdminInfo()
        {
            Clear(false);
        }

        #endregion

        #region IClearable
        public void Clear(bool RaiseChangedEvent)
        {
            if (RaiseChangedEvent)
            {
                serverResponses.Clear();
                trackedObjects.Clear();
            }
            else
            {
                serverResponses.Clear();
                trackedObjects.Clear();
            }
        }
        #endregion

        public void ProcessServerResponse(string Text)
        {
            // track it (for console)
            serverResponses.Add(Text);

            /*** regex processing ***/

            // 1) Test for object info
            AdminInfoObject adminInfoObj = AdminInfoObject.TryParse(Text);

            if (adminInfoObj != null)
            {
                trackedObjects.Add(adminInfoObj);
                return;
            }
        }
    }
}
