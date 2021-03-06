﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using FacetedWorlds.MyCon.ViewModels;

namespace FacetedWorlds.MyCon.Schedule.Views
{
    public partial class SlotView : PhoneApplicationPage
    {
        public SlotView()
        {
            InitializeComponent();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            Guid conferenceId;
            if (!Guid.TryParse(NavigationContext.QueryString["ConferenceId"], out conferenceId))
                return;

            Guid timeId;
            if (!Guid.TryParse(NavigationContext.QueryString["TimeId"], out timeId))
                return;

            ViewModelLocator locator = Application.Current.Resources["Locator"] as ViewModelLocator;
            if (locator != null)
                DataContext = locator.GetSlotViewModel(conferenceId, timeId);
        }
    }
}