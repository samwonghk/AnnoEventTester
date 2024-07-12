using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Events;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;

namespace AnnoEventTester.Events
{
    internal class EventModule: Module
    {
        private static EventModule _this = null;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static EventModule Current => _this ??= (EventModule)FrameworkApplication.FindModule("AnnoEventTester_EventModule");

        private Dictionary<AnnotationLayer, SubscriptionToken> rowChangedTokens = new Dictionary<AnnotationLayer, SubscriptionToken>();
        private Dictionary<AnnotationLayer, SubscriptionToken> rowCreatedTokens = new Dictionary<AnnotationLayer, SubscriptionToken>();
        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            //TODO - add your business logic
            //return false to ~cancel~ Application close
            QueuedTask.Run(() =>
            {
                foreach (var token in rowChangedTokens.Values)
                {
                    RowChangedEvent.Unsubscribe(token);
                }
                foreach (var token in rowCreatedTokens.Values)
                {
                    RowCreatedEvent.Unsubscribe(token);
                }
            });
            return true;
        }

        #endregion Overrides

        protected override bool Initialize()
        {
            ProjectOpenedEvent.Subscribe(OnProjectLoaded);

            return base.Initialize();
        }

        private void OnProjectLoaded(ProjectEventArgs args)
        {
            RegisterAnnotationClasses();
        }

        private void RegisterAnnotationClasses()
        {
            QueuedTask.Run(() =>
            {
                var mapItem = Project.Current.GetItems<MapProjectItem>().FirstOrDefault(x => x.Name == "AMFM Editor");
                if (mapItem is null) return;
                var map = mapItem.GetMap();
                if (map is null) return;
                var layers = map.GetLayersAsFlattenedList().OfType<AnnotationLayer>();
                foreach (var layer in layers)
                {
                    if (layer.GetFeatureClass() is null) continue;
                    var featureClass = layer.GetFeatureClass();
                    var updateToken = RowChangedEvent.Subscribe(OnRowUpdated, featureClass);
                    rowChangedTokens.Add(layer, updateToken);
                    var createToken = RowCreatedEvent.Subscribe(OnRowCreated, featureClass);
                    rowCreatedTokens.Add(layer, createToken);
                }
            });
        }

        private void OnRowUpdated(RowChangedEventArgs args)
        {
            MessageBox.Show($"Row updated: {args.Row.GetObjectID()}");
        }

        private void OnRowCreated(RowChangedEventArgs args)
        {
           MessageBox.Show($"Row created: {args.Row.GetObjectID()}");
        }
    }
}
