﻿using System;
using System.Collections.Generic;
using Nop.Services.Logging;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.Fedex
{
    public class FedexShipmentTracker : IShipmentTracker
    {
        private readonly ILogger _logger;
        private readonly FedexSettings _fedexSettings;

        public FedexShipmentTracker(ILogger logger, FedexSettings fedexSettings)
        {
            this._logger = logger;
            this._fedexSettings = fedexSettings;
        }


        /// <summary>
        /// Gets if the current tracker can track the tracking number.
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>True if the tracker can track, otherwise false.</returns>
        public virtual bool IsMatch(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                return false;

            //What is a FedEx tracking number format?
            return false;
        }

        /// <summary>
        /// Gets an URL for a page to show tracking info (third party tracking page).
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>URL of a tracking page.</returns>
        public virtual string GetUrl(string trackingNumber)
        {
            //What is a FedEx tracking page URL?
            return "";
        }

        /// <summary>
        /// Gets all events for a tracking number.
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track</param>
        /// <returns>List of Shipment Events.</returns>
        public virtual IList<ShipmentStatusEvent> GetShipmentEvents(string trackingNumber)
        {
            if (string.IsNullOrEmpty(trackingNumber))
                return new List<ShipmentStatusEvent>();

            var result = new List<ShipmentStatusEvent>();
            try
            {
                //use try-catch to ensure exception won't be thrown is web service is not available

                //build the TrackRequest
                var request = new TrackRequest
                {

                    //
                    WebAuthenticationDetail = new WebAuthenticationDetail
                    {
                        UserCredential = new WebAuthenticationCredential
                        {
                            Key = _fedexSettings.Key, // Replace "XXX" with the Key
                            Password = _fedexSettings.Password // Replace "XXX" with the Password
                        }
                    },
                    //
                    ClientDetail = new ClientDetail
                    {
                        AccountNumber = _fedexSettings.AccountNumber, // Replace "XXX" with client's account number
                        MeterNumber = _fedexSettings.MeterNumber // Replace "XXX" with client's meter number
                    },
                    //
                    TransactionDetail = new TransactionDetail
                    {
                        CustomerTransactionId = "***nopCommerce v16 Request using VC#***"
                    },

                    //creates the Version element with all child elements populated from the wsdl
                    Version = new VersionId(),
                    //tracking information
                    PackageIdentifier = new TrackPackageIdentifier
                    {
                        Value = trackingNumber,
                        Type = TrackIdentifierType.TRACKING_NUMBER_OR_DOORTAG
                    },

                    IncludeDetailedScans = true,
                    IncludeDetailedScansSpecified = true
                };

                //initialize the service
                var service = new TrackService(_fedexSettings.Url);
                //this is the call to the web service passing in a TrackRequest and returning a TrackReply
                var reply = service.track(request);
                //parse response
                if (reply.HighestSeverity == NotificationSeverityType.SUCCESS || reply.HighestSeverity == NotificationSeverityType.NOTE || reply.HighestSeverity == NotificationSeverityType.WARNING) // check if the call was successful
                {

                    foreach (var trackDetail in reply.TrackDetails)
                    {

                        if (trackDetail.Events != null)
                        {
                            //Set the parent level attributes
                            //var statusDescription = trackDetail.StatusDescription;
                            //var tatusCode = trackDetail.StatusCode;
                            //if (statusCode == "DL")
                            //{
                            //    var delivered = true;
                            //}


                            //if (trackDetail.SignatureProofOfDeliveryAvailable == true)
                            //{
                            //    trackResults.SignedForBy = trackDetail.DeliverySignatureName;
                            //}

                            //if (trackDetail.ShipmentWeight != null)
                            //{
                            //    var shipmentWeight = $"{trackDetail.ShipmentWeight.Value} {trackDetail.ShipmentWeight.Units}";
                            //}
                            //else
                            //{
                            //    var shipmentWeight = $"{trackDetail.PackageWeight.Value} {trackDetail.PackageWeight.Units}";
                            //}

                            //var shipDate = trackDetail.ShipTimestamp;
                            //var serviceType = trackDetail.ServiceInfo;
                            //var packageCount = int.Parse(trackDetail.PackageCount);
                            //var destination = $"{trackDetail.DestinationAddress.City}, {trackDetail.DestinationAddress.StateOrProvinceCode} {trackDetail.DestinationAddress.CountryCode}";
                            //var deliveryDate = trackDetail.ActualDeliveryTimestamp;

                            //Set the TrackingActivity
                            foreach (var trackevent in trackDetail.Events)
                            {
                                var sse = new ShipmentStatusEvent();

                                if (trackevent.TimestampSpecified)
                                {
                                    sse.Date = trackevent.Timestamp;
                                }
                                sse.EventName = $"{trackevent.EventDescription} ({trackevent.EventType})";
                                sse.Location = trackevent.Address.City;
                                sse.CountryCode = trackevent.Address.CountryCode;
                                //other properties (not used yet)
                                //trackevent.EventType;
                                //trackevent.Address.PostalCode;
                                //trackevent.Address.StateOrProvinceCode;
                                //trackevent.StatusExceptionCode;
                                //trackevent.StatusExceptionDescription;

                                result.Add(sse);
                            }
                        }
                    }
                }


                //result.AddRange(trackResponse.Shipment.SelectMany(c => c.Package[0].Activity.Select(x => ToStatusEvent(x))).ToList());
            }
            catch (Exception ex)
            {
                _logger.Error($"Error while getting Fedex shipment tracking info - {trackingNumber}", ex);
            }

            return result;
        }
    }

}