/**
 * Copyright (c) 2008-2020 Bryan Biedenkapp., All Rights Reserved.
 * MIT Open Source. Use is subject to license terms.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 */
/*
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including 
 * without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject 
 * to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN 
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
/*
 * Based on code from Lidgren Network v3
 * Copyright (C) 2010 Michael Lidgren., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Status of the UPnP capabilities
    /// </summary>
    public enum UPnPStatus
    {
        /// <summary>
        /// Still discovering UPnP capabilities
        /// </summary>
        Discovering,

        /// <summary>
        /// UPnP is not available
        /// </summary>
        NotAvailable,

        /// <summary>
        /// UPnP is available and ready to use
        /// </summary>
        Available
    } // public enum UPnPStatus

    /// <summary>
    /// UPnP support class
    /// </summary>
    public class UPnP
    {
        /**
         * Constants
         */
        private const int DISCOVERY_TIMEOUT = 1000;

        private string serviceUrl;
        private string serviceName = "";

        private Peer peer;
        private ManualResetEvent discoveryComplete = new ManualResetEvent(false);

        internal double discoveryResponseDeadline;
        private UPnPStatus status;

        /*
        ** Properties
        */

        /// <summary>
        /// Status of the UPnP capabilities of this NetPeer
        /// </summary>
        public UPnPStatus Status
        {
            get { return status; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the UPnP class.
        /// </summary>
        /// <param name="peer">Peer that owns this</param>
        public UPnP(Peer peer)
        {
            this.peer = peer;
            discoveryResponseDeadline = double.MinValue;
        }

        /// <summary>
        /// Discover UPnP gateway
        /// </summary>
        /// <param name="peer">Network peer</param>
        internal void Discover(Peer peer)
        {
            string str =
                "M-SEARCH * HTTP/1.1\r\n" +
                "HOST: 239.255.255.250:1900\r\n" +
                "ST:upnp:rootdevice\r\n" +
                "MAN:\"ssdp:discover\"\r\n" +
                "MX:3\r\n\r\n";

            discoveryResponseDeadline = NetTime.Now + 6.0; // arbitrarily chosen number, router gets 6 seconds to respond
            status = UPnPStatus.Discovering;

            byte[] arr = System.Text.Encoding.ASCII.GetBytes(str);

            peer.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            peer.RawSend(arr, 0, arr.Length, new IPEndPoint(IPAddress.Broadcast, 1900));
            peer.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, false);

            // allow some extra time for router to respond
            // System.Threading.Thread.Sleep(50);
        }

        /// <summary>
        ///
        /// </summary>
        internal void CheckForDiscoveryTimeout()
        {
            if ((status != UPnPStatus.Discovering) || (NetTime.Now < discoveryResponseDeadline))
                return;
            //peer.LogDebug("UPnP discovery timed out");
            status = UPnPStatus.NotAvailable;
        }

        /// <summary>
        /// Extract the UPnP service URL
        /// </summary>
        /// <param name="resp"></param>
        internal void ExtractServiceUrl(string resp)
        {
            XmlDocument desc = new XmlDocument();
            using (var response = WebRequest.Create(resp).GetResponse())
                desc.Load(response.GetResponseStream());

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(desc.NameTable);
            nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
            XmlNode typen = desc.SelectSingleNode("//tns:device/tns:deviceType/text()", nsMgr);
            if (!typen.Value.Contains("InternetGatewayDevice"))
                return;

            serviceName = "WANIPConnection";
            XmlNode node = desc.SelectSingleNode("//tns:service[tns:serviceType=\"urn:schemas-upnp-org:service:" + serviceName + ":1\"]/tns:controlURL/text()", nsMgr);
            if (node == null)
            {
                // try another service name
                serviceName = "WANPPPConnection";
                node = desc.SelectSingleNode("//tns:service[tns:serviceType=\"urn:schemas-upnp-org:service:" + serviceName + ":1\"]/tns:controlURL/text()", nsMgr);
                if (node == null)
                    return;
            }

            serviceUrl = CombineUrls(resp, node.Value);
            RPCLogger.Trace("UPnP service ready");
            status = UPnPStatus.Available;
            discoveryComplete.Set();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="gatewayURL"></param>
        /// <param name="subURL"></param>
        /// <returns></returns>
        private static string CombineUrls(string gatewayURL, string subURL)
        {
            // Is Control URL an absolute URL?
            if (subURL.Contains("http:") || subURL.Contains("."))
                return subURL;

            gatewayURL = gatewayURL.Replace("http://", string.Empty);  // strip any protocol
            int n = gatewayURL.IndexOf("/");
            if (n != -1)
                gatewayURL = gatewayURL.Substring(0, n);  // Use first portion of URL
            return "http://" + gatewayURL + subURL;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private bool CheckAvailability()
        {
            switch (status)
            {
                case UPnPStatus.NotAvailable:
                    return false;

                case UPnPStatus.Available:
                    return true;

                case UPnPStatus.Discovering:
                    if (discoveryComplete.WaitOne(DISCOVERY_TIMEOUT))
                        return true;
                    if (NetTime.Now > discoveryResponseDeadline)
                        status = UPnPStatus.NotAvailable;
                    return false;
            }
            return false;
        }

        /// <summary>
        /// Add a forwarding rule to the router using UPnP
        /// </summary>
        /// <returns></returns>
        public bool ForwardPort(int port, string description)
        {
            if (!CheckAvailability())
                return false;

            IPAddress mask;
            var client = NetUtility.GetMyAddress(out mask);
            if (client == null)
                return false;

            try
            {
                SOAPRequest(serviceUrl,
                    "<u:DeletePortMapping xmlns:u=\"urn:schemas-upnp-org:service:" + serviceName + ":1\">" +
                    "<NewRemoteHost>" +
                    "</NewRemoteHost>" +
                    "<NewExternalPort>" + port + "</NewExternalPort>" +
                    "<NewProtocol>" + ProtocolType.Udp.ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture) + "</NewProtocol>" +
                    "</u:DeletePortMapping>", "DeletePortMapping");

                RPCLogger.Trace("Sent UPnP port forward request");
                return true;
            }
            catch (Exception ex)
            {
                RPCLogger.Trace("UPnP port forward failed");
                RPCLogger.StackTrace(ex, false);
                return false;
            }
        }

        /// <summary>
        /// Delete a forwarding rule from the router using UPnP
        /// </summary>
        /// <returns></returns>
        public bool DeleteForwardingRule(int port)
        {
            if (!CheckAvailability())
                return false;
            try
            {
                SOAPRequest(serviceUrl,
                    "<u:DeletePortMapping xmlns:u=\"urn:schemas-upnp-org:service:" + serviceName + ":1\">" +
                    "<NewRemoteHost>" +
                    "</NewRemoteHost>" +
                    "<NewExternalPort>" + port + "</NewExternalPort>" +
                    "<NewProtocol>" + ProtocolType.Udp.ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture) + "</NewProtocol>" +
                    "</u:DeletePortMapping>", "DeletePortMapping");
                return true;
            }
            catch (Exception ex)
            {
                RPCLogger.Trace("UPnP delete forwarding rule failed");
                RPCLogger.StackTrace(ex, false);
                return false;
            }
        }

        /// <summary>
        /// Retrieve the extern ip using UPnP
        /// </summary>
        /// <returns></returns>
        public IPAddress GetExternalIP()
        {
            if (!CheckAvailability())
                return null;
            try
            {
                XmlDocument xdoc = SOAPRequest(serviceUrl, "<u:GetExternalIPAddress xmlns:u=\"urn:schemas-upnp-org:service:" + serviceName + ":1\">" +
                "</u:GetExternalIPAddress>", "GetExternalIPAddress");
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(xdoc.NameTable);
                nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
                string IP = xdoc.SelectSingleNode("//NewExternalIPAddress/text()", nsMgr).Value;
                return IPAddress.Parse(IP);
            }
            catch (Exception ex)
            {
                RPCLogger.Trace("Failed to get external IP");
                RPCLogger.StackTrace(ex, false);
                return null;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="url"></param>
        /// <param name="soap"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        private XmlDocument SOAPRequest(string url, string soap, string function)
        {
            string req = "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                "<s:Body>" +
                soap +
                "</s:Body>" +
                "</s:Envelope>";

            WebRequest r = HttpWebRequest.Create(url);
            r.Method = "POST";

            byte[] b = System.Text.Encoding.UTF8.GetBytes(req);
            r.Headers.Add("SOAPACTION", "\"urn:schemas-upnp-org:service:WANIPConnection:1#" + function + "\"");
            r.ContentType = "text/xml; charset=\"utf-8\"";
            r.ContentLength = b.Length;
            r.GetRequestStream().Write(b, 0, b.Length);

            XmlDocument resp = new XmlDocument();
            WebResponse wres = r.GetResponse();
            Stream ress = wres.GetResponseStream();
            resp.Load(ress);

            return resp;
        }
    } // public class UPnP
} // namespace TridentFramework.RPC.Net.PeerConnection
